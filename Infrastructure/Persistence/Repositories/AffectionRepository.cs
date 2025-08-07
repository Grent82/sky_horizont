using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class AffectionRepository : IAffectionRepository
    {
        private readonly IAffectionDbContext _context;

        public AffectionRepository(IAffectionDbContext context)
        {
            _context = context;
        }

        public void AdjustAffection(Guid sourceCharacterId, Guid targetCharacterId, int delta)
        {
            var key = (sourceCharacterId, targetCharacterId);
            _context.Affection.TryGetValue(key, out var current);
            _context.Affection[key] = Math.Clamp(current + delta, -100, 100);
        }

        public int GetAffection(Guid sourceCharacterId, Guid targetCharacterId)
        {
            _context.Affection.TryGetValue((sourceCharacterId, targetCharacterId), out var val);
            return val;
        }
    }
}