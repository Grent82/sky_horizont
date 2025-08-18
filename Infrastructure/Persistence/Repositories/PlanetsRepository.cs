using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class PlanetsRepository : IPlanetRepository
    {
        private readonly IPlanetsDbContext _context;

        public PlanetsRepository(IPlanetsDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<Planet> GetAll()
        {
            return _context.Planets.Values.ToList();
        }

        public Planet? GetById(Guid planetId)
        {
            if (planetId == Guid.Empty)
                throw new ArgumentException("Invalid planet ID", nameof(planetId));
            return _context.Planets.TryGetValue(planetId, out var planet) ? planet : null;
        }

        public void Save(Planet planet)
        {
            if (planet == null)
                throw new ArgumentNullException(nameof(planet));
            if (planet.Id == Guid.Empty)
                throw new ArgumentException("Planet ID cannot be empty", nameof(planet.Id));
            _context.Planets[planet.Id] = planet;
        }

        public IEnumerable<Planet> GetPlanetsControlledByFaction(Guid factionId)
        {
            if (factionId == Guid.Empty)
                return Enumerable.Empty<Planet>();
            return _context.Planets.Values
                .Where(p => p.FactionId == factionId)
                .ToList();
        }
    }
}
