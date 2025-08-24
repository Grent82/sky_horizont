using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class RecruitIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;
        private readonly IPiracyService _piracy;
        private readonly IPlanetRepository _planets;

        public RecruitIntentRule(IFactionService factions, IRandomService rng, IPiracyService piracy, IPlanetRepository planets)
        {
            _factions = factions;
            _rng = rng;
            _piracy = piracy;
            _planets = planets;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (!IsRecruiter(ctx.Actor))
                yield break;

            var target = PickRecruitTarget(ctx.ActorFactionId, ctx.OtherFactionCharacters, ctx.FactionOf);
            if (target == null)
                yield break;

            var score = ScoreRecruit(ctx.Actor, target, ctx.ActorFactionId, ctx.FactionStatus, ctx.FactionOf, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias.Recruit;
            if (score > 0)
                yield return new ScoredIntent(IntentType.Recruit, score, target.Id, null, null);
        }

        private bool IsRecruiter(Character actor) => actor.Rank >= Rank.Captain || actor.Skills.Military >= 70;

        private Character? PickRecruitTarget(Guid actorFactionId, IReadOnlyList<Character> candidates, Func<Guid, Guid> fac)
        {
            var pool = candidates.Where(c => fac(c.Id) != actorFactionId).ToList();
            if (pool.Count == 0) return null;
            return pool.Select(c => new
            {
                C = c,
                Talent = (c.Skills.Military + c.Skills.Intelligence + c.Skills.Economy + c.Skills.Research) / 4.0
            })
            .OrderByDescending(x => x.Talent + _rng.NextInt(0, 10))
            .First().C;
        }

        private double ScoreRecruit(Character actor, Character target, Guid actorFactionId, FactionStatus factionStatus, Func<Guid, Guid> fac, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var baseScore = 30.0;
            var talent = (target.Skills.Military + target.Skills.Intelligence + target.Skills.Economy + target.Skills.Research) / 4.0;
            baseScore += talent * 0.4;
            baseScore += (int)actor.Rank * 3;
            baseScore += (actor.Personality.Agreeableness - 50) * 0.2;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;

            var targetFactionId = fac(target.Id);
            var sameFaction = actorFactionId == targetFactionId;
            if (sameFaction)
                baseScore -= 10;
            if (factionStatus.HasUnrest)
                baseScore += 10;

            try
            {
                int opinion = opin(target.Id);
                baseScore += Math.Clamp(opinion, -50, 50) * 0.10;
            }
            catch { }

            if (_piracy.IsPirateFaction(actorFactionId))
            {
                if (target.Rank >= Rank.General)
                    baseScore -= 10;
                var isGovernor = _planets.GetAll().Any(p => p.GovernorId == target.Id);
                if (isGovernor)
                    baseScore -= 15;
            }

            return Clamp0to100(baseScore * cfg.RecruitWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
