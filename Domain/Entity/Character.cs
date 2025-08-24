using SkyHorizont.Domain.Entity.Task;
using SkyHorizont.Domain.Shared;
using System;
using System.Collections.Generic;

namespace SkyHorizont.Domain.Entity
{
    public enum TraumaType
    {
        None,
        Torture,
        Rape
    }

    public enum CharacterAmbition
    {
        None,
        GainPower,
        BuildWealth,
        EnsureFamilyLegacy,
        SeekAdventure
    }

    public class Character
    {
        private static readonly Dictionary<Rank, int> MeritThresholds = new()
        {
            { Rank.Civilian, 0 },
            { Rank.Courtesan, 100 },
            { Rank.Lieutenant, 100 },
            { Rank.Captain, 300 },
            { Rank.Major, 700 },
            { Rank.Colonel, 1500 },
            { Rank.General, 3000 },
        };

        private int _balance;
        private readonly List<TraumaType> _traumas = new();

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
        public bool IsAssigned => AssignedTask != null;
        public EntityTask? AssignedTask { get; private set; }
        public int Balance => _balance;
        public IReadOnlyList<TraumaType> Traumas => _traumas.AsReadOnly();
        public CharacterAmbition? Ambition { get; set; }

        public Character(
            Guid id,
            string name,
            int age,
            int birthYear,
            int birthMonth,
            Sex sex,
            Personality personality,
            SkillSet skills,
            Rank initialRank = Rank.Civilian,
            int initialMerit = 0)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty or whitespace.", nameof(name));
            Age = age >= 0 ? age : throw new ArgumentException("Age cannot be negative.", nameof(age));
            BirthYear = birthYear;
            BirthMonth = birthMonth > 0 && birthMonth <= 12 ? birthMonth : throw new ArgumentException("Birth month must be between 1 and 12.", nameof(birthMonth));
            Sex = sex;
            Personality = personality ?? throw new ArgumentNullException(nameof(personality));
            Skills = skills ?? throw new ArgumentNullException(nameof(skills));
            Rank = initialRank;
            Merit = initialMerit >= 0 ? initialMerit : throw new ArgumentException("Merit cannot be negative.", nameof(initialMerit));
        }

        public bool AssignTo(EntityTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
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
            if (amount <= 0)
                return;

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
            if (Rank == Rank.Leader)
                return;
            Rank = Rank + 1;
            // TODO: better promotion logic and Publish promotion event?
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
            if (Sex != Sex.Female)
                throw new DomainException("Only female characters can be pregnant.");
            if (ActivePregnancy is { Status: PregnancyStatus.Active })
                throw new DomainException("Already pregnant.");
            ActivePregnancy = Pregnancy.Start(fatherId, conceptionYear, conceptionMonth);
        }

        public void EndPregnancy(PregnancyStatus status)
        {
            if (ActivePregnancy == null)
                return;
            ActivePregnancy = ActivePregnancy.WithStatus(status);
        }

        public void ClearPregnancy() => ActivePregnancy = null;

        #endregion

        public void ApplyTrauma(TraumaType trauma)
        {
            if (trauma != TraumaType.None && !_traumas.Contains(trauma))
                _traumas.Add(trauma);
        }

        public bool HasTrauma(TraumaType trauma) => _traumas.Contains(trauma);

        public void IncreaseAge() => Age++;

        public void MarkDead() => IsAlive = false;

        public void LinkFamilyMember(Guid otherId)
        {
            if (otherId == Guid.Empty)
                throw new ArgumentException("Family member ID cannot be empty.", nameof(otherId));
            if (otherId != Id && !_familyLinkIds.Contains(otherId))
                _familyLinkIds.Add(otherId);
        }

        public void AddRelationship(Guid otherCharacterId, RelationshipType type)
        {
            if (otherCharacterId == Guid.Empty)
                throw new ArgumentException("Other character ID cannot be empty.", nameof(otherCharacterId));
            if (otherCharacterId == Id)
                throw new ArgumentException("Cannot create relationship with self.", nameof(otherCharacterId));
            if (!Relationships.Any(r => r.TargetCharacterId == otherCharacterId))
                Relationships.Add(new CharacterRelationship(otherCharacterId, type));
        }

        public void RemoveRelationship(Guid otherCharacterId)
        {
            if (otherCharacterId == Guid.Empty)
                throw new ArgumentException("Other character ID cannot be empty.", nameof(otherCharacterId));
            Relationships.RemoveAll(r => r.TargetCharacterId == otherCharacterId);
        }

        internal void CompleteAssignedTask(bool success, int meritReward)
        {
            if (!IsAssigned)
                throw new DomainException("No task to complete.");
            AssignedTask = null;
            if (success)
                GainMerit(meritReward);
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

        public void Credit(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Credit amount cannot be negative.", nameof(amount));
            _balance += amount;
        }

        public bool Deduct(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Deduct amount cannot be negative.", nameof(amount));
            if (_balance < amount)
                return false;
            _balance -= amount;
            return true;
        }
    }
}