using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence.Diplomacy
{
    public sealed class InMemoryDiplomacyDbContext : IDiplomacyDbContext
    {
        public Dictionary<Guid, Treaty> Treaties { get; } = new();

        public void SaveChanges()
        {
            // in-memory no-op
        }
    }
}
