using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class RansomServiceTests
{
    private static Character CreateCharacter(Guid id, string name) =>
        new(id, name, 30, 0, 1, Sex.Male,
            new Personality(50, 50, 50, 50, 50),
            new SkillSet(0, 0, 0, 0));

    [Fact]
    public void TryResolveRansom_PayerRefuses_ExchangeFails_DoesNotDeductFunds()

    {
        var captiveId = Guid.NewGuid();
        var associateId = Guid.NewGuid();
        const int amount = 100;

        var captive = CreateCharacter(captiveId, "Captive");

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);
        repo.Setup(r => r.GetAssociates(captiveId))
            .Returns(new[] { CreateCharacter(associateId, "Assoc") });

        var funds = new Mock<ICharacterFundsService>();
        var decision = new Mock<IRansomDecisionService>();

        var factions = new Mock<IFactionService>();
        decision.Setup(d => d.WillPayRansom(payerId, captiveId, amount)).Returns(false);
        factions.Setup(f => f.NegotiatePrisonerExchange(payerId, captiveId)).Returns(false);

        var service = new RansomService(
            Mock.Of<ICharacterRepository>(),
            funds.Object,
            Mock.Of<IFactionFundsRepository>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            decision.Object,
            factions.Object);

        var result = service.TryResolveRansom(captiveId, amount);

        result.Should().BeFalse();
        funds.Verify(f => f.DeductCharacter(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        factions.Verify(f => f.NegotiatePrisonerExchange(payerId, captiveId), Times.Once);
    }

    [Fact]
    public void TryResolveRansom_PayerRefuses_ExchangeSucceeds()
    {
        var payerId = Guid.NewGuid();
        var captiveId = Guid.NewGuid();
        const int amount = 100;

        var funds = new Mock<ICharacterFundsService>();
        var decision = new Mock<IRansomDecisionService>();
        var factions = new Mock<IFactionService>();
        decision.Setup(d => d.WillPayRansom(payerId, captiveId, amount)).Returns(false);
        factions.Setup(f => f.NegotiatePrisonerExchange(payerId, captiveId)).Returns(true);

        var service = new RansomService(
            Mock.Of<ICharacterRepository>(),
            funds.Object,
            Mock.Of<IFactionFundsRepository>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            decision.Object,
            factions.Object);

        var result = service.TryResolveRansom(payerId, captiveId, amount);

        result.Should().BeTrue();
        funds.Verify(f => f.DeductCharacter(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        factions.Verify(f => f.NegotiatePrisonerExchange(payerId, captiveId), Times.Once);
    }

    [Fact]
    public void TryResolveRansom_AssociatePays_WhenWilling()
    {
        var captiveId = Guid.NewGuid();
        var associateId = Guid.NewGuid();
        const int amount = 200;

        var captive = CreateCharacter(captiveId, "Captive");
        var associate = CreateCharacter(associateId, "Friend");

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);
        repo.Setup(r => r.GetAssociates(captiveId)).Returns(new[] { associate });

        var funds = new Mock<ICharacterFundsService>();
        funds.Setup(f => f.DeductCharacter(associateId, amount)).Returns(true);

        var decision = new Mock<IRansomDecisionService>();
        decision.Setup(d => d.WillPayRansom(associateId, captiveId, amount)).Returns(true);

        var service = new RansomService(repo.Object, funds.Object,
            Mock.Of<IFactionFundsRepository>(), Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(), decision.Object);

        var result = service.TryResolveRansom(captiveId, amount);

        result.Should().BeTrue();
        funds.Verify(f => f.DeductCharacter(associateId, amount), Times.Once);
        funds.Verify(f => f.CreditCharacter(captiveId, amount), Times.Once);
    }
}

