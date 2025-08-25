using FluentAssertions;
using Moq;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Infrastructure.Social;
using Xunit;
using SkyHorizont.Domain.Economy;

namespace SkyHorizont.Tests.Infrastructure;

public class BuildInfrastructureResolverTests
{
    [Fact]
    public void Resolve_build_infrastructure_invests_in_planet()
    {
        var actorId = Guid.NewGuid();
        var factionId = Guid.NewGuid();
        var systemId = Guid.NewGuid();

        var actor = new Character(actorId, "A", 30, 1000, 1, Sex.Male, new Personality(50,50,50,50,50), new SkillSet(0,0,0,0));
        var charRepo = new Mock<ICharacterRepository>();
        charRepo.Setup(r => r.GetById(actorId)).Returns(actor);

        var planetRepo = new Mock<IPlanetRepository>();
        var ecoRepo = new Mock<IPlanetEconomyRepository>();
        var budget = 0;
        ecoRepo.Setup(e => e.GetPlanetBudget(It.IsAny<Guid>())).Returns(() => budget);
        ecoRepo.Setup(e => e.AddBudget(It.IsAny<Guid>(), It.IsAny<int>())).Callback<Guid, int>((_, amt) => budget += amt);
        ecoRepo.Setup(e => e.TryDebitBudget(It.IsAny<Guid>(), It.IsAny<int>())).Returns<Guid, int>((_, amt) =>
        {
            if (budget >= amt)
            {
                budget -= amt;
                return true;
            }
            return false;
        });
        var planet = new Planet(Guid.NewGuid(), "Home", systemId, factionId, new Resources(0,0,0), charRepo.Object, planetRepo.Object, ecoRepo.Object, infrastructureLevel: 10, credits: 500);
        planet.Citizens.Add(actorId);
        planetRepo.Setup(r => r.GetById(planet.Id)).Returns(planet);
        planetRepo.Setup(r => r.GetAll()).Returns(new[] { planet });
        planetRepo.Setup(r => r.GetPlanetsControlledByFaction(factionId)).Returns(new[] { planet });
        planetRepo.Setup(r => r.Save(It.IsAny<Planet>()));

        var factionSvc = new Mock<IFactionService>();
        factionSvc.Setup(f => f.GetFactionIdForCharacter(actorId)).Returns(factionId);
        factionSvc.Setup(f => f.GetAllRivalFactions(factionId)).Returns(Array.Empty<Guid>());
        factionSvc.Setup(f => f.GetEconomicStrength(factionId)).Returns(100);

        var fleetRepo = new Mock<IFleetRepository>();
        fleetRepo.Setup(f => f.GetAll()).Returns(Array.Empty<Fleet>());
        fleetRepo.Setup(f => f.GetFleetsForFaction(factionId)).Returns(Array.Empty<Fleet>());

        var piracy = new Mock<IPiracyService>();
        piracy.Setup(p => p.GetPirateActivity(systemId)).Returns(0);
        piracy.Setup(p => p.GetTrafficLevel(systemId)).Returns(0);

        var eventBus = new Mock<IEventBus>();

        var resolver = new InteractionResolver(
            charRepo.Object,
            Mock.Of<IOpinionRepository>(),
            factionSvc.Object,
            Mock.Of<ISecretsRepository>(),
            Mock.Of<IRandomService>(),
            Mock.Of<IDiplomacyService>(),
            Mock.Of<ITravelService>(),
            piracy.Object,
            planetRepo.Object,
            fleetRepo.Object,
            Mock.Of<IFundsService>(),
            eventBus.Object,
            Mock.Of<IBattleOutcomeService>(),
            Mock.Of<IIntimacyLog>(),
            Mock.Of<IMeritPolicy>()
        );

        var intent = new CharacterIntent(actorId, IntentType.BuildInfrastructure, null, null, planet.Id);
        var events = resolver.Resolve(intent, 3000, 1).ToList();

        events.Should().HaveCount(1);
        planet.InfrastructureLevel.Should().Be(20);
        planet.Credits.Should().Be(300);
    }
}
