using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class TorturePrisonerIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public TorturePrisonerIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickTortureTarget(ctx.Captives, ctx.OpinionOf);
            if (target == null)
                yield break;

            var score = ScoreTorture(ctx.Actor, target, ctx.ActorFactionId, ctx.FactionStatus, ctx.FactionOf, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias[IntentType.TorturePrisoner];
            if (score > 0)
                yield return new ScoredIntent(IntentType.TorturePrisoner, score, target.Id, null, null);
        }

        private Character? PickTortureTarget(IReadOnlyList<Character> captives, Func<Guid, int> opin)
        {
            if (captives.Count == 0) return null;
            var pool = captives
                .Select(c => new { C = c, Score = (int)c.Rank * 10 + c.Skills.Intelligence + _rng.NextInt(0, 20) })
                .OrderByDescending(x => x.Score)
                .ToList();
            return pool.First().C;
        }

        private double ScoreTorture(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.4;
            baseScore += (actor.Personality.Conscientiousness - 50) * 0.2;
            baseScore += (int)target.Rank * 5;
            baseScore += target.Skills.Intelligence * 0.3;
            if (opinion < 0) baseScore += Clamp0to100Map(-opinion) * 0.5;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Neuroticism"])
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += PersonalityTraits.GetTraitCombinationEffect("ImpulsiveAnger", actor.Personality);
            var targetFaction = fac(target.Id);
            if (_factions.IsAtWar(actorFactionId, targetFaction)) baseScore += 15;
            if (factionStatus.IsAtWar) baseScore += 10;
            baseScore += (int)actor.Rank * 3;
            if (actor.Rank < Rank.Captain) baseScore -= 20;
            return Clamp0to100(baseScore * cfg.TortureWeight);
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
