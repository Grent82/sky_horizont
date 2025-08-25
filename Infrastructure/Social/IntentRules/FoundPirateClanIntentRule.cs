using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class FoundPirateClanIntentRule : IIntentRule
    {
        private readonly IPiracyService _piracy;

        public FoundPirateClanIntentRule(IPiracyService piracy)
        {
            _piracy = piracy;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (ctx.ActorSystemId == null)
                yield break;
            if (_piracy.IsPirateFaction(ctx.ActorFactionId))
                yield break;

            var score = ScoreFoundPirateClan(ctx.Actor, ctx.SystemSecurity, ctx.Config) * ctx.AmbitionBias.FoundPirateClan;
            if (score > 0)
                yield return new ScoredIntent(IntentType.FoundPirateClan, score, null, null, null);
        }

        private double ScoreFoundPirateClan(Character actor, SystemSecurity? security, PlannerConfig cfg)
        {
            double baseScore = 25.0;
            baseScore += (actor.Skills.Military - 50) * 0.4;
            baseScore += (50 - actor.Personality.Conscientiousness) * 0.3;
            baseScore += PersonalityTraits.GetTraitEffect("ThrillSeeker", actor.Personality);
            if (security != null)
            {
                baseScore += security.PirateActivity * 0.3;
                baseScore -= security.PatrolStrength * 0.1;
            }
            return Clamp0to100(baseScore * cfg.FoundPirateClanWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
