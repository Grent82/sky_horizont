using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Economy;
using System.Linq;
using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;

namespace SkyHorizont.Infrastructure.Social
{
    public sealed class InteractionResolver : IInteractionResolver
    {
        private readonly ICharacterRepository _chars;
        private readonly IOpinionRepository _opinions;
        private readonly IFactionService _factions;
        private readonly ISecretsRepository _secrets;
        private readonly IRandomService _rng;
        private readonly IDiplomacyService _diplomacy;
        private readonly ITravelService _travel;
        private readonly IPiracyService _piracy;
        private readonly IPlanetRepository _planets;
        private readonly IFleetRepository _fleets;
        private readonly IPlanetEconomyRepository _economy;
        private readonly IEventBus _events;
        private readonly IBattleOutcomeService _battleOutcomeService;
        private readonly IIntimacyLog _intimacy;
        private readonly IMeritPolicy _merit;
        private readonly InteractionConfig _cfg;
        private readonly Dictionary<Guid, FactionStatus> _factionStatusCache;
        private readonly Dictionary<Guid, SystemSecurity> _systemSecurityCache;
        private readonly Dictionary<Guid, (Guid? PlanetId, Guid? SystemId)> _characterLocationCache;

        private record ShipSpec(int Cost, double Attack, double Defense, double Cargo, double Speed, int ProductionRequired, Resources ResourceCost);
        private static readonly Dictionary<ShipClass, ShipSpec> ShipSpecs = new()
        {
            { ShipClass.Corvette, new ShipSpec(800, 10, 10, 10, 2.0, 50, new Resources(10,20,5)) },
            { ShipClass.Frigate, new ShipSpec(1500, 20, 20, 20, 1.5, 70, new Resources(20,40,10)) },
            { ShipClass.Destroyer, new ShipSpec(2500, 30, 25, 15, 1.3, 90, new Resources(30,60,15)) },
            { ShipClass.Freighter, new ShipSpec(1200, 5, 8, 100, 1.5, 60, new Resources(10,30,5)) },
            { ShipClass.Scout, new ShipSpec(500, 5, 5, 10, 3.0, 40, new Resources(5,10,5)) },
            { ShipClass.Carrier, new ShipSpec(5000, 40, 40, 20, 1.0, 120, new Resources(50,80,20)) }
        };

        public InteractionResolver(
            ICharacterRepository characters,
            IOpinionRepository opinions,
            IFactionService factions,
            ISecretsRepository secrets,
            IRandomService rng,
            IDiplomacyService diplomacy,
            ITravelService travel,
            IPiracyService piracy,
            IPlanetRepository planets,
            IFleetRepository fleets,
            IPlanetEconomyRepository economy,
            IEventBus events,
            IBattleOutcomeService battleOutcomeService,
            IIntimacyLog intimacy,
            IMeritPolicy merit,
            InteractionConfig? config = null)
        {
            _chars = characters ?? throw new ArgumentNullException(nameof(characters));
            _opinions = opinions ?? throw new ArgumentNullException(nameof(opinions));
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
            _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            _travel = travel ?? throw new ArgumentNullException(nameof(travel));
            _piracy = piracy ?? throw new ArgumentNullException(nameof(piracy));
            _planets = planets ?? throw new ArgumentNullException(nameof(planets));
            _fleets = fleets ?? throw new ArgumentNullException(nameof(fleets));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _battleOutcomeService = battleOutcomeService ?? throw new ArgumentNullException(nameof(battleOutcomeService));
            _intimacy = intimacy ?? throw new ArgumentNullException(nameof(intimacy));
            _merit = merit ?? throw new ArgumentNullException(nameof(merit));
            _cfg = config ?? InteractionConfig.Default;
            _factionStatusCache = new Dictionary<Guid, FactionStatus>();
            _systemSecurityCache = new Dictionary<Guid, SystemSecurity>();
            _characterLocationCache = new Dictionary<Guid, (Guid? PlanetId, Guid? SystemId)>();
        }

