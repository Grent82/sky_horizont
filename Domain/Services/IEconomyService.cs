
using SkyHorizont.Domain.Economy;

namespace SkyHorizont.Domain.Services
{
    public interface IEconomyService
    {

        // Turn driver
        void EndOfTurnUpkeep();                 // fleets/planets/characters, tariffs, trade, loans

        // Policies
        void SetTariff(Guid factionId, int percent);     // 0..100; applied to legal routes owned by faction (via planet control)
        int GetTariff(Guid factionId);

        // Trade
        Guid CreateTradeRoute(Guid fromPlanetId, Guid toPlanetId, int capacity, bool smuggling = false);
        void RemoveTradeRoute(Guid routeId);

        // Black‑market one‑offs
        void RecordBlackMarketTrade(Guid planetId, int credits, Guid counterpartyFactionId, string note = "Black market trade");

        // Loans
        Guid CreateLoan(LoanAccountType type, Guid ownerId, int principal, double monthlyInterestRate, int termMonths);
        void MakeLoanPayment(Guid loanId, int amount);

        // Budget
        int GetPlanetBudget(Guid planetId);
        void CreditPlanetBudget(Guid planetId, int amount);
    }
}

