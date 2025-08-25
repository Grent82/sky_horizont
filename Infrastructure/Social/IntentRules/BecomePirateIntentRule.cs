using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class BecomePirateIntentRule : IIntentRule
    {
        private readonly IPlanetRepository _planets;
        private readonly IPiracyService _piracy;

        public BecomePirateIntentRule(IPlanetRepository planets, IPiracyService piracy)
        {
            _planets = planets;
            _piracy = piracy;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (_piracy.IsPirateFaction(ctx.ActorFactionId))
                yield break;

            var score = ScoreBecomePirate(ctx.Actor, ctx.ActorFactionId, ctx.ActorLeaderId, ctx.SystemSecurity, ctx.OpinionOf, ctx.FactionStatus, ctx.Config) * ctx.AmbitionBias.BecomePirate;
            if (score > 0)
                yield return new ScoredIntent(IntentType.BecomePirate, score, null, null, null);
        }

        private double ScoreBecomePirate(Character actor, Guid actorFactionId, Guid? actorLeaderId, SystemSecurity? systemSecurity, Func<Guid, int> opin, FactionStatus factionStatus, PlannerConfig cfg)
        {
            var planets = _planets.GetPlanetsControlledByFaction(actorFactionId);
            var isGovernor = planets.Any(p => p.GovernorId == actor.Id);
            if (isGovernor || actor.Rank >= Rank.General || actor.Balance > 2000)
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
            if (factionStatus.HasUnrest)
                baseScore += 15;
            return Clamp0to100(baseScore * cfg.BecomePirateWeight);
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
