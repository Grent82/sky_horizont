using System;
using System.Linq;
using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence.Intrigue;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class IntrigueServiceTests
{
    [Fact]
    public void TickPlots_removes_plots_with_dead_leader()
    {
        var ctx = new InMemoryIntrigueDbContext();
        var plotRepo = new PlotRepository(ctx);
        var leaderId = Guid.NewGuid();
        plotRepo.Create(leaderId, "goal", Enumerable.Empty<Guid>(), Enumerable.Empty<Guid>());

        var leader = new Character(
            leaderId,
            "Leader",
            30,
            0,
            1,
            Sex.Male,
            new Personality(10,10,10,10,10),
            new SkillSet(10,10,10,10));
        leader.MarkDead();

        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(c => c.GetById(leaderId)).Returns(leader);

        var service = new IntrigueService(
            plotRepo,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IOpinionRepository>(),
            Mock.Of<IFactionService>(),
            charRepo.Object,
            Mock.Of<IIntelService>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IGameClockService>());

        service.TickPlots();

        plotRepo.GetAll().Should().BeEmpty();
    }
}

