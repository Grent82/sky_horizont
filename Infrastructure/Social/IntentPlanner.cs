using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.Social
{
    public sealed class IntentPlanner : IIntentPlanner
    {
        private readonly ICharacterRepository _chars;
        private readonly IOpinionRepository _opinions;
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;
        private readonly IPlanetRepository _planets;
        private readonly IFleetRepository _fleets;
        private readonly IPiracyService _piracy;
        private readonly PlannerConfig _cfg;
        private readonly IEnumerable<IIntentRule> _rules;
        private readonly Dictionary<Guid, FactionStatus> _factionStatusCache;
        private readonly Dictionary<Guid, SystemSecurity> _systemSecurityCache;

        public IntentPlanner(
            ICharacterRepository characters,
            IOpinionRepository opinions,
            IFactionService factions,
            IRandomService rng,
            IPlanetRepository planets,
            IFleetRepository fleets,
            IPiracyService piracy,
            IEnumerable<IIntentRule> rules,
            PlannerConfig? config = null)
        {
            _chars = characters ?? throw new ArgumentNullException(nameof(characters));
            _opinions = opinions ?? throw new ArgumentNullException(nameof(opinions));
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _planets = planets ?? throw new ArgumentNullException(nameof(planets));
            _fleets = fleets ?? throw new ArgumentNullException(nameof(fleets));
            _piracy = piracy ?? throw new ArgumentNullException(nameof(piracy));
            _rules = rules ?? Enumerable.Empty<IIntentRule>();
            _cfg = config ?? PlannerConfig.Default;
            _factionStatusCache = new Dictionary<Guid, FactionStatus>();
            _systemSecurityCache = new Dictionary<Guid, SystemSecurity>();
        }

        public IEnumerable<CharacterIntent> PlanMonthlyIntents(Character actor)
        {
            var intents = new List<ScoredIntent>();
            if (!actor.IsAlive || actor.IsAssigned || actor.Age < 13)
                return Enumerable.Empty<CharacterIntent>();

            var actorFactionId = _factions.GetFactionIdForCharacter(actor.Id);
            var factionStatus = GetFactionStatus(actorFactionId);
            var actorSystemId = GetCharacterSystemId(actor.Id);
            var systemSecurity = actorSystemId.HasValue ? GetSystemSecurity(actorSystemId.Value) : null;
            var actorLeaderId = actorFactionId != Guid.Empty ? _factions.GetLeaderId(actorFactionId) : null;

            var opinionCache = new Dictionary<Guid, int>(32);
            int GetOpinionOfCharacter(Guid otherId)
                => opinionCache.TryGetValue(otherId, out var v)
                    ? v
                    : (opinionCache[otherId] = _opinions.GetOpinion(actor.Id, otherId));

            var factionCache = new Dictionary<Guid, Guid>(32);
            Guid GetFactionForCharacter(Guid charId)
                => factionCache.TryGetValue(charId, out var f)
                    ? f
                    : (factionCache[charId] = _factions.GetFactionIdForCharacter(charId));

            var (sameFaction, otherFaction, captives) = SelectRelevantCharacters(actor, actorFactionId, GetFactionForCharacter, GetOpinionOfCharacter);
            var ambition = actor.Ambition ?? AssignAmbition(actor);
            var ambitionBias = GetAmbitionBias(ambition);

            var ctx = new IntentContext(
                actor,
                actorFactionId,
                factionStatus,
                actorSystemId,
                systemSecurity,
                actorLeaderId,
                sameFaction,
                otherFaction,
                captives,
                ambition,
                ambitionBias,
                GetOpinionOfCharacter,
                GetFactionForCharacter,
                _cfg);

            foreach (var rule in _rules)
                intents.AddRange(rule.Generate(ctx));


            if (intents.Count == 0)
                return Enumerable.Empty<CharacterIntent>();

            intents = intents
                .Select(si => new ScoredIntent(
                    si.Type,
                    si.Score + _rng.NextDouble() * _cfg.ScoreNoiseMax,
                    si.TargetCharacterId,
                    si.TargetFactionId,
                    si.TargetPlanetId))
                .ToList();

            var prelim = intents
                .OrderByDescending(i => i.Score)
                .Take(Math.Min(_cfg.ConflictBuffer, Math.Max(_cfg.MaxIntentsPerMonth * 3, 6)))
                .ToList();

            var filtered = ResolveConflictsTargetAware(actor, prelim, _cfg.MaxIntentsPerMonth, GetFactionForCharacter);
            return filtered.Select(i => new CharacterIntent(actor.Id, i.Type, i.TargetCharacterId, i.TargetFactionId, i.TargetPlanetId))
                .ToList();
        }

        private CharacterAmbition AssignAmbition(Character actor)
        {
            var scores = new Dictionary<CharacterAmbition, double>
            {
                { CharacterAmbition.GainPower, (actor.Rank >= Rank.Captain ? 20 : 0) + actor.Skills.Military * 0.3 + (50 - actor.Personality.Agreeableness) * 0.2 },
                { CharacterAmbition.BuildWealth, actor.Skills.Economy * 0.4 + (actor.Personality.Conscientiousness - 50) * 0.2 },
                { CharacterAmbition.EnsureFamilyLegacy, actor.FamilyLinkIds.Count * 10 + (actor.Personality.Agreeableness - 50) * 0.3 },
                { CharacterAmbition.SeekAdventure, actor.Skills.Intelligence * 0.3 + actor.Personality.Openness * 0.3 + PersonalityTraits.GetTraitEffect("ThrillSeeker", actor.Personality) }
            };
            return scores.OrderByDescending(kv => kv.Value + _rng.NextDouble() * 5).First().Key;
        }

        private (List<Character> sameFaction, List<Character> otherFaction, List<Character> captives) SelectRelevantCharacters(
            Character actor, Guid actorFactionId, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var knownPeopleRaw = _chars.GetAll().Where(c => c.IsAlive && c.Id != actor.Id);
            var scored = knownPeopleRaw.Select(c =>
            {
                var relSet = actor.Relationships.Count == 0
                    ? null
                    : actor.Relationships.Select(r => r.TargetCharacterId).ToHashSet();
                var familySet = actor.FamilyLinkIds.Count == 0
                    ? null
                    : actor.FamilyLinkIds.ToHashSet();
                int op = opin(c.Id);
                bool related = relSet != null && relSet.Contains(c.Id);
                bool family = familySet != null && familySet.Contains(c.Id);
                bool sameFac = fac(c.Id) == actorFactionId;
                double w =
                    Math.Abs(op)
                    + (related ? 25 : 0)
                    + (family ? 20 : 0)
                    + (sameFac ? 15 : 0);
                return new { C = c, W = w };
            })
            .OrderByDescending(x => x.W)
            .Take(_cfg.MaxCandidatePool)
            .Select(x => x.C)
            .ToList();

            var sameFaction = scored.Where(c => fac(c.Id) == actorFactionId).ToList();
            var otherFaction = scored.Where(c => fac(c.Id) != actorFactionId)
                                    .Take(_cfg.MaxCrossFactionPool)
                                    .ToList();

            var myPlanets = _planets.GetPlanetsControlledByFaction(actorFactionId);
            var myFleets = _fleets.GetFleetsForFaction(actorFactionId);
            var captiveIds = myPlanets.SelectMany(p => p.Prisoners)
                                      .Concat(myFleets.SelectMany(f => f.Prisoners))
                                      .Distinct()
                                      .ToList();
            var captives = _chars.GetByIds(captiveIds).Where(c => c.IsAlive).ToList();

            return (sameFaction, otherFaction, captives);
        }

        private Guid? GetCharacterSystemId(Guid characterId)
        {
            foreach (var p in _planets.GetAll())
                if (p.Citizens.Contains(characterId) || p.Prisoners.Contains(characterId))
                    return p.SystemId;
            foreach (var f in _fleets.GetAll())
                if (f.AssignedCharacterId == characterId || f.Prisoners.Contains(characterId))
                    return f.CurrentSystemId;
            return null;
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
                .Sum(p => p.BaseDefense);

            security = new SystemSecurity(systemId, (int)patrolStrength, securityLevel, traffic);
            _systemSecurityCache[systemId] = security;
            return security;
        }

        private IReadOnlyDictionary<IntentType, double> GetAmbitionBias(CharacterAmbition ambition)
        {
            var bias = Enum.GetValues<IntentType>().ToDictionary(t => t, _ => 1.0);

            switch (ambition)
            {
                case CharacterAmbition.GainPower:
                    bias[IntentType.Court] = 0.8;
                    bias[IntentType.VisitFamily] = 0.7;
                    bias[IntentType.Spy] = 1.2;
                    bias[IntentType.Bribe] = 1.1;
                    bias[IntentType.Recruit] = 1.2;
                    bias[IntentType.Defect] = 1.3;
                    bias[IntentType.Negotiate] = 1.0;
                    bias[IntentType.Quarrel] = 1.0;
                    bias[IntentType.Assassinate] = 1.3;
                    bias[IntentType.TorturePrisoner] = 1.0;
                    bias[IntentType.RapePrisoner] = 0.9;
                    bias[IntentType.TravelToPlanet] = 0.8;
                    bias[IntentType.BecomePirate] = 0.9;
                    bias[IntentType.RaidConvoy] = 0.8;
                    break;
                case CharacterAmbition.BuildWealth:
                    bias[IntentType.Court] = 0.9;
                    bias[IntentType.VisitFamily] = 0.8;
                    bias[IntentType.Spy] = 1.1;
                    bias[IntentType.Bribe] = 1.3;
                    bias[IntentType.Recruit] = 1.1;
                    bias[IntentType.Defect] = 0.8;
                    bias[IntentType.Negotiate] = 1.2;
                    bias[IntentType.Quarrel] = 0.7;
                    bias[IntentType.Assassinate] = 0.8;
                    bias[IntentType.TorturePrisoner] = 0.7;
                    bias[IntentType.RapePrisoner] = 0.6;
                    bias[IntentType.TravelToPlanet] = 1.0;
                    bias[IntentType.BecomePirate] = 1.2;
                    bias[IntentType.RaidConvoy] = 1.3;
                    break;
                case CharacterAmbition.EnsureFamilyLegacy:
                    bias[IntentType.Court] = 1.2;
                    bias[IntentType.VisitFamily] = 1.3;
                    bias[IntentType.Spy] = 0.8;
                    bias[IntentType.Bribe] = 0.9;
                    bias[IntentType.Recruit] = 0.9;
                    bias[IntentType.Defect] = 0.7;
                    bias[IntentType.Negotiate] = 0.9;
                    bias[IntentType.Quarrel] = 0.8;
                    bias[IntentType.Assassinate] = 0.7;
                    bias[IntentType.TorturePrisoner] = 0.6;
                    bias[IntentType.RapePrisoner] = 0.5;
                    bias[IntentType.TravelToPlanet] = 1.1;
                    bias[IntentType.BecomePirate] = 0.7;
                    bias[IntentType.RaidConvoy] = 0.6;
                    break;
                case CharacterAmbition.SeekAdventure:
                    bias[IntentType.Court] = 0.9;
                    bias[IntentType.VisitFamily] = 0.8;
                    bias[IntentType.Spy] = 1.2;
                    bias[IntentType.Bribe] = 0.9;
                    bias[IntentType.Recruit] = 0.9;
                    bias[IntentType.Defect] = 1.0;
                    bias[IntentType.Negotiate] = 0.9;
                    bias[IntentType.Quarrel] = 1.0;
                    bias[IntentType.Assassinate] = 1.0;
                    bias[IntentType.TorturePrisoner] = 0.8;
                    bias[IntentType.RapePrisoner] = 0.7;
                    bias[IntentType.TravelToPlanet] = 1.3;
                    bias[IntentType.BecomePirate] = 1.2;
                    bias[IntentType.RaidConvoy] = 1.2;
                    break;
            }

            return bias;
        }

        private List<ScoredIntent> ResolveConflictsTargetAware(Character actor, List<ScoredIntent> candidates, int take, Func<Guid, Guid> fac)
        {
            var kept = new List<ScoredIntent>(take);
            var chosenCharTargets = new HashSet<Guid>();
            var chosenFactionTargets = new HashSet<Guid>();
            var chosenPlanetTargets = new HashSet<Guid>();

            foreach (var si in candidates)
            {
                if (kept.Count >= take) break;
                bool conflict = false;

                var tc = si.TargetCharacterId;
                var tf = si.TargetFactionId;
                var tp = si.TargetPlanetId;

                if (si.Type == IntentType.Bribe || si.Type == IntentType.Recruit ||
                    si.Type == IntentType.Court || si.Type == IntentType.Assassinate ||
                    si.Type == IntentType.Quarrel || si.Type == IntentType.TorturePrisoner ||
                    si.Type == IntentType.RapePrisoner || si.Type == IntentType.VisitLover)
                {
                    if (tc.HasValue && chosenCharTargets.Contains(tc.Value))
                        conflict = true;
                }

                if (si.Type == IntentType.Negotiate || si.Type == IntentType.Defect)
                {
                    if (tf.HasValue && chosenFactionTargets.Contains(tf.Value))
                        conflict = true;
                }

                if (si.Type == IntentType.TravelToPlanet)
                {
                    if (tp.HasValue && chosenPlanetTargets.Contains(tp.Value))
                        conflict = true;
                }

                if (si.Type == IntentType.RaidConvoy)
                {
                    if (tf.HasValue && chosenFactionTargets.Contains(tf.Value))
                        conflict = true;
                }

                if (!conflict)
                {
                    kept.Add(si);
                    if (tc.HasValue) chosenCharTargets.Add(tc.Value);
                    if (tf.HasValue) chosenFactionTargets.Add(tf.Value);
                    if (tp.HasValue) chosenPlanetTargets.Add(tp.Value);
                }
            }

            return kept;
        }

        public void ClearCaches()
        {
            _factionStatusCache.Clear();
            _systemSecurityCache.Clear();
        }
    }
}
