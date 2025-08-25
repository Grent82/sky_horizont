using System.Reflection;
using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Infrastructure.Social;
using Xunit;

namespace SkyHorizont.Tests.Infrastructure;

public class AmbitionBiasTests
{
    private static AmbitionBias Invoke(CharacterAmbition ambition)
    {
        var planner = new IntentPlanner(
            Mock.Of<ICharacterRepository>(),
            Mock.Of<IOpinionRepository>(),
            Mock.Of<IFactionService>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IPlanetRepository>(),
            Mock.Of<IFleetRepository>(),
            Mock.Of<IPiracyService>(),
            Enumerable.Empty<IIntentRule>());

        var method = typeof(IntentPlanner).GetMethod("GetAmbitionBias", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (AmbitionBias)method.Invoke(planner, new object[] { ambition })!;
    }

    public static IEnumerable<object[]> BiasData()
    {
        yield return new object[]
        {
            CharacterAmbition.GainPower,
            new Dictionary<IntentType, double>
            {
                { IntentType.Court, 0.8 },
                { IntentType.VisitFamily, 0.7 },
                { IntentType.Spy, 1.2 },
                { IntentType.Bribe, 1.1 },
                { IntentType.Recruit, 1.2 },
                { IntentType.Defect, 1.3 },
                { IntentType.Negotiate, 1.0 },
                { IntentType.Quarrel, 1.0 },
                { IntentType.Assassinate, 1.3 },
                { IntentType.TorturePrisoner, 1.0 },
                { IntentType.RapePrisoner, 0.9 },
                { IntentType.TravelToPlanet, 0.8 },
                { IntentType.BecomePirate, 0.9 },
                { IntentType.RaidConvoy, 0.8 }
            }
        };

        yield return new object[]
        {
            CharacterAmbition.BuildWealth,
            new Dictionary<IntentType, double>
            {
                { IntentType.Court, 0.9 },
                { IntentType.VisitFamily, 0.8 },
                { IntentType.Spy, 1.1 },
                { IntentType.Bribe, 1.3 },
                { IntentType.Recruit, 1.1 },
                { IntentType.Defect, 0.8 },
                { IntentType.Negotiate, 1.2 },
                { IntentType.Quarrel, 0.7 },
                { IntentType.Assassinate, 0.8 },
                { IntentType.TorturePrisoner, 0.7 },
                { IntentType.RapePrisoner, 0.6 },
                { IntentType.TravelToPlanet, 1.0 },
                { IntentType.BecomePirate, 1.2 },
                { IntentType.RaidConvoy, 1.3 }
            }
        };

        yield return new object[]
        {
            CharacterAmbition.EnsureFamilyLegacy,
            new Dictionary<IntentType, double>
            {
                { IntentType.Court, 1.2 },
                { IntentType.VisitFamily, 1.3 },
                { IntentType.Spy, 0.8 },
                { IntentType.Bribe, 0.9 },
                { IntentType.Recruit, 0.9 },
                { IntentType.Defect, 0.7 },
                { IntentType.Negotiate, 0.9 },
                { IntentType.Quarrel, 0.8 },
                { IntentType.Assassinate, 0.7 },
                { IntentType.TorturePrisoner, 0.6 },
                { IntentType.RapePrisoner, 0.5 },
                { IntentType.TravelToPlanet, 1.1 },
                { IntentType.BecomePirate, 0.7 },
                { IntentType.RaidConvoy, 0.6 }
            }
        };

        yield return new object[]
        {
            CharacterAmbition.SeekAdventure,
            new Dictionary<IntentType, double>
            {
                { IntentType.Court, 0.9 },
                { IntentType.VisitFamily, 0.8 },
                { IntentType.Spy, 1.2 },
                { IntentType.Bribe, 0.9 },
                { IntentType.Recruit, 0.9 },
                { IntentType.Defect, 1.0 },
                { IntentType.Negotiate, 0.9 },
                { IntentType.Quarrel, 1.0 },
                { IntentType.Assassinate, 1.0 },
                { IntentType.TorturePrisoner, 0.8 },
                { IntentType.RapePrisoner, 0.7 },
                { IntentType.TravelToPlanet, 1.3 },
                { IntentType.BecomePirate, 1.2 },
                { IntentType.RaidConvoy, 1.2 }
            }
        };
    }

    [Theory]
    [MemberData(nameof(BiasData))]
    public void GetAmbitionBias_maps_values(CharacterAmbition ambition, Dictionary<IntentType, double> expected)
    {
        var bias = Invoke(ambition);
        foreach (var intent in Enum.GetValues<IntentType>())
        {
            var expectedValue = expected.TryGetValue(intent, out var v) ? v : 1.0;
            bias[intent].Should().Be(expectedValue);
        }
    }
}
