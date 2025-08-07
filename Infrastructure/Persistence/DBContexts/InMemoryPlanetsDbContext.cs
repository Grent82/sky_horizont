using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryPlanetsDbContext : IPlanetsDbContext
    {
        public IDictionary<Guid, Planet> Planets { get; } = new Dictionary<Guid, Planet>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
