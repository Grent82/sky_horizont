using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Social;
using Infrastructure.Persistence.Repositories;
using SkyHorizont.Infrastructure.Persistence;

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
        private readonly IntentPlanner _planner;
        private readonly InteractionResolver _resolver;
        private readonly InMemorySocialEventLog _socialLog;
        private readonly IGameClockService _clock;
        private readonly IAffectionService _affection;

        public LifecycleSimulationRunner(
            ICharacterRepository characters,
            ILineageRepository lineage,
            IPlanetRepository planets,
            CharacterLifecycleService lifecycle,
            IntentPlanner planner,
            InteractionResolver resolver,
            InMemorySocialEventLog socialLog,
            IGameClockService clock,
            IAffectionService affection)
        {
            _characters = characters;
            _lineage = lineage;
            _planets = planets;
            _lifecycle = lifecycle;
            _planner = planner;
            _resolver = resolver;
            _socialLog = socialLog;
            _clock = clock;
            _affection = affection;
        }

        public void SeedCoupleOnPlanet(Guid systemId)
        {
            // Create planet
            var eco = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
            var planet = new Planet(
                id: Guid.NewGuid(),
                name: "Testia Prime",
                systemId: systemId,
                factionId: Guid.NewGuid(),
                initialResources: new Resources(10_000, 10_000, 10_000),
                characterRepository: _characters,
                planetRepository: _planets,
                economyRepository: eco,
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

            _characters.Save(she);
            _characters.Save(he);

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
                ProcessAllTurnEvents();
            }
        }

        public void ProcessAllTurnEvents()
        {
            // 1) Advance game time (month → year rollover if needed)
            SafeRun("Clock.AdvanceTurn", () => _clock.AdvanceTurn());

            // 2) Lifecycle first (age up, pregnancies, births, deaths)
            SafeRun("Lifecycle.Process", () => _lifecycle.ProcessLifecycleTurn());

            // 3) Social layer: plan intents per living character and resolve them
            SafeRun("Social.Intents", () =>
            {
                _planner.ClearCaches();
                _resolver.ClearCaches();
                foreach (var actor in _characters.GetLiving())
                {
                    var intents = _planner.PlanMonthlyIntents(actor);
                    foreach (var intent in intents)
                    {
                        try
                        {
                            var events = _resolver.Resolve(intent, _clock.CurrentYear, _clock.CurrentMonth);
                            foreach (var ev in events)
                                _socialLog.Append(ev);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TurnProcessor] Error resolving intent {intent.Type} for {actor.Id}: {ex}");
                            throw;
                        }
                    }
                }
            });

            // 4) Affection drift / captive affection adjustments (monthly)
            SafeRun("Affection.Update", () => _affection.UpdateAffection());
        }

        private static void SafeRun(string label, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                Console.WriteLine($"[TurnProcessor] {label} failed: {ex.Message}");
                throw;
            }
        }
    }
}
