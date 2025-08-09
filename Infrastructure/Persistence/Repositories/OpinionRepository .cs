using SkyHorizont.Domain.Social;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class OpinionRepository : IOpinionRepository
    {
        private readonly IOpinionsDbContext _context;

        public OpinionRepository(IOpinionsDbContext context)
        {
            _context = context;
        }

        public int GetOpinion(Guid sourceId, Guid targetId)
        {
            _context.Opinions.TryGetValue((sourceId, targetId), out var score);
            return score;
        }

        public void AdjustOpinion(Guid sourceId, Guid targetId, int delta, string reason)
        {
            var key = (sourceId, targetId);
            _context.Opinions.TryGetValue(key, out var current);
            var updated = Math.Clamp(current + delta, -100, 100);
            _context.Opinions[key] = updated;

            if (!_context.OpinionReasons.TryGetValue(key, out var list))
            {
                list = new List<string>();
                _context.OpinionReasons[key] = list;
            }
            list.Add($"{DateTime.UtcNow:o} | Δ{delta:+#;-#;0} | {reason}");

            _context.SaveChanges();
        }

        public IEnumerable<(Guid targetId, int score)> GetAllFor(Guid sourceId)
        {
            // Enumerate all opinion entries for this source
            return _context.Opinions
                .Where(kv => kv.Key.source == sourceId)
                .Select(kv => (kv.Key.target, kv.Value));
        }
    }
}
