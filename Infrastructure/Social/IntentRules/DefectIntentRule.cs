using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.Social.IntentRules
{
    public sealed class DefectIntentRule : IIntentRule
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;

        public DefectIntentRule(IFactionService factions, IRandomService rng)
        {
            _factions = factions;
            _rng = rng;
        }

        public IEnumerable<ScoredIntent> Generate(IntentContext ctx)
        {
            if (!ctx.ActorLeaderId.HasValue)
                yield break;

            var targetFaction = PickDefectionFaction(ctx.ActorFactionId);
            if (targetFaction == Guid.Empty)
                yield break;

            var score = ScoreDefect(ctx.Actor, ctx.ActorLeaderId.Value, ctx.ActorFactionId, targetFaction, ctx.FactionStatus, ctx.OpinionOf, ctx.Config) * ctx.AmbitionBias.Defect;
            if (score > 0)
                yield return new ScoredIntent(IntentType.Defect, score, null, targetFaction, null);
        }

        private Guid PickDefectionFaction(Guid actorFactionId)
        {
            var rivals = _factions.GetAllRivalFactions(actorFactionId).ToList();
            if (rivals.Count == 0) return Guid.Empty;
            var war = rivals.Where(f => _factions.IsAtWar(actorFactionId, f)).ToList();
            if (war.Count > 0) return war[_rng.NextInt(0, war.Count)];
            return rivals[_rng.NextInt(0, rivals.Count)];
        }

        private double ScoreDefect(Character actor, Guid leaderId, Guid actorFactionId, Guid targetFactionId, FactionStatus factionStatus, Func<Guid, int> opin, PlannerConfig cfg)
        {
            var opinionLeader = opin(leaderId);
            var baseScore = Clamp0to100Map(-opinionLeader) * 0.8;
            baseScore += (50 - actor.Personality.Conscientiousness) * 0.2;
            baseScore += (50 - actor.Personality.Agreeableness) * 0.2;
            baseScore += (actor.Personality.Openness - 50) * 0.15;
            baseScore -= (actor.Personality.Neuroticism - 50) * 0.15;
            if (_factions.IsAtWar(actorFactionId, targetFactionId)) baseScore += 10;
            if (factionStatus.HasUnrest) baseScore += 15;
            baseScore -= (int)actor.Rank * 2;
            return Clamp0to100(baseScore * cfg.DefectionWeight);
        }

        private static double Clamp0to100Map(double v) => Math.Clamp(v, 0, 100);
        private static double Clamp0to100(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);
    }
}
