using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryPlanetRepository : IPlanetRepository
    {
        private readonly Dictionary<Guid, Planet> _storage = new();

        public Planet? GetById(Guid planetId) =>
            _storage.TryGetValue(planetId, out var cmd) ? cmd : null;

        public void Save(Planet fleet)
        {
            _storage[fleet.Id] = fleet;
        }
    }
}
