using SkyHorizont.Application;
using SkyHorizont.Application.Turns;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Intrigue;
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


            services.AddScoped<ISocialEventLog, InMemorySocialEventLog>();

            // Domain Service Interfaces → Infrastructure Implementations
            services.AddScoped<IAffectionService, AffectionService>();
            services.AddScoped<IBattleOutcomeService, BattleOutcomeService>();
            services.AddScoped<ICharacterFundsService, CharacterFundsService>();
            services.AddScoped<IFactionTaxService, FactionTaxService>();
            services.AddScoped<IFundsService, FundsService>();
            services.AddScoped<IMoraleService, MoraleService>();
            services.AddScoped<IRansomService, RansomService>();
            services.AddScoped<ICharacterLifecycleService, CharacterLifecycleService>();
            services.AddSingleton<IPersonalityInheritanceService, SimplePersonalityInheritanceService>();
            services.AddSingleton<IMortalityModel, GompertzMortalityModel>();
            services.AddSingleton<IFactionInfo, FactionInfoService>();

            services.AddScoped<IIntentPlanner, IntentPlanner>();
            services.AddScoped<IInteractionResolver, InteractionResolver>();


            // Application Layer
            services.AddScoped<IGameClockService, GameClockService>();
            services.AddScoped<ITurnProcessor, TurnProcessor>();
            services.AddSingleton<IRandomService>(_ => new RandomService(rngSeed));
            services.AddSingleton<INameGenerator, NameGenerator>();
            

            return services;
        }
    }
}
