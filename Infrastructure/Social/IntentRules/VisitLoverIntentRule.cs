using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class VisitLoverIntentRule : IIntentRule
    {
        private readonly ICharacterRepository _chars;
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public VisitLoverIntentRule(ICharacterRepository chars, IFactionService factions, IRandomService rng)
        {
            _chars = chars;
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var target = PickLoverTarget(ctx.Actor, ctx.OpinionOf);
            if (target == null)
                yield break;

            var score = ScoreVisitLover(ctx.Actor, target, ctx.FactionStatus, ctx.OpinionOf, ctx.FactionOf) * ctx.Config.LoverVisitWeight;
            if (score > 0)
                yield return new ScoredIntent(IntentType.VisitLover, score, target.Id, null, null);
        }

        private Character? PickLoverTarget(Character actor, Func<Guid, int> opin)
        {
            if (actor.Relationships.Count == 0)
                return null;

            var loverIds = actor.Relationships
                .Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                .Select(r => r.TargetCharacterId)
                .ToHashSet();

            if (loverIds.Count == 0)
                return null;

            var lovers = _chars.GetByIds(loverIds).Where(c => c.IsAlive).ToList();
            if (lovers.Count == 0)
                return null;

            return lovers
                .Select(c => new { C = c, O = opin(c.Id) + _rng.NextInt(0, 10) })
                .OrderByDescending(x => x.O)
                .First().C;
        }

        private double ScoreVisitLover(Character actor, Character lover, FactionStatus factionStatus, Func<Guid, int> opin, Func<Guid, Guid> fac)
        {
            var opinion = opin(lover.Id);
            double baseScore = 35.0;

            baseScore += Clamp0to100Map(opinion + 50) * 0.5;
            baseScore += (actor.Personality.Agreeableness - 50) * 0.25;
            baseScore += (actor.Personality.Extraversion - 50) * 0.20;

            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Agreeableness"].Concat(traits["Extraversion"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality) * 0.6;

            var actorFactionId = fac(actor.Id);
            var loverFactionId = fac(lover.Id);
            if (actorFactionId != Guid.Empty && loverFactionId != Guid.Empty && actorFactionId != loverFactionId)
                baseScore += 15;

            if (_factions.HasAlliance(actorFactionId, loverFactionId))
                baseScore += 10;

            if (factionStatus.HasUnrest)
                baseScore += 5;

            return Clamp0to100(baseScore);
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
