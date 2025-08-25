using Infrastructure.Persistence.Repositories;
using SkyHorizont.Application;
using SkyHorizont.Application.Turns;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Domain.Economy;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Research;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Persistence.Diplomacy;
using SkyHorizont.Infrastructure.Persistence.Interfaces;
using SkyHorizont.Infrastructure.Persistence.Intrigue;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Infrastructure.Social;
using SkyHorizont.Infrastructure.Social.IntentRules;

namespace SkyHorizont.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSkyHorizontSimulationServices(
            this IServiceCollection services, int rngSeed = 0)
        {
            services.AddScoped<IAffectionDbContext, InMemoryAffectionDbContext>();
            services.AddScoped<ICharacterFundsDbContext, InMemoryCharacterFundsDbContext>();
            services.AddScoped<ICharactersDbContext, InMemoryCharactersDbContext>();
            services.AddScoped<IDiplomacyDbContext, InMemoryDiplomacyDbContext>();
            services.AddScoped<IFactionFundsDbContext, InMemoryFundsDbContext>();
            services.AddScoped<IFactionsDbContext, InMemoryFactionsDbContext>();
            services.AddScoped<IFleetsDbContext, InMemoryFleetsDbContext>();
            services.AddScoped<IIntelDbContext, InMemoryIntelDbContext>();
            services.AddScoped<IIntrigueDbContext, InMemoryIntrigueDbContext>();
            services.AddScoped<ILineageDbContext, InMemoryLinageDbContext>();
            services.AddScoped<IOpinionsDbContext, InMemoryOpinionsDbContext>();
            services.AddScoped<IPlanetEconomyDbContext, InMemoryPlanetEconomyDbContext>();
            services.AddScoped<IPlanetsDbContext, InMemoryPlanetsDbContext>();
            services.AddScoped<IResearchDbContext, InMemoryResearchDbContext>();
            services.AddScoped<ISecretsDbContext, InMemorySecretsDbContext>();
            services.AddScoped<ITravelDbContext, InMemoryTravelDbContext>();

            services.AddScoped<IAffectionRepository, AffectionRepository>();
            services.AddScoped<ICharacterFundsRepository, CharacterFundsRepository>();
            services.AddScoped<ICharacterRepository, CharactersRepository>();
            services.AddScoped<IDiplomacyRepository, DiplomacyRepository>();
            services.AddScoped<IFactionFundsRepository, FactionFundsRepository>();
            services.AddScoped<IFactionRepository, FactionRepository>();
            services.AddScoped<IFleetRepository, FleetsRepository>();
            services.AddScoped<IIntelRepository, IntelRepository>();
            services.AddScoped<ILineageRepository, LineageRepository>();
            services.AddScoped<IOpinionRepository, OpinionRepository>();
            services.AddScoped<IPlanetEconomyRepository, PlanetEconomyRepository>();
            services.AddScoped<IPlanetRepository, PlanetsRepository>();
            services.AddScoped<IPlotRepository, PlotRepository>();
            services.AddScoped<IResearchRepository, ResearchRepository>();
            services.AddScoped<ISecretsRepository, SecretsRepository>();
            services.AddScoped<ITravelRepository, TravelRepository>();

            services.AddScoped<ISocialEventLog, InMemorySocialEventLog>();

            services.AddScoped<IAffectionService, AffectionService>();
            services.AddScoped<IBattleOutcomeService, BattleOutcomeService>();
            services.AddScoped<ICharacterFundsService, CharacterFundsService>();
            services.AddScoped<ICharacterLifecycleService, CharacterLifecycleService>();
            services.AddScoped<IDiplomacyService, DiplomacyService>();
            services.AddScoped<IEconomyService, EconomyService>();
            services.AddScoped<IFactionService, FactionService>();
            services.AddScoped<IFactionTaxService, FactionTaxService>();
            services.AddScoped<IFundsService, FundsService>();
            services.AddScoped<IMortalityModel, GompertzMortalityModel>();
            services.AddScoped<IIntelService, IntelService>();
            services.AddScoped<IIntimacyLog, InMemoryIntimacyLog>();
            services.AddScoped<IIntrigueService, IntrigueService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IMeritPolicy, MeritPolicy>();
            services.AddScoped<IMoraleService, MoraleService>();
            services.AddScoped<IPiracyService, PiracyService>();
            services.AddScoped<IPlanetService, PlanetService>();
            services.AddScoped<IPregnancyPolicy, DefaultPregnancyPolicy>();
            services.AddScoped<IRansomService, RansomService>();
            services.AddScoped<IResearchService, ResearchService>();
            services.AddScoped<ITravelService, TravelService>();


            services.AddScoped<IIntentRule, CourtshipIntentRule>();
            services.AddScoped<IIntentRule, VisitFamilyIntentRule>();
            services.AddScoped<IIntentRule, VisitLoverIntentRule>();
            services.AddScoped<IIntentRule, SpyIntentRule>();
            services.AddScoped<IIntentRule, BribeIntentRule>();
            services.AddScoped<IIntentRule, RecruitIntentRule>();
            services.AddScoped<IIntentRule, DefectIntentRule>();
            services.AddScoped<IIntentRule, NegotiateIntentRule>();
            services.AddScoped<IIntentRule, QuarrelIntentRule>();
            services.AddScoped<IIntentRule, AssassinateIntentRule>();
            services.AddScoped<IIntentRule, TorturePrisonerIntentRule>();
            services.AddScoped<IIntentRule, RapePrisonerIntentRule>();
            services.AddScoped<IIntentRule, TravelIntentRule>();
            services.AddScoped<IIntentRule, BecomePirateIntentRule>();
            services.AddScoped<IIntentRule, RaidConvoyIntentRule>();
            services.AddScoped<IIntentRule, FoundHouseIntentRule>();
            services.AddScoped<IIntentRule, FoundPirateClanIntentRule>();
            services.AddScoped<IIntentRule, ExpelFromHouseIntentRule>();
            services.AddScoped<IIntentRule, ClaimPlanetSeatIntentRule>();
            services.AddScoped<IIntentRule, BuildFleetIntentRule>();
            services.AddScoped<IIntentPlanner, IntentPlanner>();
            services.AddScoped<IInteractionResolver, InteractionResolver>();

            services.AddSingleton<IGameClockService, GameClockService>();
            services.AddScoped<ITurnProcessor, TurnProcessor>();
            services.AddSingleton<IRandomService>(_ => new RandomService(rngSeed));
            services.AddSingleton<INameGenerator, NameGenerator>();
            services.AddSingleton<IPersonalityInheritanceService, SimplePersonalityInheritanceService>();
            services.AddSingleton<ISkillInheritanceService, SimpleSkillInheritanceService>();
            
        
            return services;
        }
    }
}
