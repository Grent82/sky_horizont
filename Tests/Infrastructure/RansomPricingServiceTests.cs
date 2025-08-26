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

public class RansomPricingServiceTests
{
    [Fact]
    public void EstimateRansomValue_ConsidersRankParentsAndFaction()
    {
        var captiveId = Guid.NewGuid();
        var factionId = Guid.NewGuid();
        var captive = CharacterFactory.CreateSuperPositive(captiveId, "Cap", Sex.Male, 20, 2000, 1, Rank.Captain);
        var mother = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Mom", Sex.Female, 40, 1980, 1, Rank.General);
        var father = CharacterFactory.CreateSuperPositive(Guid.NewGuid(), "Dad", Sex.Male, 42, 1978, 1, Rank.Colonel);

        mother.AddRelationship(father.Id, RelationshipType.Spouse);
        father.AddRelationship(mother.Id, RelationshipType.Spouse);

        var repo = new Mock<ICharacterRepository>();
        repo.Setup(r => r.GetById(captiveId)).Returns(captive);
        repo.Setup(r => r.GetFamilyMembers(captiveId)).Returns(new[] { mother, father });

        var factions = new Mock<IFactionService>();
        factions.Setup(f => f.GetFactionIdForCharacter(captiveId)).Returns(factionId);
        factions.Setup(f => f.GetEconomicStrength(factionId)).Returns(1000);

        var service = new RansomPricingService(repo.Object, factions.Object);

        var price = service.EstimateRansomValue(captiveId);

        // base 800 * parent modifier (>1) * faction factor (2.0)
        price.Should().BeGreaterThan(800);
    }
}
