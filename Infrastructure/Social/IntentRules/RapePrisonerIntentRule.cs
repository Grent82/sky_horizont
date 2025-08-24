using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class RapePrisonerIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public RapePrisonerIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickRapeTarget(ctx.Captives, ctx.OpinionOf);
            if (target == null)
                yield break;

            var score = ScoreRape(ctx.Actor, target, ctx.ActorFactionId, ctx.FactionStatus, ctx.FactionOf, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias.Rape;
            if (score > 0)
                yield return new ScoredIntent(IntentType.RapePrisoner, score, target.Id, null, null);
        }

        private Character? PickRapeTarget(IReadOnlyList<Character> captives, Func<Guid, int> opin)
        {
            if (captives.Count == 0)
                return null;
            var pool = captives
                .Select(c => new { C = c, Score = -opin(c.Id) + _rng.NextInt(0, 30) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();
            return pool.Count == 0 ? null : pool.First().C;
        }

        private double ScoreRape(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin, PlannerConfig cfg)
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
            return Clamp0to100(baseScore * cfg.RapeWeight);
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
