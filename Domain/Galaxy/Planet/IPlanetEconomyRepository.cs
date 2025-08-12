using SkyHorizont.Domain.Economy;

namespace SkyHorizont.Domain.Galaxy.Planet
{
    public interface IPlanetEconomyRepository
    {
        void AddBudget(Guid planetId, int credits);
        int GetPlanetBudget(Guid planetId);
        bool TryDebitBudget(Guid planetId, int credits);

        TariffPolicy? GetTariffPolicy(Guid factionId);
        void SetTariffPolicy(Guid factionId, TariffPolicy tariffPolicy);

        IEnumerable<TradeRoute> GetTradeRoutes();
        void SetTradeRoutes(TradeRoute route);
        void RemoveTradeRoute(Guid routeId);
        
        void AddEventLog(EconomyEvent economyEvent);

        void SetLoan(Loan loan);
        IEnumerable<Loan> GetLoans();
        Loan? GetLoan(Guid loanId);
    }
}