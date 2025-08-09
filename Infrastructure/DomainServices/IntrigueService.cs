using System;
using System.Collections.Generic;
using System.Linq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Drives plot progression, defections, and blackmail outcomes.
    /// This is intentionally deterministic-capable via IRandomService.
    /// </summary>
    public class IntrigueService : IIntrigueService
    {
        private readonly IPlotRepository _plots;
        private readonly ISecretsRepository _secrets;
        private readonly IOpinionRepository _opinions;
        private readonly IFactionInfo _factionInfo;
        private readonly IFactionMembershipService _factionMembership;
        private readonly ICharacterRepository _characters;
        private readonly IRandomService _rng;

        // Tuning knobs (quick defaults; externalize later if you want)
        private const int ProgressTarget = 100;
        private const int BaseProgressPerTurn = 6; // % per month
        private const double ExposureBaseChance = 0.05; // 5% / turn baseline
        private const double RecruitBaseChance = 0.04;  // 4% / turn baseline
        private const int OpinionRecruitThreshold = 25; // min opinion to join
        private const int OpinionPenaltyExposed = -20;
        private const int OpinionPenaltyBlackmailed = -30;
        private const int OpinionBonusDefectedTo = +15;

        public IntrigueService(
            IPlotRepository plots,
            ISecretsRepository secrets,
            IOpinionRepository opinions,
            IFactionInfo factionInfo,
            IFactionMembershipService factionMembership,
            ICharacterRepository characters,
            IRandomService rng)
        {
            _plots = plots;
            _secrets = secrets;
            _opinions = opinions;
            _factionInfo = factionInfo;
            _factionMembership = factionMembership;
            _characters = characters;
            _rng = rng;
        }

        public void TickPlots()
        {
            foreach (var plot in _plots.GetAll().ToList())
            {
                if (plot.Exposed) continue;

                var leader = _characters.GetById(plot.LeaderId);
                if (leader is null || !leader.IsAlive)
                    continue;

                // Progress speed from conspirators' Intelligence + leader’s Conscientiousness
                var conspirators = plot.Conspirators
                    .Select(_characters.GetById)
                    .Where(c => c != null && c.IsAlive)
                    .ToList()!;

                int avgIntel = conspirators.Any()
                    ? (int)conspirators.Average(c => c!.Skills.Intelligence)
                    : leader.Skills.Intelligence;

                int leaderBoost = (int)(leader.Personality.Conscientiousness * 0.10); // up to +10
                int progressDelta = BaseProgressPerTurn + (avgIntel / 20) + (leaderBoost / 10);
                progressDelta = Math.Clamp(progressDelta, 3, 20);

                // Exposure chance: grows with plot size and target count; reduced by conspirators’ Intelligence
                double exposureChance = ExposureBaseChance
                                        + 0.01 * Math.Max(0, plot.Conspirators.Count - 2)
                                        + 0.01 * Math.Max(0, plot.Targets.Count - 1)
                                        - 0.005 * (avgIntel / 30.0);
                exposureChance = Math.Clamp(exposureChance, 0.01, 0.25);

                // Recruit chance: if any candidate in leader's faction has opinion >= threshold, might be added
                double recruitChance = RecruitBaseChance - 0.005 * (plot.Conspirators.Count / 3.0);
                recruitChance = Math.Clamp(recruitChance, 0.01, 0.06);

                bool exposed = _rng.NextDouble() < exposureChance;
                var newConspirators = new List<Guid>(plot.Conspirators);

                if (!exposed && _rng.NextDouble() < recruitChance)
                {
                    var leaderFaction = _factionInfo.GetFactionIdForCharacter(leader.Id);
                    var candidates = _characters.GetAll()
                        .Where(c => c.IsAlive
                                    && c.Id != leader.Id
                                    && !_factionInfo.IsAtWar(leaderFaction, _factionInfo.GetFactionIdForCharacter(c.Id))
                                    && !plot.Conspirators.Contains(c.Id))
                        .ToList();

                    // pick the best opinion candidate who meets threshold
                    var pick = candidates
                        .Select(c => new { C = c, Opinion = _opinions.GetOpinion(leader.Id, c.Id) })
                        .Where(x => x.Opinion >= OpinionRecruitThreshold)
                        .OrderByDescending(x => x.Opinion)
                        .FirstOrDefault();

                    if (pick != null)
                        newConspirators.Add(pick.C.Id);
                }

                int newProgress = Math.Clamp(plot.Progress + progressDelta, 0, ProgressTarget);
                var updated = plot with
                {
                    Progress = newProgress,
                    Conspirators = newConspirators
                };

                // If exposed → apply blowback
                if (exposed)
                {
                    updated = updated with { Exposed = true };
                    // Targets dislike all conspirators; conspirators also suffer internal distrust
                    foreach (var target in plot.Targets)
                    {
                        foreach (var consp in newConspirators.Prepend(plot.LeaderId).Distinct())
                            _opinions.AdjustOpinion(target, consp, OpinionPenaltyExposed, $"Exposed {plot.Goal} plot");
                    }
                }
                else if (newProgress >= ProgressTarget)
                {
                    // Resolve the plot (very simple outcomes; expand later)
                    ResolveCompletedPlot(updated);
                }

                _plots.Save(updated);
            }
        }

        public void AttemptDefection(Guid actorId, Guid newFactionId)
        {
            var actor = _characters.GetById(actorId)
                        ?? throw new ArgumentException("Unknown character", nameof(actorId));

            if (!actor.IsAlive) return;

            var oldFaction = _factionInfo.GetFactionIdForCharacter(actorId);
            if (oldFaction == newFactionId) return;

            // Simple gate: defecting to an enemy is easier if at war with old faction
            bool atWar = _factionInfo.IsAtWar(oldFaction, newFactionId);

            // Personality: low Agreeableness & high Openness favors defection
            double personalityBias =
                (actor.Personality.Openness - 50) * 0.004  // -0.2..+0.2
              - (actor.Personality.Agreeableness - 50) * 0.003;

            double baseChance = atWar ? 0.40 : 0.20;
            double chance = Math.Clamp(baseChance + personalityBias, 0.05, 0.75);

            if (_rng.NextDouble() <= chance)
            {
                _factionMembership.MoveCharacterToFaction(actorId, newFactionId);

                // Opinion effects: old faction leader hates you, new one likes you
                var oldLeader = _factionInfo.GetLeaderId(oldFaction);
                if (oldLeader.HasValue)
                    _opinions.AdjustOpinion(oldLeader.Value, actorId, -50, "Defected away");

                var newLeader = _factionInfo.GetLeaderId(newFactionId);
                if (newLeader.HasValue)
                    _opinions.AdjustOpinion(newLeader.Value, actorId, OpinionBonusDefectedTo, "Defected to our faction");
            }
            else
            {
                // Failed attempt could sour relations with both sides slightly
                var oldLeader = _factionInfo.GetLeaderId(oldFaction);
                if (oldLeader.HasValue)
                    _opinions.AdjustOpinion(oldLeader.Value, actorId, -10, "Attempted to defect");

                var newLeader = _factionInfo.GetLeaderId(newFactionId);
                if (newLeader.HasValue)
                    _opinions.AdjustOpinion(newLeader.Value, actorId, -5, "Failed defection attempt reached us");
            }
        }

        public void AttemptBlackmail(Guid holderId, Guid targetId, Secret secret)
        {
            var holder = _characters.GetById(holderId);
            var target = _characters.GetById(targetId);
            if (holder is null || target is null || !holder.IsAlive || !target.IsAlive) return;

            // Store secret if not present
            if (_secrets.GetById(secret.Id) is null)
                _secrets.Add(secret);

            // Resolve: severity drives compliance chance; target’s Conscientiousness lowers compliance (principled),
            // Neuroticism raises compliance (fearful).
            double sev = Math.Clamp(secret.Severity, 1, 100) / 100.0;
            double bias = (target.Personality.Neuroticism - target.Personality.Conscientiousness) * 0.003; // -0.3..+0.3
            double complianceChance = Math.Clamp(0.35 + 0.5 * sev + bias, 0.05, 0.95);

            bool complied = _rng.NextDouble() <= complianceChance;

            if (complied)
            {
                // Target resents holder (but less than if exposed)
                _opinions.AdjustOpinion(targetId, holderId, (int)(OpinionPenaltyBlackmailed * 0.6), $"Complied due to blackmail ({secret.Type})");
                // Holder’s view improves slightly (power satisfaction)
                _opinions.AdjustOpinion(holderId, targetId, +5, "Blackmail succeeded");
                // TODO: trigger actual grant (e.g., leak info, stand down, pay credits) via separate command/use-case.
            }
            else
            {
                // Target is angry and may counter-expose; for now: big opinion hit
                _opinions.AdjustOpinion(targetId, holderId, OpinionPenaltyBlackmailed, $"Resisted blackmail ({secret.Type})");
                // Optional: create a counter-plot or attempt exposure
                MaybeCreateExposurePlot(targetId, holderId, secret);
            }
        }

        #region Internals

        private void ResolveCompletedPlot(Plot plot)
        {
            // TODO: Extremely lightweight outcomes; expand to real use-cases later:
            // Examples:
            // - "Coup": try to oust target faction leader
            // - "Assassinate": mark target dead
            // - "StealTech": generate a TechBreakthrough secret/benefit

            var leader = _characters.GetById(plot.LeaderId);
            if (leader is null) return;

            var goalKey = (plot.Goal ?? "").Trim().ToLowerInvariant();

            if (goalKey.Contains("assassin"))
            {
                // Pick a target and… remove.
                var victim = plot.Targets.Select(_characters.GetById).FirstOrDefault(c => c != null && c.IsAlive);
                victim?.MarkDead();
            }
            else if (goalKey.Contains("coup"))
            {
                // Attempt to replace target's faction leader with plot leader (drastic, simplified)
                var targetChar = plot.Targets.Select(_characters.GetById).FirstOrDefault(c => c != null && c.IsAlive);
                if (targetChar != null)
                {
                    var targetFaction = _factionInfo.GetFactionIdForCharacter(targetChar.Id);
                    // Move leader into that faction as "leader" via membership/service in your command layer (not shown here)
                    _factionMembership.MoveCharacterToFaction(leader.Id, targetFaction);
                    // Opinions: target hates conspirators (already handled on exposure, but give extra sting)
                    _opinions.AdjustOpinion(targetChar.Id, leader.Id, -40, "Coup executed");
                }
            }
            else if (goalKey.Contains("steal") || goalKey.Contains("tech"))
            {
                // Create a juicy secret representing stolen knowledge
                var secret = new Secret(
                    Id: Guid.NewGuid(),
                    Type: SecretType.TechBreakthrough,
                    Summary: $"Illicit tech advantage gained by {leader.Name}",
                    AboutCharacterId: leader.Id,
                    AboutFactionId: _factionInfo.GetFactionIdForCharacter(leader.Id),
                    Severity: 70,
                    TurnDiscovered: 0
                );
                _secrets.Add(secret);
            }

            // After resolution, mark plot as exposed (spent) to stop further progression
            var spent = plot with { Exposed = true, Progress = ProgressTarget };
            _plots.Save(spent);
        }

        private void MaybeCreateExposurePlot(Guid actorId, Guid targetId, Secret secret)
        {
            // Very small chance to spin a counter-exposure mini-plot
            if (_rng.NextDouble() < 0.15)
            {
                _plots.Create(
                    leaderId: actorId,
                    goal: $"Expose {secret.Type} on {targetId}",
                    conspirators: new[] { actorId },
                    targets: new[] { targetId }
                );
            }
        }

        #endregion
    }
}
