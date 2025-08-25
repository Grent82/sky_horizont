using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Economy;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure.Economy;

public class EconomyServiceTradeTests
{
    private sealed class TestStarmapService : IStarmapService
    {
        private readonly Dictionary<Guid, (double x, double y)> _systems = new();
        private readonly Dictionary<Guid, Guid> _pirateBases = new(); // faction -> system

        public void SetSystem(Guid id, double x, double y) => _systems[id] = (x, y);
        public void RegisterPirate(Guid factionId, Guid systemId) => _pirateBases[factionId] = systemId;

        public double GetDistance(Guid systemA, Guid systemB)
        {
            var a = _systems[systemA];
            var b = _systems[systemB];
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Guid? GetNearestPirateFaction(Guid systemId)
        {
            Guid? bestFaction = null;
            var bestDist = double.MaxValue;
            foreach (var kv in _pirateBases)
            {
                var dist = GetDistance(systemId, kv.Value);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestFaction = kv.Key;
                }
            }
            return bestFaction;
        }
    }

    private sealed class TestClock : IGameClockService
    {
        public int CurrentYear { get; private set; } = 1;
        public int CurrentMonth { get; private set; } = 1;
        public int MonthsPerYear => 12;
        public void AdvanceTurn() { }
    }

    private static Planet CreatePlanet(Guid id, Guid systemId, Guid factionId, IPlanetRepository repo, ICharacterRepository chars, IPlanetEconomyRepository eco)
    {
        var planet = new Planet(id, $"P{id.ToString()[..4]}", systemId, factionId,
            new Resources(0, 0, 0), chars, repo, eco, infrastructureLevel: 0);
        repo.Save(planet);
        return planet;
    }

    [Fact]
    public void Smuggling_payouts_credit_nearest_pirate_faction()
    {
        var starmap = new TestStarmapService();
        var systemA = Guid.NewGuid();
        var systemB = Guid.NewGuid();
        starmap.SetSystem(systemA, 0, 0);
        starmap.SetSystem(systemB, 100, 0);

        var pirate1 = Guid.NewGuid();
        var pirate2 = Guid.NewGuid();
        starmap.RegisterPirate(pirate1, systemA);
        starmap.RegisterPirate(pirate2, systemB);

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planetCtx = new InMemoryPlanetsDbContext();
        var planetRepo = new PlanetsRepository(planetCtx);
        var charRepo = Mock.Of<ICharacterRepository>(c => c.GetAll() == Array.Empty<Character>() && c.GetLiving() == Array.Empty<Character>());
        var planetA = CreatePlanet(Guid.NewGuid(), systemA, Guid.NewGuid(), planetRepo, charRepo, ecoRepo);
        var planetB = CreatePlanet(Guid.NewGuid(), systemB, Guid.NewGuid(), planetRepo, charRepo, ecoRepo);
        var fundsRepo = new FactionFundsRepository(new InMemoryFundsDbContext());
        var fleetRepo = Mock.Of<IFleetRepository>(f => f.GetAll() == Array.Empty<Fleet>());
        var factionService = Mock.Of<IFactionService>();
        var factionTax = Mock.Of<IFactionTaxService>();
        var clock = new TestClock();

        var economy = new EconomyService(ecoRepo, planetRepo, fleetRepo, charRepo, fundsRepo, factionService, factionTax, clock, starmap);

        economy.CreateTradeRoute(planetA.Id, planetB.Id, 10, smuggling: true);
        economy.CreateTradeRoute(planetB.Id, planetA.Id, 10, smuggling: true);
        economy.EndOfTurnUpkeep();

        fundsRepo.GetBalance(pirate1).Should().Be(55);
        fundsRepo.GetBalance(pirate2).Should().Be(55);
    }

    [Fact]
    public void Trade_value_scales_with_distance()
    {
        var starmap = new TestStarmapService();
        var systemA = Guid.NewGuid();
        var systemB = Guid.NewGuid();
        var systemC = Guid.NewGuid();
        starmap.SetSystem(systemA, 0, 0);
        starmap.SetSystem(systemB, 100, 0);
        starmap.SetSystem(systemC, 10, 0);

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planetCtx = new InMemoryPlanetsDbContext();
        var planetRepo = new PlanetsRepository(planetCtx);
        var charRepo = Mock.Of<ICharacterRepository>(c => c.GetAll() == Array.Empty<Character>() && c.GetLiving() == Array.Empty<Character>());
        var planetA = CreatePlanet(Guid.NewGuid(), systemA, Guid.NewGuid(), planetRepo, charRepo, ecoRepo);
        var planetB = CreatePlanet(Guid.NewGuid(), systemB, Guid.NewGuid(), planetRepo, charRepo, ecoRepo);
        var planetC = CreatePlanet(Guid.NewGuid(), systemC, Guid.NewGuid(), planetRepo, charRepo, ecoRepo);
        var fundsRepo = new FactionFundsRepository(new InMemoryFundsDbContext());
        var fleetRepo = Mock.Of<IFleetRepository>(f => f.GetAll() == Array.Empty<Fleet>());
        var factionService = Mock.Of<IFactionService>();
        var factionTax = Mock.Of<IFactionTaxService>();
        var clock = new TestClock();

        var economy = new EconomyService(ecoRepo, planetRepo, fleetRepo, charRepo, fundsRepo, factionService, factionTax, clock, starmap);

        var far = economy.CreateTradeRoute(planetA.Id, planetB.Id, 10);
        var near = economy.CreateTradeRoute(planetA.Id, planetC.Id, 10);
        economy.EndOfTurnUpkeep();

        var tradeEvents = ecoRepo.GetEventLog().Where(e => e.Kind == "Trade").ToList();
        tradeEvents.Should().HaveCount(2);
        var farEvent = tradeEvents.Single(e => e.Note.Contains(far.ToString()));
        var nearEvent = tradeEvents.Single(e => e.Note.Contains(near.ToString()));
        farEvent.Amount.Should().BeGreaterThan(nearEvent.Amount);
    }
}
