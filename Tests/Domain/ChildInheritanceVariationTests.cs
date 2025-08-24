using System;
using System.Reflection;
using FluentAssertions;
using Xunit;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence.Repositories;
using SkyHorizont.Infrastructure.Persistence;

namespace SkyHorizont.Tests.Domain
{
    public class ChildInheritanceVariationTests
    {
        [Fact]
        public void BirthLocationInfluencesTraits()
        {
            var seed = 123;
            var fleetService = BuildService(new RandomService(seed), new FixedLocationService(new CharacterLocation(LocationKind.Fleet, Guid.NewGuid(), Guid.NewGuid())));
            var planetService = BuildService(new RandomService(seed), new FixedLocationService(new CharacterLocation(LocationKind.Planet, Guid.NewGuid(), Guid.NewGuid())));

            var motherFleet = CreateMother();
            var motherPlanet = CreateMother();

            var method = typeof(CharacterLifecycleService).GetMethod("CreateNewborn", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var fleetBaby = (Character)method.Invoke(fleetService, new object?[] { motherFleet, null })!;
            var planetBaby = (Character)method.Invoke(planetService, new object?[] { motherPlanet, null })!;

            fleetBaby.Skills.Military.Should().BeGreaterThan(planetBaby.Skills.Military);
            planetBaby.Skills.Economy.Should().BeGreaterThan(fleetBaby.Skills.Economy);

            fleetBaby.Personality.Conscientiousness.Should().BeGreaterThan(planetBaby.Personality.Conscientiousness);
            planetBaby.Personality.Agreeableness.Should().BeGreaterThan(fleetBaby.Personality.Agreeableness);
        }

        private static Character CreateMother()
        {
            return new Character(
                Guid.NewGuid(),
                "Test Mother",
                age: 25,
                birthYear: 2975,
                birthMonth: 1,
                sex: Sex.Female,
                personality: new Personality(50, 50, 50, 50, 50),
                skills: new SkillSet(50, 50, 50, 50));
        }

        private static CharacterLifecycleService BuildService(IRandomService rng, ILocationService loc)
        {
            var characters = new CharactersRepository(new InMemoryCharactersDbContext());
            var lineage = new LineageRepository(new InMemoryLinageDbContext());
            var clock = new GameClockService(3001, 1, 1);
            var mortality = new GompertzMortalityModel();
            var names = new NameGenerator(rng);
            var inherit = new SimplePersonalityInheritanceService(rng);
            var pregPolicy = new DummyPregnancyPolicy();
            var skillInherit = new SimpleSkillInheritanceService();
            var events = new DummyEventBus();
            var intimacy = new DummyIntimacyLog();

            return new CharacterLifecycleService(
                characters, lineage, clock, rng, mortality, names, inherit,
                pregPolicy, skillInherit, loc, events, intimacy);
        }

        private sealed class DummyPregnancyPolicy : IPregnancyPolicy
        {
            public bool ShouldHaveTwins(Character mother, int year, int month) => false;
            public bool ShouldHaveComplications(Character mother, int year, int month, out string? note) { note = null; return false; }
            public bool IsPostpartumProtected(Character mother, int year, int month) => false;
            public void RecordDelivery(Guid motherId, int year, int month) { }
            public bool CanConceiveWith(Character potentialMother, Character partner, int year, int month) => false;
        }

        private sealed class DummyEventBus : IEventBus
        {
            public void Publish<T>(T @event) { }
        }

        private sealed class DummyIntimacyLog : IIntimacyLog
        {
            public void RecordIntimacyEncounter(Guid charA, Guid charB, int year, int month) { }
            public IReadOnlyList<Guid> GetPartnersForMother(Guid motherId, int year, int month) => Array.Empty<Guid>();
            public void PurgeOlderThan(int year, int month) { }
        }

        private sealed class FixedLocationService : ILocationService
        {
            private readonly CharacterLocation _loc;
            public FixedLocationService(CharacterLocation loc) => _loc = loc;
            public CharacterLocation? GetCharacterLocation(Guid characterId) => _loc;
            public bool AreCoLocated(Guid characterA, Guid characterB) => false;
            public bool AreInSameSystem(Guid characterA, Guid characterB) => false;
            public IEnumerable<Guid> GetCharactersOnPlanet(Guid planetId) => Array.Empty<Guid>();
            public IEnumerable<Guid> GetCharactersOnFleet(Guid fleetId) => Array.Empty<Guid>();
            public IEnumerable<Guid> GetCaptivesOnPlanet(Guid planetId) => Array.Empty<Guid>();
            public IEnumerable<Guid> GetCaptivesOnFleet(Guid fleetId) => Array.Empty<Guid>();
            public void AddCitizenToPlanet(Guid character, Guid locationId) { }
            public void AddPassengerToFleet(Guid character, Guid fleetId) { }
            public void StageAtHolding(Guid character, Guid fleetId) { }
            public bool IsPrisonerOf(Guid prisonerId, Guid captorId) => false;
        }
    }
}
