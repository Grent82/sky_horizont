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

        private readonly int _elderlyAge = 95;

        public CharacterLifecycleService(ICharacterRepository characters,
                                         ILineageRepository lineage,
                                         IGameClockService clock)
        {
            _characters = characters;
            _lineage = lineage;
            _clock = clock;
        }

        public void ProcessLifecycleTurn()
        {
            var all = _characters.GetAll().ToList();

            foreach (var character in _characters.GetAll().ToList())
            {
                if (!character.IsAlive) continue;

                // Birthday month → increment age
                if (character.BirthMonth == _clock.CurrentMonth)
                    character.IncreaseAge();

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
                // Child creation (eventual consistency acceptable)
                var child = CreateNewborn(mother, preg.FatherId);
                _characters.Save(child);

                // Wire lineage
                var childLine = _lineage.FindByChildId(child.Id) ?? new EntityLineage(child.Id);
                childLine.SetBiologicalParents(preg.FatherId, mother.Id);
                _lineage.Upsert(childLine);

                mother.EndPregnancy(PregnancyStatus.Delivered);
                mother.ClearPregnancy();
            }
        }

        private Character CreateNewborn(Character mother, Guid fatherId)
        {
            // newborn birth month = current month; birth year = current year
            var babyId = Guid.NewGuid();
            var name = $"Newborn-{babyId.ToString()[..6]}"; // ToDo
            var sex = Random.Shared.Next(0, 2) == 0 ? Sex.Male : Sex.Female;

            // Inherit personality faintly or randomize – simple placeholder
            var p = mother.Personality with { }; // ToDo

            var baby = new Character(
                babyId,
                name,
                0,
                birthYear: _clock.CurrentYear,
                birthMonth: _clock.CurrentMonth,
                sex,
                personality: p,
                skills: new SkillSet(0,0,0,0) // ToDo: genetic seeding later
            );

            // link family (optional convenience)
            baby.LinkFamilyMember(mother.Id);
            baby.LinkFamilyMember(fatherId);
            mother.LinkFamilyMember(baby.Id);

            return baby;
        }

        private void HandleMortality(Character character)
        {
            // Placeholder: very simple age-based mortality trigger
            if (character.Age > _elderlyAge)
            {
                // 2% monthly chance after threshold
                if (Random.Shared.NextDouble() < 0.02)
                    character.MarkDead();
            }
        }
    }
}
