using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class SpyIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public SpyIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var scoreBase = ScoreSpy(ctx.Actor, ctx.FactionStatus, ctx.Config) * ctx.AmbitionBias.Spy;
            if (scoreBase <= 0)
                yield break;

            var faction = PickSpyFaction(ctx.ActorFactionId);
            if (faction != Guid.Empty)
                yield return new ScoredIntent(IntentType.Spy, scoreBase, null, faction, null);

            var character = PickSpyCharacter(ctx.ActorFactionId, ctx.OtherFactionCharacters, ctx.FactionOf, ctx.OpinionOf);
            if (character != null)
                yield return new ScoredIntent(IntentType.Spy, scoreBase, character.Id, null, null);
        }

        private Guid PickSpyFaction(Guid actorFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(actorFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;
            var war = rivals.Where(f => _factions.IsAtWar(actorFactionId, f)).ToList();
            if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private Character? PickSpyCharacter(Guid actorFactionId, IReadOnlyList<Character> candidates, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var pool = candidates
                .Where(c => fac(c.Id) != actorFactionId)
                .Select(c => new { C = c, Score = (int)c.Rank * 10 - opin(c.Id) + _rng.NextInt(0, 10) })
                .OrderByDescending(x => x.Score)
                .ToList();
            return pool.Count == 0 ? null : pool.First().C;
        }

        private double ScoreSpy(Character actor, FactionStatus factionStatus, PlannerConfig cfg)
        {
            var baseScore = actor.Skills.Intelligence * 0.6;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Openness"].Concat(traits["Neuroticism"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            baseScore += (int)actor.Rank * 2;
            if (actor.Rank == Rank.Civilian && actor.Skills.Intelligence < 65) baseScore -= 15;
            if (factionStatus.IsAtWar) baseScore += 15;
            return Clamp0to100(baseScore * cfg.SpyWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
