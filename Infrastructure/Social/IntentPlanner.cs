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
        private readonly ITravelService _travel;
        private readonly IPiracyService _piracy;
        private readonly PlannerConfig _cfg;
        private readonly Dictionary<Guid, FactionStatus> _factionStatusCache;
        private readonly Dictionary<Guid, SystemSecurity> _systemSecurityCache;

        public IntentPlanner(
            ICharacterRepository characters,
            IOpinionRepository opinions,
            IFactionService factions,
            IRandomService rng,
            IPlanetRepository planets,
            IFleetRepository fleets,
            ITravelService travel,
            IPiracyService piracy,
            PlannerConfig? config = null)
        {
            _chars = characters ?? throw new ArgumentNullException(nameof(characters));
            _opinions = opinions ?? throw new ArgumentNullException(nameof(opinions));
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _planets = planets ?? throw new ArgumentNullException(nameof(planets));
            _fleets = fleets ?? throw new ArgumentNullException(nameof(fleets));
            _travel = travel ?? throw new ArgumentNullException(nameof(travel));
            _piracy = piracy ?? throw new ArgumentNullException(nameof(piracy));
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

            // Courtship
            var romanticTarget = PickRomanticTarget(actor, sameFaction, GetFactionForCharacter, GetOpinionOfCharacter);
            if (romanticTarget != null)
                AddIfAboveZero(intents, ScoreCourtship(actor, romanticTarget, factionStatus, GetOpinionOfCharacter) * ambitionBias.Court, IntentType.Court, romanticTarget.Id);

            // Family Visit
            var familyTarget = PickFamilyTarget(actor, GetOpinionOfCharacter);
            if (familyTarget.HasValue)
                AddIfAboveZero(intents, ScoreVisitFamily(actor, familyTarget.Value, factionStatus, GetOpinionOfCharacter) * ambitionBias.Family, IntentType.VisitFamily, familyTarget.Value);

            // Spy
            var spyTargetFaction = PickSpyFaction(actorFactionId);
            if (spyTargetFaction != Guid.Empty)
                AddIfAboveZero(intents, ScoreSpy(actor, factionStatus) * ambitionBias.Spy, IntentType.Spy, null, spyTargetFaction);

            var spyTargetChar = PickSpyCharacter(actorFactionId, otherFaction, GetFactionForCharacter, GetOpinionOfCharacter);
            if (spyTargetChar != null)
                AddIfAboveZero(intents, ScoreSpy(actor, factionStatus) * ambitionBias.Spy, IntentType.Spy, spyTargetChar.Id, null);

            // Bribe
            var bribeTarget = PickBribeTarget(otherFaction, GetOpinionOfCharacter);
            if (bribeTarget != null)
                AddIfAboveZero(intents, ScoreBribe(actor, bribeTarget, actorFactionId, factionStatus, GetFactionForCharacter) * ambitionBias.Bribe, IntentType.Bribe, bribeTarget.Id);

            // Recruit
            if (IsRecruiter(actor))
            {
                var recruitTarget = PickRecruitTarget(actorFactionId, otherFaction, GetFactionForCharacter);
                if (recruitTarget != null)
                    AddIfAboveZero(intents, ScoreRecruit(actor, recruitTarget, actorFactionId, factionStatus, GetFactionForCharacter) * ambitionBias.Recruit, IntentType.Recruit, recruitTarget.Id);
            }

            // Defection
            if (actorLeaderId.HasValue)
            {
                var defectTargetFaction = PickDefectionFaction(actorFactionId);
                if (defectTargetFaction != Guid.Empty)
                    AddIfAboveZero(intents, ScoreDefect(actor, actorLeaderId.Value, actorFactionId, defectTargetFaction, factionStatus, GetOpinionOfCharacter) * ambitionBias.Defect, IntentType.Defect, null, defectTargetFaction);
            }

            // Negotiate
            var negotiateTargetFaction = PickNegotiateFaction(actor, actorFactionId);
            if (negotiateTargetFaction != Guid.Empty)
                AddIfAboveZero(intents, ScoreNegotiate(actor, actorFactionId, negotiateTargetFaction, factionStatus) * ambitionBias.Negotiate, IntentType.Negotiate, null, negotiateTargetFaction);

            // Quarrel
            var quarrelTarget = PickQuarrelTarget(actor, otherFaction, GetOpinionOfCharacter);
            if (quarrelTarget != null)
                AddIfAboveZero(intents, ScoreQuarrel(actor, quarrelTarget, factionStatus, GetOpinionOfCharacter) * ambitionBias.Quarrel, IntentType.Quarrel, quarrelTarget.Id);

            // Assassinate
            var assassinateTarget = PickAssassinationTarget(otherFaction, GetOpinionOfCharacter);
            if (assassinateTarget != null)
                AddIfAboveZero(intents, ScoreAssassinate(actor, assassinateTarget, actorFactionId, factionStatus, GetFactionForCharacter, GetOpinionOfCharacter) * ambitionBias.Assassinate, IntentType.Assassinate, assassinateTarget.Id);

            // Torture
            var tortureTarget = PickTortureTarget(captives, GetOpinionOfCharacter);
            if (tortureTarget != null)
                AddIfAboveZero(intents, ScoreTorture(actor, tortureTarget, actorFactionId, factionStatus, GetFactionForCharacter, GetOpinionOfCharacter) * ambitionBias.Torture, IntentType.TorturePrisoner, tortureTarget.Id);

            // Rape
            var rapeTarget = PickRapeTarget(captives, GetOpinionOfCharacter);
            if (rapeTarget != null)
                AddIfAboveZero(intents, ScoreRape(actor, rapeTarget, actorFactionId, factionStatus, GetFactionForCharacter, GetOpinionOfCharacter) * ambitionBias.Rape, IntentType.RapePrisoner, rapeTarget.Id);

            // Travel
            var travelDest = PickTravelDestinationPlanet(actor, actorFactionId, systemSecurity, GetFactionForCharacter, GetOpinionOfCharacter);
            if (travelDest.HasValue)
                AddIfAboveZero(intents, ScoreTravel(actor, travelDest.Value, factionStatus, systemSecurity) * ambitionBias.Travel, IntentType.TravelToPlanet, targetPlanetId: travelDest.Value);

            // Become Pirate
            if (!_piracy.IsPirateFaction(actorFactionId))
            {
                var pirateScore = ScoreBecomePirate(actor, actorFactionId, actorLeaderId, systemSecurity, GetOpinionOfCharacter);
                AddIfAboveZero(intents, pirateScore * ambitionBias.BecomePirate, IntentType.BecomePirate);
            }

            // Raid Convoy
            if (_piracy.IsPirateFaction(actorFactionId))
            {
                var raidTargetSystem = PickRaidTargetSystem(actorSystemId, actorFactionId);
                if (raidTargetSystem.HasValue)
                    AddIfAboveZero(intents, ScoreRaidConvoy(actor, raidTargetSystem.Value, systemSecurity) * ambitionBias.RaidConvoy, IntentType.RaidConvoy, targetFactionId: raidTargetSystem.Value);
            }

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

        private double ScoreCourtship(Character actor, Character target, FactionStatus factionStatus, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;

            baseScore += Clamp0to100Map(actor.Personality.CheckCompatibility(target.Personality));
            baseScore += Clamp0to100Map(Math.Max(0, opinion + 50));

            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Agreeableness"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);

            if (actor.Relationships.Any(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse))
                baseScore += 15;

            if (actor.Personality.Extraversion < 40) baseScore -= 10;
            if (factionStatus.HasAlliance) baseScore += 10;

            return Clamp0to100(baseScore * _cfg.RomanceWeight);
        }

        private double ScoreVisitFamily(Character actor, Guid familyId, FactionStatus factionStatus, Func<Guid, int> opin)
        {
            var opinion = opin(familyId);
            var baseScore = 30.0
                + Clamp0to100Map(opinion + 50)
                + (actor.Personality.Agreeableness - 50) * 0.3
                + (actor.Personality.Conscientiousness - 50) * 0.2;

            if (factionStatus.HasUnrest)
                baseScore += 10;
            return Clamp0to100(baseScore * _cfg.FamilyWeight);
        }

        private double ScoreSpy(Character actor, FactionStatus factionStatus)
        {
            var baseScore = actor.Skills.Intelligence * 0.6;

            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Openness"].Concat(traits["Neuroticism"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);

            baseScore += (int)actor.Rank * 2;
            if (actor.Rank == Rank.Civilian && actor.Skills.Intelligence < 65) baseScore -= 15;
            if (factionStatus.IsAtWar) baseScore += 15;

            return Clamp0to100(baseScore * _cfg.SpyWeight);
        }

        private double ScoreBribe(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac)
        {
            var baseScore = 20.0;
            var hardness = (target.Personality.Conscientiousness + target.Personality.Agreeableness) / 2.0;
            baseScore += (100 - hardness) * 0.4;
            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 10;
            if (factionStatus.EconomyWeak) baseScore += 10;
            if (actor.Balance < _cfg.MinBribeBudget) baseScore -= 25;

            return Clamp0to100(baseScore * _cfg.BribeWeight);
        }

        private double ScoreRecruit(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac)
        {
            var baseScore = 30.0;
            var talent = (target.Skills.Military + target.Skills.Intelligence + target.Skills.Economy + target.Skills.Research) / 4.0;
            baseScore += talent * 0.4;
            baseScore += (int)actor.Rank * 3;
            baseScore += (actor.Personality.Agreeableness - 50) * 0.2;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;
            var sameFaction = actorFactionId == fac(target.Id);
            if (sameFaction) baseScore -= 10;
            if (factionStatus.HasUnrest) baseScore += 10;

            return Clamp0to100(baseScore * _cfg.RecruitWeight);
        }

        private double ScoreDefect(Character actor, Guid leaderId, Guid actorFactionId, Guid targetFactionId, FactionStatus factionStatus, Func<Guid, int> opin)
        {
            var opinionLeader = opin(leaderId);
            var baseScore = Clamp0to100Map(-opinionLeader) * 0.8;
            baseScore += (50 - actor.Personality.Conscientiousness) * 0.2;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.2;
            baseScore += (actor.Personality.Openness - 50) * 0.15;
            baseScore -= (actor.Personality.Neuroticism - 50) * 0.15;
            if (_factions.IsAtWar(actorFactionId, targetFactionId)) baseScore += 10;
            if (factionStatus.HasUnrest) baseScore += 15;
            baseScore -= (int)actor.Rank * 2;

            return Clamp0to100(baseScore * _cfg.DefectionWeight);
        }

        private double ScoreNegotiate(Character actor, Guid myFactionId, Guid targetFactionId, FactionStatus factionStatus)
        {
            var baseScore = 25.0;
            baseScore += (actor.Personality.Agreeableness - 50) * 0.3;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Agreeableness"].Concat(traits["Extraversion"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            if (_factions.IsAtWar(myFactionId, targetFactionId)) baseScore += 10;
            if (factionStatus.EconomyWeak) baseScore += 10;
            baseScore += (int)actor.Rank * 3;

            return Clamp0to100(baseScore * _cfg.NegotiateWeight);
        }

        private double ScoreQuarrel(Character actor, Character target, FactionStatus factionStatus, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;
            if (opinion < _cfg.QuarrelOpinionThreshold)
                baseScore += Clamp0to100Map(_cfg.QuarrelOpinionThreshold - opinion);
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"])
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            if ((int)target.Rank > (int)actor.Rank) baseScore -= 10;
            if (factionStatus.HasUnrest) baseScore += 10;

            return Clamp0to100(baseScore * _cfg.QuarrelWeight);
        }

        private double ScoreAssassinate(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;
            baseScore += actor.Skills.Military * 0.4;
            baseScore += Math.Max(0, -opinion) * 0.2;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"].Concat(traits["Extraversion"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality);
            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 20;
            if (factionStatus.HasUnrest) baseScore += 10;
            if (actor.Rank <= Rank.Captain && actor.Skills.Military < 70) baseScore -= 20;
            baseScore -= 15;

            return Clamp0to100(baseScore * _cfg.AssassinateWeight);
        }

        private double ScoreTorture(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.4;
            baseScore += (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore += (int)target.Rank * 5;
            baseScore += target.Skills.Intelligence * 0.3;
            if (opinion < 0) baseScore += Clamp0to100Map(-opinion) * 0.5;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"])
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality);
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 15;
            if (factionStatus.IsAtWar) baseScore += 10;
            baseScore += (int)actor.Rank * 3;
            if (actor.Rank < Rank.Captain) baseScore -= 20;

            return Clamp0to100(baseScore * _cfg.TortureWeight);
        }

        private double ScoreRape(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.5;
            baseScore += (50 - actor.Personality.Conscientiousness) * 0.3;
            if (opinion < 0) baseScore += Clamp0to100Map(-opinion) * 0.6;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"].Concat(traits["Extraversion"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality);
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 15;
            if (factionStatus.HasUnrest) baseScore += 5;
            baseScore += (int)actor.Rank * 2;
            baseScore -= (actor.Personality.Neuroticism - 50) * 0.2;
            if (actor.Rank < Rank.Captain) baseScore -= 25;

            return Clamp0to100(baseScore * _cfg.RapeWeight);
        }

        private double ScoreTravel(Character actor, Guid destinationPlanetId, FactionStatus factionStatus, SystemSecurity? systemSecurity)
        {
            var baseScore = 20.0;
            baseScore += (actor.Personality.Extraversion - 50) * 0.3;
            baseScore += (actor.Personality.Openness - 50) * 0.3;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Openness"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            var destPlanet = _planets.GetById(destinationPlanetId);
            if (destPlanet != null && destPlanet.UnrestLevel > 50)
                baseScore -= 10; 
            if (factionStatus.HasAlliance && _factions.GetFactionIdForPlanet(destinationPlanetId) == _factions.GetFactionIdForCharacter(actor.Id))
                baseScore += 15;
            if (systemSecurity != null && systemSecurity.PirateActivity > 50)
                baseScore -= systemSecurity.PirateActivity * 0.1;

            return Clamp0to100(baseScore * _cfg.TravelWeight);
        }

        private double ScoreBecomePirate(Character actor, Guid actorFactionId, Guid? actorLeaderId, SystemSecurity? systemSecurity, Func<Guid, int> opin)
        {
            // Hard filters first
            var planets = _planets.GetPlanetsControlledByFaction(actorFactionId);
            var IsGovernor = false;
            foreach (var planet in planets)
            {
                if (planet.GovernorId == actor.Id)
                {
                    IsGovernor = false;
                    break;
                }
            }
            if (IsGovernor || actor.Rank >= Rank.General || actor.Balance > 2000)
                    return 0;
            
            double baseScore = 0.0;

            if (actorLeaderId.HasValue)
                baseScore += Clamp0to100Map(Math.Max(0, -opin(actorLeaderId.Value)));

            baseScore += (50 - actor.Personality.Conscientiousness) * 0.3;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.3;
            baseScore += PersonalityTraits.GetTraitEffect("ThrillSeeker", actor.Personality);

            if (actor.Balance < 500)
                baseScore += 20;
            else if (actor.Balance < 1000)
                baseScore += 10;

            if (systemSecurity != null)
            {
                baseScore += systemSecurity.PirateActivity * 0.2;
                baseScore -= systemSecurity.PatrolStrength * 0.1;
            }

            if (GetFactionStatus(actorFactionId).HasUnrest)
                baseScore += 15;

            return Clamp0to100(baseScore * _cfg.BecomePirateWeight);
        }


        private double ScoreRaidConvoy(Character actor, Guid systemId, SystemSecurity? systemSecurity)
        {
            var baseScore = 0.0;
            baseScore += (actor.Skills.Military - 50) * 0.4;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.2;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Neuroticism"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality);
            if (systemSecurity != null)
            {
                baseScore += systemSecurity.PirateActivity * 0.4;
                baseScore += systemSecurity.Traffic * 0.3;
                baseScore -= systemSecurity.PatrolStrength * 0.2;
            }
            return Clamp0to100(baseScore * _cfg.RaidConvoyWeight);
        }

        private Guid? PickTravelDestinationPlanet(Character actor, Guid actorFactionId, SystemSecurity? systemSecurity, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var currentPlanetId = GetCharacterPlanetId(actor.Id);
            if (!currentPlanetId.HasValue) return null;

            var loved = actor.Relationships
                .Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                .Select(r => r.TargetCharacterId)
                .Concat(actor.FamilyLinkIds)
                .Distinct()
                .ToList();

            var alliedPlanets = _planets.GetAll()
                .Where(p => fac(p.FactionId) == actorFactionId || _factions.HasAlliance(actorFactionId, p.FactionId))
                .ToList();

            var pool = _planets.GetAll()
                .Where(p => p.Id != currentPlanetId.Value)
                .Select(p => new
                {
                    Planet = p,
                    Score = (loved.Any(id => p.Citizens.Contains(id) || p.Prisoners.Contains(id)) ? 50 : 0) +
                            (fac(p.FactionId) == actorFactionId ? 20 : 0) +
                            (_factions.HasAlliance(actorFactionId, p.FactionId) ? 15 : 0) +
                            (p.IsTradeHub ? 10 : 0) -
                            (p.UnrestLevel > 50 ? p.UnrestLevel * 0.1 : 0) +
                            _rng.NextInt(0, 20)
                })
                .OrderByDescending(x => x.Score)
                .Take(5) // Limit to top 5 candidates
                .ToList();

            return pool.Count > 0 ? pool[_rng.NextInt(0, pool.Count)].Planet.Id : null;
        }

        private Guid? PickRaidTargetSystem(Guid? actorSystemId, Guid actorFactionId)
        {
            if (!actorSystemId.HasValue) return null;
            var systems = _planets.GetAll()
                .GroupBy(p => p.SystemId)
                .Select(g => new
                {
                    SystemId = g.Key,
                    Traffic = _piracy.GetTrafficLevel(g.Key),
                    PirateActivity = _piracy.GetPirateActivity(g.Key),
                    PatrolStrength = GetSystemSecurity(g.Key).PatrolStrength
                })
                .Where(s => s.SystemId != actorSystemId.Value && !_piracy.IsPirateFaction(_factions.GetFactionIdForSystem(s.SystemId)))
                .OrderByDescending(s => s.Traffic * 0.5 + s.PirateActivity * 0.3 - s.PatrolStrength * 0.2)
                .Take(3) // Limit to top 3 systems
                .ToList();

            return systems.Count > 0 ? systems[_rng.NextInt(0, systems.Count)].SystemId : null;
        }

        private Guid? GetCharacterPlanetId(Guid characterId)
        {
            foreach (var p in _planets.GetAll())
                if (p.Citizens.Contains(characterId) || p.Prisoners.Contains(characterId))
                    return p.Id;
            return null;
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

            security = new SystemSecurity(systemId, 0, securityLevel, traffic); // ToDo: patrolls?
            _systemSecurityCache[systemId] = security;
            return security;
        }

        private (double Court, double Family, double Spy, double Bribe, double Recruit, double Defect, double Negotiate, double Quarrel, double Assassinate, double Torture, double Rape, double Travel, double BecomePirate, double RaidConvoy) GetAmbitionBias(CharacterAmbition ambition)
        {
            return ambition switch
            {
                CharacterAmbition.GainPower => (0.8, 0.7, 1.2, 1.1, 1.2, 1.3, 1.0, 1.0, 1.3, 1.0, 0.9, 0.8, 0.9, 0.8),
                CharacterAmbition.BuildWealth => (0.9, 0.8, 1.1, 1.3, 1.1, 0.8, 1.2, 0.7, 0.8, 0.7, 0.6, 1.0, 1.2, 1.3),
                CharacterAmbition.EnsureFamilyLegacy => (1.2, 1.3, 0.8, 0.9, 0.9, 0.7, 0.9, 0.8, 0.7, 0.6, 0.5, 1.1, 0.7, 0.6),
                CharacterAmbition.SeekAdventure => (0.9, 0.8, 1.2, 0.9, 0.9, 1.0, 0.9, 1.0, 1.0, 0.8, 0.7, 1.3, 1.2, 1.2),
                _ => (1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0)
            };
        }

        private Character? PickRomanticTarget(Character actor, List<Character> candidates, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var lovers = actor.Relationships.Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                                            .Select(r => r.TargetCharacterId)
                                            .ToHashSet();
            var known = candidates.Where(c => lovers.Contains(c.Id)).ToList();
            if (known.Count > 0) return known[_rng.NextInt(0, known.Count)];
            var myFaction = fac(actor.Id);
            var pool = candidates.Where(c => fac(c.Id) == myFaction).ToList();
            if (pool.Count == 0) return null;
            return pool.Select(c => new
            {
                C = c,
                Score = 0.6 * opin(c.Id) + 0.4 * actor.Personality.CheckCompatibility(c.Personality)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault()?.C;
        }

        private Guid? PickFamilyTarget(Character actor, Func<Guid, int> opin)
        {
            if (actor.FamilyLinkIds.Count == 0) return null;
            var weighted = actor.FamilyLinkIds
                .Select(fid => new { Id = fid, W = opin(fid) + 60 + _rng.NextInt(0, 20) })
                .OrderByDescending(x => x.W)
                .FirstOrDefault();
            return weighted?.Id;
        }

        private Guid PickSpyFaction(Guid actorFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(actorFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;
            var war = rivals.Where(f => _factions.IsAtWar(actorFactionId, f)).ToList();
            if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Character? PickBribeTarget(List<Character> otherFaction, Func<Guid, int> opin)
        {
            var pool = otherFaction
                .Where(c => opin(c.Id) > -50)
                .ToList();
            if (pool.Count == 0) return null;
            return pool.Select(c => new
            {
                C = c,
                Score = (100 - c.Personality.Conscientiousness) + (100 - c.Personality.Agreeableness) + _rng.NextInt(0, 20)
            })
            .OrderByDescending(x => x.Score)
            .First().C;
        }

        private bool IsRecruiter(Character actor) => actor.Rank >= Rank.Captain || actor.Skills.Military >= 70;

        private Character? PickRecruitTarget(Guid actorFactionId, List<Character> candidates, Func<Guid, Guid> fac)
        {
            var pool = candidates.Where(c => fac(c.Id) != actorFactionId).ToList();
            if (pool.Count == 0) return null;
            return pool.Select(c => new
            {
                C = c,
                Talent = (c.Skills.Military + c.Skills.Intelligence + c.Skills.Economy + c.Skills.Research) / 4.0
            })
            .OrderByDescending(x => x.Talent + _rng.NextInt(0, 10))
            .First().C;
        }

        private Guid PickDefectionFaction(Guid actorFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(actorFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;
            var war = rivals.Where(f => _factions.IsAtWar(actorFactionId, f)).ToList();
            if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Guid PickNegotiateFaction(Character actor, Guid actorFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(actorFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;
            if (PersonalityTraits.Cheerful(actor.Personality) || PersonalityTraits.Trusting(actor.Personality))
            {
                var war = rivals.Where(f => _factions.IsAtWar(actorFactionId, f)).ToList();
                if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            }
            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Character? PickQuarrelTarget(Character actor, List<Character> candidates, Func<Guid, int> opin)
        {
            var negatives = candidates
                .Select(c => new { C = c, O = opin(c.Id) })
                .Where(x => x.O < _cfg.QuarrelOpinionThreshold)
                .OrderBy(x => x.O)
                .ToList();
            if (negatives.Count == 0) return null;
            return negatives[_rng.NextInt(0, Math.Min(3, negatives.Count))].C;
        }

        private Character? PickAssassinationTarget(List<Character> otherFaction, Func<Guid, int> opin)
        {
            if (_rng.NextDouble() > _cfg.AssassinateFrequency) return null;
            var pool = otherFaction
                .Select(c => new { C = c, O = opin(c.Id) })
                .Where(x => x.O < _cfg.AssassinationOpinionThreshold && x.C.Rank >= Rank.Major)
                .OrderBy(x => x.O)
                .ToList();
            if (pool.Count == 0) return null;
            return pool.First().C;
        }

        private Character? PickSpyCharacter(Guid actorFactionId, List<Character> candidates, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var pool = candidates
                .Where(c => fac(c.Id) != actorFactionId)
                .Select(c => new
                {
                    C = c,
                    Score = (int)c.Rank * 10 - opin(c.Id) + _rng.NextInt(0, 10)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            return pool.Count == 0 ? null : pool.First().C;
        }

        private Character? PickTortureTarget(List<Character> captives, Func<Guid, int> opin)
        {
            if (captives.Count == 0) return null;
            var pool = captives
                .Select(c => new
                {
                    C = c,
                    Score = (int)c.Rank * 10 + c.Skills.Intelligence + _rng.NextInt(0, 20)
                })
                .OrderByDescending(x => x.Score)
                .ToList();
            return pool.First().C;
        }

        private Character? PickRapeTarget(List<Character> captives, Func<Guid, int> opin)
        {
            if (captives.Count == 0) return null;
            var pool = captives
                .Select(c => new
                {
                    C = c,
                    Score = -opin(c.Id) + _rng.NextInt(0, 30)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();
            return pool.Count == 0 ? null : pool.First().C;
        }

        private (List<Character> sameFaction, List<Character> otherFaction, List<Character> captives) SelectRelevantCharacters(
            Character actor, Guid actorFactionId, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var knownPeopleRaw = _chars.GetAll().Where(c => c.IsAlive && c.Id != actor.Id);
            var scored = knownPeopleRaw.Select(c =>
            {
                var relSet = actor.Relationships.Count == 0
                    ? (HashSet<Guid>?)null
                    : actor.Relationships.Select(r => r.TargetCharacterId).ToHashSet();
                var familySet = actor.FamilyLinkIds.Count == 0
                    ? (HashSet<Guid>?)null
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

        private static void AddIfAboveZero(List<ScoredIntent> list, double score, IntentType type,
                                           Guid? targetCharacterId = null, Guid? targetFactionId = null, Guid? targetPlanetId = null)
        {
            if (score <= 0) return;
            list.Add(new ScoredIntent(type, score, targetCharacterId, targetFactionId, targetPlanetId));
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        private sealed record ScoredIntent(IntentType Type, double Score, Guid? TargetCharacterId, Guid? TargetFactionId, Guid? TargetPlanetId);

        private static IEnumerable<CharacterIntent> FilterConflicts(IEnumerable<CharacterIntent> intents, Func<CharacterIntent, double> scoreOf)
        {
            var conflictSets = new[]
            {
                new HashSet<IntentType> { IntentType.Court, IntentType.Assassinate },
                new HashSet<IntentType> { IntentType.Negotiate, IntentType.Quarrel },
                new HashSet<IntentType> { IntentType.Bribe, IntentType.Recruit },
                new HashSet<IntentType> { IntentType.TorturePrisoner, IntentType.Negotiate },
                new HashSet<IntentType> { IntentType.RapePrisoner, IntentType.Negotiate },
                new HashSet<IntentType> { IntentType.TravelToPlanet, IntentType.RaidConvoy }
            };

            var chosen = intents.ToList();
            foreach (var set in conflictSets)
            {
                var inSet = chosen.Where(i => set.Contains(i.Type)).ToList();
                if (inSet.Count <= 1) continue;
                var best = inSet.OrderByDescending(scoreOf).First();
                chosen = chosen.Except(inSet).ToList();
                chosen.Add(best);
            }
            return chosen;
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

                if (si.Type == IntentType.Bribe || si.Type == IntentType.Recruit || si.Type == IntentType.Court || si.Type == IntentType.Assassinate || si.Type == IntentType.Quarrel || si.Type == IntentType.TorturePrisoner || si.Type == IntentType.RapePrisoner)
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
    }

    public sealed class PlannerConfig
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
        public double TortureWeight { get; init; } = 0.75;
        public double RapeWeight { get; init; } = 0.6;
        public double TravelWeight { get; init; } = 0.8;
        public double BecomePirateWeight { get; init; } = 0.9;
        public double RaidConvoyWeight { get; init; } = 1.0;
        public int MinBribeBudget { get; init; } = 200;
        public double AssassinateFrequency { get; init; } = 0.05;
        public int MaxCandidatePool { get; init; } = 60;
        public int MaxCrossFactionPool { get; init; } = 40;
        public int QuarrelOpinionThreshold { get; init; } = -25;
        public int AssassinationOpinionThreshold { get; init; } = -50;
        public int ConflictBuffer { get; init; } = 8;

        public static PlannerConfig Default => new();
    }
}