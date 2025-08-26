using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Infrastructure.Utility;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class FactionServiceNegotiationTests
{
    [Fact]
    public void NegotiatePrisonerExchange_Succeeds_AdjustsGoodwill()
    {
        var ctx = new InMemoryFactionsDbContext();
        var repo = new FactionRepository(ctx);
        var svc = new FactionService(repo, Mock.Of<IPlanetRepository>());

        var factionA = new Faction(Guid.NewGuid(), "A", Guid.NewGuid());
        var factionB = new Faction(Guid.NewGuid(), "B", Guid.NewGuid());
        ctx.Factions[factionA.Id] = factionA;
        ctx.Factions[factionB.Id] = factionB;

        var charA = Guid.NewGuid();
        var charB = Guid.NewGuid();
        ctx.MapCharacterToFaction(charA, factionA.Id);
        ctx.MapCharacterToFaction(charB, factionB.Id);

        var result = svc.NegotiatePrisonerExchange(charA, charB);

        result.Should().BeTrue();
        factionA.Diplomacy[factionB.Id].Value.Should().BeGreaterThan(0);
        factionB.Diplomacy[factionA.Id].Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void NegotiatePrisonerExchange_FailsDueToWar_AdjustsHostility()
    {
        var ctx = new InMemoryFactionsDbContext();
        var repo = new FactionRepository(ctx);
        var svc = new FactionService(repo, Mock.Of<IPlanetRepository>());

        var factionA = new Faction(Guid.NewGuid(), "A", Guid.NewGuid());
        var factionB = new Faction(Guid.NewGuid(), "B", Guid.NewGuid());
        ctx.Factions[factionA.Id] = factionA;
        ctx.Factions[factionB.Id] = factionB;

        var charA = Guid.NewGuid();
        var charB = Guid.NewGuid();
        ctx.MapCharacterToFaction(charA, factionA.Id);
        ctx.MapCharacterToFaction(charB, factionB.Id);
        ctx.AddWar(factionA.Id, factionB.Id);

        var result = svc.NegotiatePrisonerExchange(charA, charB);

        result.Should().BeFalse();
        factionA.Diplomacy[factionB.Id].Value.Should().BeLessThan(0);
        factionB.Diplomacy[factionA.Id].Value.Should().BeLessThan(0);
    }
}

