using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Application;
using SkyHorizont.Infrastructure.DomainServices;
using SkyHorizont.Infrastructure.Persistence;
using SkyHorizont.Infrastructure.Persistence.Interfaces;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Services;
using SkyHorizont.Application.Turns;
using SkyHorizont.Infrastructure.Repository;
using SkyHorizont.Domain.Entity.Lineage;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

// Repositories & DBContexts
services.AddScoped<IAffectionDbContext, InMemoryAffectionDbContext>();
services.AddScoped<ICharacterFundsDbContext, InMemoryCharacterFundsDbContext>();
services.AddScoped<ICharactersDbContext, InMemoryCharactersDbContext>();
services.AddScoped<IFactionFundsDbContext, InMemoryFundsDbContext>();
services.AddScoped<IFleetsDbContext, InMemoryFleetsDbContext>();
services.AddScoped<IPlanetsDbContext, InMemoryPlanetsDbContext>();
services.AddScoped<ILineageDbContext, InMemoryLinageDbContext>();

services.AddScoped<IAffectionRepository, AffectionRepository>();
services.AddScoped<ICharacterFundsRepository, CharacterFundsRepository>();
services.AddScoped<ICharacterRepository, CharactersRepository>();
services.AddScoped<IFactionFundsRepository, FactionFundsRepository>();
services.AddScoped<IFleetRepository, FleetsRepository>();
services.AddScoped<IPlanetRepository, PlanetsRepository>();
services.AddScoped<ILineageRepository, LineageRepository>();

// Domain Service Interfaces → Infrastructure Implementations
services.AddScoped<IAffectionService, AffectionService>();
services.AddScoped<IBattleOutcomeService, BattleOutcomeService>();
services.AddScoped<ICharacterFundsService, CharacterFundsService>();
services.AddScoped<IFactionTaxService, FactionTaxService>();
services.AddScoped<IFundsService, FundsService>();
services.AddScoped<IMoraleService, MoraleService>();
services.AddScoped<IRansomService, RansomService>();
services.AddScoped<ICharacterLifecycleService, CharacterLifecycleService>();

// Application Layer
services.AddScoped<IGameClockService, GameClockService>();
services.AddScoped<ITurnProcessor, TurnProcessor>();

var app = builder.Build();
// app.MapControllers(), etc.
app.Run();
