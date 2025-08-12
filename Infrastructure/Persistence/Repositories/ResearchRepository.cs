
using SkyHorizont.Domain.Research;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class ResearchRepository : IResearchRepository
    {
        private readonly IResearchDbContext _ctx;

        public ResearchRepository(IResearchDbContext ctx)
        {
            _ctx = ctx;
        }

        public void AddProgress((Guid factionId, string) key, int points)
        {
            _ctx.ResearchProgress.TryGetValue(key, out var current);
            _ctx.ResearchProgress[key] = current + points;
            _ctx.SaveChanges();
        }

        public int GetByKey((Guid factionId, string) key)
        {
            return _ctx.ResearchProgress.TryGetValue(key, out var val) ? val : 0;
        }
    }
}
