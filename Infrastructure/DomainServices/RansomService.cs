using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class RansomService : IRansomService
    {
        private readonly ICommanderRepository _cmdRepo;
        private readonly ICommanderFundsService _funds;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;

        public RansomService(ICommanderRepository commanderRepository, ICommanderFundsService commanderFundsService, IFactionFundsRepository fleetRepository, IPlanetRepository planetRepo, IFleetRepository fleetRepo)
        {
            _cmdRepo = commanderRepository;
            _funds = commanderFundsService;
            _factionFunds = fleetRepository;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
        }

        public void TryRequestRansoms()
        {
            // Loop through captives—planet and fleet—and attempt ransom
            // Try family commander, then faction, then offer to others
        }
    }
}
