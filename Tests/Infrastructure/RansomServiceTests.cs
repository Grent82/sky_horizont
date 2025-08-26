using System;
using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Testing;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class RansomServiceTests
{
    private static Character CreateCharacter(Guid id, string name) =>
        new(id, name, 30, 0, 1, Sex.Male,
            new Personality(50, 50, 50, 50, 50),
            new SkillSet(0, 0, 0, 0));

    [Fact]
    public void ProcessRansomTurn_AsksOneCandidatePerCall()
    {
        var captiveId = Guid.NewGuid();
        var captorId = Guid.NewGuid();
        var firstCandidate = Guid.NewGuid();
        var secondCandidate = Guid.NewGuid();
        const int amount = 100;

        var captive = CreateCharacter(captiveId, "Captive");
        var parent1 = CreateCharacter(firstCandidate, "Parent1");
        var parent2 = CreateCharacter(secondCandidate, "Parent2");

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);
        repo.Setup(r => r.GetFamilyMembers(captiveId)).Returns(new[] { parent1, parent2 });

        var funds = new Mock<ICharacterFundsService>();
        funds.Setup(f => f.DeductCharacter(secondCandidate, amount)).Returns(true);

        var decision = new Mock<IRansomDecisionService>();
        decision.Setup(d => d.WillPayRansom(firstCandidate, captiveId, amount)).Returns(false);
        decision.Setup(d => d.WillPayRansom(secondCandidate, captiveId, amount)).Returns(true);

        var factionSvc = new Mock<IFactionService>();
        var faction = new Faction(Guid.NewGuid(), "f", Guid.NewGuid());
        faction.AddCharacter(captiveId);
        faction.AddCharacter(firstCandidate);
        faction.AddCharacter(secondCandidate);
        factionSvc.Setup(f => f.GetFactionIdForCharacter(It.IsAny<Guid>())).Returns(Guid.NewGuid());
        factionSvc.Setup(f => f.GetFaction(It.IsAny<Guid>())).Returns(faction);

        var factionFunds = new Mock<IFactionFundsRepository>();
        var pricing = new Mock<IRansomPricingService>();
        pricing.Setup(p => p.EstimateRansomValue(captiveId)).Returns(amount);

        var service = new RansomService(repo.Object, funds.Object, decision.Object,
            factionSvc.Object, factionFunds.Object, Mock.Of<IRansomMarketplaceService>(), pricing.Object);

        service.StartRansom(captiveId, captorId);

        // First turn: first candidate refuses
        service.ProcessRansomTurn(captiveId).Should().BeTrue();
        funds.Verify(f => f.CreditCharacter(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);

        // Second turn: second candidate pays
        service.ProcessRansomTurn(captiveId).Should().BeFalse();
        funds.Verify(f => f.CreditCharacter(captorId, amount), Times.Once);
    }

    [Fact]
    public void KeepInHarem_CreatesRelationship()
    {
        var captiveId = Guid.NewGuid();
        var captorId = Guid.NewGuid();
        var captive = CharacterFactory.CreateSuperPositive(captiveId, "Cap", Sex.Female, 20, 2000, 1);

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);

        var service = new RansomService(repo.Object, Mock.Of<ICharacterFundsService>(), Mock.Of<IRansomDecisionService>(),
            Mock.Of<IFactionService>(), Mock.Of<IFactionFundsRepository>(), Mock.Of<IRansomMarketplaceService>(),
            Mock.Of<IRansomPricingService>());

        service.KeepInHarem(captiveId, captorId);

        captive.IsEnslaved.Should().BeTrue();
        captive.HaremOwnerId.Should().Be(captorId);
        captive.Relationships.Should().Contain(r => r.TargetCharacterId == captorId && r.Type == RelationshipType.HaremMember);
    }

    [Fact]
    public void HandleUnpaidRansom_AddsMarketplaceListing()
    {
        var captiveId = Guid.NewGuid();
        var captorFaction = Guid.NewGuid();
        const int amount = 300;

        var captive = CharacterFactory.CreateSuperPositive(captiveId, "Captive", Sex.Female, 20, 2000, 1);
        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);

        var market = new Mock<IRansomMarketplaceService>();
        var service = new RansomService(repo.Object, Mock.Of<ICharacterFundsService>(),
            Mock.Of<IRansomDecisionService>(), Mock.Of<IFactionService>(), Mock.Of<IFactionFundsRepository>(),
            market.Object, Mock.Of<IRansomPricingService>());

        service.HandleUnpaidRansom(captiveId, captorFaction, amount, true);

        market.Verify(m => m.AddListing(It.Is<RansomListing>(l =>
            l.CaptiveId == captiveId &&
            l.CaptorId == captorFaction &&
            l.Amount == amount &&
            l.CaptorIsFaction)), Times.Once);
    }
}