        public IEnumerable<ISocialEvent> Resolve(CharacterIntent intent, int currentYear, int currentMonth)
        {
            if (intent == null)
                return Array.Empty<ISocialEvent>();

            var actor = _chars.GetById(intent.ActorId);
            if (actor == null || !actor.IsAlive)
                return Array.Empty<ISocialEvent>();

            if (_cfg.DisableSensitiveIntents && (intent.Type == IntentType.TorturePrisoner || intent.Type == IntentType.RapePrisoner))
                return Array.Empty<ISocialEvent>();

            var actorFactionId = _factions.GetFactionIdForCharacter(actor.Id);
            var factionStatus = GetFactionStatus(actorFactionId);
            var actorSystemId = GetSystemOfCharacter(actor.Id);
            var systemSecurity = actorSystemId.HasValue ? GetSystemSecurity(actorSystemId.Value) : null;

            switch (intent.Type)
            {
                case IntentType.Court:
                    return ResolveCourt(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.VisitFamily:
                    return ResolveVisitFamily(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.VisitLover:
                    return ResolveVisitLover(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.Spy:
                    return ResolveSpy(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.Bribe:
                    return ResolveBribe(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.Recruit:
                    return ResolveRecruit(actor, intent, currentYear, currentMonth, factionStatus, actorFactionId);
                case IntentType.Defect:
                    return ResolveDefect(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.Negotiate:
                    return ResolveNegotiate(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.Quarrel:
                    return ResolveQuarrel(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.Assassinate:
                    return ResolveAssassinate(actor, intent, currentYear, currentMonth, factionStatus, actorFactionId);
                case IntentType.TorturePrisoner:
                    return ResolveTorture(actor, intent, currentYear, currentMonth, factionStatus, actorFactionId);
                case IntentType.RapePrisoner:
                    return ResolveRape(actor, intent, currentYear, currentMonth, factionStatus, actorFactionId);
                case IntentType.TravelToPlanet:
                    return ResolveTravelToPlanet(actor, intent, currentYear, currentMonth, factionStatus, systemSecurity);
                case IntentType.BecomePirate:
                    return ResolveBecomePirate(actor, intent, currentYear, currentMonth, factionStatus);
                case IntentType.RaidConvoy:
                    return ResolveRaidConvoy(actor, intent, currentYear, currentMonth, factionStatus, systemSecurity);
                case IntentType.FoundHouse:
                    return ResolveFoundGreatHouse(actor, intent, currentYear, currentMonth);
                case IntentType.FoundPirateClan:
                    return ResolveFoundPirateClan(actor, intent, currentYear, currentMonth, actorSystemId);
                case IntentType.ExpelFromHouse:
                    return ResolveExpelFromHouse(actor, intent, currentYear, currentMonth);
                case IntentType.ClaimPlanetSeat:
                    return ResolveClaimPlanetSeat(actor, intent, currentYear, currentMonth);
                case IntentType.BuildInfrastructure:
                    return ResolveBuildInfrastructure(actor, intent, currentYear, currentMonth);
                case IntentType.BuildFleet:
                    return ResolveBuildFleet(actor, intent, currentYear, currentMonth, actorFactionId, actorSystemId);

                default:
                    return Array.Empty<ISocialEvent>();
            }
        }

        #region Courtship
        private IEnumerable<ISocialEvent> ResolveCourt(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.25 + actor.Personality.CheckCompatibility(target.Personality) / 300.0;
            baseChance += (actor.Personality.Extraversion - 50) * 0.002;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Agreeableness"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.HasAlliance) baseChance += 0.1;
            if (actor.Ambition == CharacterAmbition.EnsureFamilyLegacy) baseChance += 0.15;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var deltaActorToTarget = success ? 5 : -3;
            var deltaTargetToActor = success ? 7 : -4;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, success ? "Romance success" : "Awkward attempt");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, success ? "Charmed" : "Turned down");
            if (success)
            {
                if (!actor.Relationships.Any(r => r.TargetCharacterId == target.Id))
                    actor.AddRelationship(target.Id, RelationshipType.Lover);
                _chars.Save(actor);
                _diplomacy.AdjustRelations(_factions.GetFactionIdForCharacter(actor.Id), _factions.GetFactionIdForCharacter(target.Id), 5);

                AwardMerit(actor, _merit.Compute(MeritAction.Courtship, MeritContext.Succeeded(actor.Ambition)));
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.CourtshipAttempt,
                actor.Id, target.Id, null, null, success,
                deltaActorToTarget, deltaTargetToActor, Array.Empty<Guid>(),
                success ? "Shared time and chemistry." : "It didn’t land."
            );
            _events.Publish(ev);
            return new[] { ev };
        }

        #endregion

        #region VisitFamily
        private IEnumerable<ISocialEvent> ResolveVisitFamily(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var relative = _chars.GetById(intent.TargetCharacterId.Value);
            if (relative == null || !relative.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.4 + (actor.Personality.Agreeableness - 50) * 0.003 + (actor.Personality.Conscientiousness - 50) * 0.002;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Agreeableness"])
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.HasUnrest)
                baseChance += 0.1;
            if (actor.Ambition == CharacterAmbition.EnsureFamilyLegacy)
                baseChance += 0.15;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var delta = success ? 6 : 2;

            _opinions.AdjustOpinion(actor.Id, relative.Id, delta, "Family time");
            _opinions.AdjustOpinion(relative.Id, actor.Id, delta, "Family time");
            if (success)
            {
                var lover = _chars.GetById(intent.TargetCharacterId.Value);
                if (lover == null || !lover.IsAlive)
                    return Array.Empty<ISocialEvent>();

                if (actor.Relationships.Any(r => r.TargetCharacterId == lover.Id && r.Type == RelationshipType.Spouse))
                {
                    _intimacy.RecordIntimacyEncounter(actor.Id, lover.Id, currentYear, currentMonth);
                    _opinions.AdjustOpinion(actor.Id, relative.Id, delta, "Intimicy time");
                    _opinions.AdjustOpinion(relative.Id, actor.Id, delta, "Intimicy time");
                }
            }
            _chars.Save(actor);
            _chars.Save(relative);

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.FamilyVisit,
                actor.Id, relative.Id, null, null, success,
                delta, delta, Array.Empty<Guid>(),
                success ? "A warm reunion." : "A brief check-in."
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Visit Lover
        private IEnumerable<ISocialEvent> ResolveVisitLover(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();

            var lover = _chars.GetById(intent.TargetCharacterId.Value);
            if (lover == null || !lover.IsAlive)
                return Array.Empty<ISocialEvent>();

            var isRomantic = actor.Relationships.Any(r => r.TargetCharacterId == lover.Id &&
                (r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse));
            if (!isRomantic)
                return Array.Empty<ISocialEvent>();

            var originPlanetId = GetPlanetOfCharacter(actor.Id);
            var loverPlanetId = GetPlanetOfCharacter(lover.Id);

            var travelSucceeded = true;
            if (originPlanetId.HasValue && loverPlanetId.HasValue && originPlanetId.Value != loverPlanetId.Value)
            {
                var baseTravelChance = 0.75;
                var systemSecurity = loverPlanetId.HasValue
                    ? GetSystemSecurity(_planets.GetById(loverPlanetId.Value)?.SystemId ?? Guid.Empty)
                    : null;

                var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
                foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Openness"]))
                    baseTravelChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;

                if (systemSecurity != null)
                    baseTravelChance -= systemSecurity.PirateActivity / 500.0;

                travelSucceeded = _rng.NextDouble() < Clamp01(baseTravelChance);

                if (travelSucceeded)
                {
                    var fleetId = GetAvailableFleet(_factions.GetFactionIdForCharacter(actor.Id));
                    if (fleetId != Guid.Empty)
                    {
                        _travel.PlanFleetTravel(
                            fleetId: fleetId,
                            originPlanetId: originPlanetId.Value,
                            destPlanetId: loverPlanetId.Value,
                            cargo: null,
                            passengerIds: new[] { actor.Id }
                        );
                    }
                    else
                    {
                        travelSucceeded = false;
                    }
                }
            }

            if (!travelSucceeded)
            {
                var failEv = new SocialEvent(
                    Guid.NewGuid(), currentYear, currentMonth, SocialEventType.LoverVisit,
                    actor.Id, lover.Id, null, null, Success: false,
                    0, 0, Array.Empty<Guid>(), "Travel failed; could not visit lover."
                );
                _events.Publish(failEv);
                return new[] { failEv };
            }

            // together — improve bond, small merit, possible conception (age‑gated)
            var baseChance = 0.55
                + (actor.Personality.Agreeableness - 50) * 0.003
                + (actor.Personality.Extraversion - 50) * 0.002;

            var traits2 = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits2["Agreeableness"].Concat(traits2["Extraversion"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;

            if (factionStatus.HasAlliance) baseChance += 0.05;
            var success = _rng.NextDouble() < Clamp01(baseChance);

            var deltaMutual = success ? 8 : 3;
            _opinions.AdjustOpinion(actor.Id, lover.Id, deltaMutual, "Quality time");
            _opinions.AdjustOpinion(lover.Id, actor.Id, deltaMutual, "Quality time");

            if (success)
            {
                _intimacy.RecordIntimacyEncounter(actor.Id, lover.Id, currentYear, currentMonth);
            }

            _chars.Save(actor);
            _chars.Save(lover);

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.LoverVisit,
                actor.Id, lover.Id, null, null, success,
                deltaMutual, deltaMutual, Array.Empty<Guid>(),
                success ? "Shared intimate time together." : "Brief encounter; still appreciated."
            );
            _events.Publish(ev);
            return new[] { ev };
        }

        #endregion

        #region Spy
        private IEnumerable<ISocialEvent> ResolveSpy(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetFactionId.HasValue && !intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.2 + actor.Skills.Intelligence / 200.0;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Openness"].Concat(traits["Neuroticism"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.IsAtWar)
                baseChance += 0.15;
            if (actor.Ambition == CharacterAmbition.SeekAdventure)
                baseChance += 0.1;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();

            string notes;

            if (success)
            {
                var severity = 50 + _rng.NextInt(0, 40);
                Secret secret;
                if (intent.TargetCharacterId.HasValue)
                {
                    var about = _chars.GetById(intent.TargetCharacterId.Value);
                    secret = new Secret(
                        Guid.NewGuid(), SecretType.PersonalInformation,
                        about != null ? $"Information observed for {about.Name}" : "Target information observed",
                        intent.TargetCharacterId.Value, null, severity, currentYear, currentMonth);
                }
                else
                {
                    secret = new Secret(
                        Guid.NewGuid(), SecretType.MilitaryDisposition,
                        "Enemy fleet movement intel", null, intent.TargetFactionId, severity, currentYear, currentMonth);
                }
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = intent.TargetCharacterId.HasValue
                    ? "Acquired sensitive personal information."
                    : "Acquired fleet disposition report.";

                AwardMerit(actor, _merit.Compute(MeritAction.Spy, new MeritContext { Success = success, Ambition = actor.Ambition, ProducedIntel = success, IntelSeverity = success ? severity : 0 }));
            }
            else
            {
                notes = "Operation compromised or yielded nothing.";
                if (intent.TargetFactionId.HasValue)
                    _diplomacy.AdjustRelations(_factions.GetFactionIdForCharacter(actor.Id), intent.TargetFactionId.Value, -5);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.EspionageOperation,
                actor.Id, intent.TargetCharacterId, intent.TargetFactionId, null, success,
                0, 0, secrets, notes
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Bribe
        private IEnumerable<ISocialEvent> ResolveBribe(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var corruptibility = (100 - target.Personality.Conscientiousness + 100 - target.Personality.Agreeableness) / 200.0;
            var baseChance = 0.15 + corruptibility * 0.6;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"])
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.EconomyWeak) baseChance += 0.1;
            if (actor.Ambition == CharacterAmbition.BuildWealth) baseChance += 0.1;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var deltaActorToTarget = success ? 3 : -5;
            var deltaTargetToActor = success ? 8 : -10;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, success ? "Successful bribe" : "Bribe rebuffed");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, success ? "Took the money" : "Offended by bribe");

            AwardMerit(actor, _merit.Compute(MeritAction.Bribe, new MeritContext { Success = success, Ambition = actor.Ambition }));

            var secrets = new List<Guid>();
            if (success)
            {
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.Corruption,
                    $"{target.Name} accepted bribes.", target.Id, null,
                    60 + _rng.NextInt(0, 25), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                _diplomacy.AdjustRelations(_factions.GetFactionIdForCharacter(actor.Id), _factions.GetFactionIdForCharacter(target.Id), 5);
            }
            else
            {
                _diplomacy.AdjustRelations(_factions.GetFactionIdForCharacter(actor.Id), _factions.GetFactionIdForCharacter(target.Id), -5);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.BriberyAttempt,
                actor.Id, target.Id, null, null, success,
                deltaActorToTarget, deltaTargetToActor, secrets,
                success ? "Greased palms; influence secured." : "Refusal and offense taken."
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Recruit
        private IEnumerable<ISocialEvent> ResolveRecruit(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus, Guid actorFactionId)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (myFaction == Guid.Empty)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.25 + (actor.Personality.Agreeableness - 50) * 0.002 + (actor.Personality.Extraversion - 50) * 0.002 + (int)actor.Rank * 0.02;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Agreeableness"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.HasUnrest)
                baseChance += 0.1;
            if (actor.Ambition == CharacterAmbition.GainPower)
                baseChance += 0.1;

            baseChance += Math.Clamp(_opinions.GetOpinion(target.Id, actor.Id), -50, 50) / 100.0;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var deltaActorToTarget = success ? 5 : -2;
            var deltaTargetToActor = success ? 6 : -4;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, success ? "Effective recruitment talk" : "Rejected recruitment");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, success ? "Motivated by recruiter" : "Annoyed by recruiter");

            if (success)
            {
                _factions.MoveCharacterToFaction(target.Id, actorFactionId);
                _chars.Save(target);

                var oldFactionId = _factions.GetFactionIdForCharacter(target.Id);
                if (oldFactionId != Guid.Empty && oldFactionId != actorFactionId)
                    _diplomacy.AdjustRelations(actorFactionId, oldFactionId, -5);

                AwardMerit(actor, _merit.Compute(MeritAction.Recruit, new MeritContext { Success = success, Ambition = actor.Ambition }));
            }
            else
            {
                AwardMerit(actor, _merit.Compute(MeritAction.Recruit, new MeritContext { Success = success, Ambition = actor.Ambition }));
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.RecruitmentAttempt,
                actor.Id, target.Id, null, null, success,
                deltaActorToTarget, deltaTargetToActor, Array.Empty<Guid>(),
                success ? "Target agreed to join faction." : "Recruitment declined."
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Defect
        private IEnumerable<ISocialEvent> ResolveDefect(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetFactionId.HasValue)
                return Array.Empty<ISocialEvent>();

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (myFaction == Guid.Empty)
                return Array.Empty<ISocialEvent>();

            var myLeader = _factions.GetLeaderId(myFaction);
            var dislikeLeader = myLeader.HasValue ? Math.Max(0, -_opinions.GetOpinion(actor.Id, myLeader.Value)) : 0;
            var baseChance = 0.05 + dislikeLeader / 200.0 + (50 - actor.Personality.Conscientiousness) * 0.002 + (50 - actor.Personality.Agreeableness) * 0.002;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Openness"].Concat(traits["Neuroticism"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.HasUnrest) baseChance += 0.15;
            if (actor.Ambition == CharacterAmbition.SeekAdventure) baseChance += 0.1;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            var meritChange = _merit.Compute(MeritAction.Defect, new MeritContext { Success = success, Ambition = actor.Ambition });
            if (success)
            {
                _factions.MoveCharacterToFaction(actor.Id, intent.TargetFactionId.Value);
                AwardMerit(actor, meritChange);
                if (myLeader.HasValue)
                    _opinions.AdjustOpinion(actor.Id, myLeader.Value, -20, "Defected from faction");
                _diplomacy.AdjustRelations(myFaction, intent.TargetFactionId.Value, -10);
            }
            else
            {
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.DefectionPlot,
                    $"{actor.Name} plotted to defect.", actor.Id, null,
                    50 + _rng.NextInt(0, 20), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                AwardMerit(actor, meritChange);
                if (myLeader.HasValue)
                    _opinions.AdjustOpinion(actor.Id, myLeader.Value, -10, "Suspected defection");
                _diplomacy.AdjustRelations(myFaction, intent.TargetFactionId.Value, -5);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.DefectionAttempt,
                actor.Id, null, intent.TargetFactionId, null, success,
                0, myLeader.HasValue ? (success ? -20 : -10) : 0, secrets,
                success ? "Defection succeeded; new allegiance sworn." : "Attempt to defect failed or aborted."
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Negotiate
        private IEnumerable<ISocialEvent> ResolveNegotiate(
            Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetFactionId.HasValue)
                return Array.Empty<ISocialEvent>();

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (myFaction == Guid.Empty)
                return Array.Empty<ISocialEvent>();

            var atWar = _factions.IsAtWar(myFaction, intent.TargetFactionId.Value);

            // chance: personality + rank + economy nudge + ambition
            var baseChance = 0.2
                + (actor.Personality.Agreeableness - 50) * 0.003
                + (actor.Personality.Extraversion - 50) * 0.002
                + (int)actor.Rank * 0.02;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Agreeableness"].Concat(traits["Extraversion"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.EconomyWeak) baseChance += 0.15;
            if (actor.Ambition == CharacterAmbition.BuildWealth) baseChance += 0.1;
            if (atWar) baseChance += 0.05;

            var success = _rng.NextDouble() < Clamp01(baseChance);

            var treaty = ChooseTreatyType(actor, myFaction, intent.TargetFactionId.Value, atWar);
            int relationDeltaIfSuccess = treaty switch
            {
                TreatyType.Ceasefire => 15,
                TreatyType.Alliance => 20,
                TreatyType.Trade => 10,
                TreatyType.ResearchPact => 10,
                TreatyType.NonAggression => 8,
                _ => 10
            };

            if (success)
                _diplomacy.AdjustRelations(myFaction, intent.TargetFactionId.Value, relationDeltaIfSuccess);
            else
                _diplomacy.AdjustRelations(myFaction, intent.TargetFactionId.Value, -5);

            // merit
            AwardMerit(actor, _merit.Compute(MeritAction.Negotiate,
                new MeritContext { Success = success, Ambition = actor.Ambition, AtWar = atWar }));

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.Negotiation,
                actor.Id, null, intent.TargetFactionId, null, success,
                0, 0, Array.Empty<Guid>(),
                success ? $"Talks progressed ({treaty})." : "Talks stalled."
            );
            _events.Publish(ev);
            return new[] { ev };
        }

        #endregion

        #region Quarrel
        private IEnumerable<ISocialEvent> ResolveQuarrel(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.4 + (actor.Personality.Neuroticism - 50) * 0.003 + (target.Personality.Neuroticism - 50) * 0.002 - (actor.Personality.Agreeableness - 50) * 0.003;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"])
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            if (factionStatus.HasUnrest) baseChance += 0.1;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var deltaActorToTarget = success ? -6 : -2;
            var deltaTargetToActor = success ? -6 : -2;
            var meritChange = success ? -3 : -1;

            _opinions.AdjustOpinion(actor.Id, target.Id, deltaActorToTarget, "Quarrel");
            _opinions.AdjustOpinion(target.Id, actor.Id, deltaTargetToActor, "Quarrel");
            actor.GainMerit(meritChange);
            _chars.Save(actor);
            if (success)
                _diplomacy.AdjustRelations(_factions.GetFactionIdForCharacter(actor.Id), _factions.GetFactionIdForCharacter(target.Id), -5);

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.Quarrel,
                actor.Id, target.Id, null, null, success,
                deltaActorToTarget, deltaTargetToActor, Array.Empty<Guid>(),
                success ? "Harsh words exchanged." : "Tense but contained."
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Assassinate
        private IEnumerable<ISocialEvent> ResolveAssassinate(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus, Guid actorFactionId)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.05 + actor.Skills.Military / 150.0 + (actor.Personality.Neuroticism - 50) * 0.002 + (actor.Personality.Extraversion - 50) * 0.001;
            baseChance -= (target.Rank - actor.Rank) * 0.02;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"].Concat(traits["Extraversion"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            baseChance += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality) / 100.0;
            var targetTraits = PersonalityTraits.GetActiveTraits(target.Personality);
            foreach (var (traitName, intensity) in targetTraits["Conscientiousness"])
                baseChance -= PersonalityTraits.GetTraitEffect(traitName, target.Personality) / 100.0;
            if (_factions.IsAtWar(actorFactionId, _factions.GetFactionIdForCharacter(target.Id))) baseChance += 0.2;
            if (factionStatus.HasUnrest) baseChance += 0.1;
            if (actor.Ambition == CharacterAmbition.GainPower) baseChance += 0.1;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            var meritChange = _merit.Compute(MeritAction.Assassinate, new MeritContext { Success = success, Ambition = actor.Ambition });
            if (!success)
            {
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.AssassinationPlot,
                    $"{actor.Name} plotted against {target.Name}", actor.Id, null,
                    70 + _rng.NextInt(0, 20), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                AwardMerit(actor, meritChange);
                _opinions.AdjustOpinion(target.Id, actor.Id, -10, "Suspected assassination attempt");
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForCharacter(target.Id), -10);
            }
            else
            {
                target.MarkDead();
                _chars.Save(target);
                AwardMerit(actor, meritChange);
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForCharacter(target.Id), -20);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.AssassinationAttempt,
                actor.Id, target.Id, null, null, success,
                0, success ? 0 : -10, secrets,
                success ? "Target eliminated." : "Attempt failed; whispers spread."
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Torture
        private IEnumerable<ISocialEvent> ResolveTorture(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus, Guid actorFactionId)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.3 + actor.Skills.Military / 200.0 + (50 - actor.Personality.Agreeableness) * 0.003 + (actor.Personality.Conscientiousness - 50) * 0.002;
            baseChance -= (target.Rank - actor.Rank) * 0.01;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"])
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            baseChance += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality) / 100.0;
            var targetTraits = PersonalityTraits.GetActiveTraits(target.Personality);
            foreach (var (traitName, intensity) in targetTraits["Conscientiousness"])
                baseChance -= PersonalityTraits.GetTraitEffect(traitName, target.Personality) / 100.0;
            if (_factions.IsAtWar(actorFactionId, _factions.GetFactionIdForCharacter(target.Id))) baseChance += 0.15;
            if (factionStatus.IsAtWar) baseChance += 0.1;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            string notes;

            if (success)
            {
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.PersonalInformation,
                    $"Interrogation of {target.Name} yielded sensitive information.",
                    target.Id, null, 60 + _rng.NextInt(0, 30), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Torture yielded valuable information.";
                _opinions.AdjustOpinion(actor.Id, target.Id, -10, "Tortured prisoner");
                _opinions.AdjustOpinion(target.Id, actor.Id, -20, "Victim of torture");
                target.ApplyTrauma(TraumaType.Torture);
                AwardMerit(actor, _merit.Compute(MeritAction.Torture, new MeritContext { Success = success, Ambition = actor.Ambition, ProducedIntel = true, IntelSeverity = secret.Severity }));
                _chars.Save(target);
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForCharacter(target.Id), -15);
            }
            else
            {
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.TortureAttempt,
                    $"{actor.Name} attempted to torture {target.Name}.",
                    actor.Id, null, 50 + _rng.NextInt(0, 20), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Torture attempt failed; rumors spread.";
                AwardMerit(actor, _merit.Compute(MeritAction.Torture, new MeritContext { Success = success, Ambition = actor.Ambition, ProducedIntel = false }));
                _opinions.AdjustOpinion(target.Id, actor.Id, -10, "Failed torture attempt");
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForCharacter(target.Id), -5);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.TortureAttempt,
                actor.Id, target.Id, null, null, success,
                success ? -10 : 0, success ? -20 : -10, secrets, notes
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Rape
        private IEnumerable<ISocialEvent> ResolveRape(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus, Guid actorFactionId)
        {
            if (!intent.TargetCharacterId.HasValue)
                return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive)
                return Array.Empty<ISocialEvent>();

            var baseChance = 0.2 + (50 - actor.Personality.Agreeableness) * 0.004 + (50 - actor.Personality.Conscientiousness) * 0.003 + (int)actor.Rank * 0.02;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"].Concat(traits["Extraversion"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            baseChance += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality) / 100.0;
            var targetTraits = PersonalityTraits.GetActiveTraits(target.Personality);
            foreach (var (traitName, intensity) in targetTraits["Conscientiousness"])
                baseChance -= PersonalityTraits.GetTraitEffect(traitName, target.Personality) / 100.0;
            if (_factions.IsAtWar(actorFactionId, _factions.GetFactionIdForCharacter(target.Id))) baseChance += 0.15;
            if (factionStatus.HasUnrest) baseChance += 0.05;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            string notes;

            if (success)
            {
                _intimacy.RecordIntimacyEncounter(actor.Id, target.Id, currentYear, currentMonth);
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.RapeIncident,
                    $"{actor.Name} committed a heinous act against {target.Name}.",
                    actor.Id, null, 80 + _rng.NextInt(0, 15), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Act committed; grave consequences loom.";
                _opinions.AdjustOpinion(actor.Id, target.Id, 5, "Committed rape");
                _opinions.AdjustOpinion(target.Id, actor.Id, -30, "Victim of rape");
                target.ApplyTrauma(TraumaType.Rape);
                _chars.Save(actor);
                _chars.Save(target);
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForCharacter(target.Id), -30);
            }
            else
            {
                var secret = new Secret(
                    Guid.NewGuid(), SecretType.RapeAttempt,
                    $"{actor.Name} attempted to assault {target.Name}.",
                    actor.Id, null, 60 + _rng.NextInt(0, 20), currentYear, currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Attempt failed; whispers spread.";
                _opinions.AdjustOpinion(target.Id, actor.Id, -15, "Failed assault attempt");
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForCharacter(target.Id), -10);
            }
            AwardMerit(actor, _merit.Compute(MeritAction.Rape, new MeritContext { Success = success, Ambition = actor.Ambition }));

            var ev = new SocialEvent(
                Guid.NewGuid(), currentYear, currentMonth, SocialEventType.RapeAttempt,
                actor.Id, target.Id, null, null, success,
                success ? -15 : 0, success ? -30 : -15, secrets, notes
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Travel
        private IEnumerable<ISocialEvent> ResolveTravelToPlanet(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus, SystemSecurity? systemSecurity)
        {
            if (!intent.TargetPlanetId.HasValue)
                return Array.Empty<ISocialEvent>();

            var originPlanetId = GetPlanetOfCharacter(actor.Id);
            if (!originPlanetId.HasValue)
                return Array.Empty<ISocialEvent>();

            var destPlanet = _planets.GetById(intent.TargetPlanetId.Value);
            if (destPlanet == null)
                return Array.Empty<ISocialEvent>();

            var actorFactionId = _factions.GetFactionIdForCharacter(actor.Id);

            var baseChance = 0.8;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Openness"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;

            if (systemSecurity != null && systemSecurity.PirateActivity > 50)
                baseChance -= systemSecurity.PirateActivity / 500.0;
            if (destPlanet.UnrestLevel > 50)
                baseChance -= destPlanet.UnrestLevel / 500.0;
            if (factionStatus.HasAlliance && _factions.GetFactionIdForPlanet(destPlanet.Id) == actorFactionId)
                baseChance += 0.1;
            if (actor.Ambition == CharacterAmbition.SeekAdventure)
                baseChance += 0.15;

            bool success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            string notes;

            if (success)
            {
                var fleetId = GetAvailableFleet(actorFactionId);
                if (fleetId == Guid.Empty)
                {
                    success = false;
                    notes = "No available fleet to transport the passenger.";
                }
                else
                {
                    _travel.PlanFleetTravel(
                        fleetId: fleetId,
                        originPlanetId: originPlanetId.Value,
                        destPlanetId: destPlanet.Id,
                        cargo: null,
                        passengerIds: new[] { actor.Id }
                    );

                    _chars.Save(actor);
                    notes = "Fleet travel planned; passenger added to manifest.";

                    if (_factions.HasAlliance(actorFactionId, _factions.GetFactionIdForPlanet(destPlanet.Id)))
                        _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForPlanet(destPlanet.Id), 5);
                }
            }
            else
            {
                notes = "Travel planning failed due to logistical issues or risks.";
                if (systemSecurity != null && systemSecurity.PirateActivity > 50)
                {
                    var secret = new Secret(
                        Guid.NewGuid(),
                        SecretType.TravelRisk,
                        $"{actor.Name} faced high piracy risk en route.",
                        actor.Id,
                        null,
                        50 + _rng.NextInt(0, 20),
                        currentYear,
                        currentMonth);
                    _secrets.Add(secret);
                    secrets.Add(secret.Id);
                }
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForPlanet(destPlanet.Id), -3);
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.TravelBooked,
                actor.Id,
                null,
                null,
                intent.TargetPlanetId,
                success,
                0,
                0,
                secrets,
                notes
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region BecomePirate
        private IEnumerable<ISocialEvent> ResolveBecomePirate(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus)
        {
            var actorFactionId = _factions.GetFactionIdForCharacter(actor.Id);
            if (_piracy.IsPirateFaction(actorFactionId))
                return Array.Empty<ISocialEvent>();

            double baseChance = 0.3
                + (50 - actor.Personality.Conscientiousness) * 0.003
                + (50 - actor.Personality.Agreeableness) * 0.003
                + PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality) / 100.0;

            if (factionStatus.HasUnrest) baseChance += 0.15;
            if (actor.Ambition == CharacterAmbition.SeekAdventure) baseChance += 0.15;

            var actorSystemId = GetSystemOfCharacter(actor.Id);
            if (actorSystemId.HasValue)
            {
                var sec = GetSystemSecurity(actorSystemId.Value);
                baseChance += sec.PirateActivity / 500.0;
                baseChance -= sec.PatrolStrength / 1200.0;
            }

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            string notes;

            var meritDelta = _merit.Compute(MeritAction.BecomePirate,
                new MeritContext { Success = success, Ambition = actor.Ambition });

            if (!success)
            {
                notes = "Decided against piracy (for now).";
                AwardMerit(actor, meritDelta);
                var evFail = new SocialEvent(Guid.NewGuid(), currentYear, currentMonth, SocialEventType.PirateDefection,
                    actor.Id, null, null, null, false, 0, 0, secrets, notes);
                _events.Publish(evFail);
                return new[] { evFail };
            }

            Guid? clanToJoin = actorSystemId.HasValue ? PickLocalPirateClan(actorSystemId.Value) : null;

            if (!clanToJoin.HasValue)
            {
                var globalPirates = _piracy.GetPirateFactionId();
                if (globalPirates != Guid.Empty && _piracy.IsPirateFaction(globalPirates))
                    clanToJoin = globalPirates;
            }

            bool createdClan = false;
            if (!clanToJoin.HasValue)
            {
                var clan = new Faction(Guid.NewGuid(), $"{actor.Name.Split(' ').First()}'s Cutthroats", actor.Id); // ToDo: random pirate clan names
                _factions.Save(clan);
                _piracy.RegisterPirateFaction(clan.Id);
                clanToJoin = clan.Id;
                createdClan = true;
            }

            var oldFactionId = actorFactionId;
            _factions.MoveCharacterToFaction(actor.Id, clanToJoin.Value);

            var oldLeader = _factions.GetLeaderId(oldFactionId);
            if (oldLeader.HasValue)
                _opinions.AdjustOpinion(actor.Id, oldLeader.Value, -20, "Defected to pirates");
            _diplomacy.AdjustRelations(oldFactionId, clanToJoin.Value, -15);

            AwardMerit(actor, meritDelta);

            notes = createdClan
                ? $"Founded a pirate clan and joined: {_factions.GetFaction(clanToJoin.Value)?.Name ?? "New Pirate Clan"}."
                : $"Joined pirate clan: {_factions.GetFaction(clanToJoin.Value)?.Name ?? "Pirates"}.";

            var ev = new SocialEvent(Guid.NewGuid(), currentYear, currentMonth, SocialEventType.PirateDefection,
                actor.Id, null, clanToJoin, null, true, 0, 0, secrets, notes);
            _events.Publish(ev);
            return new[] { ev };
        }

        #endregion

        #region RaidConvoy
        private IEnumerable<ISocialEvent> ResolveRaidConvoy(Character actor, CharacterIntent intent, int currentYear, int currentMonth, FactionStatus factionStatus, SystemSecurity? systemSecurity)
        {
            if (!intent.TargetFactionId.HasValue || systemSecurity == null)
                return Array.Empty<ISocialEvent>();

            var actorFactionId = _factions.GetFactionIdForCharacter(actor.Id);
            if (!_piracy.IsPirateFaction(actorFactionId))
                return Array.Empty<ISocialEvent>();

            var targetSystemId = intent.TargetFactionId!.Value;
            var targetSec = GetSystemSecurity(targetSystemId);

            var baseChance = 0.3 + actor.Skills.Military / 200.0;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Neuroticism"]))
                baseChance += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) / 100.0;
            baseChance += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality) / 100.0;

            baseChance += targetSec.Traffic / 500.0;
            baseChance -= targetSec.PatrolStrength / 500.0;

            if (actor.Ambition == CharacterAmbition.SeekAdventure)
                baseChance += 0.15;

            var success = _rng.NextDouble() < Clamp01(baseChance);
            var secrets = new List<Guid>();
            var meritChange = success ? (actor.Ambition == CharacterAmbition.SeekAdventure ? 12 : 8) : -5;
            string notes;

            if (success)
            {
                var plan = GetRandomTravelPlanInSystem(targetSystemId);
                if (plan != null)
                {
                    var convoyFleet = _fleets.GetById(plan.FleetId);
                    var pirateFleet = GetPirateFleet(actorFactionId);

                    if (convoyFleet == null || pirateFleet == null)
                    {
                        notes = "No valid fleets available for the raid.";
                        success = false;
                    }
                    else
                    {
                        var result = ResolveFleetBattleQuick(pirateFleet, convoyFleet);

                        _battleOutcomeService.ProcessFleetBattle(pirateFleet, convoyFleet, result);

                        if (result.WinnerFleet?.Id == pirateFleet.Id)
                        {
                            notes = "Convoy raid succeeded; resources captured.";
                            actor.GainMerit(meritChange);

                            // give pirates some physical loot too
                            var resources = new Resources(_rng.NextInt(50, 200), _rng.NextInt(50, 200), _rng.NextInt(50, 200));
                            pirateFleet.AddCargo(resources);

                            _chars.Save(actor);
                            _fleets.Save(pirateFleet);
                            _fleets.Save(convoyFleet);

                            _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForSystem(targetSystemId), -20);
                        }
                        else
                        {
                            notes = "Convoy raid failed in battle.";
                            success = false;
                            actor.GainMerit(-meritChange);
                            _chars.Save(actor);
                            _fleets.Save(pirateFleet);
                            _fleets.Save(convoyFleet);
                            _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForSystem(targetSystemId), -10);
                        }
                    }
                }
                else
                {
                    notes = "No suitable convoy found for raid.";
                    success = false;
                    _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForSystem(targetSystemId), -5);
                }
            }
            else
            {
                var secret = new Secret(
                    Guid.NewGuid(),
                    SecretType.PiracyAttempt,
                    $"{actor.Name} planned a convoy raid.",
                    actor.Id,
                    null,
                    60 + _rng.NextInt(0, 20),
                    currentYear,
                    currentMonth);
                _secrets.Add(secret);
                secrets.Add(secret.Id);
                notes = "Raid planning failed; whispers spread.";
                _diplomacy.AdjustRelations(actorFactionId, _factions.GetFactionIdForSystem(targetSystemId), -10);
            }

