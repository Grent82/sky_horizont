using SkyHorizont.Domain.Economy;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence.Repositories
{
    public class PlanetEconomyRepository : IPlanetEconomyRepository
    {
        private readonly IPlanetEconomyDbContext _eco;

        public PlanetEconomyRepository(IPlanetEconomyDbContext ctx) => _eco = ctx;

        public void AddBudget(Guid planetId, int amount)
        {
            if (amount <= 0) return;
            var current = GetPlanetBudget(planetId);
            _eco.PlanetBudgets[planetId] = current + amount;
            _eco.SaveChanges();
        }



        public bool TryDebitBudget(Guid planetId, int amount)
        {
            if (amount <= 0) return true;
            var current = GetPlanetBudget(planetId);
            var newBalance = current - amount;
            if (newBalance < 0)
            {
                return false;
            }
            else
            {
                _eco.PlanetBudgets[planetId] = newBalance;
                _eco.SaveChanges();
                return true;
            }
        }

        public int GetPlanetBudget(Guid planetId) => _eco.PlanetBudgets.TryGetValue(planetId, out var current) ? current : 0;

        public TariffPolicy? GetTariffPolicy(Guid factionId) => _eco.Tariffs.TryGetValue(factionId, out var policy) ? policy : null;

        public void SetTariffPolicy(Guid factionId, TariffPolicy tariffPolicy)
        {
            _eco.Tariffs[factionId] = tariffPolicy;
        }

        public IEnumerable<TradeRoute> GetTradeRoutes()
        {
            return _eco.TradeRoutes.Values.ToList();
        }

        public void SetTradeRoutes(TradeRoute route)
        {
            _eco.TradeRoutes[route.Id] = route;
        }
        
        public void RemoveTradeRoute(Guid routeId)
        {
            _eco.TradeRoutes.Remove(routeId);
        }

        public void AddEventLog(EconomyEvent economyEvent)
        {
            _eco.EventLog.Add(economyEvent);
        }


        public IEnumerable<EconomyEvent> GetEventLog() => _eco.EventLog.AsReadOnly();

        public void SetLoan(Loan loan)
        {
            _eco.Loans[loan.Id] = loan;
        }

        public IEnumerable<Loan> GetLoans()
        {
            return _eco.Loans.Values.ToList();
        }

        public Loan? GetLoan(Guid loanId)
        {
            return _eco.Loans.TryGetValue(loanId, out var loan) ? loan : null;
        }
    }
}