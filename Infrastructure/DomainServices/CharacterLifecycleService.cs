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
        private readonly ILocationService _loc;
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
            ILocationService loc,
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
            _loc = loc;
            _events = events;
        }

        public void ProcessLifecycleTurn()
        {
            var all = _characters.GetAll().ToList();
            var byId = all.ToDictionary(c => c.Id);

            foreach (var character in all)
            {
                if (!character.IsAlive)
                    continue;

                // Birthday month → increment age
                if (character.BirthMonth == _clock.CurrentMonth && _clock.CurrentYear > character.BirthYear)
                {
                    character.IncreaseAge();
                    _events.Publish(new BirthdayOccurred(character.Id, _clock.CurrentYear, _clock.CurrentMonth));
                }

                HandleConception(character, byId);

                HandlePregnancy(character);

                HandleMortality(character);

                _characters.Save(character);
            }
        }
        
        private void HandleConception(Character potentialMother, IReadOnlyDictionary<Guid, Character> byId)
        {
            if (potentialMother.Sex != Sex.Female)
                return;
            if (!potentialMother.IsAlive)
                return;
            if (_pregPolicy.IsPostpartumProtected(potentialMother, _clock.CurrentYear, _clock.CurrentMonth))
                return;

            // Find consensual partners: Lovers/Spouse links
            var partnerIds = potentialMother.Relationships
                .Where(r => r.Type == RelationshipType.Lover || r.Type == RelationshipType.Spouse)
                .Select(r => r.TargetCharacterId)
                .Distinct()
                .ToList();

            if (partnerIds.Count == 0)
                return;

            foreach (var pid in partnerIds)
            {
                byId.TryGetValue(pid, out var partner);
                if (partner is null)
                    continue;

                if (!_pregPolicy.CanConceiveWith(potentialMother, partner, _clock.CurrentYear, _clock.CurrentMonth))
                    continue;

                var chance = ComputeMonthlyConceptionChance(potentialMother, partner);
                if (_rng.NextDouble() < chance)
                {
                    potentialMother.StartPregnancy(partner.Id, _clock.CurrentYear, _clock.CurrentMonth);
                    _events.Publish(new DomainEventLog("Conception", potentialMother.Id, $"partner={partner.Id}; year={_clock.CurrentYear}; month={_clock.CurrentMonth}"));
                    break;
                }
            }
        }

        private double ComputeMonthlyConceptionChance(Character mother, Character partner)
        {
            double chance = 0.10; // 10% baseline per month for active couples

            if (mother.Age < 14)
                return 0.0;
            else if (mother.Age <= 16) chance += 0.04;
            else if (mother.Age <= 20) chance += 0.02;
            else if (mother.Age <= 28) chance += 0.01;
            else if (mother.Age <= 34) chance += 0.00;   // baseline
            else if (mother.Age <= 40) chance -= 0.03;
            else if (mother.Age <= 45) chance -= 0.06;
            else chance -= 0.10;

            chance += (mother.Personality.Agreeableness - 50) * 0.0008;
            chance += (mother.Personality.Extraversion  - 50) * 0.0008;
            chance -= (mother.Personality.Conscientiousness - 50) * 0.0006;

            chance += (_rng.NextDouble() - 0.5) * 0.02;

            if (chance < 0.0)
                chance = 0.0;
            if (chance > 0.30)
                chance = 0.30;
            return chance;
        }


        private void HandlePregnancy(Character mother)
        {
            if (mother.Sex != Sex.Female || mother.ActivePregnancy is null)
                return;

            var preg = mother.ActivePregnancy;
            if (preg.Status != PregnancyStatus.Active)
                return;

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

                Character? child2 = null;
                if (twins)
                {
                    child2 = CreateNewborn(mother, preg.FatherId);
                    _characters.Save(child2);
                    WireLineage(child2, mother, preg.FatherId);
                }

                mother.EndPregnancy(PregnancyStatus.Delivered);
                mother.ClearPregnancy();

                _events.Publish(new ChildBorn(child1.Id, mother.Id, preg.FatherId, _clock.CurrentYear, _clock.CurrentMonth));
                if (twins)
                    _events.Publish(new ChildBorn(child2!.Id, mother.Id, preg.FatherId, _clock.CurrentYear, _clock.CurrentMonth));

                _pregPolicy.RecordDelivery(mother.Id, _clock.CurrentYear, _clock.CurrentMonth);
            }
        }

        private Character CreateNewborn(Character mother, Guid? fatherId)
        {
            var babyId = Guid.NewGuid();
            var sex = _rng.NextDouble() < 0.5 ? Sex.Male : Sex.Female;
            var father = fatherId.HasValue ? _characters.GetById(fatherId.Value) : null;
            string childSurname = father != null ? ExtractSurname(father.Name) : ExtractSurname(mother.Name); // ToDo: if mother singel then get mother name
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
                _characters.Save(father);
            }
            _characters.Save(mother);

            var loc = _loc.GetCharacterLocation(mother.Id);
            switch (loc.Kind)
            {
                case LocationKind.Planet:
                    _loc.AddCitizenToPlanet(baby.Id, loc.HostId);
                    break;
                case LocationKind.Fleet:
                    _loc.AddPassengerToFleet(baby.Id, loc.HostId);
                    break;
                default:
                    _loc.StageAtHolding(baby.Id, loc.HostId);
                    break;
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
                if (character.Sex == Sex.Female && character.ActivePregnancy is { Status: PregnancyStatus.Active } preg)
                {
                    _events.Publish(new DomainEventLog("PregnancyTerminatedByDeath", character.Id, $"Father={preg.FatherId}"));
                    character.ClearPregnancy();
                }
            }
        }
        
        private string ExtractSurname(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "Doe";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[^1] : parts[0];
        }
    }
}
