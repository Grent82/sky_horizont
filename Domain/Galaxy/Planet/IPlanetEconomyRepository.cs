using System;
using System.Collections.Generic;
using SkyHorizont.Domain.Economy;

namespace SkyHorizont.Domain.Economy
{
    /// <summary>
    /// Economy persistence focused on per-planet budgets + trade/tariffs/loans + event log.
    /// </summary>
    public interface IPlanetEconomyRepository
    {
        // --- Budgets ---
        int GetPlanetBudget(Guid planetId);
        void AddBudget(Guid planetId, int amount);                 // amount > 0
        bool TryDebitBudget(Guid planetId, int amount);            // returns false if insufficient

        // --- Tariffs (per Faction) ---
        void SetTariffPolicy(Guid factionId, TariffPolicy policy);
        TariffPolicy? GetTariffPolicy(Guid factionId);

        // --- Trade Routes ---
        IEnumerable<TradeRoute> GetTradeRoutes();
        void SetTradeRoutes(TradeRoute route);                     // upsert
        void RemoveTradeRoute(Guid routeId);

        // --- Loans ---
        IEnumerable<Loan> GetLoans();
        Loan? GetLoan(Guid loanId);
        void SetLoan(Loan loan);                                   // upsert

        // --- Event Log ---
        void AddEventLog(EconomyEvent e);
        IEnumerable<EconomyEvent> GetEventLog();
    }
}
