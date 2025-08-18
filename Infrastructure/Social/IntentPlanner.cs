using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet; // Added for IPlanetRepository
using SkyHorizont.Domain.Fleets; // Added for IFleetRepository

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
        private readonly IPlanetRepository _planets;
        private readonly IFleetRepository _fleets;

        // Tuning knobs (0..100 style target values; scores roughly 0..100 range)
        private readonly PlannerConfig _cfg;

        public IntentPlanner(
            ICharacterRepository characters,
            IOpinionRepository opinions,
            IFactionService factions,
            IRandomService rng,
            IPlanetRepository planets, // Added
            IFleetRepository fleets, // Added
            PlannerConfig? config = null)
        {
            _chars = characters;
            _opinions = opinions;
            _factions = factions;
            _rng = rng;
            _planets = planets;
            _fleets = fleets;
            _cfg = config ?? PlannerConfig.Default;
        }

        public IEnumerable<CharacterIntent> PlanMonthlyIntents(Character actor)
        {
            var intents = new List<ScoredIntent>();
            if (!actor.IsAlive || actor.IsAssigned || actor.Age < 13)
                return Enumerable.Empty<CharacterIntent>();

            // Basic context
            var actorFactionId = _factions.GetFactionIdForCharacter(actor.Id);
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

            // Gather potential targets (characters, captives)
            var (sameFaction, otherFaction, captives) = SelectRelevantCharacters(actor, actorFactionId, GetFactionForCharacter, GetOpinionOfCharacter);

            // ── Score core intents ─────────────────────────────────────────────

            // Courtship / Maintain romance
            var romanticTarget = PickRomanticTarget(actor, sameFaction, GetFactionForCharacter, GetOpinionOfCharacter);
            if (romanticTarget != null)
                AddIfAboveZero(intents, ScoreCourtship(actor, romanticTarget, GetOpinionOfCharacter), IntentType.Court, romanticTarget.Id);

            // Visit family (keeps ties warm)
            var familyTarget = PickFamilyTarget(actor, GetOpinionOfCharacter);
            if (familyTarget.HasValue)
                AddIfAboveZero(intents, ScoreVisitFamily(actor, familyTarget.Value, GetOpinionOfCharacter), IntentType.VisitFamily, familyTarget.Value);

            // Spy (enemy or rival)
            var spyTargetFaction = PickSpyFaction(actorFactionId);
            if (spyTargetFaction != Guid.Empty)
                AddIfAboveZero(intents, ScoreSpy(actor), IntentType.Spy, null, spyTargetFaction);

            var spyTargetChar = PickSpyCharacter(actorFactionId, otherFaction, GetFactionForCharacter, GetOpinionOfCharacter);
            if (spyTargetChar != null)
                AddIfAboveZero(intents, ScoreSpy(actor), IntentType.Spy, spyTargetChar.Id, null);

            // Bribe (swing neutral or enemy asset)
            var bribeTarget = PickBribeTarget(otherFaction, GetOpinionOfCharacter);
            if (bribeTarget != null)
                AddIfAboveZero(intents, ScoreBribe(actor, bribeTarget, actorFactionId, GetFactionForCharacter), IntentType.Bribe, bribeTarget.Id);

            // Recruit (leaders/commanders recruiting sub-commanders)
            if (IsRecruiter(actor))
            {
                var recruitTarget = PickRecruitTarget(actorFactionId, otherFaction, GetFactionForCharacter);
                if (recruitTarget != null)
                    AddIfAboveZero(intents, ScoreRecruit(actor, recruitTarget, actorFactionId, GetFactionForCharacter), IntentType.Recruit, recruitTarget.Id);
            }

            // Defect (if hates leader / wooed by enemy)
            if (actorLeaderId.HasValue)
            {
                var defectTargetFaction = PickDefectionFaction(actorFactionId);
                if (defectTargetFaction != Guid.Empty)
                    AddIfAboveZero(intents, ScoreDefect(actor, actorLeaderId.Value, actorFactionId, defectTargetFaction, GetOpinionOfCharacter), IntentType.Defect, null, defectTargetFaction);
            }

            // Negotiate (diplomatic outreach)
            var negotiateTargetFaction = PickNegotiateFaction(actor, actorFactionId);
            if (negotiateTargetFaction != Guid.Empty)
                AddIfAboveZero(intents, ScoreNegotiate(actor, actorFactionId, negotiateTargetFaction), IntentType.Negotiate, null, negotiateTargetFaction);

            // Quarrel (pick fight with rival/enemy)
            var quarrelTarget = PickQuarrelTarget(actor, otherFaction, GetOpinionOfCharacter);
            if (quarrelTarget != null)
                AddIfAboveZero(intents, ScoreQuarrel(actor, quarrelTarget, GetOpinionOfCharacter), IntentType.Quarrel, quarrelTarget.Id);

            // Assassinate (very rare, high stakes)
            var assassinateTarget = PickAssassinationTarget(otherFaction, GetOpinionOfCharacter);
            if (assassinateTarget != null)
                AddIfAboveZero(intents, ScoreAssassinate(actor, assassinateTarget, actorFactionId, GetFactionForCharacter, GetOpinionOfCharacter), IntentType.Assassinate, assassinateTarget.Id);

            // Torture prisoner for information
            var tortureTarget = PickTortureTarget(captives, GetOpinionOfCharacter);
            if (tortureTarget != null)
                AddIfAboveZero(intents, ScoreTorture(actor, tortureTarget, actorFactionId, GetFactionForCharacter, GetOpinionOfCharacter), IntentType.TorturePrisoner, tortureTarget.Id);

            // Rape prisoner
            var rapeTarget = PickRapeTarget(captives, GetOpinionOfCharacter);
            if (rapeTarget != null)
                AddIfAboveZero(intents, ScoreRape(actor, rapeTarget, actorFactionId, GetFactionForCharacter, GetOpinionOfCharacter), IntentType.RapePrisoner, rapeTarget.Id);

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

            var prelim = intents
                .OrderByDescending(i => i.Score)
                .Take(Math.Min(_cfg.ConflictBuffer, Math.Max(_cfg.MaxIntentsPerMonth * 3, 6)))
                .ToList();

            var filtered = ResolveConflictsTargetAware(actor, prelim, _cfg.MaxIntentsPerMonth, GetFactionForCharacter);
            return filtered.Select(i => new CharacterIntent(actor.Id, i.Type, i.TargetCharacterId, i.TargetFactionId))
                .ToList();
        }

        // ────────────────────────── Scoring ──────────────────────────

        private double ScoreCourtship(Character actor, Character target, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);   // -100..+100
            var compat = actor.Personality.CheckCompatibility(target.Personality); // 0..100
            var baseScore = 0.0;

            baseScore += Clamp0to100Map(compat);
            baseScore += Clamp0to100Map(Math.Max(0, opinion + 50));

            if (PersonalityTraits.Cheerful(actor.Personality)) baseScore += 10;
            if (PersonalityTraits.WarmAndFriendly(actor.Personality)) baseScore += 10;

            if (actor.Relationships.Any(r => r.TargetCharacterId == target.Id &&
                                             (r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)))
                baseScore += 15;

            if (actor.Personality.Extraversion < 40) baseScore -= 10;

            return Clamp0to100(baseScore * _cfg.RomanceWeight);
        }

        private double ScoreVisitFamily(Character actor, Guid familyId, Func<Guid, int> opin)
        {
            var opinion = opin(familyId);
            var s = 30.0
                    + Clamp0to100Map(opinion + 50)
                    + (actor.Personality.Agreeableness - 50) * 0.3
                    + (actor.Personality.Conscientiousness - 50) * 0.2;

            return Clamp0to100(s * _cfg.FamilyWeight);
        }

        private double ScoreSpy(Character actor)
        {
            var intel = actor.Skills.Intelligence; // 0..100
            var s = intel * 0.6;

            if (PersonalityTraits.Adventurous(actor.Personality)) s += 10;
            if (PersonalityTraits.IntellectuallyCurious(actor.Personality)) s += 10;
            if (PersonalityTraits.Anxious(actor.Personality)) s -= 10;

            s += (int)actor.Rank * 2;

            if (actor.Rank == Rank.Civilian && intel < 65) s -= 15;

            return Clamp0to100(s * _cfg.SpyWeight);
        }

        private double ScoreBribe(Character actor, Character target, Guid actorFactionId, Func<Guid, Guid> fac)
        {
            var baseScore = 20.0;

            var hardness = (target.Personality.Conscientiousness + target.Personality.Agreeableness) / 2.0;
            baseScore += (100 - hardness) * 0.4;

            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;

            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 10;

            if (actor.Balance < _cfg.MinBribeBudget) baseScore -= 25;

            return Clamp0to100(baseScore * _cfg.BribeWeight);
        }

        private double ScoreRecruit(Character actor, Character target, Guid actorFactionId, Func<Guid, Guid> fac)
        {
            var baseScore = 30.0;

            var talent = (target.Skills.Military + target.Skills.Intelligence + target.Skills.Economy + target.Skills.Research) / 4.0;
            baseScore += talent * 0.4;

            baseScore += (int)actor.Rank * 3;

            baseScore += (actor.Personality.Agreeableness - 50) * 0.2;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;

            var sameFaction = actorFactionId == fac(target.Id);
            if (sameFaction) baseScore -= 10;

            return Clamp0to100(baseScore * _cfg.RecruitWeight);
        }

        private double ScoreDefect(Character actor, Guid leaderId, Guid actorFactionId, Guid targetFactionId, Func<Guid, int> opin)
        {
            var opinionLeader = opin(leaderId); // -100..+100
            var baseScore = 0.0;

            baseScore += Clamp0to100Map(-opinionLeader) * 0.8;

            baseScore += (50 - actor.Personality.Conscientiousness) * 0.2;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.2;

            baseScore += (actor.Personality.Openness - 50) * 0.15;
            baseScore -= (actor.Personality.Neuroticism - 50) * 0.15;

            if (_factions.IsAtWar(actorFactionId, targetFactionId)) baseScore += 10;

            baseScore -= (int)actor.Rank * 2;

            return Clamp0to100(baseScore * _cfg.DefectionWeight);
        }

        private double ScoreNegotiate(Character actor, Guid myFactionId, Guid targetFactionId)
        {
            var baseScore = 25.0;

            baseScore += (actor.Personality.Agreeableness - 50) * 0.3;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;

            if (_factions.IsAtWar(myFactionId, targetFactionId))
            {
                baseScore += 10;
                if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore -= 10;
                if (PersonalityTraits.Anxious(actor.Personality)) baseScore -= 10;
            }

            baseScore += (int)actor.Rank * 3;

            return Clamp0to100(baseScore * _cfg.NegotiateWeight);
        }

        private double ScoreQuarrel(Character actor, Character target, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;

            if (opinion < _cfg.QuarrelOpinionThreshold)
                baseScore += Clamp0to100Map(_cfg.QuarrelOpinionThreshold - opinion);

            if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore += 15;
            if (PersonalityTraits.Cheerful(actor.Personality)) baseScore -= 5;

            if ((int)target.Rank > (int)actor.Rank) baseScore -= 10;

            return Clamp0to100(baseScore * _cfg.QuarrelWeight);
        }

        private double ScoreAssassinate(Character actor, Character target, Guid actorFactionId, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;

            baseScore += actor.Skills.Military * 0.4;
            baseScore += Math.Max(0, -opinion) * 0.2;

            if (PersonalityTraits.Impulsive(actor.Personality)) baseScore += 10;
            if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore += 10;
            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;

            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 10;

            if (actor.Rank <= Rank.Captain && actor.Skills.Military < 70) baseScore -= 20;

            baseScore -= 15;

            return Clamp0to100(baseScore * _cfg.AssassinateWeight);
        }

        private double ScoreTorture(Character actor, Character target, Guid actorFactionId, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;

            // Low agreeableness (cruelty) and high conscientiousness (duty to extract info) increase score
            baseScore += (50 - actor.Personality.Agreeableness) * 0.4;
            baseScore += (actor.Personality.Conscientiousness - 50) * 0.2;

            // Target value: high rank or intelligence means more info potential
            baseScore += (int)target.Rank * 5;
            baseScore += target.Skills.Intelligence * 0.3;

            // Negative opinion boosts (personal grudge)
            if (opinion < 0) baseScore += Clamp0to100Map(-opinion) * 0.5;

            // At war with target's faction? Higher urgency for info
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 15;

            // Traits: angry/impulsive raise, anxious lower
            if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore += 10;
            if (PersonalityTraits.Impulsive(actor.Personality)) baseScore += 10;
            if (PersonalityTraits.Anxious(actor.Personality)) baseScore -= 15;

            // High rank actor more likely (authority to order torture)
            baseScore += (int)actor.Rank * 3;

            // Make it somewhat rare, especially for civilians
            if (actor.Rank < Rank.Captain) baseScore -= 20;

            return Clamp0to100(baseScore * _cfg.TortureWeight);
        }

        private double ScoreRape(Character actor, Character target, Guid actorFactionId, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;

            // Low agreeableness (lack of empathy) and low conscientiousness (impulsivity) increase score
            baseScore += (50 - actor.Personality.Agreeableness) * 0.5;
            baseScore += (50 - actor.Personality.Conscientiousness) * 0.3;

            // Negative opinion boosts (dominance/humiliation motive)
            if (opinion < 0) baseScore += Clamp0to100Map(-opinion) * 0.6;

            // Traits: angry/impulsive raise, cheerful/warm lower
            if (PersonalityTraits.EasilyAngered(actor.Personality)) baseScore += 15;
            if (PersonalityTraits.Impulsive(actor.Personality)) baseScore += 15;
            if (PersonalityTraits.Cheerful(actor.Personality)) baseScore -= 10;
            if (PersonalityTraits.WarmAndFriendly(actor.Personality)) baseScore -= 15;

            // At war? Small boost (wartime atrocities)
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 10;

            // High rank actor more likely (power abuse)
            baseScore += (int)actor.Rank * 2;

            // Make it rare, deduct for high neuroticism (guilt/fear)
            baseScore -= (actor.Personality.Neuroticism - 50) * 0.2;
            if (actor.Rank < Rank.Captain) baseScore -= 25;

            return Clamp0to100(baseScore * _cfg.RapeWeight);
        }

        // ───────────────────── Target pickers ───────────────────────

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

            // Prefer high-value targets: high rank or intelligence (likely to have useful info)
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

            // Prefer targets actor hates (personal motive), with some randomness
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

        // ───────────────────── Helpers ──────────────────────────────

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

            // Gather captives from planets and fleets controlled by actor's faction
            var myPlanets = _planets.GetPlanetsControlledByFaction(actorFactionId);
            var myFleets = _fleets.GetFleetsForFaction(actorFactionId);
            var captiveIds = myPlanets.SelectMany(p => p.CapturedCharacterIds)
                                      .Concat(myFleets.SelectMany(f => f.CapturedCharacterIds))
                                      .Distinct()
                                      .ToList();
            var captives = _chars.GetByIds(captiveIds).Where(c => c.IsAlive).ToList();

            return (sameFaction, otherFaction, captives);
        }

        private static void AddIfAboveZero(List<ScoredIntent> list, double score, IntentType type,
                                           Guid? targetCharacterId = null, Guid? targetFactionId = null)
        {
            if (score <= 0) return;
            list.Add(new ScoredIntent(type, score, targetCharacterId, targetFactionId));
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        private sealed record ScoredIntent(IntentType Type, double Score, Guid? TargetCharacterId, Guid? TargetFactionId);

        private static IEnumerable<CharacterIntent> FilterConflicts(IEnumerable<CharacterIntent> intents, Func<CharacterIntent, double> scoreOf)
        {
            var conflictSets = new[]
            {
                new HashSet<IntentType>{ IntentType.Court, IntentType.Assassinate },
                new HashSet<IntentType>{ IntentType.Negotiate, IntentType.Quarrel },
                new HashSet<IntentType>{ IntentType.Bribe, IntentType.Recruit },
                new HashSet<IntentType>{ IntentType.TorturePrisoner, IntentType.Negotiate },
                new HashSet<IntentType>{ IntentType.RapePrisoner, IntentType.Negotiate }
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

            foreach (var si in candidates)
            {
                if (kept.Count >= take) break;

                bool conflict = false;

                var tc = si.TargetCharacterId;
                var tf = si.TargetFactionId;

                if (!conflict && (si.Type == IntentType.Bribe || si.Type == IntentType.Recruit))
                {
                    if (tc.HasValue && chosenCharTargets.Contains(tc.Value))
                        conflict = true;
                }

                if (!conflict && (si.Type == IntentType.Negotiate || si.Type == IntentType.Quarrel || si.Type == IntentType.TorturePrisoner || si.Type == IntentType.RapePrisoner))
                {
                    Guid? targetFaction = tf;
                    if (si.Type == IntentType.Quarrel || si.Type == IntentType.TorturePrisoner || si.Type == IntentType.RapePrisoner)
                        if (tc.HasValue) targetFaction = fac(tc.Value);

                    if (targetFaction.HasValue && chosenFactionTargets.Contains(targetFaction.Value))
                        conflict = true;
                }

                if (!conflict && (si.Type == IntentType.Assassinate || si.Type == IntentType.Court))
                {
                    if (tc.HasValue && chosenCharTargets.Contains(tc.Value))
                        conflict = true;
                }

                if (!conflict && si.Type == IntentType.Defect && tf.HasValue && chosenFactionTargets.Contains(tf.Value))
                {
                    conflict = true;
                }

                if (!conflict)
                {
                    kept.Add(si);
                    if (tc.HasValue) chosenCharTargets.Add(tc.Value);
                    if (tf.HasValue) chosenFactionTargets.Add(tf.Value);
                    if ((si.Type == IntentType.Quarrel || si.Type == IntentType.TorturePrisoner || si.Type == IntentType.RapePrisoner) && si.TargetCharacterId.HasValue)
                    {
                        var f = fac(si.TargetCharacterId.Value);
                        if (f != Guid.Empty) chosenFactionTargets.Add(f);
                    }
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