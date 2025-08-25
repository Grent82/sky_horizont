using System;
using System.Linq;
using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Infrastructure.Social;
using Infrastructure.Persistence.Repositories;
using SkyHorizont.Infrastructure.Persistence;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class BuildFleetResolverTests
{
    private static Character NewActor()
        => new Character(Guid.NewGuid(), "A", 30, 3000, 1, Sex.Male,
            new Personality(50,50,50,50,50), new SkillSet(50,50,50,50));

    [Fact]
    public void ResolveBuildFleet_creates_ships_when_budget_and_capacity_sufficient()
    {
        var actor = NewActor();
        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(c => c.GetById(actor.Id)).Returns(actor);

        var factionId = Guid.NewGuid();
        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(Array.Empty<Guid>());
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(10000);
        factionSvc.Setup(f => f.GetFaction(factionId)).Returns(new Faction(factionId, "T", actor.Id, FactionDoctrine.Balanced));

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planetRepo = new Mock<IPlanetRepository>();
        var planet = new Planet(Guid.NewGuid(), "Home", Guid.NewGuid(), factionId, new Resources(100,100,100), charRepo.Object, planetRepo.Object, ecoRepo, productionCapacity: 200);
        planetRepo.Setup(p => p.GetPlanetsControlledByFaction(factionId)).Returns(new[] { planet });
        planetRepo.Setup(p => p.Save(It.IsAny<Planet>()));

        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.Setup(f => f.GetFleetsForFaction(factionId)).Returns(Array.Empty<Fleet>());
        Fleet? savedFleet = null;
        fleetRepo.Setup(f => f.Save(It.IsAny<Fleet>())).Callback<Fleet>(f => savedFleet = f);

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.GetBalance(factionId)).Returns(10000);
        funds.Setup(f => f.HasFunds(It.IsAny<Guid>(), It.IsAny<int>())).Returns(true);
        funds.Setup(f => f.Deduct(It.IsAny<Guid>(), It.IsAny<int>()));

        var resolver = new InteractionResolver(
            charRepo.Object,
            Mock.Of<IOpinionRepository>(),
            factionSvc.Object,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IDiplomacyService>(),
            Mock.Of<ITravelService>(),
            Mock.Of<IPiracyService>(),
            planetRepo.Object,
            fleetRepo.Object,
            funds.Object,
            Mock.Of<IEventBus>(),
            Mock.Of<IBattleOutcomeService>(),
            Mock.Of<IIntimacyLog>(),
            Mock.Of<IMeritPolicy>()
        );

        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);
        var ev = resolver.Resolve(intent, 3000, 1).Single();

        savedFleet.Should().NotBeNull();
        savedFleet!.Ships.Should().NotBeEmpty();
        ev.Success.Should().BeTrue();
        funds.Verify(f => f.Deduct(factionId, It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ResolveBuildFleet_fails_when_budget_exhausted()
    {
        var actor = NewActor();
        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(c => c.GetById(actor.Id)).Returns(actor);

        var factionId = Guid.NewGuid();
        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(Array.Empty<Guid>());
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(0);
        factionSvc.Setup(f => f.GetFaction(factionId)).Returns(new Faction(factionId, "T", actor.Id, FactionDoctrine.Balanced));

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planetRepo = new Mock<IPlanetRepository>();
        var planet = new Planet(Guid.NewGuid(), "Home", Guid.NewGuid(), factionId, new Resources(100,100,100), charRepo.Object, planetRepo.Object, ecoRepo, productionCapacity: 200);
        planetRepo.Setup(p => p.GetPlanetsControlledByFaction(factionId)).Returns(new[] { planet });

        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.Setup(f => f.GetFleetsForFaction(factionId)).Returns(Array.Empty<Fleet>());

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.GetBalance(factionId)).Returns(0);
        funds.Setup(f => f.HasFunds(It.IsAny<Guid>(), It.IsAny<int>())).Returns(false);

        var resolver = new InteractionResolver(
            charRepo.Object,
            Mock.Of<IOpinionRepository>(),
            factionSvc.Object,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IDiplomacyService>(),
            Mock.Of<ITravelService>(),
            Mock.Of<IPiracyService>(),
            planetRepo.Object,
            fleetRepo.Object,
            funds.Object,
            Mock.Of<IEventBus>(),
            Mock.Of<IBattleOutcomeService>(),
            Mock.Of<IIntimacyLog>(),
            Mock.Of<IMeritPolicy>()
        );

        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);
        var ev = resolver.Resolve(intent, 3000, 1).Single();

        ev.Success.Should().BeFalse();
        fleetRepo.Verify(f => f.Save(It.IsAny<Fleet>()), Times.Never);
    }

    [Fact]
    public void ResolveBuildFleet_replenishes_existing_fleet_after_losses()
    {
        var actor = NewActor();
        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(c => c.GetById(actor.Id)).Returns(actor);

        var factionId = Guid.NewGuid();
        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(Array.Empty<Guid>());
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(10000);
        factionSvc.Setup(f => f.GetFaction(factionId)).Returns(new Faction(factionId, "T", actor.Id, FactionDoctrine.Balanced));

        var ecoRepo = new PlanetEconomyRepository(new InMemoryPlanetEconomyDbContext());
        var planetRepo = new Mock<IPlanetRepository>();
        var planet = new Planet(Guid.NewGuid(), "Home", Guid.NewGuid(), factionId, new Resources(500,500,500), charRepo.Object, planetRepo.Object, ecoRepo, productionCapacity: 200);
        planetRepo.Setup(p => p.GetPlanetsControlledByFaction(factionId)).Returns(new[] { planet });
        planetRepo.Setup(p => p.Save(It.IsAny<Planet>()));

        Fleet? savedFleet = null;
        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.SetupSequence(f => f.GetFleetsForFaction(factionId))
            .Returns(Array.Empty<Fleet>())
            .Returns(() => new[] { savedFleet! });
        fleetRepo.Setup(f => f.Save(It.IsAny<Fleet>())).Callback<Fleet>(f => savedFleet = f);

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.GetBalance(factionId)).Returns(10000);
        funds.Setup(f => f.HasFunds(It.IsAny<Guid>(), It.IsAny<int>())).Returns(true);
        funds.Setup(f => f.Deduct(It.IsAny<Guid>(), It.IsAny<int>()));

        var resolver = new InteractionResolver(
            charRepo.Object,
            Mock.Of<IOpinionRepository>(),
            factionSvc.Object,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IDiplomacyService>(),
            Mock.Of<ITravelService>(),
            Mock.Of<IPiracyService>(),
            planetRepo.Object,
            fleetRepo.Object,
            funds.Object,
            Mock.Of<IEventBus>(),
            Mock.Of<IBattleOutcomeService>(),
            Mock.Of<IIntimacyLog>(),
            Mock.Of<IMeritPolicy>()
        );

        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);
        resolver.Resolve(intent, 3000, 1); // initial build

        var desired = savedFleet!.DesiredComposition.Values.Sum();
        var lostShip = savedFleet.Ships.First();
        savedFleet.DestroyShip(lostShip.Id);

        resolver.Resolve(intent, 3000, 2); // replenish

        savedFleet.Ships.Count().Should().Be(desired);
    }
}
