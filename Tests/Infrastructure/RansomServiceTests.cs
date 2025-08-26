using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Testing;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class RansomServiceTests
{
    [Fact]
    public void TryResolveRansom_PayerRefuses_DoesNotDeductFunds()
    {
        var payerId = Guid.NewGuid();
        var captiveId = Guid.NewGuid();
        const int amount = 100;

        var funds = new Mock<ICharacterFundsService>();
        var decision = new Mock<IRansomDecisionService>();
        decision.Setup(d => d.WillPayRansom(payerId, captiveId, amount)).Returns(false);

        var service = new RansomService(
            Mock.Of<ICharacterRepository>(),
            funds.Object,
            Mock.Of<IFactionFundsRepository>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            decision.Object,
            Mock.Of<IFactionService>(),
            Mock.Of<IRandomService>());

        var result = service.TryResolveRansom(payerId, captiveId, amount);

        result.Should().BeFalse();
        funds.Verify(f => f.DeductCharacter(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void HandleUnpaidRansom_SoldToSlavery_EnslavesCharacter()
    {
        var captiveId = Guid.NewGuid();
        var captorFaction = Guid.NewGuid();
        var captive = CharacterFactory.CreateSuperPositive(captiveId, "Captive", Sex.Female, 20, 2000, 1);
        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);

        var rng = new Mock<IRandomService>();
        rng.Setup(r => r.NextInt(0, 3)).Returns(0);

        var service = new RansomService(
            repo.Object,
            Mock.Of<ICharacterFundsService>(),
            Mock.Of<IFactionFundsRepository>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            Mock.Of<IRansomDecisionService>(),
            Mock.Of<IFactionService>(),
            rng.Object);

        service.HandleUnpaidRansom(captiveId, captorFaction);

        captive.IsEnslaved.Should().BeTrue();
        captive.HaremOwnerId.Should().BeNull();
    }

    [Fact]
    public void HandleUnpaidRansom_TransferredToHarem_SetsOwnerAndMovesFaction()
    {
        var captiveId = Guid.NewGuid();
        var captorFaction = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var captive = CharacterFactory.CreateSuperPositive(captiveId, "Captive", Sex.Female, 20, 2000, 1);
        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);

        var rng = new Mock<IRandomService>();
        rng.Setup(r => r.NextInt(0, 3)).Returns(1);

        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetLeaderId(captorFaction)).Returns(leaderId);

        var service = new RansomService(
            repo.Object,
            Mock.Of<ICharacterFundsService>(),
            Mock.Of<IFactionFundsRepository>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            Mock.Of<IRansomDecisionService>(),
            factionSvc.Object,
            rng.Object);

        service.HandleUnpaidRansom(captiveId, captorFaction);

        captive.IsEnslaved.Should().BeTrue();
        captive.HaremOwnerId.Should().Be(leaderId);
        factionSvc.Verify(f => f.MoveCharacterToFaction(captiveId, captorFaction), Times.Once);
    }

    [Fact]
    public void HandleUnpaidRansom_Executed_MarksCharacterDead()
    {
        var captiveId = Guid.NewGuid();
        var captorFaction = Guid.NewGuid();
        var captive = CharacterFactory.CreateSuperPositive(captiveId, "Captive", Sex.Female, 20, 2000, 1);
        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);

        var rng = new Mock<IRandomService>();
        rng.Setup(r => r.NextInt(0, 3)).Returns(2);

        var service = new RansomService(
            repo.Object,
            Mock.Of<ICharacterFundsService>(),
            Mock.Of<IFactionFundsRepository>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            Mock.Of<IRansomDecisionService>(),
            Mock.Of<IFactionService>(),
            rng.Object);

        service.HandleUnpaidRansom(captiveId, captorFaction);

        captive.IsAlive.Should().BeFalse();
        captive.IsEnslaved.Should().BeFalse();
    }
}
