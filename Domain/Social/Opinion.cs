using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Social
{
    public record Opinion(Guid SourceId, Guid TargetId, int Score); // -100..+100

    public interface IOpinionRepository
    {
        int GetOpinion(Guid sourceId, Guid targetId);
        void AdjustOpinion(Guid sourceId, Guid targetId, int delta, string reason);
        IEnumerable<(Guid targetId, int score)> GetAllFor(Guid sourceId);
    }

    public static class OpinionRules
    {
        public static int AffectionTick(Personality p)
        {
            // Big Five-driven baseline monthly drift toward 0 unless supported by relation
            var warmth = (p.Agreeableness + p.Extraversion) / 2;
            var stability = 100 - p.Neuroticism;
            return Math.Clamp((warmth - 50)/25 + (stability-50)/50, -1, +1);
        }
    }
}
