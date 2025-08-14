using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;

namespace SkyHorizont.Infrastructure.Testing
{
    /// <summary>
    /// Minimal harness to stress the CharacterLifecycleService across generations on a single planet.
    /// </summary>
    public sealed class LifecycleSimulationRunner
    {
        private readonly ICharacterRepository _characters;
        private readonly ILineageRepository _lineage;
        private readonly IPlanetRepository _planets;
        private readonly CharacterLifecycleService _lifecycle;
        private readonly IGameClockService _clock;

        public LifecycleSimulationRunner(
            ICharacterRepository characters,
            ILineageRepository lineage,
            IPlanetRepository planets,
            CharacterLifecycleService lifecycle,
            IGameClockService clock)
        {
            _characters = characters;
            _lineage = lineage;
            _planets = planets;
            _lifecycle = lifecycle;
            _clock = clock;
        }

        public void SeedCoupleOnPlanet(Guid systemId)
        {
            // Create planet
            var planet = new Planet(
                id: Guid.NewGuid(),
                name: "Testia Prime",
                systemId: systemId,
                controllingFactionId: Guid.NewGuid(),
                initialResources: new Resources(10_000, 10_000, 10_000),
                initialStability: 1.0,
                infrastructureLevel: 50,
                baseTaxRate: 15.0
            );

            // Create two extreme archetypes
            var she = CharacterFactory.CreateSuperPositive(
                id: Guid.NewGuid(), name: "Aeva Bright",
                sex: Sex.Female, age: 22,
                birthYear: _clock.CurrentYear - 22, birthMonth: 6);

            var he = CharacterFactory.CreateSuperNegative(
                id: Guid.NewGuid(), name: "Vor Drak",
                sex: Sex.Male, age: 24,
                birthYear: _clock.CurrentYear - 24, birthMonth: 3);

            // Co-locate & romance link
            TestWorldBuilder.SeedSinglePlanetWithCouple(planet, she, he, makePositiveGovernor: true);

            // Persist
            _planets.Save(planet);
            _characters.Save(she);
            _characters.Save(he);
        }

        /// <summary>
        /// Runs N years * 12 months, calling Lifecycle each month.
        /// Expect newborns to be saved to repositories by the lifecycle service.
        /// </summary>
        public void RunYears(int years)
        {
            int turns = years * _clock.MonthsPerYear;
            for (int i = 0; i < turns; i++)
            {
                _lifecycle.ProcessLifecycleTurn();
                _clock.AdvanceTurn();
            }
        }
    }
}
