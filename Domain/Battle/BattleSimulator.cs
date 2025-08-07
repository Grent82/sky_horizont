using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Battle
{
    public class BattleSimulator : IBattleSimulator
    {
        private const int MaxRounds = 10;
        private readonly Random _random;

        public ICharacterRepository CharacterRepo { get; }
        public int? Seed { get; }

        public BattleSimulator(ICharacterRepository characterRepo, int seed = 0)
        {
            CharacterRepo = characterRepo;
            Seed = seed;
            _random = new(seed);
        }

        public BattleResult SimulateFleetBattle(Fleet attacker, IEnumerable<Fleet> defenders)
        {
            var rng = new Random(_random.Next());

            double atkPower = attacker.CalculateStrength().MilitaryPower * CharacterAttackModifier(attacker);
            double defPower = defenders.Sum(f => f.CalculateStrength().MilitaryPower * CharacterDefenseModifier(f));

            for (int round = 0; round < MaxRounds && atkPower > 0 && defPower > 0; round++)
            {
                defPower -= atkPower * 0.1;
                atkPower -= defPower * 0.1;
            }

            bool attackerWins = atkPower >= defPower;
            bool defenderRetreats = !attackerWins && (rng.NextDouble() < RetreatChance(defenders, atkPower, defPower));

            foreach (var fleet in defenders)
            {
                var lost = fleet.ComputeLostShips(defPower, defenderRetreats);
                foreach (var shipId in lost)
                    fleet.DestroyShip(shipId);
            }

            var defenderFaction = defenders.FirstOrDefault()?.FactionId ?? Guid.Empty;

            return new BattleResult(
                Guid.NewGuid(),
                attackerWins || defenderRetreats ? attacker.FactionId : defenderFaction,
                attackerWins || defenderRetreats ? defenderFaction : attacker.FactionId,
                attackerWins ? attacker : (defenderRetreats ? attacker : null),
                defenderRetreats ? defenders.FirstOrDefault() : (attackerWins ? null : attacker),
                occupationDurationHours: 0,
                outcomeMerit: attackerWins ? 50 : 10,
                lootCredits: (int)(atkPower * 0.5),
                planetCaptureBonus: 0,
                defenseRetreated: defenderRetreats,
                attackerWins: attackerWins
            );
        }

        public BattleResult SimulatePlanetConquest(
            Fleet attacker, Planet planet,
            double researchAtkPct, double researchDefPct)
        {
            var rng = new Random(_random.Next());
            var defenderFleets = planet.GetStationedFleets();
            
            BattleResult? fleetBattleResult = null;

            if (defenderFleets.Any())
            {
                fleetBattleResult = SimulateFleetBattle(attacker, defenderFleets);
            }

            double defenderFleetPower = defenderFleets.Sum(f => f.CalculateStrength().MilitaryPower);
            double defPower = planet.EffectiveDefense(researchDefPct) + defenderFleetPower;
            double atkPower = attacker.CalculateStrength().MilitaryPower * CharacterAttackModifier(attacker)
                          + researchAtkPct;

            int troops = planet.StationedTroops;

            for (int round = 0; round < MaxRounds && atkPower > 0 && defPower + troops > 0; round++)
            {
                troops -= (int)(atkPower * 0.05);
                atkPower -= (defPower + troops) * 0.05;
            }

            bool attackerWins = atkPower >= defPower + troops;
            bool defenderRetreated = fleetBattleResult?.DefenseRetreated ?? false;

            var result = new BattleResult(
                Guid.NewGuid(),
                attackerWins || defenderRetreated ? attacker.FactionId : planet.ControllingFactionId,
                attackerWins || defenderRetreated ? planet.ControllingFactionId : attacker.FactionId,
                attackerWins ? attacker : (defenderRetreated ? attacker : null),
                defenderRetreated ? defenderFleets.FirstOrDefault() : (attackerWins ? null : attacker),
                attackerWins ? 24 : 0,
                outcomeMerit: attackerWins ? 100 : 20,
                lootCredits: attackerWins ? (int)(planet.BaseDefense * 2) : 0,
                planetCaptureBonus: attackerWins ? 200 : 0,
                defenseRetreated: defenderRetreated,
                attackerWins
            );

            return result;
        }

        private double CharacterAttackModifier(Fleet fleet) =>
        fleet.AssignedCharacterId.HasValue
            ? 1.0 + CharacterRepo.GetById(fleet.AssignedCharacterId.Value)!.GetAttackBonus()
            : 1.0;

        private double CharacterDefenseModifier(Fleet fleet) =>
            fleet.AssignedCharacterId.HasValue
                ? 1.0 + CharacterRepo.GetById(fleet.AssignedCharacterId.Value)!.GetDefenseBonus()
                : 1.0;

        private double RetreatChance(IEnumerable<Fleet> defenders, double atkPower, double defPower)
        {
            double ratio = defPower > 0 ? atkPower / defPower : double.PositiveInfinity;
            double baseChance = ratio >= 2.0 ? 0.4 : 0.0;

            // If any defending fleet has a character, apply best modifier (e.g. boldness)
            foreach (var def in defenders)
            {
                if (def.AssignedCharacterId.HasValue)
                {
                    var cmd = CharacterRepo.GetById(def.AssignedCharacterId.Value);
                    baseChance += cmd!.GetRetreatModifier();
                }
            }

            return Math.Clamp(baseChance, 0.0, 1.0);
        }
    }
}
