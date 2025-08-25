using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class NegotiateIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public NegotiateIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            var targetFaction = PickNegotiateFaction(ctx.Actor, ctx.ActorFactionId);
            if (targetFaction == Guid.Empty)
                yield break;

            var score = ScoreNegotiate(ctx.Actor, ctx.ActorFactionId, targetFaction, ctx.FactionStatus, ctx.Config) * ctx.AmbitionBias.Negotiate;
            if (score > 0)
                yield return new ScoredIntent(IntentType.Negotiate, score, null, targetFaction, null);
        }

        private Guid PickNegotiateFaction(Character actor, Guid actorFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(actorFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;
            if (PersonalityTraits.Cheerful(actor.Personality) || PersonalityTraits.Trusting(actor.Personality))
            {
                var war = rivals.Where(f => _factions.IsAtWar(actorFactionId, f)).ToList();
                if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            }
            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private double ScoreNegotiate(Character actor, Guid myFactionId, Guid targetFactionId, FactionStatus factionStatus, PlannerConfig cfg)
        {
            var baseScore = 25.0;
            baseScore += (actor.Personality.Agreeableness - 50) * 0.3;
            baseScore += (actor.Personality.Extraversion - 50) * 0.2;
            var traits = PersonalityTraits.GetActiveTraits(actor.Personality);
            foreach (var (traitName, intensity) in traits["Agreeableness"].Concat(traits["Extraversion"]))
                baseScore += PersonalityTraits.GetTraitEffect(traitName, actor.Personality);
            if (_factions.IsAtWar(myFactionId, targetFactionId)) baseScore += 10;
            if (factionStatus.EconomyWeak) baseScore += 10;
            baseScore += (int)actor.Rank * 3;
            return Clamp0to100(baseScore * cfg.NegotiateWeight);
        }

        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
