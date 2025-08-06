using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class PlanetsRepository : IPlanetRepository
    {
        private readonly IPlanetsDbContext _context;

        public PlanetsRepository(IPlanetsDbContext context)
        {
            _context = context;
        }

        public Planet? GetById(Guid planetId) =>
            _context.Planets.TryGetValue(planetId, out var cmd) ? cmd : null;

        public void Save(Planet fleet)
        {
            _context.Planets[fleet.Id] = fleet;
        }
    }
}
