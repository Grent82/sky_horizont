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
        private readonly IMoraleService _moraleService;

        public BattleOutcomeService(
            ICommanderFundsService commanderFundsService,
            IFactionFundsRepository factionFundRepo,
            IFactionTaxService factionTaxService,
            ICommanderRepository commanderRepo,
            IMoraleService moraleService)
        {
            _commanderFundsService = commanderFundsService;
            _factionFundRepo = factionFundRepo;
            _factionTaxService = factionTaxService;
            _commanderRepo = commanderRepo;
            _moraleService = moraleService;
        }

        public void ProcessFleetBattle(Fleet attacker, Fleet defender, BattleResult result)
        {
            if (attacker.AssignedCommanderId is Guid attackerCmdId)
            {
                var commander = _commanderRepo.GetById(attackerCmdId);
                if (commander != null)
                {
                    commander.GainMerit(result.OutcomeMerit);
                    _commanderFundsService.CreditCommander(commander.Id, result.LootCredits);

                    // Bonus for thrill-seeking commanders
                    if (PersonalityTraits.ThrillSeeker(commander.Personality))
                    {
                        int bonus = (int)(result.LootCredits * 0.1);
                        _commanderFundsService.CreditCommander(commander.Id, bonus);
                    }

                    _moraleService.AdjustMoraleForVictory(commander.Id, result);
                }
            }

            if (defender.AssignedCommanderId is Guid defenderCmdId)
            {
                _moraleService.AdjustMoraleForDefeat(defenderCmdId, result);
            }
        }

        public void ProcessPlanetConquest(Planet planet, Fleet attackerFleet, BattleResult result)
        {
            planet.ChangeControl(attackerFleet.FactionId);
            planet.StationFleet(attackerFleet);
            planet.SetStationedTroops(result.OccupationDurationHours * 10);

            if (attackerFleet.AssignedCommanderId is Guid leaderId)
            {
                var leader = _commanderRepo.GetById(leaderId);
                if (leader != null)
                {
                    leader.GainMerit(result.OutcomeMerit);

                    // Bonus for achievement-oriented commanders
                    if (PersonalityTraits.AchievementOriented(leader.Personality))
                    {
                        int bonus = (int)(result.PlanetCaptureBonus * 0.2);
                        _commanderFundsService.CreditCommander(leader.Id, bonus);
                        result = result with { PlanetCaptureBonus = result.PlanetCaptureBonus - bonus };
                    }

                    _moraleService.AdjustMoraleForConquest(leader.Id, planet);
                }
            }

            _factionFundRepo.AddBalance(attackerFleet.FactionId, result.PlanetCaptureBonus);

            if (attackerFleet.AssignedCommanderId.HasValue)
            {
                var subordinates = planet.GetAssignedSubCommanders();
                _factionTaxService.DistributeLoot(attackerFleet.AssignedCommanderId.Value, result.LootCredits, subordinates);
            }
        }
    }
}
