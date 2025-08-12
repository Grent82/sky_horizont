using SkyHorizont.Domain.Entity.Task;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity
{
    public class Character
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
        public int BirthYear { get; }
        public int BirthMonth { get; }
        public int Age { get; private set; } = 0;
        public bool IsAlive { get; private set; } = true;
        public Sex Sex { get; }
        public Personality Personality { get; }
        public SkillSet Skills { get; private set; }
        public Rank Rank { get; private set; }
        public int Merit { get; private set; }

        public List<CharacterRelationship> Relationships { get; } = new();

        public Pregnancy? ActivePregnancy { get; private set; }

        private readonly List<Guid> _familyLinkIds = new();
        public IReadOnlyList<Guid> FamilyLinkIds => _familyLinkIds.AsReadOnly();

        public bool IsAssigned => AssignedTask is not null;
        public EntityTask? AssignedTask { get; private set; }
        public int Balance => _balance;

        public Character(
            Guid id,
            string name,
            int age, int birthYear, int birthMonth,
            Sex sex,
            Personality personality,
            SkillSet skills,
            Rank initialRank = Rank.Civilian,
            int initialMerit = 0)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Age = age;
            BirthYear = birthYear;
            BirthMonth = birthMonth;
            Sex = sex;
            Personality = personality;
            Skills = skills;
            Rank = initialRank;
            Merit = initialMerit;
        }

        public bool AssignTo(EntityTask task)
        {
            if (IsAssigned)
                throw new DomainException($"Character {Name} is already assigned to a task.");

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
            // Todo: side-effects
            // ToDo: events? is this thrigth place for promote

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

        #region Pregnancy control

        public void StartPregnancy(Guid fatherId, int conceptionYear, int conceptionMonth)
        {
            if (Sex != Sex.Female) throw new DomainException("Only female characters can be pregnant.");
            if (ActivePregnancy is { Status: PregnancyStatus.Active })
                throw new DomainException("Already pregnant.");
            ActivePregnancy = Pregnancy.Start(fatherId, conceptionYear, conceptionMonth);
        }

        public void EndPregnancy(PregnancyStatus status)
        {
            if (ActivePregnancy is null) return;
            ActivePregnancy = ActivePregnancy.WithStatus(status);
        }

        public void ClearPregnancy() => ActivePregnancy = null;

        #endregion

        public void IncreaseAge() => Age++;
        public void MarkDead() => IsAlive = false;

        public void LinkFamilyMember(Guid otherId)
        {
            if (otherId != Id && !_familyLinkIds.Contains(otherId))
                _familyLinkIds.Add(otherId);
        }

        public void AddRelationship(Guid otherCharacterId, RelationshipType type)
        {
            if (!Relationships.Any(r => r.TargetCharacterId == otherCharacterId))
                Relationships.Add(new CharacterRelationship(otherCharacterId, type));
        }

        public void RemoveRelationship(Guid otherCharacterId)
        {
            Relationships.RemoveAll(r => r.TargetCharacterId == otherCharacterId);
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
