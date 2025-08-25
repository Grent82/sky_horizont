using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

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
        private readonly IIntimacyLog _intimacy;
        private readonly IFactionService _factions;

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
            IEventBus events,
            IIntimacyLog intimacy,
            IFactionService factions)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _lineage = lineage ?? throw new ArgumentNullException(nameof(lineage));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _mortality = mortality ?? throw new ArgumentNullException(nameof(mortality));
            _names = names ?? throw new ArgumentNullException(nameof(names));
            _inherit = inherit ?? throw new ArgumentNullException(nameof(inherit));
            _pregPolicy = pregPolicy ?? throw new ArgumentNullException(nameof(pregPolicy));
            _skillInherit = skillInherit ?? throw new ArgumentNullException(nameof(skillInherit));
            _loc = loc ?? throw new ArgumentNullException(nameof(loc));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _intimacy = intimacy ?? throw new ArgumentNullException(nameof(intimacy));
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
        }

        public void ProcessLifecycleTurn()
        {
            var all = _characters.GetAll().ToList();
            var byId = all.ToDictionary(c => c.Id);

            foreach (var character in all)
            {
                if (!character.IsAlive)
                    continue;

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

        private static (int y, int m) PreviousMonth(int y, int m) => m == 1 ? (y - 1, 12) : (y, m - 1);
        
        private void HandleConception(Character potentialMother, IReadOnlyDictionary<Guid, Character> byId)
        {
            if (potentialMother.Sex != Sex.Female)
                return;
            if (!potentialMother.IsAlive)
                return;
            if (_pregPolicy.IsPostpartumProtected(potentialMother, _clock.CurrentYear, _clock.CurrentMonth))
                return;

            var (yPrev, mPrev) = PreviousMonth(_clock.CurrentYear, _clock.CurrentMonth);

            var partners = _intimacy.GetPartnersForMother(potentialMother.Id, yPrev, mPrev);
            if (partners.Count == 0)
                return;

            foreach (var pid in partners)
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
            double chance = 0.10;

            if (mother.Age < 14)
                return 0.0;
            else if (mother.Age <= 16) chance += 0.04;
            else if (mother.Age <= 20) chance += 0.02;
            else if (mother.Age <= 28) chance += 0.01;
            else if (mother.Age <= 34) chance += 0.00;
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
            var father = fatherId.HasValue && fatherId.Value != Guid.Empty
                ? _characters.GetById(fatherId.Value)
                : null;
            string childSurname = DetermineChildSurname(mother, father);
            string given = _names.GenerateFirstName(sex); // ToDo: By Faction/Clan/House
            string full = $"{given} {childSurname}";

            
            var babyPersonality = father != null
                ? _inherit.Inherit(mother.Personality, father.Personality)
                : _inherit.Inherit(mother.Personality, mother.Personality);

            var babySkills = _skillInherit.Inherit(mother.Skills, father?.Skills, _rng);


            var loc = _loc.GetCharacterLocation(mother.Id);
            (babyPersonality, babySkills) = ApplyBirthVariance(babyPersonality, babySkills, loc);

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

            loc = _loc.GetCharacterLocation(mother.Id);
            if (loc != null)
            {
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
            }

            var motherFactionId = _factions.GetFactionIdForCharacter(mother.Id);
            _factions.MoveCharacterToFaction(babyId, motherFactionId);

            return baby;
        }

        private (Personality, SkillSet) ApplyBirthVariance(Personality p, SkillSet s, CharacterLocation? loc)
        {
            int Clamp(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);
            int Jitter() => _rng.NextInt(-2, 3);

            var p2 = new Personality(
                Clamp(p.Openness + Jitter()),
                Clamp(p.Conscientiousness + Jitter()),
                Clamp(p.Extraversion + Jitter()),
                Clamp(p.Agreeableness + Jitter()),
                Clamp(p.Neuroticism + Jitter())
            );

            var s2 = new SkillSet(
                Clamp(s.Research + Jitter()),
                Clamp(s.Economy + Jitter()),
                Clamp(s.Intelligence + Jitter()),
                Clamp(s.Military + Jitter())
            );

            if (loc != null)
            {
                switch (loc.Kind)
                {
                    case LocationKind.Planet:
                        s2 = s2 with { Economy = Clamp(s2.Economy + _rng.NextInt(1, 4)) };
                        p2 = p2 with { Agreeableness = Clamp(p2.Agreeableness + _rng.NextInt(1, 3)) };
                        break;
                    case LocationKind.Fleet:
                        s2 = s2 with { Military = Clamp(s2.Military + _rng.NextInt(1, 4)) };
                        p2 = p2 with { Conscientiousness = Clamp(p2.Conscientiousness + _rng.NextInt(1, 3)) };
                        break;
                }
            }

            return (p2, s2);
        }

        private string DetermineChildSurname(Character mother, Character? father)
        {
            if (father != null && AreSpouses(mother, father))
            {
                var surname = ExtractSurname(father.Name);
                if (!string.IsNullOrWhiteSpace(surname))
                    return surname;
            }

            var motherSurname = ExtractSurname(mother.Name);
            if (!string.IsNullOrWhiteSpace(motherSurname))
                return motherSurname;

            return _names.GenerateSurname();
        }

        private static bool AreSpouses(Character mother, Character father)
        {
            return mother.Relationships.Any(r => r.TargetCharacterId == father.Id && r.Type == RelationshipType.Spouse)
                || father.Relationships.Any(r => r.TargetCharacterId == mother.Id && r.Type == RelationshipType.Spouse);
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
