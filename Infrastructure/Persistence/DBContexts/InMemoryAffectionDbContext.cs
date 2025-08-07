using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryAffectionDbContext : IAffectionDbContext
    {
        public IDictionary<Guid, int> FactionFunds { get; } = new Dictionary<Guid, int>();

        public Dictionary<(Guid source, Guid target), int> Affection  { get; } = new Dictionary<(Guid source, Guid target), int>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
