using SkyHorizont.Application.Fleets.Commands;
using SkyHorizont.Application.Shared;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Application.Fleets.Handlers
{
    public class ExecutePlanetConquestHandler
    {
        private readonly ICommanderRepository _commanderRepo;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;
        private readonly IBattleOutcomeService _battleOutcomeService;

        public ExecutePlanetConquestHandler(
            ICommanderRepository commanderRepo,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IBattleOutcomeService battleOutcomeService)
        {
            _commanderRepo = commanderRepo;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _battleOutcomeService = battleOutcomeService;
        }

        public void Handle(ExecutePlanetConquestCommand cmd)
        {
            var planet = _planetRepo.GetById(cmd.PlanetId)
                         ?? throw new NotFoundException("Planet not found");
            var fleet = _fleetRepo.GetById(cmd.AttackerFleetId)
                        ?? throw new NotFoundException("Fleet not found");

            planet.ConqueredBy(fleet.FactionId, cmd.BattleResult, _battleOutcomeService);

            // Delegate side effects via service
            _battleOutcomeService.ProcessPlanetConquest(planet, fleet, cmd.BattleResult);

            // Persist entities and commander merit changes
            _planetRepo.Save(planet);
            _fleetRepo.Save(fleet);

            if (fleet.AssignedCommanderId.HasValue)
            {
                var cmdr = _commanderRepo.GetById(fleet.AssignedCommanderId.Value);
                if (cmdr != null)
                {
                    // merit applied via service, but ensure persistence
                    _commanderRepo.Save(cmdr);
                }
            }
        }
    }
}
