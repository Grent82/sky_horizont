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

        public IEnumerable<Planet> GetAll() =>_context.Planets.Values;

        public Planet? GetById(Guid planetId) =>
            _context.Planets.TryGetValue(planetId, out var cmd) ? cmd : null;

        public void Save(Planet planet)
        {
            _context.Planets[planet.Id] = planet;
        }
    }
}
