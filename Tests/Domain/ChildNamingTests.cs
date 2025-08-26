using System.Reflection;
using FluentAssertions;
using Xunit;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Testing;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Application.Turns;

namespace SkyHorizont.Tests.Domain
{
    public class ChildNamingTests
    {
        [Fact]
        public void FatherSurnameUsed_WhenParentsAreSpouses()
        {
            var service = BuildService();
            var mother = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Aeva Bright", Sex.Female, 22, 2979, 6);
            var father = CharacterFactory.CreateSuperNegative(Guid.NewGuid(), "Vor Drak", Sex.Male, 24, 2977, 3);

            mother.AddRelationship(father.Id, RelationshipType.Spouse);
            father.AddRelationship(mother.Id, RelationshipType.Spouse);

            var method = typeof(CharacterLifecycleService).GetMethod("DetermineChildSurname", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var surname = (string)method.Invoke(service, new object[] { mother, father })!;

            surname.Should().Be("Drak");
        }

        [Fact]
        public void MotherSurnameUsed_WhenFatherNotSpouse()
        {
            var service = BuildService();
            var mother = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Aeva Bright", Sex.Female, 22, 2979, 6);
            var father = CharacterFactory.CreateSuperNegative(Guid.NewGuid(), "Vor Drak", Sex.Male, 24, 2977, 3);

            mother.AddRelationship(father.Id, RelationshipType.Lover);
            father.AddRelationship(mother.Id, RelationshipType.Lover);

            var method = typeof(CharacterLifecycleService).GetMethod("DetermineChildSurname", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var surname = (string)method.Invoke(service, new object[] { mother, father })!;

            surname.Should().Be("Bright");
        }

        [Fact]
        public void MotherSurnameUsed_WhenFatherMissing()
        {
            var service = BuildService();
            var mother = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Aeva Bright", Sex.Female, 22, 2979, 6);

            var method = typeof(CharacterLifecycleService).GetMethod("DetermineChildSurname", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var surname = (string)method.Invoke(service, new object[] { mother, null })!;

            surname.Should().Be("Bright");
        }

        private static CharacterLifecycleService BuildService()
        {
            var characters = new CharactersRepository(new InMemoryCharactersDbContext());
            var lineage = new LineageRepository(new InMemoryLinageDbContext());
            var clock = new GameClockService(3001, 1, 1);
            var rng = new RandomService(123);
            var mortality = new GompertzMortalityModel();
            var names = new NameGenerator(rng);
            var inherit = new SimplePersonalityInheritanceService(rng);
            var pregPolicy = new DummyPregnancyPolicy();
            var skillInherit = new SimpleSkillInheritanceService();
            var loc = new DummyLocationService();
            var events = new DummyEventBus();
            var intimacy = new DummyIntimacyLog();

            return new CharacterLifecycleService(
                characters, lineage, clock, rng, mortality, names, inherit,
                pregPolicy, skillInherit, loc, events, intimacy, new DummyFactionService());
        }

        private sealed class DummyPregnancyPolicy : IPregnancyPolicy
        {
            public bool ShouldHaveTwins(Character mother, int year, int month) => false;
            public bool ShouldHaveComplications(Character mother, int year, int month, out string? note) { note = null; return false; }
            public bool IsPostpartumProtected(Character mother, int year, int month) => false;
            public void RecordDelivery(Guid motherId, int year, int month) { }
            public bool CanConceiveWith(Character potentialMother, Character partner, int year, int month) => false;
        }

        private sealed class DummyLocationService : ILocationService
        {
            public CharacterLocation? GetCharacterLocation(Guid characterId) => null;
            public bool AreCoLocated(Guid characterA, Guid characterB) => false;
            public bool AreInSameSystem(Guid characterA, Guid characterB) => false;
            public IEnumerable<Guid> GetCharactersOnPlanet(Guid planetId) => Array.Empty<Guid>();
            public IEnumerable<Guid> GetCharactersOnFleet(Guid fleetId) => Array.Empty<Guid>();
            public IEnumerable<Guid> GetCaptivesOnPlanet(Guid planetId) => Array.Empty<Guid>();
            public IEnumerable<Guid> GetCaptivesOnFleet(Guid fleetId) => Array.Empty<Guid>();
            public void AddCitizenToPlanet(Guid character, Guid locationId) { }
            public void AddPassengerToFleet(Guid character, Guid fleetId) { }
            public void StageAtHolding(Guid character, Guid locationId) { }
            public bool IsPrisonerOf(Guid prisonerId, Guid captorId) => false;
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

        private sealed class DummyFactionService : IFactionService
        {
            public Guid GetFactionIdForCharacter(Guid characterId) => Guid.Empty;
            public Guid GetFactionIdForPlanet(Guid planetId) => Guid.Empty;
            public Guid GetFactionIdForSystem(Guid systemId) => Guid.Empty;
            public Guid? GetLeaderId(Guid factionId) => null;
            public bool IsAtWar(Guid a, Guid b) => false;
            public IEnumerable<Guid> GetAllRivalFactions(Guid forFaction) => Array.Empty<Guid>();
            public bool HasAlliance(Guid factionA, Guid factionB) => false;
            public int GetEconomicStrength(Guid factionId) => 0;
            public void MoveCharacterToFaction(Guid characterId, Guid newFactionId) { }
            public void Save(Faction faction) { }
            public Faction GetFaction(Guid factionId) => new Faction(factionId, "Dummy", Guid.Empty);
        }
    }
}
