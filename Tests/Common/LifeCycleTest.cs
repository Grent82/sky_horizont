using FluentAssertions;
using SkyHorizont.Application.Turns;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Persistence.Diplomacy;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Infrastructure.Social;
using SkyHorizont.Infrastructure.Testing;
using Xunit;

namespace SkyHorizont.Tests.Common
{
    public class LifecycleTests
    {
        [Fact]
        public void Common()
        {
            // somewhere in your test project
            // (using your real in-memory repos / stubs for services you already have)
            var characters = new CharactersRepository(new InMemoryCharactersDbContext());
            var lineage = new LineageRepository(new InMemoryLinageDbContext());
            var planets = new PlanetsRepository(new InMemoryPlanetsDbContext());
            var fleets = new FleetsRepository(new InMemoryFleetsDbContext());
            var opinions = new OpinionRepository(new InMemoryOpinionsDbContext());
            var factions = new FactionRepository(new InMemoryFactionsDbContext());
            var secrets = new SecretsRepository(new InMemorySecretsDbContext());
            var diplomacies = new DiplomacyRepository(new InMemoryDiplomacyDbContext());
            var socialLog = new InMemorySocialEventLog();

            var clock = new GameClockService(3001, 1, 12);
            var rng = new RandomService(12345);
            var mortality = new GompertzMortalityModel(); // your test implementation
            var nameGen = new NameGenerator(rng);     // you already have this
            var inherit = new SimplePersonalityInheritanceService(rng); // your implementation
            var pregPolicy = new DefaultPregnancyPolicy(rng);     // your implementation
            var skillInh = new SimpleSkillInheritanceService(); // your implementation
            var loc = new LocationService(planets, fleets);
            var bus = new InMemoryEventBus();              // your test event bus
            var faction = new FactionService(factions);
            var planner = new IntentPlanner(characters, opinions, faction, rng);
            var diplomacy = new DiplomacyService(diplomacies, faction, clock, opinions);
            var resolver = new InteractionResolver(characters, opinions, faction, secrets, rng, diplomacy);

            var lifecycle = new CharacterLifecycleService(
                characters, lineage, clock, rng, mortality, nameGen,
                inherit, pregPolicy, skillInh, loc, bus);

            var runner = new LifecycleSimulationRunner(characters, lineage, planets, lifecycle, planner, resolver, socialLog, clock);

            // One planet, one super couple:
            runner.SeedCoupleOnPlanet(systemId: Guid.NewGuid());

            // Run multiple generations (e.g., 50 years):
            runner.RunYears(50);

            // Now assert on characters.GetAll() for newborns, lineage links, etc.
            clock.CurrentYear.Should().Be(3051);
            characters.GetAll().Count().Should().Be(45);

        }

        internal class InMemoryEventBus : IEventBus
        {
            public void Publish<T>(T @event)
            {
                
            }
        }
    }
}
