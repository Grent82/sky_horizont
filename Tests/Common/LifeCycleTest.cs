using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using SkyHorizont.Application.Turns;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Persistence.Diplomacy;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Infrastructure.Social;
using SkyHorizont.Infrastructure.Social.IntentRules;
using SkyHorizont.Infrastructure.Testing;
using Xunit;

namespace SkyHorizont.Tests.Common
{
    public class LifecycleTests
    {
        [Fact(Skip = "Flaky under current environment")]
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
            var travels = new TravelRepository(new InMemoryTravelDbContext());
            var diplomacies = new DiplomacyRepository(new InMemoryDiplomacyDbContext());
            var funds = new CharacterFundsRepository(new InMemoryCharacterFundsDbContext());
            var factionFunds = new FactionFundsRepository(new InMemoryFundsDbContext());
            var eco = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
            var socialLog = new InMemorySocialEventLog();
            var events = new InMemoryEventBus();
            var affections = new AffectionRepository(new InMemoryAffectionDbContext());

            var clock = new GameClockService(3001, 1, 12);
            var rng = new RandomService(12345);
            var mortality = new GompertzMortalityModel();
            var nameGen = new NameGenerator(rng);
            var inherit = new SimplePersonalityInheritanceService(rng);
            var loc = new LocationService(planets, fleets);
            var pregPolicy = new DefaultPregnancyPolicy(rng, opinions, loc);
            var skillInh = new SimpleSkillInheritanceService();
            var faction = new FactionService(factions, planets);
            var piracy = new PiracyService(faction, rng, Guid.NewGuid());
            var travel = new TravelService(planets, fleets, rng, travels, piracy, clock);
            var fund = new CharacterFundsService(funds);
            var tax = new FactionTaxService(factionFunds, funds, planets, eco, faction, characters, clock);
            var factionFundsSvc = new FundsService(factionFunds);
            var moral = new MoraleService(characters);
            var battle = new BattleOutcomeService(fund, factionFunds, tax, characters, moral);
            var affection = new AffectionService(characters, planets, fleets, affections);
            var intimacy = new InMemoryIntimacyLog();
            var merit = new MeritPolicy();
            
            var bus = new InMemoryEventBus();
            var rules = new IIntentRule[]
            {
                new CourtshipIntentRule(rng),
                new VisitFamilyIntentRule(rng)
            };
            var planner = new IntentPlanner(characters, opinions, faction, rng, planets, fleets, piracy, rules);
            var diplomacy = new DiplomacyService(diplomacies, faction, clock, opinions);
            var resolver = new InteractionResolver(characters, opinions, faction, secrets, rng, diplomacy, travel, piracy, planets, fleets, factionFundsSvc, events, battle, intimacy, merit);

            var lifecycle = new CharacterLifecycleService(
                characters, lineage, clock, rng, mortality, nameGen,
                inherit, pregPolicy, skillInh, loc, bus, intimacy, faction);

            var runner = new LifecycleSimulationRunner(characters, lineage, planets, lifecycle, planner, resolver, socialLog, clock, affection);

            // One planet, one super couple:
            runner.SeedCoupleOnPlanet(systemId: Guid.NewGuid());

            // Run multiple generations (e.g., 50 years):
            runner.RunYears(50);

            // Now assert on characters.GetAll() for newborns, lineage links, etc.
            clock.CurrentYear.Should().Be(3051);
            characters.GetAll().Count().Should().Be(25);

        }

        internal class InMemoryEventBus : IEventBus
        {
            public void Publish<T>(T @event)
            {
                
            }
        }
    }
}
