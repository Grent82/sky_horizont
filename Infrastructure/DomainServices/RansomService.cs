using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class RansomService : IRansomService
    {
        private readonly ICharacterRepository _cmdRepo;
        private readonly ICharacterFundsService _funds;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;

        public RansomService(ICharacterRepository characterRepository, ICharacterFundsService characterFundsService, IFactionFundsRepository fleetRepository, IPlanetRepository planetRepo, IFleetRepository fleetRepo)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _factionFunds = fleetRepository;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
        }

        public void TryRequestRansoms()
        {
            // Loop through captives—planet and fleet—and attempt ransom
            // Try family character, then faction, then offer to others
        }
    }
}
