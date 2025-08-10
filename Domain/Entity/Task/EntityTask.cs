using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity.Task
{
    public abstract class EntityTask
    {
        public Guid Id { get; }
        public TaskType Type { get; }
        public Guid AssignedCharacterId { get; private set; }
        public Guid TargetId { get; }
        public TaskStatus Status { get; private set; }
        public double Progress { get; private set; } // 0.0–1.0
        public int RewardMerit { get; }
        public int? CompletedYear { get; private set; }
        public int? CompletedMonth { get; private set; }
        public string? FailureReason { get; private set; }

        protected EntityTask(Guid id, TaskType type, Guid targetId, int rewardMerit)
        {
            Id = id;
            Type = type;
            TargetId = targetId;
            RewardMerit = rewardMerit;
            Status = TaskStatus.Pending;
        }

        internal bool AssignTo(Character chr)
        {
            if (!chr.CanPerform(Type))
                return false;

            AssignedCharacterId = chr.Id;
            Status = TaskStatus.Active;
            OnAssigned(chr);
            return true;
        }

        /// <summary>
        /// Advance the task. When finishing, emits a TaskEffect via sink.
        /// </summary>
        public void Tick(
            double delta,
            IGameClockService clock,
            IRandomService rng,
            ITaskEffectSink sink,
            Func<Guid, Character?> getCharacterById // helper from app layer to rehydrate performer if needed
        )
        {
            if (Status != TaskStatus.Active)
                throw new DomainException("Task is not active.");

            // Update progress
            double increment = delta * SpeedFactor(rng);
            Progress = Math.Min(1.0, Progress + increment);

            // Finish?
            if (Progress >= 1.0)
            {
                var chr = getCharacterById(AssignedCharacterId)
                          ?? throw new DomainException("Assigned character no longer available.");
                Finish(clock, rng, sink, chr);
            }
        }

        protected virtual double SpeedFactor(IRandomService rng) => 0.01;

        private void Finish(IGameClockService clock, IRandomService rng, ITaskEffectSink sink, Character chr)
        {
            bool success = EvaluateSuccess(chr, rng, out string? reason);
            Status = success ? TaskStatus.Success : TaskStatus.Failed;
            CompletedYear  = clock.CurrentYear;
            CompletedMonth = clock.CurrentMonth;
            FailureReason  = success ? null : reason;

            var effect = CreateEffect(chr, success, clock, rng);
            if (effect is not null) sink.Publish(effect);

            OnFinished(chr, success);

            // Merit is still awarded only on success (as you had)
            chr.CompleteAssignedTask(success, RewardMerit);
        }

        /// <summary>Domain-specific success roll.</summary>
        protected abstract bool EvaluateSuccess(Character chr, IRandomService rng, out string? failureReason);

        /// <summary>Translate the finished task into a serializable effect payload.</summary>
        protected abstract TaskEffect? CreateEffect(Character chr, bool success, IGameClockService clock, IRandomService rng);

        protected virtual void OnAssigned(Character chr) { }
        protected virtual void OnFinished(Character chr, bool success) { }

        public void Abort()
        {
            if (Status is TaskStatus.Active or TaskStatus.Pending)
                Status = TaskStatus.Aborted;
        }
    }
}
