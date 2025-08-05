using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Battle
{
    public class BattleSimulator : IBattleSimulator
    {
        private const int MaxRounds = 10;
        private readonly Random _random = new();

        public BattleResult SimulateFleetBattle(Fleet attacker, Fleet defender)
        {
            double atkPower = attacker.CalculateStrength().MilitaryPower;
            double defPower = defender.CalculateStrength().MilitaryPower;
            int atkRounds = 0, defRounds = 0;

            for (int round = 0; round < MaxRounds && atkPower > 0 && defPower > 0; round++)
            {
                defPower -= atkPower * 0.1;
                atkPower -= defPower * 0.1;
                atkRounds++; defRounds++;
            }

            bool attackerWins = atkPower >= defPower;
            double ratio = defPower > 0 ? atkPower / defPower : double.PositiveInfinity;

            bool defenderRetreats = !attackerWins && ratio <= 0.5;

            var lostDefenders = defender.ComputeLostShips(defPower, defenderRetreats);
            foreach (var id in lostDefenders)
                defender.DestroyShip(id);

            return new BattleResult(
                Guid.NewGuid(),
                attackerWins || defenderRetreats ? attacker.FactionId : defender.FactionId,
                attackerWins || defenderRetreats ? defender.FactionId : attacker.FactionId,
                attackerWins ? attacker : defender,
                defenderRetreats ? defender : attacker,
                occupationDurationHours: 0,
                outcomeMerit: attackerWins ? 50 : 10,
                lootCredits: (int)(atkPower * 0.5),
                planetCaptureBonus: 0,
                defenderRetreats,
                attackerWins
            );
        }


        public BattleResult SimulatePlanetConquest(
            Fleet attacker, Planet planet,
            double researchAtkPct, double researchDefPct)
        {
            var defenderFleet = planet.GetStationedFleet();
            BattleResult? fleetBattle = null;

            if (defenderFleet != null)
            {
                fleetBattle = SimulateFleetBattle(attacker, defenderFleet);

                // defender lost ships already removed inside SimulateFleetBattle
            }

            double defenderFleetPower = defenderFleet?.CalculateStrength().MilitaryPower ?? 0;
            double defPower = planet.EffectiveDefense(researchDefPct) + defenderFleetPower;
            double atkPower = attacker.CalculateStrength().MilitaryPower
                            + (attacker.AssignedCommanderId.HasValue ? 10 : 0);

            int troops = planet.StationedTroops;

            for (int round = 0; round < MaxRounds && atkPower > 0 && defPower + troops > 0; round++)
            {
                troops -= (int)(atkPower * 0.05);
                atkPower -= (defPower + troops) * 0.05;
            }

            bool attackerWins = atkPower >= defPower + troops;
            bool defenderRetreated = fleetBattle?.DefenseRetreated ?? false;

            var result = new BattleResult(
                Guid.NewGuid(),
                attackerWins || defenderRetreated ? attacker.FactionId : planet.ControllingFactionId,
                attackerWins || defenderRetreated ? planet.ControllingFactionId : attacker.FactionId,
                attackerWins ? attacker : (defenderRetreated ? attacker : null),
                defenderRetreated ? defenderFleet : (attackerWins ? null : attacker),
                attackerWins ? 24 : 0,
                outcomeMerit: attackerWins ? 100 : 20,
                lootCredits: attackerWins ? (int)(planet.BaseDefense * 2) : 0,
                planetCaptureBonus: attackerWins ? 200 : 0,
                defenseRetreated: defenderRetreated,
                attackerWins
            );

            return result;
        }

    }
}
