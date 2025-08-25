using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Task;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Economy;

namespace SkyHorizont.Domain.Galaxy.Planet
{
    public class Planet
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        public Guid SystemId { get; }
        public Guid FactionId { get; private set; }
        public double Stability { get; private set; }
        public Resources Resources { get; set; }
        public int InfrastructureLevel { get; private set; }
        public Guid? GovernorId { get; private set; }
        public double BaseAttack { get; private set; }
        public double BaseDefense { get; private set; }
        public int StationedTroops { get; private set; }
        public int ProductionCapacity { get; private set; }
        public double Satisfaction { get; private set; }
        public int Population { get; private set; }
        public double Research { get; private set; }
        public int Credits => _economy.GetPlanetBudget(Id);
        public double BaseTaxRate { get; private set; }
        public Guid? SeatFactionId { get; private set; }

        public IList<Guid> Prisoners { get; } = new List<Guid>();
        public IList<Guid> Citizens { get; } = new List<Guid>();
        private readonly List<Fleet> _stationedFleets = new();
        public bool IsTradeHub => InfrastructureLevel > 75;
        public int UnrestLevel => (int)(100 - Stability * 100);

        private readonly ICharacterRepository _characterRepository;
        private readonly IPlanetRepository _planetRepository;
        private readonly IPlanetEconomyRepository _economy;

        public Planet(
            Guid id,
            string name,
            Guid systemId,
            Guid factionId,
            Resources initialResources,
            ICharacterRepository characterRepository,
            IPlanetRepository planetRepository,
            IPlanetEconomyRepository economyRepository,
            double initialStability = 1.0,
            int infrastructureLevel = 10,
            double satisfaction = 50.0,
            int population = 1000000,
            double research = 0.0,
            int credits = 1000,
            double baseTaxRate = 1.0,
            double baseAtk = 0, double baseDef = 0, int troops = 0,
            int productionCapacity = 100)
        {
            Id = id != Guid.Empty ? id : throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SystemId = systemId;
            FactionId = factionId;
            Resources = initialResources;
            Stability = Math.Clamp(initialStability, 0.0, 1.0);
            InfrastructureLevel = Math.Clamp(infrastructureLevel, 0, 100);
            Satisfaction = Math.Clamp(satisfaction, 0.0, 100.0);
            Population = Math.Max(0, population);
            Research = Math.Clamp(research, 0.0, 1000.0);
            BaseTaxRate = Math.Clamp(baseTaxRate, 0.0, 2.0); // ToDo: make configureable
            BaseAttack = baseAtk;
            BaseDefense = baseDef;
            StationedTroops = troops;
            ProductionCapacity = productionCapacity;
            _characterRepository = characterRepository ?? throw new ArgumentNullException(nameof(characterRepository));
            _planetRepository = planetRepository ?? throw new ArgumentNullException(nameof(planetRepository));
            _economy = economyRepository ?? throw new ArgumentNullException(nameof(economyRepository));
            var startCredits = Math.Max(0, credits);
            if (startCredits > 0)
                _economy.AddBudget(Id, startCredits);
        }

        public void AssignGovernor(Guid? characterId)
        {
            if (characterId.HasValue && characterId != Guid.Empty)
            {
                var governor = _characterRepository.GetById(characterId.Value);
                if (governor == null || !governor.IsAlive)
                    throw new DomainException($"Character {characterId} is invalid or not alive.");
                if (!governor.CanPerform(TaskType.Govern))
                    throw new DomainException($"Character {characterId} cannot govern.");
            }
            GovernorId = characterId;
            AdjustStabilityBasedOnGovernor();
            _planetRepository.Save(this);
        }

        public Resources HarvestResources(double factor = 1.0)
        {
            var productionFactor = factor * (Population / 1_000_000.0) * (Satisfaction / 100.0) * (InfrastructureLevel / 100.0);
            var harvested = Resources.Scale(productionFactor);
            Resources = Resources - harvested;
            Satisfaction = Math.Clamp(Satisfaction - (productionFactor * 5.0), 0, 100);
            AdjustStabilityBasedOnSatisfaction();
            _planetRepository.Save(this);
            return harvested;
        }

