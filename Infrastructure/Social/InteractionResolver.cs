using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social
{
    /// <summary>
    /// Turns planned intents into outcomes. Applies opinion deltas and produces secrets.
    /// All numeric tuning is in the #region Tuning.
    /// </summary>
    public sealed class InteractionResolver : IInteractionResolver
    {
        private readonly ICharacterRepository _chars;
        private readonly IOpinionRepository _opinions;
        private readonly IFactionService _factions;
        private readonly ISecretsRepository _secrets;
        private readonly IRandomService _rng;
        private readonly IDiplomacyService _diplomacy;
        private readonly IEventBus _events;

        public InteractionResolver(
            ICharacterRepository characters,
            IOpinionRepository opinions,
            IFactionService factions,
            ISecretsRepository secrets,
            IRandomService rng,
            IDiplomacyService diplomacy,
            IEventBus events)
        {
            _chars = characters ?? throw new ArgumentNullException(nameof(characters));
            _opinions = opinions ?? throw new ArgumentNullException(nameof(opinions));
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
            _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IEnumerable<ISocialEvent> Resolve(CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (intent == null)
                return Array.Empty<ISocialEvent>();

            var actor = _chars.GetById(intent.ActorId);
            if (actor == null || !actor.IsAlive)
                return Array.Empty<ISocialEvent>();

            switch (intent.Type)
            {
                case IntentType.Court:
                    return ResolveCourt(actor, intent, currentYear, currentMonth);
                case IntentType.VisitFamily:
                    return ResolveVisitFamily(actor, intent, currentYear, currentMonth);
                case IntentType.Spy:
                    return ResolveSpy(actor, intent, currentYear, currentMonth);
                case IntentType.Bribe:
                    return ResolveBribe(actor, intent, currentYear, currentMonth);
                case IntentType.Recruit:
                    return ResolveRecruit(actor, intent, currentYear, currentMonth);
                case IntentType.Defect:
                    return ResolveDefect(actor, intent, currentYear, currentMonth);
                case IntentType.Negotiate:
                    return ResolveNegotiate(actor, intent, currentYear, currentMonth);
                case IntentType.Quarrel:
                    return ResolveQuarrel(actor, intent, currentYear, currentMonth);
                case IntentType.Assassinate:
                    return ResolveAssassinate(actor, intent, currentYear, currentMonth);
                case IntentType.TorturePrisoner:
                    return ResolveTorture(actor, intent, currentYear, currentMonth);
                case IntentType.RapePrisoner:
                    return ResolveRape(actor, intent, currentYear, currentMonth);
                default:
                    return Array.Empty<ISocialEvent>();
            }
        }

        #region Courtship
        private IEnumerable<ISocialEvent> ResolveCourt(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var compat = actor.Personality.CheckCompatibility(target.Personality); // 0..100
            var baseChance = 0.25 + compat / 300.0; // ~0.25..0.58
            baseChance += (actor.Personality.Extraversion - 50) * 0.002; // +/- 0.1

            var success = _rng.NextDouble() < Clamp01(baseChance);

            var deltaActorToTarget = success ? +4 : -2;
            var deltaTargetToActor = success ? +6 : -3;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, success ? "Romance success" : "Awkward attempt");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, success ? "Charmed" : "Turned down");

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.CourtshipAttempt,
                actor.Id,
                target.Id,
                null,
                success,
                deltaActorToTarget,
                deltaTargetToActor,
                Array.Empty<Guid>(),
                success ? "Shared time and chemistry." : "It didn’t land."
            );

            if (success && !actor.Relationships.Any(r => r.TargetCharacterId == target.Id))
            {
                actor.AddRelationship(target.Id, RelationshipType.Lover);
                _chars.Save(actor);
            }

            return new[] { ev };
        }
        #endregion

        #region VisitFamily
        private IEnumerable<ISocialEvent> ResolveVisitFamily(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var relative = _chars.GetById(intent.TargetCharacterId.Value);
            if (relative == null || !relative.IsAlive)
                return Array.Empty<ISocialEvent>();

            var pleasantness = 0.4
                + (actor.Personality.Agreeableness - 50) * 0.003
                + (actor.Personality.Conscientiousness - 50) * 0.002;
            var success = _rng.NextDouble() < Clamp01(pleasantness);

            var delta = success ? +5 : +1; // Even awkward visits help a bit
            _opinions.AdjustOpinion(actor.Id, relative.Id, delta, "Family time");
            _opinions.AdjustOpinion(relative.Id, actor.Id, delta, "Family time");

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.FamilyVisit,
                actor.Id,
                relative.Id,
                null,
                success,
                delta,
                delta,
                Array.Empty<Guid>(),
                success ? "A warm reunion." : "A brief check-in."
            );

            return new[] { ev };
        }
        #endregion

        #region Spy
        private IEnumerable<ISocialEvent> ResolveSpy(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetFactionId.HasValue && !intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();

            var intel = actor.Skills.Intelligence; // 0..100
            var baseChance = 0.2 + intel / 200.0; // 0.2..0.7
            baseChance += (actor.Personality.Openness - 50) * 0.002;
            baseChance -= (actor.Personality.Neuroticism - 50) * 0.002;

            var success = _rng.NextDouble() < Clamp01(baseChance);

            var secrets = new List<Guid>();
            string notes;

            if (success)
            {
                var severity = 50 + _rng.NextInt(0, 40); // 50..89
                Secret secret;

                if (intent.TargetCharacterId.HasValue)
                {
                    var about = _chars.GetById(intent.TargetCharacterId.Value);
                    secret = new Secret(
                        Guid.NewGuid(),
                        SecretType.PersonalInformation,
                        about != null ? $"Informations observed for {about.Name}" : "Target informations observed",
                        intent.TargetCharacterId.Value,
                        null,
                        severity,
                        currentYear,
                        currentMonth);
                }
                else
                {
                    secret = new Secret(
                        Guid.NewGuid(),
                        SecretType.MilitaryDisposition,
                        "Enemy fleet movement intel",
                        null,
                        intent.TargetFactionId,
                        severity,
                        currentYear,
                        currentMonth);
                }

                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = intent.TargetCharacterId.HasValue
                    ? "Acquired sensitive personal informations."
                    : "Acquired fleet disposition report.";
            }
            else
            {
                notes = "Operation compromised or yielded nothing.";
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.EspionageOperation,
                actor.Id,
                intent.TargetCharacterId,
                intent.TargetFactionId,
                success,
                0,
                0,
                secrets,
                notes
            );

            return new[] { ev };
        }
        #endregion

        #region Bribe
        private IEnumerable<ISocialEvent> ResolveBribe(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var corruptibility = (100 - target.Personality.Conscientiousness + 100 - target.Personality.Agreeableness) / 200.0; // 0..1
            var baseChance = 0.15 + corruptibility * 0.6; // Up to ~0.75
            baseChance += (actor.Personality.Extraversion - 50) * 0.002;

            var success = _rng.NextDouble() < Clamp01(baseChance);

            var deltaActorToTarget = success ? +3 : -5;
            var deltaTargetToActor = success ? +8 : -10;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, success ? "Successful bribe" : "Bribe rebuffed");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, success ? "Took the money" : "Offended by bribe");

            var secrets = new List<Guid>();
            if (success)
            {
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.Corruption,
                    $"{target.Name} accepted bribes.",
                    target.Id,
                    null,
                    60 + _rng.NextInt(0, 25),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.BriberyAttempt,
                actor.Id,
                target.Id,
                null,
                success,
                deltaActorToTarget,
                deltaTargetToActor,
                secrets,
                success ? "Greased palms; influence secured." : "Refusal and offense taken."
            );

            return new[] { ev };
        }
        #endregion

        #region Recruit
        private IEnumerable<ISocialEvent> ResolveRecruit(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var pitch = 0.25
                + (actor.Personality.Agreeableness - 50) * 0.002
                + (actor.Personality.Extraversion - 50) * 0.002
                + (int)actor.Rank * 0.02;

            var success = _rng.NextDouble() < Clamp01(pitch);

            var deltaActorToTarget = success ? +5 : -2;
            var deltaTargetToActor = success ? +6 : -4;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, success ? "Effective recruitment talk" : "Rejected recruitment");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, success ? "Motivated by recruiter" : "Annoyed by recruiter");

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.RecruitmentAttempt,
                actor.Id,
                target.Id,
                null,
                success,
                deltaActorToTarget,
                deltaTargetToActor,
                Array.Empty<Guid>(),
                success ? "Target agreed to collaborate." : "Recruitment declined."
            );

            return new[] { ev };
        }
        #endregion

        #region Defect
        private IEnumerable<ISocialEvent> ResolveDefect(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetFactionId.HasValue)
                return Array.Empty<ISocialEvent>();

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (myFaction == Guid.Empty)
                return Array.Empty<ISocialEvent>();

            var myLeader = _factions.GetLeaderId(myFaction);
            var dislikeLeader = myLeader.HasValue ? Math.Max(0, -_opinions.GetOpinion(actor.Id, myLeader.Value)) : 0; // 0..100

            var baseChance = 0.05
                + dislikeLeader / 200.0
                + (50 - actor.Personality.Conscientiousness) * 0.002
                + (50 - actor.Personality.Agreeableness) * 0.002;

            var success = _rng.NextDouble() < Clamp01(baseChance);

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.DefectionAttempt,
                actor.Id,
                null,
                intent.TargetFactionId,
                success,
                0,
                0,
                Array.Empty<Guid>(),
                success ? "Defection succeeded; new allegiance sworn." : "Attempt to defect failed or aborted."
            );

            return new[] { ev };
        }
        #endregion

        #region Negotiate
        private IEnumerable<ISocialEvent> ResolveNegotiate(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetFactionId.HasValue)
                return Array.Empty<ISocialEvent>();
            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);

            var baseChance = 0.2
                + (actor.Personality.Agreeableness - 50) * 0.003
                + (actor.Personality.Extraversion - 50) * 0.002
                + (int)actor.Rank * 0.02;

            bool atWar = _factions.IsAtWar(myFaction, intent.TargetFactionId.Value);

            if (atWar)
                baseChance += 0.0;

            var success = _rng.NextDouble() < Clamp01(baseChance);

            if (success)
            {
                var treatyType = ChooseTreatyType(actor, myFaction, intent.TargetFactionId.Value, atWar);
                _diplomacy.ProposeTreaty(myFaction, intent.TargetFactionId.Value, treatyType);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.Negotiation,
                actor.Id,
                null,
                intent.TargetFactionId,
                success,
                0,
                0,
                Array.Empty<Guid>(),
                success ? "Talks progressed." : "Talks stalled."
            );

            return new[] { ev };
        }
        #endregion

        #region Quarrel
        private IEnumerable<ISocialEvent> ResolveQuarrel(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var heat = 0.4
                + (actor.Personality.Neuroticism - 50) * 0.003
                + (target.Personality.Neuroticism - 50) * 0.002
                - (actor.Personality.Agreeableness - 50) * 0.003;

            var success = _rng.NextDouble() < Clamp01(heat);

            var deltaActorToTarget = success ? -6 : -2;
            var deltaTargetToActor = success ? -6 : -2;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, "Quarrel");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, "Quarrel");

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.Quarrel,
                actor.Id,
                target.Id,
                null,
                success,
                deltaActorToTarget,
                deltaTargetToActor,
                Array.Empty<Guid>(),
                success ? "Harsh words exchanged." : "Tense but contained."
            );

            return new[] { ev };
        }
        #endregion

        #region Assassinate
        private IEnumerable<ISocialEvent> ResolveAssassinate(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.05
                + actor.Skills.Military / 150.0 // Up to ~0.71
                + (actor.Personality.Neuroticism - 50) * 0.002
                + (actor.Personality.Extraversion - 50) * 0.001;

            baseChance -= (target.Rank - actor.Rank) * 0.02; // Harder if target outranks
            var success = _rng.NextDouble() < Clamp01(baseChance);

            var secrets = new List<Guid>();
            if (!success)
            {
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.AssassinationPlot,
                    $"{actor.Name} allegedly plotted against {target.Name}",
                    actor.Id,
                    null,
                    70 + _rng.NextInt(0, 20),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
            }
            else
            {
                target.MarkDead();
                _chars.Save(target);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.AssassinationAttempt,
                actor.Id,
                target.Id,
                null,
                success,
                0,
                0,
                secrets,
                success ? "Target eliminated." : "Attempt failed; whispers spread."
            );

            return new[] { ev };
        }
        #endregion

        #region Torture
        private IEnumerable<ISocialEvent> ResolveTorture(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.3
                + actor.Skills.Military / 200.0 // Up to ~0.8
                + (50 - actor.Personality.Agreeableness) * 0.003
                + (actor.Personality.Conscientiousness - 50) * 0.002;

            baseChance -= (target.Rank - actor.Rank) * 0.01; // Harder if target outranks
            var success = _rng.NextDouble() < Clamp01(baseChance);

            var secrets = new List<Guid>();
            string notes;

            if (success)
            {
                // Gain intelligence secret
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.PersonalInformation,
                    $"Interrogation of {target.Name} yielded sensitive information.",
                    target.Id,
                    null,
                    60 + _rng.NextInt(0, 30),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Torture yielded valuable information.";

                // Apply opinion penalties
                _opinions.AdjustOpinion(actor.Id, target.Id, -10, "Tortured prisoner");
                _opinions.AdjustOpinion(target.Id, actor.Id, -20, "Victim of torture");

                // ToDo: Apply trauma to target (increase mortality risk, handled in CharacterLifecycleService)
                //target.ApplyTrauma(TraumaType.Torture); 
                _chars.Save(target);
            }
            else
            {
                // Failed torture creates a secret about the attempt
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.TortureAttempt,
                    $"{actor.Name} attempted to torture {target.Name}.",
                    actor.Id,
                    null,
                    50 + _rng.NextInt(0, 20),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Torture attempt failed; rumors spread.";
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.TortureAttempt,
                actor.Id,
                target.Id,
                null,
                success,
                success ? -10 : 0,
                success ? -20 : 0,
                secrets,
                notes
            );

            return new[] { ev };
        }
        #endregion

        #region Rape
        private IEnumerable<ISocialEvent> ResolveRape(Character actor, CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.2
                + (50 - actor.Personality.Agreeableness) * 0.004
                + (50 - actor.Personality.Conscientiousness) * 0.003
                + (int)actor.Rank * 0.02;

            var success = _rng.NextDouble() < Clamp01(baseChance);

            var secrets = new List<Guid>();
            string notes;

            if (success)
            {
                // Create secret about the act
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.RapeIncident,
                    $"{actor.Name} committed a heinous act against {target.Name}.",
                    actor.Id,
                    null,
                    80 + _rng.NextInt(0, 15),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Act committed; grave consequences loom.";

                // Apply severe opinion penalties
                _opinions.AdjustOpinion(actor.Id, target.Id, -15, "Committed rape");
                _opinions.AdjustOpinion(target.Id, actor.Id, -30, "Victim of rape");

                // ToDo: Apply trauma to target
                //target.ApplyTrauma(TraumaType.Rape);
                _chars.Save(target);

                // If target is female and of fertile age, initiate non-consensual conception
                if (target.Sex == Sex.Female && target.Age >= 14 && target.Age <= 45)
                {
                    target.StartPregnancy(actor.Id, currentYear, currentMonth);
                    _chars.Save(target);
                    _events.Publish(new DomainEventLog("NonConsensualConception", target.Id, $"perpetrator={actor.Id}; year={currentYear}; month={currentMonth}"));
                }
            }
            else
            {
                // Failed attempt creates a secret
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.RapeAttempt,
                    $"{actor.Name} attempted to assault {target.Name}.",
                    actor.Id,
                    null,
                    60 + _rng.NextInt(0, 20),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Attempt failed; whispers spread.";
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.RapeAttempt,
                actor.Id,
                target.Id,
                null,
                success,
                success ? -15 : 0,
                success ? -30 : 0,
                secrets,
                notes
            );

            return new[] { ev };
        }
        #endregion

        #region Helpers
        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private TreatyType ChooseTreatyType(Character actor, Guid myFactionId, Guid targetFactionId, bool atWar)
        {
            if (atWar)
                return TreatyType.Ceasefire;

            var p = actor.Personality;

            double allianceScore =
                20
                + actor.Skills.Military * 0.20
                + (PersonalityTraits.Assertive(p) ? 10 : 0)
                + (PersonalityTraits.ThrillSeeker(p) ? 5 : 0)
                - (p.Agreeableness - 50) * 0.10;

            double tradeScore =
                20
                + actor.Skills.Economy * 0.30
                + (PersonalityTraits.Cooperative(p) ? 10 : 0)
                + (PersonalityTraits.Cheerful(p) ? 5 : 0);

            double researchScore =
                15
                + actor.Skills.Research * 0.35
                + (PersonalityTraits.IntellectuallyCurious(p) ? 12 : 0)
                + (PersonalityTraits.SelfEfficient(p) ? 3 : 0);

            double napScore =
                10
                + p.Agreeableness * 0.20
                + (PersonalityTraits.Trusting(p) ? 8 : 0)
                - (PersonalityTraits.EasilyAngered(p) ? 8 : 0);

            allianceScore += (int)actor.Rank * 1.5;

            var best = new[]
            {
                (TreatyType.Alliance, allianceScore),
                (TreatyType.Trade, tradeScore),
                (TreatyType.ResearchPact, researchScore),
                (TreatyType.NonAggression, napScore)
            }.OrderByDescending(t => t.Item2).First().Item1;

            return best;
        }
        #endregion
    }
}