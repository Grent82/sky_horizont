using System;
using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class RansomMarketplaceServiceTests
{
    [Fact]
    public void GetListings_ReturnsAddedListings()
    {
        var service = new RansomMarketplaceService(
            Mock.Of<ICharacterFundsService>(),
            Mock.Of<IFactionFundsRepository>());

        var listing = new RansomListing(Guid.NewGuid(), Guid.NewGuid(), 100, false);
        service.AddListing(listing);

        service.GetListings().Should().ContainSingle(l => l == listing);
    }

    [Fact]
    public void TryPurchaseAsCharacter_DeductsAndCreditsWithFee()
    {
        var captive = Guid.NewGuid();
        var captor = Guid.NewGuid();
        var rescuer = Guid.NewGuid();
        const int amount = 200;
        const double feeRate = 0.1;
        var feeRecipient = Guid.NewGuid();
        var expectedNet = amount - (int)(amount * feeRate);

        var funds = new Mock<ICharacterFundsService>();
        funds.Setup(f => f.DeductCharacter(rescuer, amount)).Returns(true);

        var factionFunds = new Mock<IFactionFundsRepository>();

        var service = new RansomMarketplaceService(funds.Object, factionFunds.Object, feeRate, feeRecipient);
        service.AddListing(new RansomListing(captive, captor, amount, false));

        var result = service.TryPurchaseAsCharacter(rescuer, captive);

        result.Should().BeTrue();
        funds.Verify(f => f.DeductCharacter(rescuer, amount), Times.Once);
        funds.Verify(f => f.CreditCharacter(captor, expectedNet), Times.Once);
        factionFunds.Verify(f => f.AddBalance(feeRecipient, amount - expectedNet), Times.Once);
        service.GetListings().Should().BeEmpty();
    }

    [Fact]
    public void TryPurchaseAsFaction_DeductsAndCreditsWithFee()
    {
        var captive = Guid.NewGuid();
        var captorFaction = Guid.NewGuid();
        var rescuerFaction = Guid.NewGuid();
        const int amount = 150;
        const double feeRate = 0.1;
        var feeRecipient = Guid.NewGuid();
        var expectedNet = amount - (int)(amount * feeRate);

        var funds = new Mock<ICharacterFundsService>();
        var factionFunds = new Mock<IFactionFundsRepository>();
        factionFunds.Setup(f => f.GetBalance(rescuerFaction)).Returns(amount);

        var service = new RansomMarketplaceService(funds.Object, factionFunds.Object, feeRate, feeRecipient);
        service.AddListing(new RansomListing(captive, captorFaction, amount, true));

        var result = service.TryPurchaseAsFaction(rescuerFaction, captive);

        result.Should().BeTrue();
        factionFunds.Verify(f => f.DeductBalance(rescuerFaction, amount), Times.Once);
        factionFunds.Verify(f => f.AddBalance(captorFaction, expectedNet), Times.Once);
        factionFunds.Verify(f => f.AddBalance(feeRecipient, amount - expectedNet), Times.Once);
        service.GetListings().Should().BeEmpty();
    }
}
