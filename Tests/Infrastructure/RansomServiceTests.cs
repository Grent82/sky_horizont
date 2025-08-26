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
    [Fact]
    public void TryResolveRansom_PayerRefuses_ExchangeFails_DoesNotDeductFunds()
    {
        var payerId = Guid.NewGuid();
        var captiveId = Guid.NewGuid();
        const int amount = 100;

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

        var result = service.TryResolveRansom(payerId, captiveId, amount);

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
}
