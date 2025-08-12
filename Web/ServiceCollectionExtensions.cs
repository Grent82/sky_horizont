using Infrastructure.Persistence.Repositories;
using SkyHorizont.Application;
using SkyHorizont.Application.Turns;
using SkyHorizont.Domain.Battle;
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
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Persistence.Interfaces;
using SkyHorizont.Infrastructure.Persistence.Intrigue;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Infrastructure.Social;

namespace SkyHorizont.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSkyHorizontSimulationServices(
            this IServiceCollection services, int rngSeed = 0)
        {
            // Core utilities
            services.AddScoped<IAffectionDbContext, InMemoryAffectionDbContext>();
            services.AddScoped<ICharacterFundsDbContext, InMemoryCharacterFundsDbContext>();
            services.AddScoped<ICharactersDbContext, InMemoryCharactersDbContext>();
            services.AddScoped<IFactionFundsDbContext, InMemoryFundsDbContext>();
            services.AddScoped<IFleetsDbContext, InMemoryFleetsDbContext>();
            services.AddScoped<IPlanetsDbContext, InMemoryPlanetsDbContext>();
            services.AddScoped<ILineageDbContext, InMemoryLinageDbContext>();
            services.AddScoped<IOpinionsDbContext, InMemoryOpinionsDbContext>();
            services.AddScoped<ISecretsDbContext, InMemorySecretsDbContext>();
            services.AddScoped<IFactionsDbContext, InMemoryFactionsDbContext>();
            services.AddScoped<IIntrigueDbContext, InMemoryIntrigueDbContext>();
            services.AddScoped<IIntelDbContext, InMemoryIntelDbContext>();
            services.AddScoped<IPlanetEconomyDbContext, InMemoryPlanetEconomyDbContext>();
            services.AddScoped<IResearchDbContext, InMemoryResearchDbContext>();


            services.AddScoped<IAffectionRepository, AffectionRepository>();
            services.AddScoped<ICharacterFundsRepository, CharacterFundsRepository>();
            services.AddScoped<ICharacterRepository, CharactersRepository>();
            services.AddScoped<IFactionFundsRepository, FactionFundsRepository>();
            services.AddScoped<IFleetRepository, FleetsRepository>();
            services.AddScoped<IPlanetRepository, PlanetsRepository>();
            services.AddScoped<ILineageRepository, LineageRepository>();
            services.AddScoped<IOpinionRepository, OpinionRepository>();
            services.AddScoped<ISecretsRepository, SecretsRepository>();
            services.AddScoped<IFactionRepository, FactionRepository>();
            services.AddScoped<IPlotRepository, PlotRepository>();
            services.AddScoped<IIntelRepository, IntelRepository>();
            services.AddScoped<IPlanetEconomyRepository, PlanetEconomyRepository>();
            services.AddScoped<IResearchRepository, ResearchRepository>();


            services.AddScoped<ISocialEventLog, InMemorySocialEventLog>();

            // Domain Service Interfaces → Infrastructure Implementations
            services.AddScoped<IAffectionService, AffectionService>();
            services.AddScoped<IBattleOutcomeService, BattleOutcomeService>();
            services.AddScoped<ICharacterFundsService, CharacterFundsService>();
            services.AddScoped<ICharacterLifecycleService, CharacterLifecycleService>();
            services.AddScoped<IFactionService, FactionService>();
            services.AddScoped<IFactionTaxService, FactionTaxService>();
            services.AddScoped<IFactionService, FactionService>();
            services.AddScoped<IMortalityModel, GompertzMortalityModel>();
            services.AddScoped<IFundsService, FundsService>();
            services.AddScoped<IIntelService, IntelService>();
            services.AddScoped<IIntrigueService, IntrigueService>();
            services.AddScoped<IMoraleService, MoraleService>();
            services.AddScoped<IPlanetService, PlanetService>();
            services.AddScoped<IRansomService, RansomService>();
            services.AddScoped<IEconomyService, EconomyService>();
            services.AddScoped<IResearchService, ResearchService>();
            services.AddScoped<IIntentPlanner, IntentPlanner>();
            services.AddScoped<IInteractionResolver, InteractionResolver>();
            services.AddSingleton<IPersonalityInheritanceService, SimplePersonalityInheritanceService>();


            // Application Layer
            services.AddSingleton<IGameClockService, GameClockService>();
            services.AddSingleton<ITurnProcessor, TurnProcessor>();
            services.AddSingleton<IRandomService>(_ => new RandomService(rngSeed));
            services.AddSingleton<INameGenerator, NameGenerator>();
            

            return services;
        }
    }
}
