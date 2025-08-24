using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class CourtshipIntentRule : IIntentRule
    {
        private readonly IRandomService _rng;

        public CourtshipIntentRule(IRandomService rng)
        {
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var candidates = ctx.SameFactionCharacters.Concat(ctx.OtherFactionCharacters).Distinct().ToList();
            var target = PickRomanticTarget(ctx.Actor, candidates, ctx.FactionOf, ctx.OpinionOf);
            if (target == null)
                yield break;

            var score = ScoreCourtship(ctx.Actor, target, ctx.FactionStatus, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias.Court;
            if (score > 0)
                yield return new ScoredIntent(IntentType.Court, score, target.Id, null, null);
        }

        private Character? PickRomanticTarget(Character actor, List<Character> candidates, Func<Guid, Guid> fac, Func<Guid, int> opin)
        {
            var lovers = actor.Relationships.Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                                            .Select(r => r.TargetCharacterId)
                                            .ToHashSet();
            var known = candidates.Where(c => lovers.Contains(c.Id)).ToList();
            if (known.Count > 0) return known[_rng.NextInt(0, known.Count)];
            var myFaction = fac(actor.Id);
            var pool = candidates.Where(c => fac(c.Id) == myFaction).ToList();
            if (pool.Count == 0) return null;
            return pool.Select(c => new
            {
                C = c,
                Score = 0.6 * opin(c.Id) + 0.4 * actor.Personality.CheckCompatibility(c.Personality)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault()?.C;
        }

        private double ScoreCourtship(Character actor, Character target, FactionStatus factionStatus, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var opinion = opin(target.Id);
            var baseScore = 0.0;

            baseScore += Math.Clamp(actor.Personality.CheckCompatibility(target.Personality), 0, 100);
            baseScore += Math.Clamp(Math.Max(0, opinion + 50), 0, 100);

            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, _) in traits["Extraversion"].Concat(traits["Agreeableness"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);

            if (actor.Relationships.Any(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse))
                baseScore += 15;

            if (actor.Personality.Extraversion < 40) baseScore -= 10;
            if (factionStatus.HasAlliance) baseScore += 10;

            return Math.Clamp(baseScore * cfg.RomanceWeight, 0, 100);
        }
    }
}
