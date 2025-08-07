using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Shared;
using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;

namespace SkyHorizont.Domain.Fleets
{
    public class Fleet
    {
        private readonly Dictionary<Guid, Ship> _ships = new();
        private readonly List<FleetOrder> _orders = new();

        public Guid Id { get; }
        public Guid FactionId { get; private set; }
        public Guid? AssignedCharacterId { get; private set; }
        public Guid CurrentSystemId { get; internal set; }
        public double TravelProgress { get; internal set; }
        public IReadOnlyCollection<Ship> Ships => _ships.Values;
        public IReadOnlyList<FleetOrder> Orders => _orders.AsReadOnly();

        public IList<Guid> CapturedCharacterIds { get; } = new List<Guid>();

        public double AverageFleetSpeed => _ships.Values.Any()
            ? _ships.Values.Average(s => s.Speed)
            : 0;
        public FleetStrength CalculateStrength()
        {
            double totalAtk = Ships.Sum(s => s.CurrentAttack);
            double totalDef = Ships.Sum(s => s.CurrentDefense);
            double cargo = Ships.Sum(s => s.CargoCapacity);
            return new FleetStrength(totalAtk + totalDef, cargo);
        }

        public Fleet(Guid id, Guid factionId, Guid startingSystem)
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
            int toRemove = retreat
                ? (int)(Ships.Count * 0.4)  // ~40% losses on retreat :contentReference[oaicite:3]{index=3}
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

        public void AddCaptured(Guid cmdrId) => CapturedCharacterIds.Add(cmdrId);
        public void ClearCapturedAfterResolution() => CapturedCharacterIds.Clear();
    }
}