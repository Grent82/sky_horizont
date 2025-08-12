using SkyHorizont.Domain.Economy;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryPlanetEconomyDbContext : IPlanetEconomyDbContext
    {
        public IDictionary<Guid, int> PlanetBudgets { get; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, TradeRoute> TradeRoutes { get; } = new();
        public Dictionary<Guid, TariffPolicy> Tariffs { get; } = new();
        public Dictionary<Guid, Loan> Loans { get; } = new();
        public List<EconomyEvent> EventLog { get; } = new();
        
        public void SaveChanges() { /* no-op in memory */ }
    }
}
