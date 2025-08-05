using SkyHorizont.Domain.Fleets;

namespace SkyHorizont.Infrastructure.Repositories
{
    public class InMemoryFleetRepository : IFleetRepository
    {
        private readonly Dictionary<Guid, Fleet> _storage = new();

        public Fleet? GetById(Guid fleetId) =>
            _storage.TryGetValue(fleetId, out var cmd) ? cmd : null;

        public void Save(Fleet fleet)
        {
            _storage[fleet.Id] = fleet;
        }
    }
}
