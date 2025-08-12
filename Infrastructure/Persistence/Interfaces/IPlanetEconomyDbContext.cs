using SkyHorizont.Domain.Economy;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IPlanetEconomyDbContext : IBaseDbContext
    {
        IDictionary<Guid, int> PlanetBudgets { get; }
        Dictionary<Guid, TradeRoute> TradeRoutes { get; }
        Dictionary<Guid, TariffPolicy> Tariffs { get; }       // keyed by FactionId
        Dictionary<Guid, Loan> Loans { get; }                 // keyed by LoanId
        List<EconomyEvent> EventLog { get; }                  // append‑only
    }
}