        public void AddResources(Resources resources)
        {
            Resources = Resources + resources;
            _planetRepository.Save(this);
        }

        public void InvestInfrastructure(int points, int creditsCost)
        {
            if (points < 0)
                throw new DomainException("Investment points must be positive.");
            if (creditsCost < 0)
                throw new DomainException("Credits cost must be non-negative.");
            if (!_economy.TryDebitBudget(Id, creditsCost))
                throw new DomainException($"Insufficient credits: {creditsCost} required, {Credits} available.");

            InfrastructureLevel = Math.Min(100, InfrastructureLevel + points);
            Stability = Math.Min(1.0, Stability + 0.01 * points);
            BaseDefense += points * 0.5;
            Satisfaction = Math.Clamp(Satisfaction + (points * 0.2), 0, 100);
            _planetRepository.Save(this);
        }

        public void GenerateResearch(double researchPoints)
        {
            Research = Math.Min(1000.0, Research + researchPoints * (InfrastructureLevel / 100.0) * (Satisfaction / 100.0));
            _planetRepository.Save(this);
        }

        public int CollectTaxes()
        {
            var taxIncome = (int)(Population * BaseTaxRate * (Satisfaction / 100.0));
            _economy.AddBudget(Id, taxIncome);
            Satisfaction = Math.Clamp(Satisfaction - (BaseTaxRate * 5.0), 0, 100);
            AdjustStabilityBasedOnSatisfaction();
            _planetRepository.Save(this);
            return taxIncome;
        }

        public double EffectiveAttack(double researchAttackBonusPct)
        {
            var governor = GovernorId.HasValue ? _characterRepository.GetById(GovernorId.Value) : null;
            var governorBonus = governor?.Skills.Military * 0.01 ?? 0.0;
            return BaseAttack * (1 + researchAttackBonusPct) * (1 + governorBonus) +
                   _stationedFleets.Sum(f => f.CalculateStrength().MilitaryPower);
        }

        public double EffectiveDefense(double researchDefenseBonusPct)
        {
            var governor = GovernorId.HasValue ? _characterRepository.GetById(GovernorId.Value) : null;
            var governorBonus = governor?.Skills.Military * 0.01 ?? 0.0;
            return BaseDefense * (1 + researchDefenseBonusPct) * (1 + governorBonus) +
                   _stationedFleets.Sum(f => f.CalculateStrength().MilitaryPower);
        }

        public void StationFleet(Fleet fleet)
        {
            if (fleet.FactionId != FactionId)
                throw new DomainException("Fleet must belong to owning faction.");
            if (!_stationedFleets.Any(f => f.Id == fleet.Id))
            {
                _stationedFleets.Add(fleet);
                BaseDefense += fleet.CalculateStrength().MilitaryPower * 0.5;
                _planetRepository.Save(this);
            }
        }

        public void RemoveStationedFleet(Fleet fleet)
        {
            if (_stationedFleets.RemoveAll(f => f.Id == fleet.Id) > 0)
            {
                BaseDefense = Math.Max(0, BaseDefense - fleet.CalculateStrength().MilitaryPower * 0.5);
                _planetRepository.Save(this);
            }
        }

        public IReadOnlyList<Fleet> GetStationedFleets() => _stationedFleets.AsReadOnly();

        public bool Revolt(IRandomService rng, double chanceThreshold = 0.3)
        {
            var revoltChance = (100 - Satisfaction) / 100.0 * (UnrestLevel / 100.0);
            if (Stability > chanceThreshold || rng.NextDouble() > revoltChance)
                return false;

            GovernorId = null;
            Stability = Math.Clamp(Stability - 0.2, 0, 1.0);
            Satisfaction = Math.Clamp(Satisfaction - 10.0, 0, 100.0);
            Population = (int)(Population * 0.9);
            var loss = (int)(Credits * 0.2);
            _economy.TryDebitBudget(Id, loss);
            _planetRepository.Save(this);
            return true;
        }