            var ok = _piracy.RegisterAmbush(actor.Id, targetSystemId, currentYear, currentMonth);
            if (!ok && success)
            {
                notes = "Raid succeeded but ambush registration failed.";
                success = false;
            }

            var ev = new SocialEvent(
                Guid.NewGuid(),
                currentYear,
                currentMonth,
                SocialEventType.RaidPlanned,
                actor.Id,
                null,
                intent.TargetFactionId,
                null,
                success,
                0,
                0,
                secrets,
                notes
            );
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Found Great House
        private IEnumerable<ISocialEvent> ResolveFoundGreatHouse(Character actor, CharacterIntent intent, int y, int m)
        {
            if (intent.TargetPlanetId == null) return Array.Empty<ISocialEvent>();
            if (_factions.GetFactionIdForCharacter(actor.Id) != Guid.Empty) return Array.Empty<ISocialEvent>();

            var planet = _planets.GetById(intent.TargetPlanetId.Value);
            if (planet == null) return Array.Empty<ISocialEvent>();

            int supporters = 0;
            foreach (var id in planet.Citizens.Take(30))
                if (id != actor.Id && _opinions.GetOpinion(actor.Id, id) >= 25) supporters++;

            double chance =
                0.35
                + (int)actor.Rank * 0.03
                + Math.Clamp(actor.Balance, 0, 2000) / 8000.0
                + supporters * 0.01;

            var success = _rng.NextDouble() < Math.Clamp(chance, 0.05, 0.9);

            if (!success)
            {
                var failEv = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.FoundGreatHouse,
                    actor.Id, null, null, intent.TargetPlanetId, false, 0, 0, Array.Empty<Guid>(),
                    "Founding a House failed to gather enough support.");
                _events.Publish(failEv);
                return new[] { failEv };
            }

