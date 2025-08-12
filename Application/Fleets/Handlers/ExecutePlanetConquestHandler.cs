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
        private readonly ICharacterRepository _characterRepo;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;
        private readonly IBattleSimulator _battleSimulator;
        private readonly IBattleOutcomeService _battleOutcomeService;

        public ExecutePlanetConquestHandler(
            ICharacterRepository characterRepo,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IBattleSimulator battleSimulator,
            IBattleOutcomeService battleOutcomeService)
        {
            _characterRepo = characterRepo;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _battleSimulator = battleSimulator;
            _battleOutcomeService = battleOutcomeService;
        }

        public void Handle(ExecutePlanetConquestCommand cmd, double researchAtkPct, double researchDefPct)
        {
            var planet = _planetRepo.GetById(cmd.PlanetId)
                         ?? throw new NotFoundException("Planet not found");
            var attackerFleet = _fleetRepo.GetById(cmd.AttackerFleetId)
                        ?? throw new NotFoundException("Fleet not found");

            cmd.BattleResult = _battleSimulator.SimulatePlanetConquest(attackerFleet, planet, researchAtkPct, researchDefPct);

            foreach (var def in planet.GetStationedFleets().ToList())
            {
                if (!cmd.BattleResult.DefenseRetreated)
                {
                    planet.RemoveStationedFleet(def);
                }
            }

            if (cmd.BattleResult.AttackerWins)
            {
                planet.ConqueredBy(attackerFleet.FactionId, cmd.BattleResult, _battleOutcomeService);
                _battleOutcomeService.ProcessPlanetConquest(planet, attackerFleet, cmd.BattleResult);
            }
            else
            {
                var defenderFleet = cmd.BattleResult.DefenseRetreated
                    ? cmd.BattleResult.LoserFleet!
                    : cmd.BattleResult.WinnerFleet!;
                _battleOutcomeService.ProcessFleetBattle(attackerFleet, defenderFleet, cmd.BattleResult); // ToDo: order seems strange; thing about it
            }

            _planetRepo.Save(planet);
            _fleetRepo.Save(attackerFleet);

            if (attackerFleet.AssignedCharacterId.HasValue)
            {
                var cmdr = _characterRepo.GetById(attackerFleet.AssignedCharacterId.Value);
                if (cmdr != null) _characterRepo.Save(cmdr);
            }
        }
    }
}
