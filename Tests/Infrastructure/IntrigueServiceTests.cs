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

    [Fact]
    public void TickPlots_removes_dead_conspirators()
    {
        var ctx = new InMemoryIntrigueDbContext();
        var plotRepo = new PlotRepository(ctx);

        var leaderId = Guid.NewGuid();
        var aliveId = Guid.NewGuid();
        var deadId = Guid.NewGuid();

        var plot = plotRepo.Create(leaderId, "goal", new[] { aliveId, deadId }, Enumerable.Empty<Guid>());

        var leader = new Character(leaderId, "Leader", 30, 0, 1, Sex.Male, new Personality(10,10,10,10,10), new SkillSet(10,10,10,10));

        var alive = new Character(aliveId, "Alive", 30, 0, 1, Sex.Male, new Personality(10,10,10,10,10), new SkillSet(10,10,10,10));

        var dead = new Character(deadId, "Dead", 30, 0, 1, Sex.Male, new Personality(10,10,10,10,10), new SkillSet(10,10,10,10));
        dead.MarkDead();

        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(c => c.GetById(leaderId)).Returns(leader);
        charRepo.Setup(c => c.GetById(aliveId)).Returns(alive);
        charRepo.Setup(c => c.GetById(deadId)).Returns(dead);

        var rng = new Mock<IRandomService>();
        rng.Setup(_ => _.NextDouble()).Returns(1); // avoid exposure and recruitment

        var service = new IntrigueService(
            plotRepo,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IOpinionRepository>(),
            Mock.Of<IFactionService>(),
            charRepo.Object,
            Mock.Of<IIntelService>(),
            rng.Object,
            Mock.Of<IGameClockService>());

        service.TickPlots();

        var updated = plotRepo.GetById(plot.PlotId);
        updated!.Conspirators.Should().Contain(aliveId);
        updated.Conspirators.Should().NotContain(deadId);
    }
}

