using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryPlanetEconomyDbContext : IPlanetEconomyDbContext
    {
        public IDictionary<Guid, int> PlanetBudgets { get; } = new Dictionary<Guid, int>();
        public void SaveChanges() { /* no-op in memory */ }
    }
}
