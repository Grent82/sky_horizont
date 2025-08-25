using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;
using SkyHorizont.Infrastructure.Social.IntentRules;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class BuildInfrastructureIntentRuleTests
{
    private static double GetScore(bool economyWeak = false, int infra = 80, CharacterAmbition ambition = CharacterAmbition.SeekAdventure)
    {
        var actorId = Guid.NewGuid();
        var factionId = Guid.NewGuid();
        var systemId = Guid.NewGuid();

        var charRepo = Mock.Of<ICharacterRepository>();
        var planetRepo = new Mock<IPlanetRepository>();
        var planet = new Planet(Guid.NewGuid(), "P", systemId, factionId, new Resources(0,0,0), charRepo, planetRepo.Object, infrastructureLevel: infra, credits: 1000);
        planet.Citizens.Add(actorId);
        planetRepo.Setup(r => r.GetAll()).Returns(new[] { planet });
        planetRepo.Setup(r => r.GetById(planet.Id)).Returns(planet);

        var rule = new BuildInfrastructureIntentRule(planetRepo.Object);

        var ctx = new IntentContext(
            new Character(actorId, "A", 30, 1000, 1, Sex.Male, new Personality(50,50,50,50,50), new SkillSet(0,0,0,0)),
            factionId,
            new FactionStatus(false, false, false, economyWeak),
            systemId,
            null,
            null,
            Array.Empty<Character>(),
            Array.Empty<Character>(),
            Array.Empty<Character>(),
            ambition,
            new AmbitionBias(),
            _ => 0,
            _ => factionId,
            PlannerConfig.Default);

        return rule.Generate(ctx).First().Score;
    }

    [Fact]
    public void Scores_higher_when_economy_weak()
    {
        var baseline = GetScore();
        var weak = GetScore(economyWeak: true);
        weak.Should().BeGreaterThan(baseline);
    }

    [Fact]
    public void Scores_higher_when_infrastructure_low()
    {
        var baseline = GetScore();
        var low = GetScore(infra: 20);
        low.Should().BeGreaterThan(baseline);
    }

    [Theory]
    [InlineData(CharacterAmbition.BuildWealth)]
    [InlineData(CharacterAmbition.EnsureFamilyLegacy)]
    public void Scores_higher_for_specific_ambitions(CharacterAmbition ambition)
    {
        var baseline = GetScore();
        var biased = GetScore(ambition: ambition);
        biased.Should().BeGreaterThan(baseline);
    }
}
