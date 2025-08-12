using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Repositories
{
    public class PlanetBudgetRepository : IPlanetBudgedRepository
    {
        private readonly IPlanetEconomyDbContext _ctx;

        public PlanetBudgetRepository(IPlanetEconomyDbContext ctx) => _ctx = ctx;

        public void AddBudget(Guid planetId, int credits)
        {

            _ctx.PlanetBudgets.TryGetValue(planetId, out var current);
            _ctx.PlanetBudgets[planetId] = current + credits;
            _ctx.SaveChanges();
        }

        public int GetPlanetBudget(Guid planetId)
        {
            _ctx.PlanetBudgets.TryGetValue(planetId, out var val);
            return val;
        }
    }
}