            var newFaction = new Faction(Guid.NewGuid(), $"{actor.Name.Split(' ').First()} House", actor.Id);
            _factions.Save(newFaction);
            _factions.MoveCharacterToFaction(actor.Id, newFaction.Id);

            planet.SetSeatPlanet(newFaction.Id);
            _planets.Save(planet);

            AwardMerit(actor, _merit.Compute(MeritAction.HouseFoundedMajor, new MeritContext { Success = success, Ambition = actor.Ambition }));

            var ev = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.FoundGreatHouse,
                actor.Id, null, newFaction.Id, planet.Id, true, 0, 0, Array.Empty<Guid>(),
                $"Founded Great House '{newFaction.Name}'. Seat established on planet.");
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Found Pirate clan
        private IEnumerable<ISocialEvent> ResolveFoundPirateClan(Character actor, CharacterIntent intent, int y, int m, Guid? actorSystemId)
        {
            if (actorSystemId == null) return Array.Empty<ISocialEvent>();
            var currentFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (currentFaction != Guid.Empty && _piracy.IsPirateFaction(currentFaction))
                return Array.Empty<ISocialEvent>(); // already a pirate

            var sec = GetSystemSecurity(actorSystemId.Value);
            double chance =
                0.4
                + (actor.Skills.Military - 50) * 0.006
                + (50 - actor.Personality.Conscientiousness) * 0.004
                + PersonalityTraits.GetTraitEffect("ThrillSeeker", actor.Personality) / 80.0
                + sec.PirateActivity / 300.0
                - sec.PatrolStrength / 1200.0;

            var success = _rng.NextDouble() < Math.Clamp(chance, 0.05, 0.9);
            if (!success)
            {
                var failEv = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.FoundPirateClan,
                    actor.Id, null, null, null, false, 0, 0, Array.Empty<Guid>(),
                    "Failed to organize a pirate clan.");
                _events.Publish(failEv);
                return new[] { failEv };
            }

