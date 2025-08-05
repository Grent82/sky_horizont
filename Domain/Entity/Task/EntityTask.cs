using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity.Task
{
    public abstract class EntityTask
    {
        public Guid Id { get; }
        public TaskType Type { get; }
        public Guid AssignedCommanderId { get; private set; }
        public Guid TargetId { get; }
        public TaskStatus Status { get; private set; }
        public double Progress { get; private set; } // 0.0–1.0
        public int RewardMerit { get; }

        protected EntityTask(Guid id, TaskType type, Guid targetId, int rewardMerit)
        {
            Id = id;
            Type = type;
            TargetId = targetId;
            RewardMerit = rewardMerit;
            Status = TaskStatus.Pending;
        }

        internal bool AssignTo(Commander cmd)
        {
            if (!cmd.CanPerform(Type))
                return false;

            AssignedCommanderId = cmd.Id;
            Status = TaskStatus.Active;
            OnAssigned(cmd);
            return true;
        }

        public void Tick(double delta)
        {
            if (Status != TaskStatus.Active)
                throw new DomainException("Task is not active.");

            double increment = delta * SpeedFactor();
            Progress = Math.Min(1.0, Progress + increment);

            if (Progress >= 1.0)
                Finish();
        }

        protected virtual double SpeedFactor() => 0.01;

        private void Finish()
        {
            bool success = EvaluateSuccess();
            Status = success ? TaskStatus.Success : TaskStatus.Failed;
            OnFinished(success);
        }

        public void Abort()
        {
            if (Status == TaskStatus.Active || Status == TaskStatus.Pending)
                Status = TaskStatus.Aborted;
        }

        protected virtual bool EvaluateSuccess() =>
            true;  // default: always succeed

        protected virtual void OnAssigned(Commander cmd) { }

        protected abstract void OnFinished(bool success);
    }

    public class ResearchTask : EntityTask
    {
        private Commander? _commander;
        public ResearchTask(Guid id, Guid planetId, int rewardMerit = 20)
            : base(id, TaskType.Research, planetId, rewardMerit) { }

        protected override double SpeedFactor() => 0.02;

        protected override bool EvaluateSuccess()
        {
            if (_commander == null)
                throw new DomainException("Task not assigned.");

            // succeed if research + loyalty × random factor beats threshold
            double threshold = 50.0;
            double roll = _commander.Skills.Research + (_commander.Personality.Loyalty / 10.0)
                          - new Random().NextDouble() * 20.0;
            return roll >= threshold;
        }

        protected override void OnAssigned(Commander cmd)
        {
            _commander = cmd;
        }

        protected override void OnFinished(bool success)
        {
            if (_commander != null)
                _commander.CompleteAssignedTask(success, RewardMerit);
        }
    }

    public class EspionageTask : EntityTask
    {
        private Commander? _commander;
        public EspionageTask(Guid id, Guid targetFactionOrPlanet, int rewardMerit = 30)
            : base(id, TaskType.Espionage, targetFactionOrPlanet, rewardMerit) { }

        protected override double SpeedFactor() => 0.015;

        protected override bool EvaluateSuccess()
        {
            if (_commander == null)
                throw new DomainException("Task not assigned.");
            double roll = _commander.Skills.Intelligence * 1.2
                          + (_commander.Personality.Boldness * 1.1)
                          - new Random().NextDouble() * 30.0;
            return roll >= 60.0;
        }

        protected override void OnAssigned(Commander cmd)
        {
            _commander = cmd;
        }

        protected override void OnFinished(bool success)
        {
            if (_commander != null)
                _commander.CompleteAssignedTask(success, RewardMerit);
        }
    }

    public class GovernTask : EntityTask
    {
        private Commander? _commander;
        public GovernTask(Guid id, Guid planetId, int rewardMerit = 25)
            : base(id, TaskType.Govern, planetId, rewardMerit) { }

        protected override double SpeedFactor() => 0.03;

        protected override bool EvaluateSuccess()
        {
            if (_commander == null)
                throw new DomainException("Task not assigned.");

            double roll = _commander.Skills.Economy * 1.1
                          + new Random().NextDouble() * 10.0;
            return roll >= 40.0;
        }

        protected override void OnAssigned(Commander cmd)
        {
            _commander = cmd;
        }

        protected override void OnFinished(bool success)
        {
            if (_commander != null)
                _commander.CompleteAssignedTask(success, RewardMerit);
        }
    }
}
