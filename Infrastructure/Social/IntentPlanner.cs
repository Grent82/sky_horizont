using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.Social
{
    /// <summary>
    /// Utility-based monthly intent planner. 
    /// Produces 0..2 intents per character, based on personality, skills, relationships,
    /// opinions, rank, and faction context.
    /// </summary>
    public sealed class IntentPlanner : IIntentPlanner
    {
        private readonly ICharacterRepository _chars;
        private readonly IOpinionRepository _opinions;
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        // Tuning knobs (0..100 style target values; scores roughly 0..100 range)
        private readonly PlannerConfig _cfg;

        public IntentPlanner(
            ICharacterRepository characters,
            IOpinionRepository opinions,
            IFactionService factions,
            IRandomService rng,
            PlannerConfig? config = null)
        {
            _chars = characters;
            _opinions = opinions;
            _factions = factions;
            _rng = rng;
            _cfg = config ?? PlannerConfig.Default;
        }

        public IEnumerable<CharacterIntent> PlanMonthlyIntents(Character actor)
        {
            var intents = new List<ScoredIntent>();
            if (!actor.IsAlive) return Enumerable.Empty<CharacterIntent>();
            if (actor.IsAssigned) return Enumerable.Empty<CharacterIntent>(); // busy on a mission this month

            // Basic context
            var myFactionId = _factions.GetFactionIdForCharacter(actor.Id);
            var myLeaderId  = myFactionId != Guid.Empty ? _factions.GetLeaderId(myFactionId) : null;

            // Gather potential social targets:
            var knownPeople = _chars.GetAll().Where(c => c.IsAlive && c.Id != actor.Id).ToList();
            var sameFaction = knownPeople.Where(c => _factions.GetFactionIdForCharacter(c.Id) == myFactionId).ToList();
            var otherFaction = knownPeople.Where(c => _factions.GetFactionIdForCharacter(c.Id) != myFactionId).ToList();

            // ── Score core intents ─────────────────────────────────────────────

            // Courtship / Maintain romance
            var romanticTarget = PickRomanticTarget(actor, knownPeople);
            if (romanticTarget != null)
                AddIfAboveZero(intents, ScoreCourtship(actor, romanticTarget), IntentType.Court, romanticTarget.Id);

            // Visit family (keeps ties warm)
            var familyTarget = PickFamilyTarget(actor);
            if (familyTarget.HasValue)
                AddIfAboveZero(intents, ScoreVisitFamily(actor, familyTarget.Value), IntentType.VisitFamily, familyTarget.Value);

            // Spy (enemy or rival)
            var spyTargetFaction = PickSpyFaction(actor, myFactionId);
            if (spyTargetFaction != Guid.Empty)
                AddIfAboveZero(intents, ScoreSpy(actor, spyTargetFaction), IntentType.Spy, null, spyTargetFaction); // ToDo: null? spy on enemy character as well (to get family members?)

            // Bribe (swing neutral or enemy asset)
            var bribeTarget = PickBribeTarget(actor, otherFaction);
            if (bribeTarget != null)
                AddIfAboveZero(intents, ScoreBribe(actor, bribeTarget), IntentType.Bribe, bribeTarget.Id);

            // Recruit (leaders/commanders recruiting sub-commanders)
            if (IsRecruiter(actor))
            {
                var recruitTarget = PickRecruitTarget(actor, knownPeople);
                if (recruitTarget != null)
                    AddIfAboveZero(intents, ScoreRecruit(actor, recruitTarget), IntentType.Recruit, recruitTarget.Id);
            }

            // Defect (if hates leader / wooed by enemy)
            if (myLeaderId.HasValue)
            {
                var defectTargetFaction = PickDefectionFaction(actor, myFactionId);
                if (defectTargetFaction != Guid.Empty)
                    AddIfAboveZero(intents, ScoreDefect(actor, myLeaderId.Value, defectTargetFaction), IntentType.Defect, null, defectTargetFaction);
            }

            // Negotiate (diplomatic outreach)
            var negotiateTargetFaction = PickNegotiateFaction(actor, myFactionId);
            if (negotiateTargetFaction != Guid.Empty)
                AddIfAboveZero(intents, ScoreNegotiate(actor, myFactionId, negotiateTargetFaction), IntentType.Negotiate, null, negotiateTargetFaction);

            // Quarrel (pick fight with rival/enemy)
            var quarrelTarget = PickQuarrelTarget(actor, knownPeople);
            if (quarrelTarget != null)
                AddIfAboveZero(intents, ScoreQuarrel(actor, quarrelTarget), IntentType.Quarrel, quarrelTarget.Id);

            // (Optional) Assassinate (very rare, high stakes)
            var assassinateTarget = PickAssassinationTarget(actor, otherFaction);
            if (assassinateTarget != null)
                AddIfAboveZero(intents, ScoreAssassinate(actor, assassinateTarget, myFactionId), IntentType.Assassinate, assassinateTarget.Id);

            // ── Pick top N with a little exploration randomness ────────────────
            if (intents.Count == 0) return Enumerable.Empty<CharacterIntent>();

            // Add small noise to avoid deterministic ties
            intents = intents
                        .Select(si => new ScoredIntent(
                            si.Type,
                            si.Score + _rng.NextDouble() * _cfg.ScoreNoiseMax,
                            si.TargetCharacterId,
                            si.TargetFactionId))
                        .ToList();

            var chosen = intents
                .OrderByDescending(i => i.Score)
                .Take(_cfg.MaxIntentsPerMonth)
                .Select(i => new CharacterIntent(actor.Id, i.Type, i.TargetCharacterId, i.TargetFactionId))
                .ToList();

            // ToDo: Optional: filter out conflicts (e.g., Court and Assassinate same month)
            return chosen;
        }

        // ────────────────────────── Scoring ──────────────────────────

        private double ScoreCourtship(Character actor, Character target)
        {
            var opinion = _opinions.GetOpinion(actor.Id, target.Id);   // -100..+100
            var compat  = actor.Personality.CheckCompatibility(target.Personality); // 0..100
            var baseScore = 0.0;

            baseScore += Map01(compat);                  // compatibility matters a lot
            baseScore += Map01(Math.Max(0, opinion + 50)); // tilt positive opinions

            // Cheerful & Warm people push up
            if (PersonalityTraits.Cheerful(actor.Personality)) baseScore += 10;
            if (PersonalityTraits.WarmAndFriendly(actor.Personality)) baseScore += 10;

            // Already close relationship? small boost
            if (actor.Relationships.Any(r => r.TargetCharacterId == target.Id &&
                                             (r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)))
                baseScore += 15;

            // Introverted strategists are a tad less likely to prioritize it
            if (actor.Personality.Extraversion < 40) baseScore -= 10;

            return Clamp0to100(baseScore * _cfg.RomanceWeight);
        }

        private double ScoreVisitFamily(Character actor, Guid familyId)
        {
            var opinion = _opinions.GetOpinion(actor.Id, familyId);
            var s = 30.0
                    + Map01(opinion + 50)
                    + (actor.Personality.Agreeableness - 50) * 0.3
                    + (actor.Personality.Conscientiousness - 50) * 0.2;

            return Clamp0to100(s * _cfg.FamilyWeight);
        }

        private double ScoreSpy(Character actor, Guid targetFactionId)
        {
            var intel = actor.Skills.Intelligence; // 0..100
            var s = intel * 0.6;

            // Bold & curious personalities bias up
            if (PersonalityTraits.Adventurous(actor.Personality)) s += 10;
            if (PersonalityTraits.IntellectuallyCurious(actor.Personality)) s += 10;

            // Very anxious actors bias down
            if (PersonalityTraits.Anxious(actor.Personality)) s -= 10;

            // Rank helps coordination
            s += (int)actor.Rank * 2;

            // Don’t send civilians on spy ops unless talented
            if (actor.Rank == Rank.Civilian && intel < 65) s -= 15;

            return Clamp0to100(s * _cfg.SpyWeight);
        }

        private double ScoreBribe(Character actor, Character target)
        {
            var baseScore = 20.0;

            // Bribery success chances rise when target conscientiousness/agreeableness are low
            var hardness = (target.Personality.Conscientiousness + target.Personality.Agreeableness) / 2.0;
            baseScore += (100 - hardness) * 0.4;

            // Actor’s own conscientiousness/agreeableness discourage corruption
            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;

            // If at war with target’s faction, urgency up
            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            var targetFaction = _factions.GetFactionIdForCharacter(target.Id);
            if (_factions.IsAtWar(myFaction, targetFaction)) baseScore += 10;

            // Money check – if broke, bribe unlikely
            if (actor.Balance < _cfg.MinBribeBudget) baseScore -= 25;

            return Clamp0to100(baseScore * _cfg.BribeWeight);
        }

        private double ScoreRecruit(Character actor, Character target)
        {
            var baseScore = 30.0;

            // Prefer high skill candidates
            var talent = (target.Skills.Military + target.Skills.Intelligence + target.Skills.Economy + target.Skills.Research) / 4.0;
            baseScore += talent * 0.4;

            // Leader/General more likely to recruit
            baseScore += (int)actor.Rank * 3;

            // Actor’s agreeableness and extraversion help recruiting
            baseScore += (actor.Personality.Agreeableness - 50) * 0.2;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;

            // Already in same faction? smaller score (already recruitable by order chain)
            var sameFaction = _factions.GetFactionIdForCharacter(actor.Id) == _factions.GetFactionIdForCharacter(target.Id);
            if (sameFaction) baseScore -= 10;

            return Clamp0to100(baseScore * _cfg.RecruitWeight);
        }

        private double ScoreDefect(Character actor, Guid leaderId, Guid targetFactionId)
        {
            var opinionLeader = _opinions.GetOpinion(actor.Id, leaderId); // -100..+100
            var baseScore = 0.0;

            // Hate your leader? go up
            baseScore += Map01(-(opinionLeader)) * 0.8;

            // Personality: low conscientiousness & low agreeableness → more likely
            baseScore += (50 - actor.Personality.Conscientiousness) * 0.2;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.2;

            // Risk sensitivity: high neuroticism discourages (fear), high openness encourages (new start)
            baseScore += (actor.Personality.Openness - 50) * 0.15;
            baseScore -= (actor.Personality.Neuroticism - 50) * 0.15;

            // If target faction is enemy of your enemy, small bump
            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (_factions.IsAtWar(myFaction, targetFactionId)) baseScore += 10;

            // High rank less likely to defect (have more to lose)
            baseScore -= (int)actor.Rank * 2;

            return Clamp0to100(baseScore * _cfg.DefectionWeight);
        }

        private double ScoreNegotiate(Character actor, Guid myFactionId, Guid targetFactionId)
        {
            var baseScore = 25.0;

            // Diplomacy loves high agreeableness and extraversion
            baseScore += (actor.Personality.Agreeableness - 50) * 0.3;
            baseScore += (actor.Personality.Extraversion  - 50) * 0.2;

            // If at war → push higher (seek ceasefire), except very angry/anxious persons
            if (_factions.IsAtWar(myFactionId, targetFactionId))
            {
                baseScore += 10;
                if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore -= 10;
                if (PersonalityTraits.Anxious(actor.Personality)) baseScore -= 10;
            }

            // Rank gates ability to negotiate
            baseScore += (int)actor.Rank * 3;

            return Clamp0to100(baseScore * _cfg.NegotiateWeight);
        }

        private double ScoreQuarrel(Character actor, Character target)
        {
            var opinion = _opinions.GetOpinion(actor.Id, target.Id);
            var baseScore = 0.0;

            if (opinion < -25) baseScore += Map01(-(opinion)); // the more hate, the higher
            if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore += 15;
            if (PersonalityTraits.Cheerful(actor.Personality)) baseScore -= 5;

            // Hierarchy discourages picking on superiors
            if ((int)target.Rank > (int)actor.Rank) baseScore -= 10;

            return Clamp0to100(baseScore * _cfg.QuarrelWeight);
        }

        private double ScoreAssassinate(Character actor, Character target, Guid myFactionId)
        {
            // Very rare: needs high military skill + strong negative opinion + wartime/plot
            var opinion = _opinions.GetOpinion(actor.Id, target.Id);
            var baseScore = 0.0;

            baseScore += actor.Skills.Military * 0.4;
            baseScore += Math.Max(0, -(opinion)) * 0.2;

            // Traits: impulsive/angry raise; conscientious/agreeable lower
            if (PersonalityTraits.Impulsive(actor.Personality)) baseScore += 10;
            if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore += 10;
            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;

            // Prefer at war
            var targetFaction = _factions.GetFactionIdForCharacter(target.Id);
            if (_factions.IsAtWar(myFactionId, targetFaction)) baseScore += 10;

            // Don’t let civilians constantly try this
            if (actor.Rank <= Rank.Captain && actor.Skills.Military < 70) baseScore -= 20;

            // Make it rare
            baseScore -= 15;

            return Clamp0to100(baseScore * _cfg.AssassinateWeight);
        }

        // ───────────────────── Target pickers ───────────────────────

        private Character? PickRomanticTarget(Character actor, List<Character> candidates)
        {
            // Prefer existing lovers/spouses; otherwise, someone compatible in same faction
            var lovers = actor.Relationships.Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                                            .Select(r => r.TargetCharacterId)
                                            .ToHashSet();

            var known = candidates.Where(c => lovers.Contains(c.Id)).ToList();
            if (known.Count > 0) return known[_rng.NextInt(0, known.Count)];

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            var pool = candidates.Where(c => _factions.GetFactionIdForCharacter(c.Id) == myFaction).ToList();
            if (pool.Count == 0) return null;

            // Pick someone actor likes / high compatibility
            return pool.Select(c => new
            {
                C = c,
                Score = 0.6 * _opinions.GetOpinion(actor.Id, c.Id) + 0.4 * actor.Personality.CheckCompatibility(c.Personality)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault()?.C;
        }

        private Guid? PickFamilyTarget(Character actor)
        {
            if (actor.FamilyLinkIds.Count == 0) return null;
            // Random family member with slightly positive opinion bias
            var weighted = actor.FamilyLinkIds
                .Select(fid => new { Id = fid, W = _opinions.GetOpinion(actor.Id, fid) + 60 + _rng.NextInt(0, 20) })
                .OrderByDescending(x => x.W)
                .FirstOrDefault();
            return weighted?.Id;
        }

        private Guid PickSpyFaction(Character actor, Guid myFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(myFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;

            // Prefer factions at war
            var war = rivals.Where(f => _factions.IsAtWar(myFactionId, f)).ToList();
            if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];

            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Character? PickBribeTarget(Character actor, List<Character> otherFaction)
        {
            var pool = otherFaction
                .Where(c => _opinions.GetOpinion(actor.Id, c.Id) > -50) // not sworn enemies
                .ToList();
            if (pool.Count == 0) return null;

            // Prefer people with lower C/A (more corruptible)
            return pool.Select(c => new
            {
                C = c,
                Score = (100 - c.Personality.Conscientiousness) + (100 - c.Personality.Agreeableness) + _rng.NextInt(0, 20)
            })
            .OrderByDescending(x => x.Score)
            .First().C;
        }

        private bool IsRecruiter(Character actor) => actor.Rank >= Rank.Captain || actor.Skills.Military >= 70;

        private Character? PickRecruitTarget(Character actor, List<Character> candidates)
        {
            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            // Prefer neutral or other factions; avoid already in chain (same faction)
            var pool = candidates.Where(c => _factions.GetFactionIdForCharacter(c.Id) != myFaction).ToList();
            if (pool.Count == 0) return null;

            return pool.Select(c => new
            {
                C = c,
                Talent = (c.Skills.Military + c.Skills.Intelligence + c.Skills.Economy + c.Skills.Research) / 4.0
            })
            .OrderByDescending(x => x.Talent + _rng.NextInt(0, 10))
            .First().C;
        }

        private Guid PickDefectionFaction(Character actor, Guid myFactionId)
        {
            // If actor dislikes the leader heavily, consider defection to a rival
            var rivals = _factions.GetAllRivalFactions(myFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;

            // Prefer factions at war with your current faction (enemy of enemy dynamic)
            var war = rivals.Where(f => _factions.IsAtWar(myFactionId, f)).ToList();
            if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];

            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Guid PickNegotiateFaction(Character actor, Guid myFactionId)
        {
            // Diplomatic actors try de-escalation with a rival
            var rivals = _factions.GetAllRivalFactions(myFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;

            // If cheerful/trusting → prefer ceasefire with current war opponent
            if (PersonalityTraits.Cheerful(actor.Personality) || PersonalityTraits.Trusting(actor.Personality))
            {
                var war = rivals.Where(f => _factions.IsAtWar(myFactionId, f)).ToList();
                if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            }

            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Character? PickQuarrelTarget(Character actor, List<Character> candidates)
        {
            var negatives = candidates
                .Select(c => new { C = c, O = _opinions.GetOpinion(actor.Id, c.Id) })
                .Where(x => x.O < -25)
                .OrderBy(x => x.O)
                .ToList();
            if (negatives.Count == 0) return null;

            return negatives[_rng.NextInt(0, Math.Min(3, negatives.Count))].C; // pick one of the worst few
        }

        private Character? PickAssassinationTarget(Character actor, List<Character> otherFaction)
        {
            // Very rare: aim at high-rank enemies with whom the actor has bad blood
            if (_rng.NextDouble() > _cfg.AssassinateFrequency) return null;

            var pool = otherFaction
                .Select(c => new { C = c, O = _opinions.GetOpinion(actor.Id, c.Id) })
                .Where(x => x.O < -50 && x.C.Rank >= Rank.Major)
                .OrderBy(x => x.O)
                .ToList();

            if (pool.Count == 0) return null;
            return pool.First().C;
        }

        // ───────────────────── Helpers ──────────────────────────────

        private static void AddIfAboveZero(List<ScoredIntent> list, double score, IntentType type,
                                           Guid? targetCharacterId = null, Guid? targetFactionId = null)
        {
            if (score <= 0) return;
            list.Add(new ScoredIntent(type, score, targetCharacterId, targetFactionId));
        }

        private static double Map01(double v)   => Math.Clamp(v, 0, 100) / 1.0;
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        private sealed record ScoredIntent(IntentType Type, double Score, Guid? TargetCharacterId, Guid? TargetFactionId);
    }

    // ───────────────────────── Config ──────────────────────────────
    public sealed class PlannerConfig // ToDo: from config file
    {
        public int MaxIntentsPerMonth { get; init; } = 2;
        public double ScoreNoiseMax { get; init; } = 5.0;

        public double RomanceWeight { get; init; } = 0.9;
        public double FamilyWeight { get; init; } = 0.7;
        public double SpyWeight { get; init; } = 1.0;
        public double BribeWeight { get; init; } = 0.8;
        public double RecruitWeight { get; init; } = 0.9;
        public double DefectionWeight { get; init; } = 1.0;
        public double NegotiateWeight { get; init; } = 0.7;
        public double QuarrelWeight { get; init; } = 0.6;
        public double AssassinateWeight { get; init; } = 0.65;

        public int MinBribeBudget { get; init; } = 200;
        public double AssassinateFrequency { get; init; } = 0.05; // 5% chance to even consider

        public static PlannerConfig Default => new();
    }
}
