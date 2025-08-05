using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Galaxy.Planet
{
    // ToDo: add Population, Satisfaction, Defense, Planet Development, Research, Credits
    public class Planet
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        public Guid SystemId { get; }
        public Guid ControllingFactionId { get; private set; }
        public double Stability { get; private set; }  // 0.0–1.0
        public Resources Resources { get; private set; }
        public int InfrastructureLevel { get; private set; }  // 0..100
        public Guid? GovernorId { get; private set; } = Guid.Empty;

        public double BaseAttack { get; private set; }
        public double BaseDefense { get; private set; }
        public int StationedTroops { get; private set; }
        private Fleet? _stationedFleet;
        public Fleet? StationedFleet => _stationedFleet;

        public Planet(
            Guid id,
            string name,
            Guid systemId,
            Guid controllingFactionId,
            Resources initialResources,
            double initialStability = 1.0,
            int infrastructureLevel = 10,
            double baseAtk = 0, double baseDef = 0, int troops = 0)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SystemId = systemId;
            ControllingFactionId = controllingFactionId;
            Resources = initialResources;
            Stability = Math.Clamp(initialStability, 0.0, 1.0);
            InfrastructureLevel = Math.Clamp(infrastructureLevel, 0, 100);
            BaseAttack = baseAtk;
            BaseDefense = baseDef;
            StationedTroops = troops;
        }

        public void AssignGovernor(Guid commanderId)
        {
            GovernorId = commanderId;
        }

        public Resources HarvestResources(double factor = 1.0)
        {
            // Reduce resources by a fraction
            int org = (int)(Resources.Organics * factor * InfrastructureLevel / 100.0);
            int ore = (int)(Resources.Ore * factor * InfrastructureLevel / 100.0);
            int vol = (int)(Resources.Volatiles * factor * InfrastructureLevel / 100.0);

            Resources = new Resources(
                Resources.Organics - org,
                Resources.Ore - ore,
                Resources.Volatiles - vol
            );

            return new Resources(org, ore, vol);
        }

        public void InvestInfrastructure(int points)
        {
            if (points < 0)
                throw new DomainException("Investment must be positive.");

            InfrastructureLevel = Math.Min(100, InfrastructureLevel + points);
            Stability = Math.Min(1.0, Stability + 0.01 * points);
            BaseDefense += points * 0.5;
        }

        public double EffectiveAttack(double researchAttackBonusPct)
            => BaseAttack * (1 + researchAttackBonusPct)
               + StationedTroops * 0.1;

        public double EffectiveDefense(double researchDefenseBonusPct)
            => BaseDefense * (1 + researchDefenseBonusPct)
               + StationedTroops * 0.2
               + (_stationedFleet?.CalculateStrength().MilitaryPower ?? 0);

        public void StationFleet(Fleet fleet)
        {
            if (fleet.FactionId != ControllingFactionId)
                throw new DomainException("Fleet must belong to owning faction.");
            _stationedFleet = fleet;
        }

        public void SetStationedTroops(int troops)
        {
            if (troops < 0) throw new DomainException("Troop count can't be negative.");
            StationedTroops = troops;
        }

        /// <summary>
        /// Can be called when stability crosses below threshold.
        /// Returns true if revolt succeeds.
        /// </summary>
        public bool Revolt(double chanceThreshold = 0.3)
        {
            if (Stability > chanceThreshold)
                return false;

            bool success = new Random().NextDouble() > Stability;
            if (success)
                GovernorId = null;

            return success;
        }

        public void ChangeControl(Guid newFaction)
        {
            ControllingFactionId = newFaction;
            // ToDo: optional: reset governor, stability drop
            Stability = 0.5;
            GovernorId = null;
        }
        
        public void ConqueredBy(Guid newFaction, BattleResult result, IBattleOutcomeService outcomeService)
        {
            ChangeControl(newFaction);
            outcomeService.ProcessPlanetConquest(this, result.WinnerFleet!, result);
        }

        public override string ToString() =>
            $"{Name} (Faction: {ControllingFactionId}, Governor: {GovernorId})";
    }
}
