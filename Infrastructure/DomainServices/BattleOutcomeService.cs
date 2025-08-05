using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Faction;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class BattleOutcomeService : IBattleOutcomeService
    {
        private readonly IFundsService _fundsService;

        public BattleOutcomeService(IFundsService fundsService)
        {
            _fundsService = fundsService;
        }

        public void ProcessFleetBattle(Fleet attacker, Fleet? defender, BattleResult result)
        {
            // Reward commander with merit, if assigned
            if (attacker.AssignedCommanderId.HasValue)
            {
                var cmd = /* retrieve Commander by ID via repository if needed */
                    attacker.AssignedCommanderId.Value;
                // Example: direct method if commander loaded
                // cmd.GainMerit(result.OutcomeMerit);
            }

            // Reward credits
            _fundsService.Credit(attacker.FactionId, result.LootCredits);

            // Optionally penalize defender
            if (defender != null)
            {
                _fundsService.Deduct(defender.FactionId, result.LootCredits);
            }
        }

        public void ProcessPlanetConquest(Planet planet, Fleet attackerFleet, BattleResult result)
        {
            planet.ChangeControl(attackerFleet.FactionId);
            planet.StationFleet(attackerFleet);
            planet.SetStationedTroops(result.OccupationDurationHours * 10);

            // Reward conquest bonus
            _fundsService.Credit(attackerFleet.FactionId, result.PlanetCaptureBonus);

            // Reward commander merit
            if (attackerFleet.AssignedCommanderId.HasValue)
            {
                var cmdId = attackerFleet.AssignedCommanderId.Value;
                //cmd.GainMerit(result.OutcomeMerit);
            }
        }
    }
}
