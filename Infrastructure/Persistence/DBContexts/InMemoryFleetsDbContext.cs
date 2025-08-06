using SkyHorizont.Domain.Fleets;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryFleetsDbContext : IFleetsDbContext
    {
        public IDictionary<Guid, Fleet> Fleets { get; } = new Dictionary<Guid, Fleet>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
