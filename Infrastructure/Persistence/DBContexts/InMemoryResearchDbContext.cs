using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryResearchDbContext : IResearchDbContext
    {
        public Dictionary<(Guid factionId, string tech), int> ResearchProgress => new();

        public void SaveChanges() { /* no-op in memory */ }
    }
}
