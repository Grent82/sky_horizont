using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class VisitFamilyIntentRule : IIntentRule
    {
        private readonly IRandomService _rng;

        public VisitFamilyIntentRule(IRandomService rng)
        {
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickFamilyTarget(ctx.Actor, ctx.OpinionOf);
            if (!target.HasValue)
                yield break;

            var score = ScoreVisitFamily(ctx.Actor, target.Value, ctx.FactionStatus, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias[IntentType.VisitFamily];
            if (score > 0)
                yield return new ScoredIntent(IntentType.VisitFamily, score, target.Value, null, null);
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

        private double ScoreVisitFamily(Character actor, Guid familyId, FactionStatus factionStatus, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var opinion = opin(familyId);
            var baseScore = 30.0
                + Math.Clamp(opinion + 50, 0, 100)
                + (actor.Personality.Agreeableness - 50) * 0.3
                + (actor.Personality.Conscientiousness - 50) * 0.2;

            if (factionStatus.HasUnrest)
                baseScore += 10;
            return Math.Clamp(baseScore * cfg.FamilyWeight, 0, 100);
        }
    }
}
