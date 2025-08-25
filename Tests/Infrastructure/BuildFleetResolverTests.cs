using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Infrastructure.Social;
using SkyHorizont.Infrastructure.Persistence;
using Xunit;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Infrastructure.Persistence.Repositories;

namespace SkyHorizont.Tests.Infrastructure;

public class BuildFleetResolverTests
{
    private static Character NewActor()
        => new Character(Guid.NewGuid(), "A", 30, 3000, 1, Sex.Male,
            new Personality(50,50,50,50,50), new SkillSet(50,50,50,50));

    private static InteractionResolver CreateResolver(
        Character actor,
        Planet planet,
        IFleetRepository fleetRepo,
        IFundsService funds,
        IFactionService factionSvc,
        IPiracyService piracy)
    {
        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(c => c.GetById(actor.Id)).Returns(actor);

        var planetRepo = new Mock<IPlanetRepository>();
        planetRepo.Setup(p => p.GetPlanetsControlledByFaction(planet.FactionId)).Returns(new[] { planet });
        planetRepo.Setup(p => p.GetAll()).Returns(new[] { planet });
        planetRepo.Setup(p => p.Save(It.IsAny<Planet>()));

        return new InteractionResolver(
            charRepo.Object,
            Mock.Of<IOpinionRepository>(),
            factionSvc,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IDiplomacyService>(),
            Mock.Of<ITravelService>(),
            piracy,
            planetRepo.Object,
            fleetRepo,
            funds,
            Mock.Of<IEventBus>(),
            Mock.Of<IBattleOutcomeService>(),
            Mock.Of<IIntimacyLog>(),
            Mock.Of<IMeritPolicy>()
        );
    }

    [Fact]
    public void ResolveBuildFleet_prioritizes_freighters_when_trade_high()
    {
        var actor = NewActor();
        var factionId = Guid.NewGuid();

        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(Array.Empty<Guid>());
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(10000);
        factionSvc.Setup(f => f.GetFaction(factionId)).Returns(new Faction(factionId, "T", actor.Id, FactionDoctrine.Balanced));
        factionSvc.Setup(f => f.IsAtWar(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);
        factionSvc.Setup(f => f.HasAlliance(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);

        var piracy = new Mock<IPiracyService>();
        piracy.Setup(p => p.GetPirateActivity(It.IsAny<Guid>())).Returns(0);
        piracy.Setup(p => p.GetTrafficLevel(It.IsAny<Guid>())).Returns(100);
        piracy.Setup(p => p.IsPirateFaction(It.IsAny<Guid>())).Returns(false);

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planet = new Planet(Guid.NewGuid(), "Home", Guid.NewGuid(), factionId,
            new Resources(500,500,500), Mock.Of<ICharacterRepository>(), Mock.Of<IPlanetRepository>(),
            ecoRepo, productionCapacity: 500);
        planet.Citizens.Add(actor.Id);

        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.Setup(f => f.GetFleetsForFaction(factionId)).Returns(Array.Empty<Fleet>());
        Fleet? savedFleet = null;
        fleetRepo.Setup(f => f.Save(It.IsAny<Fleet>())).Callback<Fleet>(f => savedFleet = f);

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.GetBalance(factionId)).Returns(20000);
        funds.Setup(f => f.Deduct(It.IsAny<Guid>(), It.IsAny<int>()));

        var resolver = CreateResolver(actor, planet, fleetRepo.Object, funds.Object, factionSvc.Object, piracy.Object);
        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);

        var ev = resolver.Resolve(intent, 3000, 1).Single();

        ev.Success.Should().BeTrue();
        savedFleet.Should().NotBeNull();
        savedFleet!.DesiredComposition[ShipClass.Freighter].Should().BeGreaterThanOrEqualTo(savedFleet.DesiredComposition[ShipClass.Corvette]);
    }

