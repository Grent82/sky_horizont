using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class ExpelFromHouseIntentRule : IIntentRule
    {
        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (ctx.ActorFactionId == Guid.Empty)
                yield break;
            if ((int)ctx.Actor.Rank < (int)Rank.Captain)
                yield break;

            var candidate = ctx.SameFactionCharacters
                .Where(c => ctx.OpinionOf(c.Id) < -30)
                .OrderBy(c => ctx.OpinionOf(c.Id))
                .FirstOrDefault();
            if (candidate == null)
                yield break;

            var score = ScoreExpel(ctx.Actor, ctx.OpinionOf(candidate.Id), ctx.Config) * ctx.AmbitionBias.ExpelFromHouse;
            if (score > 0)
                yield return new ScoredIntent(IntentType.ExpelFromHouse, score, candidate.Id, null, null);
        }

        private double ScoreExpel(Character actor, int opinion, PlannerConfig cfg)
        {
            double baseScore = 35.0;
            baseScore += (int)actor.Rank * 5;
            baseScore += Math.Abs(opinion) * 0.5;
            return Clamp0to100(baseScore * cfg.ExpelFromHouseWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