            var clan = new Faction(Guid.NewGuid(), $"{actor.Name.Split(' ').First()}'s Corsairs", actor.Id);
            _factions.Save(clan);
            _factions.MoveCharacterToFaction(actor.Id, clan.Id);

            _piracy.RegisterPirateFaction(clan.Id);

            AwardMerit(actor, _merit.Compute(MeritAction.PirateClanFounded, new MeritContext { Success = success, Ambition = actor.Ambition }));

            var ev = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.FoundPirateClan,
                actor.Id, null, clan.Id, null, true, 0, 0, Array.Empty<Guid>(),
                $"Founded pirate clan '{clan.Name}'.");
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Expel From House
        private IEnumerable<ISocialEvent> ResolveExpelFromHouse(Character actor, CharacterIntent intent, int y, int m)
        {
            if (!intent.TargetCharacterId.HasValue) return Array.Empty<ISocialEvent>();
            var target = _chars.GetById(intent.TargetCharacterId.Value);
            if (target == null || !target.IsAlive) return Array.Empty<ISocialEvent>();

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (myFaction == Guid.Empty) return Array.Empty<ISocialEvent>();
            if (_factions.GetFactionIdForCharacter(target.Id) != myFaction) return Array.Empty<ISocialEvent>();

            if ((int)actor.Rank < (int)Rank.Captain) return Array.Empty<ISocialEvent>();

            _factions.MoveCharacterToFaction(target.Id, Guid.Empty);
            _opinions.AdjustOpinion(target.Id, actor.Id, -10, "Expelled from House");
            _opinions.AdjustOpinion(actor.Id, target.Id, -3, "Expelled a problematic member");

            var ev = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.ExpelFromHouse,
                actor.Id, target.Id, myFaction, null, true, -3, -10, Array.Empty<Guid>(), "Member expelled from House.");
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Claim Planet Seat
        private IEnumerable<ISocialEvent> ResolveClaimPlanetSeat(Character actor, CharacterIntent intent, int y, int m)
        {
            if (!intent.TargetPlanetId.HasValue)
                return Array.Empty<ISocialEvent>();
            var planet = _planets.GetById(intent.TargetPlanetId.Value);
            if (planet == null)
                return Array.Empty<ISocialEvent>();

            var myFaction = _factions.GetFactionIdForCharacter(actor.Id);
            if (myFaction == Guid.Empty)
                return Array.Empty<ISocialEvent>();

            double chance = 0.5
                + (int)actor.Rank * 0.03
                + planet.InfrastructureLevel / 400.0;

            if (planet.FactionId == myFaction)
                chance += 0.25;

            var success = _rng.NextDouble() < Math.Clamp(chance, 0.1, 0.95);
            if (!success)
            {
                var failEv = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.ClaimPlanet,
                    actor.Id, null, myFaction, planet.Id, false, 0, 0, Array.Empty<Guid>(),
                    "Failed to secure a House Seat on this world.");
                _events.Publish(failEv);
                return new[] { failEv };
            }

            planet.SetSeatPlanet(myFaction);

            if (planet.FactionId == Guid.Empty)
            {
                planet.ChangeControl(myFaction);

                if (CanAppointAsGovernorOnThisPlanet(actor, planet, myFaction))
                    planet.AssignGovernor(actor.Id);

                AwardMerit(actor, _merit.Compute(MeritAction.PlanetClaimed,
                    new MeritContext { Success = true, Ambition = actor.Ambition }));

                var evFull = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.ClaimPlanet,
                    actor.Id, null, myFaction, planet.Id, true, 0, 0, Array.Empty<Guid>(),
                    "Planet was unowned; seat claimed and full control established.");
                _events.Publish(evFull);
                return new[] { evFull };
            }

            AwardMerit(actor, _merit.Compute(MeritAction.PlanetClaimed,
                new MeritContext { Success = true, Ambition = actor.Ambition }));

            var ev = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.ClaimPlanet,
                actor.Id, null, myFaction, planet.Id, true, 0, 0, Array.Empty<Guid>(),
                "Planet claimed as House Seat.");
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Build Infrastructure
        private IEnumerable<ISocialEvent> ResolveBuildInfrastructure(Character actor, CharacterIntent intent, int y, int m)
        {
            if (!intent.TargetPlanetId.HasValue)
                return Array.Empty<ISocialEvent>();
            var planet = _planets.GetById(intent.TargetPlanetId.Value);
            if (planet == null)
                return Array.Empty<ISocialEvent>();

            const int cost = 200;
            const int points = 10;
            if (planet.Credits < cost)
            {
                var failEv = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.Custom,
                    actor.Id, null, null, planet.Id, false, 0, 0, Array.Empty<Guid>(),
                    "Insufficient credits for infrastructure investment.");
                _events.Publish(failEv);
                return new[] { failEv };
            }

            planet.InvestInfrastructure(points, cost);

            var ev = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.Custom,
                actor.Id, null, null, planet.Id, true, 0, 0, Array.Empty<Guid>(),
                $"Invested {cost} credits into infrastructure.");
            _events.Publish(ev);
            return new[] { ev };
        }
        #endregion

        #region Build Fleet
        private IEnumerable<ISocialEvent> ResolveBuildFleet(Character actor, CharacterIntent intent, int y, int m, Guid actorFactionId, Guid? actorSystemId)
        {
            var ev = new SocialEvent(Guid.NewGuid(), y, m, SocialEventType.BuildFleet, actor.Id, null, null, null, false, 0, 0, Array.Empty<Guid>(), "");

            var planets = _planets.GetPlanetsControlledByFaction(actorFactionId).ToList();
            if (!planets.Any())
            {
                _events.Publish(ev);
                return new[] { ev };
            }

            var factionStatus = GetFactionStatus(actorFactionId);
            var security = actorSystemId.HasValue ? GetSystemSecurity(actorSystemId.Value) : null;
            var faction = _factions.GetFaction(actorFactionId);

            var planet = planets[0];
            int funds = planet.Credits;
            int econStrength = _factions.GetEconomicStrength(actorFactionId);
            int budget = Math.Min(funds, (int)(econStrength * 0.5));
            if (budget < ShipSpecs.Values.Min(s => s.Cost))
            {
                _events.Publish(ev);
                return new[] { ev };
            }
            var fleet = _fleets.GetFleetsForFaction(actorFactionId).FirstOrDefault();
            if (fleet == null)
            {
                var system = actorSystemId ?? planet.SystemId;
                fleet = new Fleet(Guid.NewGuid(), actorFactionId, system, _piracy);
            }

            var desired = fleet.DesiredComposition.ToDictionary(k => k.Key, v => v.Value);
            bool builtAny = false;
            int remainingBudget = budget;

            int ResourceLimit(Resources available, Resources cost)
            {
                int limit = int.MaxValue;
                if (cost.Organics > 0) limit = Math.Min(limit, available.Organics / cost.Organics);
                if (cost.Ore > 0) limit = Math.Min(limit, available.Ore / cost.Ore);
                if (cost.Volatiles > 0) limit = Math.Min(limit, available.Volatiles / cost.Volatiles);
                return limit;
            }

            int BuildShips(ShipClass cls, int count)
            {
                var spec = ShipSpecs[cls];
                count = Math.Min(count, remainingBudget / spec.Cost);
                count = Math.Min(count, planet.ProductionCapacity / spec.ProductionRequired);
                count = Math.Min(count, ResourceLimit(planet.Resources, spec.ResourceCost));
                if (count <= 0) return 0;

                if (!_economy.TryDebitBudget(planet.Id, spec.Cost * count))
                    return 0;
                remainingBudget -= spec.Cost * count;
                planet.Resources = planet.Resources - spec.ResourceCost.Scale(count);
                for (int i = 0; i < count; i++)
                {
                    var ship = new Ship(Guid.NewGuid(), cls, spec.Attack, spec.Defense, spec.Cargo, spec.Speed, spec.Cost);
                    fleet.AddShip(ship);
                }
                if (desired.ContainsKey(cls)) desired[cls] += count; else desired[cls] = count;
                builtAny = true;
                return count;
            }

            if (fleet.DesiredComposition.Any())
            {
                foreach (var kv in fleet.DesiredComposition)
                {
                    var current = fleet.Ships.Count(s => s.Class == kv.Key);
                    var missing = kv.Value - current;
                    if (missing > 0) BuildShips(kv.Key, missing);
                }
            }
            else
            {
                double cargoWeight = 1, fighterWeight = 1, scoutWeight = 1;
                if (security != null)
                {
                    if (security.Traffic > 60) cargoWeight += 1;
                    if (security.PirateActivity > 40) fighterWeight += 1;
                }
                if (factionStatus.IsAtWar) fighterWeight += 1;
                if (actor.Personality.Agreeableness < 40) fighterWeight += 0.5;
                if (actor.Personality.Openness > 60) scoutWeight += 0.5;
                if (actor.Personality.Conscientiousness > 60) cargoWeight += 0.5;
                switch (faction.Doctrine)
                {
                    case FactionDoctrine.Carrier:
                        fighterWeight += 0.5;
                        break;
                    case FactionDoctrine.TradeProtection:
                        cargoWeight += 0.5;
                        break;
                }

                double total = cargoWeight + fighterWeight + scoutWeight;
                BuildShips(ShipClass.Freighter, (int)(budget * (cargoWeight / total) / ShipSpecs[ShipClass.Freighter].Cost));
                BuildShips(ShipClass.Corvette, (int)(budget * (fighterWeight / total) / ShipSpecs[ShipClass.Corvette].Cost));
                BuildShips(ShipClass.Scout, (int)(budget * (scoutWeight / total) / ShipSpecs[ShipClass.Scout].Cost));
            }

            if (builtAny)
            {
                fleet.SetDesiredComposition(desired);
                _fleets.Save(fleet);
                _planets.Save(planet);
                ev = ev with { Success = true };
            }

            _events.Publish(ev);
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
            var traits = PersonalityTraits.GetActiveTraits(p);
            double allianceScore = 20 + actor.Skills.Military * 0.20;
            if (traits["Extraversion"].Any(t => t.Name == "Assertive")) allianceScore += 10;
            if (traits["Extraversion"].Any(t => t.Name == "ThrillSeeker")) allianceScore += 5;
            allianceScore -= (p.Agreeableness - 50) * 0.10;

            double tradeScore = 20 + actor.Skills.Economy * 0.30;
            if (traits["Agreeableness"].Any(t => t.Name == "Cooperative")) tradeScore += 10;
            if (traits["Extraversion"].Any(t => t.Name == "Cheerful")) tradeScore += 5;

            double researchScore = 15 + actor.Skills.Research * 0.35;
            if (traits["Openness"].Any(t => t.Name == "IntellectuallyCurious")) researchScore += 12;
            if (traits["Conscientiousness"].Any(t => t.Name == "SelfEfficient")) researchScore += 3;

            double napScore = 10 + p.Agreeableness * 0.20;
            if (traits["Agreeableness"].Any(t => t.Name == "Trusting")) napScore += 8;
            if (traits["Neuroticism"].Any(t => t.Name == "EasilyAngered")) napScore -= 8;

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

        private Guid? GetPlanetOfCharacter(Guid characterId)
            => GetCharacterLocation(characterId).PlanetId;

        private Guid? GetSystemOfCharacter(Guid characterId)
            => GetCharacterLocation(characterId).SystemId;

        private Guid? PickLocalPirateClan(Guid systemId)
        {
            // Currently no local pirate tracking; always prefer global faction
            // or create a new one when none exists.
            return null;
        }

        private (Guid? PlanetId, Guid? SystemId) GetCharacterLocation(Guid characterId)
        {
            if (_characterLocationCache.TryGetValue(characterId, out var loc))
                return loc;

            foreach (var p in _planets.GetAll())
            {
                if (p.Citizens.Contains(characterId) || p.Prisoners.Contains(characterId))
                {
                    loc = (p.Id, p.SystemId);
                    _characterLocationCache[characterId] = loc;
                    return loc;
                }
            }
            foreach (var f in _fleets.GetAll())
            {
                if (f.AssignedCharacterId == characterId || f.Prisoners.Contains(characterId))
                {
                    loc = (null, f.CurrentSystemId);
                    _characterLocationCache[characterId] = loc;
                    return loc;
                }
            }

            loc = (null, null);
            _characterLocationCache[characterId] = loc;
            return loc;
        }

        private FactionStatus GetFactionStatus(Guid factionId)
        {
            if (_factionStatusCache.TryGetValue(factionId, out var status))
                return status;

            var isAtWar = _factions.GetAllRivalFactions(factionId).Any(f => _factions.IsAtWar(factionId, f));
            var hasAlliance = _factions.GetAllRivalFactions(factionId).Any(f => _factions.HasAlliance(factionId, f));
            var hasUnrest = _planets.GetPlanetsControlledByFaction(factionId).Any(p => p.UnrestLevel > 50);
            var economyWeak = _factions.GetEconomicStrength(factionId) < 50;

            status = new FactionStatus(isAtWar, hasAlliance, hasUnrest, economyWeak);
            _factionStatusCache[factionId] = status;
            return status;
        }

        private SystemSecurity GetSystemSecurity(Guid systemId)
        {
            if (_systemSecurityCache.TryGetValue(systemId, out var security))
                return security;

            var securityLevel = _piracy.GetPirateActivity(systemId);
            var traffic = _piracy.GetTrafficLevel(systemId);
            var patrolStrength = _planets.GetAll()
                .Where(p => p.SystemId == systemId)
                .Sum(p => p.StationedTroops + p.BaseDefense);

            security = new SystemSecurity(systemId, (int)patrolStrength, securityLevel, traffic);
            _systemSecurityCache[systemId] = security;
            return security;
        }

        private Guid GetAvailableFleet(Guid factionId)
        {
            var fleets = _fleets
                .GetFleetsForFaction(factionId)
                .Where(f => !f.Orders.Any(o => o.Status == TaskStatus.Active))
                .ToList();
            if (!fleets.Any()) return Guid.Empty;
            return fleets[_rng.NextInt(0, fleets.Count)].Id;
        }

        private TravelPlan? GetRandomTravelPlanInSystem(Guid systemId)
        {
            var plans = _travel.GetPlansInSystem(systemId).ToList();
            if (!plans.Any()) return null;
            return plans[_rng.NextInt(0, plans.Count)];
        }

        private Fleet GetPirateFleet(Guid factionId)
        {
            var fleets = _fleets.GetFleetsForFaction(factionId)
                                .Where(f => _piracy.IsPirateFaction(f.FactionId))
                                .ToList();
            if (!fleets.Any())
                return new Fleet(Guid.NewGuid(), factionId, Guid.Empty, _piracy);
            return fleets[_rng.NextInt(0, fleets.Count)];
        }

        // ToDo: replace with your preferred combat sim when available
        private BattleResult ResolveFleetBattleQuick(Fleet attacker, Fleet defender)
        {
            var atk = Math.Max(1.0, attacker.CalculateStrength().MilitaryPower);
            var def = Math.Max(1.0, defender.CalculateStrength().MilitaryPower);

            var p = atk / (atk + def);
            p = Math.Clamp(p + (_rng.NextDouble() - 0.5) * 0.1, 0.05, 0.95);

            var attackerWins = _rng.NextDouble() < p;

            var winner = attackerWins ? attacker : defender;
            var loser = attackerWins ? defender : attacker;

            var loot = (int)Math.Round(def * (attackerWins ? 3.0 : 1.0));
            var merit = attackerWins ? 8 : -4;

            return new BattleResult(
                Guid.NewGuid(),
                winningFactionId: winner.FactionId,
                losingFactionId: loser.FactionId,
                winnerFleet: winner,
                loserFleet: loser,
                attackerWins: attackerWins,
                defenseRetreated: !attackerWins && def > atk * 1.2,
                lootCredits: loot,
                outcomeMerit: merit,
                planetCaptureBonus: 0,
                occupationDurationHours: 0
            );
        }

        private void AwardMerit(Character actor, int amount)
        {
            if (amount == 0)
                return;
            actor.GainMerit(amount);
            _chars.Save(actor);
        }
        public void ClearCaches()
        {
            _factionStatusCache.Clear();
            _systemSecurityCache.Clear();
            _characterLocationCache.Clear();
        }
        
        private int ComputeLocalSupportScore(Planet planet, Guid houseFactionId, Guid actorId)
        {
            if (planet.Citizens.Count == 0)
                return 0;

            int score = 0;
            foreach (var cid in planet.Citizens)
            {
                var cFac = _factions.GetFactionIdForCharacter(cid);
                if (cFac == houseFactionId)
                    score += 2;

                try
                {
                    var op = _opinions.GetOpinion(cid, actorId);
                    if (op >= 25)
                        score += 1;
                }
                catch { }
            }

            return Math.Clamp(score, 0, 100);
        }

        private int ComputeSystemLeverage(Guid systemId, Guid houseFactionId)
        {
            var fleetsHere = _fleets.GetAll()
                .Where(f => f.CurrentSystemId == systemId && f.FactionId == houseFactionId)
                .ToList();
            if (fleetsHere.Count == 0)
                return 0;

            var power = fleetsHere.Sum(f => Math.Max(1.0, f.CalculateStrength().MilitaryPower));
            var leverage = (int)Math.Round(Math.Min(40.0, Math.Log10(1.0 + power) * 18.0));
            return leverage;
        }

        // Authorization rule for governor appointment under our control semantics:
        private bool CanAppointAsGovernorOnThisPlanet(Character candidate, Planet planet, Guid candidateFactionId)
        {
            if (planet.FactionId != Guid.Empty)
                return planet.FactionId == candidateFactionId && candidate.CanPerform(SkyHorizont.Domain.Entity.Task.TaskType.Govern);

            if (planet.SeatFactionId.HasValue)
                return planet.SeatFactionId.Value == candidateFactionId && candidate.CanPerform(SkyHorizont.Domain.Entity.Task.TaskType.Govern);

            return false;
        }
        #endregion
    }

    public sealed class InteractionConfig
    {
        public bool DisableSensitiveIntents { get; init; } = true;
        public static InteractionConfig Default => new();
    }
}
