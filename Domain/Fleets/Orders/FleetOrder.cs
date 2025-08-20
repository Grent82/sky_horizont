using SkyHorizont.Domain.Shared;
using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;

namespace SkyHorizont.Domain.Fleets
{
    public abstract class FleetOrder
    {
        public Guid Id { get; }
        public TaskStatus Status { get; protected set; }

        protected FleetOrder(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(id));
            Id = id;
            Status = TaskStatus.Pending;
        }

        /// <summary>
        /// Activates the order if it is in Pending state.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the order is not Pending.</exception>
        public void Activate()
        {
            if (Status != TaskStatus.Pending)
                throw new InvalidOperationException($"Cannot activate order {Id} with status {Status}.");
            Status = TaskStatus.Active;
        }

        /// <summary>
        /// Marks the order as successfully completed.
        /// </summary>
        protected void Complete() => Status = TaskStatus.Success;

        /// <summary>
        /// Marks the order as failed.
        /// </summary>
        protected void Fail() => Status = TaskStatus.Failed;

        /// <summary>
        /// Executes the order, updating the fleet's state.
        /// </summary>
        /// <param name="fleet">The fleet executing the order.</param>
        /// <param name="delta">Time delta for execution (e.g., fraction of a month).</param>
        /// <exception cref="ArgumentNullException">Thrown if fleet is null.</exception>
        public abstract void Execute(Fleet fleet, double delta);

        /// <summary>
        /// Hook for pre-execution validation or setup.
        /// </summary>
        /// <param name="fleet">The fleet executing the order.</param>
        protected virtual void PreExecute(Fleet fleet) { }

        /// <summary>
        /// Hook for post-execution cleanup or logging.
        /// </summary>
        /// <param name="fleet">The fleet that executed the order.</param>
        protected virtual void PostExecute(Fleet fleet) { }

        protected void ValidateFleet(Fleet fleet)
        {
            if (fleet == null)
                throw new ArgumentNullException(nameof(fleet));
            if (!fleet.Ships.Any())
                throw new DomainException($"Fleet {fleet.Id} has no ships and cannot execute orders.");
        }
    }
}