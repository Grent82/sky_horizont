using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class RaidConvoyIntentRule : IIntentRule
    {
        private readonly IPlanetRepository _planets;
        private readonly IFactionService _factions;
        private readonly IPiracyService _piracy;
        private readonly IRandomService _rng;

        public RaidConvoyIntentRule(IPlanetRepository planets, IFactionService factions, IPiracyService piracy, IRandomService rng)
        {
            _planets = planets;
            _factions = factions;
            _piracy = piracy;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (!_piracy.IsPirateFaction(ctx.ActorFactionId))
                yield break;

            var targetSystem = PickRaidTargetSystem(ctx.ActorSystemId, ctx.ActorFactionId);
            if (!targetSystem.HasValue)
                yield break;

            var security = GetSystemSecurity(targetSystem.Value);
            var score = ScoreRaidConvoy(ctx.Actor, targetSystem.Value, security, ctx.Config) * ctx.AmbitionBias[IntentType.RaidConvoy];
            if (score > 0)
                yield return new ScoredIntent(IntentType.RaidConvoy, score, null, targetSystem.Value, null);
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
                .Take(3)
                .ToList();
            return systems.Count > 0 ? systems[_rng.NextInt(0, systems.Count)].SystemId : null;
        }

        private double ScoreRaidConvoy(Character actor, Guid systemId, SystemSecurity security, PlannerConfig cfg)
        {
            var baseScore = 0.0;
            baseScore += (actor.Skills.Military - 50) * 0.4;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.2;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Extraversion"].Concat(traits["Neuroticism"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality);
            baseScore += security.PirateActivity * 0.4;
            baseScore += security.Traffic * 0.3;
            baseScore -= security.PatrolStrength * 0.2;
            return Clamp0to100(baseScore * cfg.RaidConvoyWeight);
        }

        private SystemSecurity GetSystemSecurity(Guid systemId)
        {
            var securityLevel = _piracy.GetPirateActivity(systemId);
            var traffic = _piracy.GetTrafficLevel(systemId);
            var patrolStrength = _planets.GetAll().Where(p => p.SystemId == systemId).Sum(p => p.BaseDefense);
            return new SystemSecurity(systemId, (int)patrolStrength, securityLevel, traffic);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
