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
    public void TryResolveRansom_FamilyMemberPays()
    {
        var captiveId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        const int amount = 100;

        var captive = CreateCharacter(captiveId, "Captive");
        var familyMember = CreateCharacter(familyId, "Kin");

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);
        repo.Setup(r => r.GetFamilyMembers(captiveId)).Returns(new[] { familyMember });

        var funds = new Mock<ICharacterFundsService>();
        funds.Setup(f => f.DeductCharacter(familyId, amount)).Returns(true);

        var decision = new Mock<IRansomDecisionService>();
        decision.Setup(d => d.WillPayRansom(familyId, captiveId, amount)).Returns(true);

        var factionSvc = new Mock<IFactionService>();
        var faction = new Faction(Guid.NewGuid(), "f", Guid.NewGuid());
        factionSvc.Setup(f => f.GetFactionIdForCharacter(It.IsAny<Guid>())).Returns(Guid.NewGuid());
        factionSvc.Setup(f => f.GetFaction(It.IsAny<Guid>())).Returns(faction);

        var factionFunds = new Mock<IFactionFundsRepository>();
        var service = new RansomService(repo.Object, funds.Object, decision.Object,
            factionSvc.Object, factionFunds.Object, Mock.Of<IRandomService>(), Mock.Of<IRansomMarketplaceService>());

        var result = service.TryResolveRansom(captiveId, amount, Guid.NewGuid());

        result.Should().BeTrue();
        funds.Verify(f => f.DeductCharacter(familyId, amount), Times.Once);
        funds.Verify(f => f.CreditCharacter(captiveId, amount), Times.Once);
    }

    [Fact]
    public void TryResolveRansom_NoCandidatePays_ReturnsFalse()
    {
        var captiveId = Guid.NewGuid();
        const int amount = 100;
        var captive = CreateCharacter(captiveId, "Captive");

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);
        repo.Setup(r => r.GetFamilyMembers(captiveId)).Returns(Array.Empty<Character>());

        var funds = new Mock<ICharacterFundsService>();
        var decision = new Mock<IRansomDecisionService>();

        var factionSvc = new Mock<IFactionService>();
        var faction = new Faction(Guid.NewGuid(), "f", Guid.NewGuid());
        factionSvc.Setup(f => f.GetFactionIdForCharacter(It.IsAny<Guid>())).Returns(Guid.NewGuid());
        factionSvc.Setup(f => f.GetFaction(It.IsAny<Guid>())).Returns(faction);

        var factionFunds = new Mock<IFactionFundsRepository>();

        var service = new RansomService(repo.Object, funds.Object, decision.Object,
            factionSvc.Object, factionFunds.Object, Mock.Of<IRandomService>(), Mock.Of<IRansomMarketplaceService>());

        var result = service.TryResolveRansom(captiveId, amount, Guid.NewGuid());

        result.Should().BeFalse();
        funds.Verify(f => f.DeductCharacter(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        factionFunds.Verify(f => f.AddBalance(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
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

        var factionSvc = new Mock<IFactionService>();
        var service = new RansomService(repo.Object, Mock.Of<ICharacterFundsService>(),
            Mock.Of<IRansomDecisionService>(), factionSvc.Object, Mock.Of<IFactionFundsRepository>(),
            rng.Object, Mock.Of<IRansomMarketplaceService>());

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

        var service = new RansomService(repo.Object, Mock.Of<ICharacterFundsService>(),
            Mock.Of<IRansomDecisionService>(), factionSvc.Object, Mock.Of<IFactionFundsRepository>(),
            rng.Object, Mock.Of<IRansomMarketplaceService>());

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

        var factionSvc = new Mock<IFactionService>();
        var service = new RansomService(repo.Object, Mock.Of<ICharacterFundsService>(),
            Mock.Of<IRansomDecisionService>(), factionSvc.Object, Mock.Of<IFactionFundsRepository>(),
            rng.Object, Mock.Of<IRansomMarketplaceService>());

        service.HandleUnpaidRansom(captiveId, captorFaction);

        captive.IsAlive.Should().BeFalse();
        captive.IsEnslaved.Should().BeFalse();
    }
}
