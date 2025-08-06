// SkyHorizont.Infrastructure.DomainServices/BattleOutcomeService.cs

using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class BattleOutcomeService : IBattleOutcomeService
    {
        private readonly ICommanderFundsService _commanderFundsService;
        private readonly IFactionFundsRepository _factionFundRepo;
        private readonly IFactionTaxService _factionTaxService;
        private readonly ICommanderRepository _commanderRepo;

        public BattleOutcomeService(
            ICommanderFundsService commanderFundsService,
            IFactionFundsRepository factionFundRepo,
            IFactionTaxService factionTaxService,
            ICommanderRepository commanderRepo)
        {
            _commanderFundsService = commanderFundsService;
            _factionFundRepo = factionFundRepo;
            _factionTaxService = factionTaxService;
            _commanderRepo = commanderRepo;
        }

        public void ProcessFleetBattle(Fleet attacker, Fleet defender, BattleResult result)
        {
            if (attacker.AssignedCommanderId.HasValue)
            {
                var cmd = _commanderRepo.GetById(attacker.AssignedCommanderId.Value);
                cmd?.GainMerit(result.OutcomeMerit);
            }

            // Distribute loot from fleet battle to commander(s)
            if (attacker.AssignedCommanderId.HasValue)
            {
                _commanderFundsService.CreditCommander(attacker.AssignedCommanderId.Value, result.LootCredits);
            }
        }

        public void ProcessPlanetConquest(Planet planet, Fleet attackerFleet, BattleResult result)
        {
            planet.ChangeControl(attackerFleet.FactionId);
            planet.StationFleet(attackerFleet);
            planet.SetStationedTroops(result.OccupationDurationHours * 10);

            if (attackerFleet.AssignedCommanderId.HasValue)
            {
                var leader = _commanderRepo.GetById(attackerFleet.AssignedCommanderId.Value);
                leader?.GainMerit(result.OutcomeMerit);

                if (leader?.Personality.Type == PersonalityType.Aggressive)
                {
                    int bonus = (int)(result.PlanetCaptureBonus * 0.2);
                    _commanderFundsService.CreditCommander(leader.Id, bonus);
                    result = result with { PlanetCaptureBonus = result.PlanetCaptureBonus - bonus };
                }
            }

            _factionFundRepo.AddBalance(attackerFleet.FactionId, result.PlanetCaptureBonus);

            if (attackerFleet.AssignedCommanderId.HasValue)
            {
                var subordinateIds = planet.GetAssignedSubCommanders();
                _factionTaxService.DistributeLoot(attackerFleet.AssignedCommanderId.Value, result.LootCredits, subordinateIds);
            }
        }
    }
}
