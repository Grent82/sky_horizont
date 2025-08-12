using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class CharacterLifecycleService : ICharacterLifecycleService
    {
        private readonly ICharacterRepository _characters;
        private readonly ILineageRepository _lineage;
        private readonly IGameClockService _clock;
        private readonly IRandomService _rng;
        private readonly IMortalityModel _mortality;
        private readonly INameGenerator _names;
        private readonly IPersonalityInheritanceService _inherit;
        private readonly IPregnancyPolicy _pregPolicy;
        private readonly ISkillInheritanceService _skillInherit;
        private readonly IEventBus _events;

        public CharacterLifecycleService(
            ICharacterRepository characters,
            ILineageRepository lineage,
            IGameClockService clock,
            IRandomService rng,
            IMortalityModel mortality,
            INameGenerator names,
            IPersonalityInheritanceService inherit,
            IPregnancyPolicy pregPolicy,
            ISkillInheritanceService skillInherit,
            IEventBus events)
        {
            _characters = characters;
            _lineage = lineage;
            _clock = clock;
            _rng = rng;
            _mortality = mortality;
            _names = names;
            _inherit = inherit;
            _pregPolicy = pregPolicy;
            _skillInherit = skillInherit;
            _events = events;
        }

        public void ProcessLifecycleTurn()
        {
            var all = _characters.GetAll().ToList();

            foreach (var character in _characters.GetAll().ToList())
            {
                if (!character.IsAlive) continue;

                // Birthday month → increment age
                if (character.BirthMonth == _clock.CurrentMonth && _clock.CurrentYear > character.BirthYear)
                {
                    character.IncreaseAge();
                    _events.Publish(new BirthdayOccurred(character.Id, _clock.CurrentYear, _clock.CurrentMonth));
                }


                HandlePregnancy(character);

                HandleMortality(character);

                _characters.Save(character);
            }
        }

        private void HandlePregnancy(Character mother)
        {
            if (mother.Sex != Sex.Female || mother.ActivePregnancy is null) return;

            var preg = mother.ActivePregnancy;
            if (preg.Status != PregnancyStatus.Active) return;

            if (preg.IsDue(_clock.CurrentYear, _clock.CurrentMonth, _clock.MonthsPerYear))
            {
                if (_pregPolicy.ShouldHaveComplications(mother, _clock.CurrentYear, _clock.CurrentMonth, out var note))
                {
                    _events.Publish(new DomainEventLog("BirthComplication", mother.Id, note));
                }

                bool twins = _pregPolicy.ShouldHaveTwins(mother, _clock.CurrentYear, _clock.CurrentMonth);
                
                var child1 = CreateNewborn(mother, preg.FatherId);
                _characters.Save(child1);
                WireLineage(child1, mother, preg.FatherId);

                if (twins)
                {
                    var child2 = CreateNewborn(mother, preg.FatherId);
                    _characters.Save(child2);
                    WireLineage(child2, mother, preg.FatherId);
                }

                mother.EndPregnancy(PregnancyStatus.Delivered);
                mother.ClearPregnancy();

                _events.Publish(new ChildBorn(child1.Id, mother.Id, preg.FatherId, _clock.CurrentYear, _clock.CurrentMonth));
                if (twins)
                    _events.Publish(new ChildBorn(child1.Id, mother.Id, preg.FatherId, _clock.CurrentYear, _clock.CurrentMonth));
            }
        }

        private Character CreateNewborn(Character mother, Guid? fatherId)
        {
            var babyId = Guid.NewGuid();
            var sex = _rng.NextDouble() < 0.5 ? Sex.Male : Sex.Female;
            var father = fatherId.HasValue ? _characters.GetById(fatherId.Value) : null;
            string childSurname = father != null ? ExtractSurname(father.Name) : ExtractSurname(mother.Name);
            string given = _names.GenerateFirstName(sex); // ToDo: By Faction/Clan/House
            string full = $"{given} {childSurname}";

            
            var babyPersonality = father != null
                ? _inherit.Inherit(mother.Personality, father.Personality)
                : _inherit.Inherit(mother.Personality, mother.Personality);

            var babySkills = _skillInherit.Inherit(mother.Skills, father?.Skills, _rng);

            var baby = new Character(
                babyId,
                full,
                0,
                birthYear: _clock.CurrentYear,
                birthMonth: _clock.CurrentMonth,
                sex,
                personality: babyPersonality,
                skills: babySkills
            );

            baby.LinkFamilyMember(mother.Id);
            mother.LinkFamilyMember(baby.Id);
            if (father != null)
            {
                baby.LinkFamilyMember(father.Id);
                father.LinkFamilyMember(baby.Id);
            }
            return baby;
        }

        private void WireLineage(Character child, Character mother, Guid? fatherId)
        {
            var childLine = _lineage.FindByChildId(child.Id) ?? new EntityLineage(child.Id);
            childLine.SetBiologicalParents(fatherId, mother.Id);
            _lineage.Upsert(childLine);
        }

        private void HandleMortality(Character character)
        {
            var p = _mortality.GetMonthlyDeathProbability(character.Age, _clock.CurrentMonth);
            if (_rng.NextDouble() < p)
            {
                character.MarkDead();
            }
        }
        
        private string ExtractSurname(string fullName)
        {
            var parts = fullName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return (parts is { Length: > 1 }) ? parts[^1] : fullName ?? "Doe";
        }
    }
}
