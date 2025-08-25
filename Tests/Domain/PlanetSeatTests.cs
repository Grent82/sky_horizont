using System;
using FluentAssertions;
using Xunit;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Persistence.Repositories;

namespace SkyHorizont.Tests.Domain
{
    public class PlanetSeatTests
    {
        [Fact]
        public void SetSeatPlanet_PersistsSeatFaction()
        {
            var chars = new CharactersRepository(new InMemoryCharactersDbContext());
            var planetsRepo = new PlanetsRepository(new InMemoryPlanetsDbContext());
            var eco = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
            var planet = new Planet(Guid.NewGuid(), "SeatWorld", Guid.NewGuid(), Guid.NewGuid(), new Resources(0, 0, 0), chars, planetsRepo, eco);
            planetsRepo.Save(planet);

            var factionId = Guid.NewGuid();
            planet.SetSeatPlanet(factionId);

            var stored = planetsRepo.GetById(planet.Id);
            stored.Should().NotBeNull();
            stored!.SeatFactionId.Should().Be(factionId);
        }
    }
}
