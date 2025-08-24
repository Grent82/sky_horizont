using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class BribeIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public BribeIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickBribeTarget(ctx.OtherFactionCharacters, ctx.OpinionOf);
            if (target == null)
                yield break;

            var score = ScoreBribe(ctx.Actor, target, ctx.ActorFactionId, ctx.FactionStatus, ctx.FactionOf, ctx.Config) * ctx.AmbitionBias.Bribe;
            if (score > 0)
                yield return new ScoredIntent(IntentType.Bribe, score, target.Id, null, null);
        }

        private Character? PickBribeTarget(IReadOnlyList<Character> candidates, Func<Guid, int> opin)
        {
            var pool = candidates
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

        private double ScoreBribe(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, PlannerConfig cfg)
        {
            var baseScore = 20.0;
            var hardness = (target.Personality.Conscientiousness + target.Personality.Agreeableness) / 2.0;
            baseScore += (100 - hardness) * 0.4;
            baseScore -= (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore -= (actor.Personality.Agreeableness - 50) * 0.2;
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 10;
            if (factionStatus.EconomyWeak) baseScore += 10;
            if (actor.Balance < cfg.MinBribeBudget) baseScore -= 25;
            return Clamp0to100(baseScore * cfg.BribeWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
