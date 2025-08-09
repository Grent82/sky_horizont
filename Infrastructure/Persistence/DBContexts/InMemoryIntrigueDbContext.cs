using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence.Intrigue
{
    public class InMemoryIntrigueDbContext : IIntrigueDbContext
    {
        public Dictionary<Guid, Plot> Plots { get; } = new();
        public Dictionary<Guid, Secret> Secrets { get; } = new();

        public void SaveChanges()
        {
            // In-memory no-op
        }
    }
}
