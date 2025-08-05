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
        private readonly IBattleSimulator _battleSimulator;
        private readonly IBattleOutcomeService _battleOutcomeService;

        public ExecutePlanetConquestHandler(
            ICommanderRepository commanderRepo,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IBattleSimulator battleSimulator,
            IBattleOutcomeService battleOutcomeService)
        {
            _commanderRepo = commanderRepo;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _battleSimulator = battleSimulator;
            _battleOutcomeService = battleOutcomeService;
        }

        public void Handle(ExecutePlanetConquestCommand cmd, double researchAtkPct, double researchDefPct)
        {
            var planet = _planetRepo.GetById(cmd.PlanetId)
                         ?? throw new NotFoundException("Planet not found");
            var fleet = _fleetRepo.GetById(cmd.AttackerFleetId)
                        ?? throw new NotFoundException("Fleet not found");

            // Simulate the battle
            cmd.BattleResult = _battleSimulator.SimulatePlanetConquest(fleet, planet, researchAtkPct, researchDefPct);

            var defenderFleet = planet.GetStationedFleet();
            if (defenderFleet != null)
            {
                if (!cmd.BattleResult.DefenseRetreated)
                {
                    planet.RemoveStationedFleet();
                }
                // If defender retreated, fleet remains but with reduced ships already removed
            }

            if (cmd.BattleResult.AttackerWins)
            {
                planet.ConqueredBy(fleet.FactionId, cmd.BattleResult, _battleOutcomeService);
                _battleOutcomeService.ProcessPlanetConquest(planet, fleet, cmd.BattleResult);
            }
            else
            {
                // outcome is failure or retreat—treat fleetBattle separately
                _battleOutcomeService.ProcessFleetBattle(fleet, defenderFleet, cmd.BattleResult);
            }

            _planetRepo.Save(planet);
            _fleetRepo.Save(fleet);

            if (fleet.AssignedCommanderId.HasValue)
            {
                var cmdr = _commanderRepo.GetById(fleet.AssignedCommanderId.Value);
                if (cmdr != null) _commanderRepo.Save(cmdr);
            }
        }
    }
}
