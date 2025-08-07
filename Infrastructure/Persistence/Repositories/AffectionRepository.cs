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

        public void AdjustAffection(Guid sourceCommanderId, Guid targetCommanderId, int delta)
        {
            var key = (sourceCommanderId, targetCommanderId);
            _context.Affection.TryGetValue(key, out var current);
            _context.Affection[key] = Math.Clamp(current + delta, -100, 100);
        }

        public int GetAffection(Guid sourceCommanderId, Guid targetCommanderId)
        {
            _context.Affection.TryGetValue((sourceCommanderId, targetCommanderId), out var val);
            return val;
        }
    }
}