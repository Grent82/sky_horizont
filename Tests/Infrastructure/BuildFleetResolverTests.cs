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
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class BuildFleetResolverTests
{
    private static Character NewActor()
    {
        return new Character(Guid.NewGuid(), "A", 30, 3000, 1, Sex.Male,
            new Personality(50,50,50,50,50), new SkillSet(50,50,50,50));
    }

    private static InteractionResolver CreateResolver(Character actor, IFleetRepository fleetRepo, IFundsService funds, out Guid factionId)
    {
        var fid = Guid.NewGuid();
        factionId = fid;

        var chars = new Mock<ICharacterRepository>();
        chars.Setup(c => c.GetById(actor.Id)).Returns(actor);

        var factions = new Mock<IFactionService>();
        factions.Setup(f => f.GetFactionIdForCharacter(actor.Id)).Returns(fid);
        factions.Setup(f => f.GetAllRivalFactions(fid)).Returns(Array.Empty<Guid>());
        factions.Setup(f => f.GetEconomicStrength(fid)).Returns(100);

        var planets = new Mock<IPlanetRepository>();
        planets.Setup(p => p.GetAll()).Returns(Array.Empty<Planet>());
        planets.Setup(p => p.GetPlanetsControlledByFaction(fid)).Returns(Array.Empty<Planet>());

        var opinions = Mock.Of<IOpinionRepository>();
        var secrets = Mock.Of<ISecretsRepository>();
        var rng = Mock.Of<IRandomService>();
        var diplomacy = Mock.Of<IDiplomacyService>();
        var travel = Mock.Of<ITravelService>();
        var piracy = Mock.Of<IPiracyService>();
        var events = Mock.Of<IEventBus>();
        var battle = Mock.Of<IBattleOutcomeService>();
        var intimacy = Mock.Of<IIntimacyLog>();
        var merit = Mock.Of<IMeritPolicy>();

        return new InteractionResolver(chars.Object, opinions, factions.Object, secrets, rng, diplomacy, travel, piracy, planets.Object, fleetRepo, funds, events, battle, intimacy, merit);
    }

    [Fact]
    public void ResolveBuildFleet_creates_fleet_and_deducts_funds_when_affordable()
    {
        var actor = NewActor();
        var fleetRepo = new Mock<IFleetRepository>();
        Fleet? savedFleet = null;
        fleetRepo.Setup(f => f.Save(It.IsAny<Fleet>())).Callback<Fleet>(f => savedFleet = f);

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.HasFunds(It.IsAny<Guid>(), 1000)).Returns(true);
        funds.Setup(f => f.Deduct(It.IsAny<Guid>(), 1000));

        var resolver = CreateResolver(actor, fleetRepo.Object, funds.Object, out var factionId);
        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);

        var ev = resolver.Resolve(intent, 3000, 1).Single();

        funds.Verify(f => f.Deduct(factionId, 1000), Times.Once);
        fleetRepo.Verify(f => f.Save(It.IsAny<Fleet>()), Times.Once);
        savedFleet.Should().NotBeNull();
        savedFleet!.Ships.Should().NotBeEmpty();
        ev.Success.Should().BeTrue();
        ev.Type.Should().Be(SocialEventType.BuildFleet);
    }

    [Fact]
    public void ResolveBuildFleet_fails_when_insufficient_funds()
    {
        var actor = NewActor();
        var fleetRepo = new Mock<IFleetRepository>();

        var funds = new Mock<IFundsService>();
        funds.Setup(f => f.HasFunds(It.IsAny<Guid>(), 1000)).Returns(false);

        var resolver = CreateResolver(actor, fleetRepo.Object, funds.Object, out var factionId);
        var intent = new CharacterIntent(actor.Id, IntentType.BuildFleet);

        var ev = resolver.Resolve(intent, 3000, 1).Single();

        funds.Verify(f => f.Deduct(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        fleetRepo.Verify(f => f.Save(It.IsAny<Fleet>()), Times.Never);
        ev.Success.Should().BeFalse();
        ev.Type.Should().Be(SocialEventType.BuildFleet);
    }
}
