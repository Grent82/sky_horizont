using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class AssassinateIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public AssassinateIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickAssassinationTarget(ctx.OtherFactionCharacters, ctx.OpinionOf, ctx.Config);
            if (target == null)
                yield break;

            var score = ScoreAssassinate(ctx.Actor, target, ctx.ActorFactionId, ctx.FactionStatus, ctx.FactionOf, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias[IntentType.Assassinate];
            if (score > 0)
                yield return new ScoredIntent(IntentType.Assassinate, score, target.Id, null, null);
        }

        private Character? PickAssassinationTarget(IReadOnlyList<Character> otherFaction, Func<Guid, int> opin, PlannerConfig cfg)
        {
            if (_rng.NextDouble() > cfg.AssassinateFrequency) return null;
            var pool = otherFaction
                .Select(c => new { C = c, O = opin(c.Id) })
                .Where(x => x.O < cfg.AssassinationOpinionThreshold && x.C.Rank >= Rank.Major)
                .OrderBy(x => x.O)
                .ToList();
            if (pool.Count == 0) return null;
            return pool.First().C;
        }

        private double ScoreAssassinate(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin, PlannerConfig cfg)
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
            return Clamp0to100(baseScore * cfg.AssassinateWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
