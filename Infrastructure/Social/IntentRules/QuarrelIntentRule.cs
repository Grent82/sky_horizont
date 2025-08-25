using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class QuarrelIntentRule : IIntentRule
    {
        private readonly IRandomService _rng;

        public QuarrelIntentRule(IRandomService rng)
        {
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickQuarrelTarget(ctx.Actor, ctx.OtherFactionCharacters, ctx.OpinionOf, ctx.Config);
            if (target == null)
                yield break;

            var score = ScoreQuarrel(ctx.Actor, target, ctx.FactionStatus, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias[IntentType.Quarrel];
            if (score > 0)
                yield return new ScoredIntent(IntentType.Quarrel, score, target.Id, null, null);
        }

        private Character? PickQuarrelTarget(Character actor, IReadOnlyList<Character> candidates, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var negatives = candidates
                .Select(c => new { C = c, O = opin(c.Id) })
                .Where(x => x.O < cfg.QuarrelOpinionThreshold)
                .OrderBy(x => x.O)
                .ToList();
            if (negatives.Count == 0) return null;
            return negatives[_rng.NextInt(0, Math.Min(3, negatives.Count))].C;
        }

        private double ScoreQuarrel(Character actor, Character target, FactionStatus factionStatus, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;
            if (opinion < cfg.QuarrelOpinionThreshold)
                baseScore += Clamp0to100Map(cfg.QuarrelOpinionThreshold - opinion);
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"])
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            if ((int)target.Rank > (int)actor.Rank) baseScore -= 10;
            if (factionStatus.HasUnrest) baseScore += 10;
            return Clamp0to100(baseScore * cfg.QuarrelWeight);
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
