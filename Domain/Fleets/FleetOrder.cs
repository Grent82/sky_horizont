using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;

namespace SkyHorizont.Domain.Fleets
{
    public abstract class FleetOrder
    {
        public Guid Id { get; }
        public TaskStatus Status { get; private set; }
        public FleetOrder(Guid id) { Id = id; Status = TaskStatus.Pending; }
        internal void Activate() { if (Status == TaskStatus.Pending) Status = TaskStatus.Active; }
        protected void Complete() => Status = TaskStatus.Success;
        protected void Fail() => Status = TaskStatus.Failed;
        public abstract void Execute(Fleet fleet, double delta);
    }

    public class MoveOrder : FleetOrder
    {
        public Guid DestinationSystemId { get; }
        public MoveOrder(Guid id, Guid destination) : base(id)
        {
            DestinationSystemId = destination;
        }

        public override void Execute(Fleet fleet, double delta)
        {
            if (fleet.CurrentSystemId == DestinationSystemId)
            {
                Complete();
                return;
            }
            var dist = fleet.GameNavService.Distance(fleet.CurrentSystemId, DestinationSystemId);
            fleet.TravelProgress += delta * fleet.AverageFleetSpeed;
            if (fleet.TravelProgress >= dist)
            {
                fleet.CurrentSystemId = DestinationSystemId;
                fleet.TravelProgress = 0;
                Complete();
            }
        }
    }

    public class AttackOrder : FleetOrder
    {
        public Guid TargetFleetId { get; }
        public AttackOrder(Guid id, Guid targetFleetId) : base(id)
        {
            TargetFleetId = targetFleetId;
        }

        public override void Execute(Fleet fleet, double delta)
        {
            if (fleet.GameCombatService.ResolveBattle(fleet.Id, TargetFleetId, out bool won, out List<Guid> lostShips))
            {
                foreach (var shipId in lostShips) fleet.DestroyShip(shipId);
                if (won) fleet.RewardAfterBattle();
                Complete();
            }
            else
            {
                Fail();
            }
        }
    }
}