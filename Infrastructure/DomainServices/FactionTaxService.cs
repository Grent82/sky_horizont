using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class FactionTaxService : IFactionTaxService
    {
        private readonly IFactionFundsRepository _factionFundsRepo;
        private readonly ICommanderFundsRepository _commanderFundsRepo;

        public FactionTaxService(
            IFactionFundsRepository factionFundsRepo,
            ICommanderFundsRepository commanderFundsRepo)
        {
            _factionFundsRepo = factionFundsRepo;
            _commanderFundsRepo = commanderFundsRepo;
        }

        public void TaxPlanet(Guid planetId, int percentage)
        {
            // ToDo
            // In actual implementation, load planet and compute base income
            int income = (int)(1000 * (percentage / 100.0));
            // For simplicity, credit faction treasury
            _factionFundsRepo.AddBalance(planetId, income);
        }

        public void DistributeLoot(Guid leaderCommanderId, int totalLoot, IEnumerable<Guid> subCommanderIds)
        {
            if (totalLoot <= 0) return;
            int share = subCommanderIds.Any()
                ? totalLoot / (subCommanderIds.Count() + 1)
                : totalLoot;

            _commanderFundsRepo.AddBalance(leaderCommanderId, share);
            foreach (var subId in subCommanderIds)
                _commanderFundsRepo.AddBalance(subId, share);
        }
    }
}
