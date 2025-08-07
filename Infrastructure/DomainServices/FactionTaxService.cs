using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class FactionTaxService : IFactionTaxService
    {
        private readonly IFactionFundsRepository _factionFundsRepo;
        private readonly ICharacterFundsRepository _characterFundsRepo;

        public FactionTaxService(
            IFactionFundsRepository factionFundsRepo,
            ICharacterFundsRepository characterFundsRepo)
        {
            _factionFundsRepo = factionFundsRepo;
            _characterFundsRepo = characterFundsRepo;
        }

        public void TaxPlanet(Guid planetId, int percentage)
        {
            // ToDo
            // In actual implementation, load planet and compute base income
            int income = (int)(1000 * (percentage / 100.0));
            // For simplicity, credit faction treasury
            _factionFundsRepo.AddBalance(planetId, income);
        }

        public void DistributeLoot(Guid leaderCharacterId, int totalLoot, IEnumerable<Guid> subCharacterIds)
        {
            if (totalLoot <= 0) return;
            int share = subCharacterIds.Any()
                ? totalLoot / (subCharacterIds.Count() + 1)
                : totalLoot;

            _characterFundsRepo.AddBalance(leaderCharacterId, share);
            foreach (var subId in subCharacterIds)
                _characterFundsRepo.AddBalance(subId, share);
        }
    }
}