        public void ChangeControl(Guid newFaction)
        {
            FactionId = newFaction;
            Stability = Math.Clamp(Stability - 0.3, 0, 1.0);
            Satisfaction = Math.Clamp(Satisfaction - 15.0, 0, 100.0);
            Population = (int)(Population * 0.95);
            var loss = (int)(Credits * 0.1);
            _economy.TryDebitBudget(Id, loss);
            GovernorId = null;
            _stationedFleets.Clear();
            BaseDefense = Math.Max(0, BaseDefense * 0.5);
            _planetRepository.Save(this);
        }

        public void ConqueredBy(Guid newFaction, BattleResult result, IBattleOutcomeService outcomeService)
        {
            ChangeControl(newFaction);
            outcomeService.ProcessPlanetConquest(this, result.WinnerFleet!, result);
            if (result.PlanetCaptureBonus > 0)
                _economy.AddBudget(Id, result.PlanetCaptureBonus);
            Population = (int)(Population * 0.9);
            Satisfaction = Math.Clamp(Satisfaction - 20.0, 0, 100.0);
            _planetRepository.Save(this);
        }

        public IReadOnlyList<Guid> GetAssignedSubCharacters()
        {
            var list = new List<Guid>();
            if (GovernorId.HasValue && GovernorId != Guid.Empty)
                list.Add(GovernorId.Value);

            foreach (var fleet in _stationedFleets)
            {
                if (fleet.AssignedCharacterId.HasValue && fleet.AssignedCharacterId != GovernorId)
                    list.Add(fleet.AssignedCharacterId.Value);
            }

            return list.Distinct().ToList();
        }

        public void AddCaptured(Guid cmdrId)
        {
            if (!Prisoners.Contains(cmdrId))
                Prisoners.Add(cmdrId);
            _planetRepository.Save(this);
        }

        public void ClearCapturedAfterResolution()
        {
            Prisoners.Clear();
            _planetRepository.Save(this);
        }

        public void AddCitizen(Guid cmdrId)
        {
            if (!Citizens.Contains(cmdrId))
                Citizens.Add(cmdrId);
            _planetRepository.Save(this);
        }

        public void RemoveCitizen(Guid cmdrId)
        {
            Citizens.Remove(cmdrId);
            _planetRepository.Save(this);
        }

        public void ClearCitizens()
        {
            Citizens.Clear();
            _planetRepository.Save(this);
        }

        public void AdjustStability(double to)
        {
            Stability = Math.Clamp(to, 0.0, 1.0);
            AdjustStabilityBasedOnSatisfaction();
            _planetRepository.Save(this);
        }

        public void AdjustTaxRate(double to)
        {
            BaseTaxRate = Math.Clamp(to, 0.0, 2.0);
            Satisfaction = Math.Clamp(Satisfaction - (to * 5.0), 0, 100.0);
            AdjustStabilityBasedOnSatisfaction();
            _planetRepository.Save(this);
        }

        public override string ToString() =>
            $"{Name} (Faction: {FactionId}, Governor: {GovernorId}, Population: {Population}, Satisfaction: {Satisfaction})";

        private void AdjustStabilityBasedOnSatisfaction()
        {
            var satisfactionImpact = (Satisfaction - 50.0) * 0.005;
            Stability = Math.Clamp(Stability + satisfactionImpact, 0.0, 1.0);
            if (GovernorId.HasValue)
            {
                var governor = _characterRepository.GetById(GovernorId.Value);
                if (governor != null)
                    Stability = Math.Clamp(Stability + (governor.Skills.Economy * 0.002), 0.0, 1.0);
            }
        }

        private void AdjustStabilityBasedOnGovernor()
        {
            if (GovernorId.HasValue)
            {
                var governor = _characterRepository.GetById(GovernorId.Value);
                if (governor != null)
                    Stability = Math.Clamp(Stability + (governor.Skills.Economy * 0.005), 0.0, 1.0);
            }
        }

        public void SetStationedTroops(int troopsToStation)
        {
            StationedTroops = troopsToStation;
        }

        public void SetSeatPlanet(Guid factionId)
        {
            SeatFactionId = factionId;
            _planetRepository.Save(this);
        }

        public bool IsSeatOf(Guid factionId) => SeatFactionId.HasValue && SeatFactionId.Value == factionId;
    }
}