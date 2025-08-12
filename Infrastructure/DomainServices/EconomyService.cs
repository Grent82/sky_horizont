using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Simple per-planet credit ledger (separate from resources).
    /// </summary>
    public class EconomyService : IEconomyService
    {
        private readonly IPlanetBudgedRepository _repository;

        public EconomyService(IPlanetBudgedRepository repository)
        {
            _repository = repository;
        }

        public void CreditPlanetBudget(Guid planetId, int credits)
        {
            if (credits <= 0) return;
            _repository.AddBudget(planetId, credits);
        }

        public int GetPlanetBudget(Guid planetId)
        {
            return _repository.GetPlanetBudget(planetId);
        }
    }
}
