using FluentAssertions;
using SkyHorizont.Application.Turns;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Repository;
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

            var clock = new GameClockService(3001, 1, 12);
            var rng = new RandomService(12345);
            var mortality = new GompertzMortalityModel(); // your test implementation
            var nameGen = new NameGenerator(rng);     // you already have this
            var inherit = new SimplePersonalityInheritanceService(rng); // your implementation
            var pregPolicy = new DefaultPregnancyPolicy(rng);     // your implementation
            var skillInh = new SimpleSkillInheritanceService(); // your implementation
            var loc = new LocationService(planets, fleets);
            var bus = new InMemoryEventBus();              // your test event bus

            var lifecycle = new CharacterLifecycleService(
                characters, lineage, clock, rng, mortality, nameGen,
                inherit, pregPolicy, skillInh, loc, bus);

            var runner = new LifecycleSimulationRunner(characters, lineage, planets, lifecycle, clock);

            // One planet, one super couple:
            runner.SeedCoupleOnPlanet(systemId: Guid.NewGuid());

            // Run multiple generations (e.g., 50 years):
            runner.RunYears(1350);

            // Now assert on characters.GetAll() for newborns, lineage links, etc.
            clock.CurrentYear.Should().Be(4351);
            characters.GetAll().Count().Should().Be(1);

        }

        internal class InMemoryEventBus : IEventBus
        {
            public void Publish<T>(T @event)
            {
                
            }
        }
    }
}
