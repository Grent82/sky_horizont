using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Services;

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

        public CharacterLifecycleService(
            ICharacterRepository characters,
            ILineageRepository lineage,
            IGameClockService clock,
            IRandomService rng,
            IMortalityModel mortality,
            INameGenerator names,
            IPersonalityInheritanceService inherit)
        {
            _characters = characters;
            _lineage = lineage;
            _clock = clock;
            _rng = rng;
            _mortality = mortality;
            _names = names;
            _inherit = inherit;
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
                    // DomainEvents.Raise(new BirthdayOccurred(c.Id)); // ToDo DomainEvents
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
                // ToDo: Twin chance? complications? (policy-driven)
                var child = CreateNewborn(mother, preg.FatherId);
                _characters.Save(child);

                // Wire lineage
                var childLine = _lineage.FindByChildId(child.Id) ?? new EntityLineage(child.Id);
                childLine.SetBiologicalParents(preg.FatherId, mother.Id);
                _lineage.Upsert(childLine);

                mother.EndPregnancy(PregnancyStatus.Delivered);
                mother.ClearPregnancy();

                // DomainEvents.Raise(new ChildBorn(child.Id, mother.Id, preg.FatherId));
            }
        }

        private Character CreateNewborn(Character mother, Guid? fatherId)
        {
            // newborn birth month = current month; birth year = current year
            var babyId = Guid.NewGuid();
            var sex = _rng.NextDouble() < 0.5 ? Sex.Male : Sex.Female;
            var name = _names.GenerateFullName(sex); // ToDo: Mother name (surname), faction/culture

            var father = fatherId.HasValue ? _characters.GetById(fatherId.Value) : null;
            var babyPersonality = father != null
                ? _inherit.Inherit(mother.Personality, father.Personality)
                : _inherit.Inherit(mother.Personality, mother.Personality);

            var baby = new Character(
                babyId,
                name,
                0,
                birthYear: _clock.CurrentYear,
                birthMonth: _clock.CurrentMonth,
                sex,
                personality: babyPersonality,
                skills: new SkillSet(0,0,0,0) // ToDo: genetic seeding later
            );

            // link family (optional convenience)
            baby.LinkFamilyMember(mother.Id);
            if (fatherId.HasValue) baby.LinkFamilyMember(fatherId.Value);
            mother.LinkFamilyMember(baby.Id);

            return baby;
        }

        private void HandleMortality(Character character)
        {
            var p = _mortality.GetMonthlyDeathProbability(character.Age, _clock.CurrentMonth);
            if (_rng.NextDouble() < p)
            {
                    character.MarkDead();
            }
        }
    }
}
