using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Domain.Travel;
using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;

namespace SkyHorizont.Domain.Fleets
{
    public class Fleet
    {
        private readonly Dictionary<Guid, Ship> _ships = new();
        private readonly List<FleetOrder> _orders = new();
        private Resources _cargo;

        public Guid Id { get; }
        public Guid FactionId { get; private set; }
        public Guid? AssignedCharacterId { get; private set; }
        public Guid CurrentSystemId { get; internal set; }
        public double TravelProgress { get; internal set; }
        public IReadOnlyCollection<Ship> Ships => _ships.Values;
        public IReadOnlyList<FleetOrder> Orders => _orders.AsReadOnly();

        public IList<Guid> Prisoners { get; } = new List<Guid>();

        public IList<Guid> Passengers { get; } = new List<Guid>();

        public double AverageFleetSpeed => _ships.Values.Any()
            ? _ships.Values.Average(s => s.Speed)
            : 0;

        public bool IsPirateFleet { get; set; }
        public bool IsAssigned { get; set; }

        public FleetStrength CalculateStrength()
        {
            double totalAtk = Ships.Sum(s => s.CurrentAttack);
            double totalDef = Ships.Sum(s => s.CurrentDefense);
            double cargo = Ships.Sum(s => s.CargoCapacity);
            return new FleetStrength(totalAtk + totalDef, cargo);
        }

        public Fleet(Guid id, Guid factionId, Guid startingSystem, IPiracyService piracyService)
        {
            Id = id != Guid.Empty ? id : throw new ArgumentException(nameof(id));
            FactionId = factionId;
            CurrentSystemId = startingSystem;
        }

        public void AssignCharacter(Guid characterId) => AssignedCharacterId = characterId;

        public bool AddShip(Ship ship)
        {
            if (_ships.ContainsKey(ship.Id)) return false;
            _ships[ship.Id] = ship;
            return true;
        }

        public bool RemoveShip(Guid shipId) => _ships.Remove(shipId);

        public void DestroyShip(Guid shipId)
        {
            if (!_ships.Remove(shipId))
                throw new DomainException($"Ship {shipId} not part of fleet.");
        }

        public IEnumerable<Guid> ComputeLostShips(double remainingPower, bool retreat)
        {
            var sorted = Ships.OrderBy(s => s.CurrentDefense).ToList();
            // ToDo: beter calculation
            int toRemove = retreat
                ? (int)(Ships.Count * 0.4)  // ~40% losses on retreat
                : Ships.Count;              // full destruction if no retreat

            return sorted.Take(toRemove).Select(s => s.Id);
        }

        public void EnqueueOrder(FleetOrder order)
        {
            if (order.Status != TaskStatus.Pending)
                throw new DomainException("Order already scheduled.");
            _orders.Add(order);
        }

        public void TickOrders(double delta)
        {
            foreach (var order in Orders.ToList())
            {
                if (order.Status == TaskStatus.Pending) order.Activate();
                if (order.Status == TaskStatus.Active)
                    order.Execute(this, delta);
            }
            _orders.RemoveAll(o => o.Status != TaskStatus.Active && o.Status != TaskStatus.Pending);
        }

        public void HirePrivateers(Guid pirateFactionId, int creditCost, int shipCount, IFundsService fundsService, IPirateContractRepository repo)
        {
            if (!fundsService.HasFunds(FactionId, creditCost))
                throw new DomainException("Insufficient credits");
            fundsService.Deduct(FactionId, creditCost);
            var mercFleet = repo.GeneratePirateFleet(pirateFactionId, shipCount);
            repo.TransferPirateFleetToFaction(mercFleet.Id, FactionId);
        }

        internal void RewardAfterBattle(BattleResult result, IBattleOutcomeService outcomeService)
        {
            outcomeService.ProcessFleetBattle(this, result.LoserFleet!, result);
        }

        public void AddCaptured(Guid cmdrId) => Prisoners.Add(cmdrId);

        public void RemoveCaptured(Guid cmdrId) => Prisoners.Remove(cmdrId);
        public void ClearCapturedAfterResolution() => Prisoners.Clear();

        public void AddPassenger(Guid cmdrId) => Passengers.Add(cmdrId);
        public void RemovePassenger(Guid cmdrId) => Passengers.Remove(cmdrId);
        public void ClearPassengers() => Passengers.Clear();

        public void AddCargo(Resources resources)
        {
            var totalCargo = _cargo + resources;
            var capacity = Ships.Sum(s => s.CargoCapacity);
            if (totalCargo.Total > capacity)
                throw new DomainException("Cargo exceeds fleet capacity.");
            _cargo = totalCargo;
        }

        public void RemoveCargo(Resources resources)
        {
            var newCargo = _cargo - resources;
            _cargo = newCargo;
        }
    }
}