    [Fact]
    public void ResolveBuildFleet_builds_fighters_during_war()
    {
        var actor = NewActor();
        var factionId = Guid.NewGuid();
        var rivalId = Guid.NewGuid();

        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(new[] { rivalId });
        factionSvc.Setup(f => f.IsAtWar(factionId, rivalId)).Returns(true);
        factionSvc.Setup(f => f.HasAlliance(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(10000);
        factionSvc.Setup(f => f.GetFaction(factionId)).Returns(new Faction(factionId, "T", actor.Id, FactionDoctrine.Balanced));

        var piracy = new Mock<IPiracyService>();
        piracy.Setup(p => p.GetPirateActivity(It.IsAny<Guid>())).Returns(80);
        piracy.Setup(p => p.GetTrafficLevel(It.IsAny<Guid>())).Returns(20);
        piracy.Setup(p => p.IsPirateFaction(It.IsAny<Guid>())).Returns(false);

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planet = new Planet(Guid.NewGuid(), "Home", Guid.NewGuid(), factionId,
            new Resources(500,500,500), Mock.Of<ICharacterRepository>(), Mock.Of<IPlanetRepository>(),
            ecoRepo, productionCapacity: 500);
        planet.Citizens.Add(actor.Id);

        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.Setup(f => f.GetFleetsForFaction(factionId)).Returns(Array.Empty<Fleet>());
        Fleet? savedFleet = null;
        fleetRepo.Setup(f => f.Save(It.IsAny<Fleet>())).Callback<Fleet>(f => savedFleet = f);

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.GetBalance(factionId)).Returns(20000);
        funds.Setup(f => f.Deduct(It.IsAny<Guid>(), It.IsAny<int>()));

        var resolver = CreateResolver(actor, planet, fleetRepo.Object, funds.Object, factionSvc.Object, piracy.Object);
        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);

        var ev = resolver.Resolve(intent, 3000, 1).Single();

        ev.Success.Should().BeTrue();
        savedFleet.Should().NotBeNull();

        savedFleet!.DesiredComposition.TryGetValue(ShipClass.Corvette, out var corvettes).Should().BeTrue();
        savedFleet.DesiredComposition.TryGetValue(ShipClass.Freighter, out var freighters);
        corvettes.Should().BeGreaterThan(freighters);
    }

    [Fact]
    public void ResolveBuildFleet_replenishes_existing_fleet_after_losses()
    {
        var actor = NewActor();
        var factionId = Guid.NewGuid();

        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(Array.Empty<Guid>());
        factionSvc.Setup(f => f.IsAtWar(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);
        factionSvc.Setup(f => f.HasAlliance(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(10000);
        factionSvc.Setup(f => f.GetFaction(factionId)).Returns(new Faction(factionId, "T", actor.Id, FactionDoctrine.Balanced));

        var piracy = new Mock<IPiracyService>();
        piracy.Setup(p => p.GetPirateActivity(It.IsAny<Guid>())).Returns(0);
        piracy.Setup(p => p.GetTrafficLevel(It.IsAny<Guid>())).Returns(50);
        piracy.Setup(p => p.IsPirateFaction(It.IsAny<Guid>())).Returns(false);

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planet = new Planet(Guid.NewGuid(), "Home", Guid.NewGuid(), factionId,
            new Resources(1000,1000,1000), Mock.Of<ICharacterRepository>(), Mock.Of<IPlanetRepository>(),
            ecoRepo, productionCapacity: 500);
        planet.Citizens.Add(actor.Id);

        Fleet? savedFleet = null;
        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.SetupSequence(f => f.GetFleetsForFaction(factionId))
            .Returns(Array.Empty<Fleet>())
            .Returns(() => new[] { savedFleet! });
        fleetRepo.Setup(f => f.Save(It.IsAny<Fleet>())).Callback<Fleet>(f => savedFleet = f);

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.GetBalance(factionId)).Returns(20000);
        funds.Setup(f => f.Deduct(It.IsAny<Guid>(), It.IsAny<int>()));

        var resolver = CreateResolver(actor, planet, fleetRepo.Object, funds.Object, factionSvc.Object, piracy.Object);
        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);

        resolver.Resolve(intent, 3000, 1); // initial build

        var desired = savedFleet!.DesiredComposition.Values.Sum();
        var lostShip = savedFleet.Ships.First();
        savedFleet.DestroyShip(lostShip.Id);

        resolver.Resolve(intent, 3000, 2); // replenish

        savedFleet.Ships.Count().Should().Be(desired);
    }
}

