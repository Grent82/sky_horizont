using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryLinageDbContext : ILineageDbContext
    {
        public Dictionary<Guid, EntityLineage> EntityLineagebyChild { get; } = new Dictionary<Guid, EntityLineage>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
