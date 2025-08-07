using SkyHorizont.Domain.Entity.Task;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity
{
    public class Commander
    {
        private static readonly Dictionary<Rank, int> MeritThresholds = new()
        {
            { Rank.Lieutenant, 100 },
            { Rank.Captain,    300 },
            { Rank.Major,      700 },
            { Rank.Colonel,    1500 },
            { Rank.General,    3000 },
        };

        private int _balance;

        public Guid Id { get; }
        public string Name { get; private set; }
        public int Age { get; private set; }
        public Sex Sex { get; }
        public Personality Personality { get; }
        public SkillSet Skills { get; private set; }
        public Rank Rank { get; private set; }
        public int Merit { get; private set; }

        private readonly List<Guid> _familyLinkIds = new();
        public IReadOnlyList<Guid> FamilyLinkIds => _familyLinkIds.AsReadOnly();

        public bool IsAssigned => AssignedTask is not null;
        public EntityTask? AssignedTask { get; private set; }
        public int Balance => _balance;

        public Commander(
            Guid id,
            string name,
            int age,
            Sex sex,
            Personality personality,
            SkillSet skills,
            Rank initialRank = Rank.Lieutenant,
            int initialMerit = 0)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Age = age;
            Sex = sex;
            Personality = personality;
            Skills = skills;
            Rank = initialRank;
            Merit = initialMerit;
        }

        public bool AssignTo(EntityTask task)
        {
            if (IsAssigned)
                throw new DomainException($"Commander {Name} is already assigned to a task.");

            if (!CanPerform(task.Type))
                return false;

            if (task.AssignTo(this))
            {
                AssignedTask = task;
                return true;
            }

            return false;
        }

        internal void ReleaseAssignment() => AssignedTask = null;

        public void GainMerit(int amount)
        {
            if (amount <= 0) return;

            Merit += amount;
            while (Merit >= NextMeritThreshold())
            {
                Promote();
            }
        }

        private int NextMeritThreshold() =>
            Rank < Rank.General ? MeritThresholds[Rank] : int.MaxValue;

        private void Promote()
        {
            if (Rank == Rank.Leader) return;
            Rank = Rank + 1;
            // Optional side-effects
        }

        public bool CanPerform(TaskType type)
        {
            return type switch
            {
                TaskType.Research => Skills.Research >= 50,
                TaskType.Espionage => Skills.Intelligence >= 50,
                TaskType.Govern => Skills.Economy >= 30,
                TaskType.Attack => Skills.Military >= 40,
                _ => true
            };
        }

        public void LinkFamilyMember(Guid otherId)
        {
            if (otherId != Id && !_familyLinkIds.Contains(otherId))
                _familyLinkIds.Add(otherId);
        }

        internal void CompleteAssignedTask(bool success, int meritReward)
        {
            if (!IsAssigned)
                throw new DomainException("No task to complete.");
            AssignedTask = null;
            if (success) GainMerit(meritReward);
        }

        public double GetAttackBonus()
            => Skills.Military * 0.001 + Personality.GetAttackBonus();

        public double GetDefenseBonus()
            => Skills.Intelligence * 0.001 + Personality.GetDefenseBonus();

        public double GetRetreatModifier()
            => Personality.GetRetreatModifier();

        public double GetAffectionModifier()
            => Personality.GetAffectionModifier();

        public override string ToString() => $"{Name} (Rank: {Rank}, Merit: {Merit})";

        public void Credit(int amount) => _balance += amount;

        public bool Deduct(int amount)
        {
            if (_balance < amount) return false;
            _balance -= amount;
            return true;
        }
    }
}
