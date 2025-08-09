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

        protected EntityTask(Guid id, TaskType type, Guid targetId, int rewardMerit)
        {
            Id = id;
            Type = type;
            TargetId = targetId;
            RewardMerit = rewardMerit;
            Status = TaskStatus.Pending;
        }

        internal bool AssignTo(Character cmd)
        {
            if (!cmd.CanPerform(Type))
                return false;

            AssignedCharacterId = cmd.Id;
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

        protected virtual void OnAssigned(Character cmd) { }

        protected abstract void OnFinished(bool success);
    }

    public class ResearchTask : EntityTask
    {
        private Character? _character;
        public ResearchTask(Guid id, Guid planetId, int rewardMerit = 20)
            : base(id, TaskType.Research, planetId, rewardMerit) { }

        protected override double SpeedFactor() => 0.02;

        protected override bool EvaluateSuccess()
        {
            if (_character == null)
                throw new DomainException("Task not assigned.");

            // succeed if research + loyalty × random factor beats threshold
            double threshold = 50.0;
            double roll = _character.Skills.Research/* + (_character.Personality.Loyalty / 10.0)
                          - new Random().NextDouble() * 20.0*/;
            return roll >= threshold;
        }

        protected override void OnAssigned(Character cmd)
        {
            _character = cmd;
        }

        protected override void OnFinished(bool success)
        {
            if (_character != null)
                _character.CompleteAssignedTask(success, RewardMerit);
        }
    }

    public class EspionageTask : EntityTask
    {
        private Character? _character;
        public EspionageTask(Guid id, Guid targetFactionOrPlanet, int rewardMerit = 30)
            : base(id, TaskType.Espionage, targetFactionOrPlanet, rewardMerit) { }

        protected override double SpeedFactor() => 0.015;

        protected override bool EvaluateSuccess()
        {
            if (_character == null)
                throw new DomainException("Task not assigned.");
            double roll = _character.Skills.Intelligence/* * 1.2
                          + (_character.Personality.Boldness * 1.1)
                          - new Random().NextDouble() * 30.0*/;
            return roll >= 60.0;
        }

        protected override void OnAssigned(Character cmd)
        {
            _character = cmd;
        }

        protected override void OnFinished(bool success)
        {
            if (_character != null)
                _character.CompleteAssignedTask(success, RewardMerit);
        }
    }

    public class GovernTask : EntityTask
    {
        private Character? _character;
        public GovernTask(Guid id, Guid planetId, int rewardMerit = 25)
            : base(id, TaskType.Govern, planetId, rewardMerit) { }

        protected override double SpeedFactor() => 0.03;

        protected override bool EvaluateSuccess()
        {
            if (_character == null)
                throw new DomainException("Task not assigned.");

            double roll = _character.Skills.Economy * 1.1
                          + new Random().NextDouble() * 10.0;
            return roll >= 40.0;
        }

        protected override void OnAssigned(Character cmd)
        {
            _character = cmd;
        }

        protected override void OnFinished(bool success)
        {
            if (_character != null)
                _character.CompleteAssignedTask(success, RewardMerit);
        }
    }
}
