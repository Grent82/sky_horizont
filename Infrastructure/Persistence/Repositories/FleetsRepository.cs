using SkyHorizont.Domain.Fleets;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class FleetsRepository : IFleetRepository
    {
        private readonly IFleetsDbContext _context;

        public FleetsRepository(IFleetsDbContext context)
        {
            _context = context;
        }

        public Fleet? GetById(Guid fleetId) =>
            _context.Fleets.TryGetValue(fleetId, out var cmd) ? cmd : null;

        public void Save(Fleet fleet)
        {
            _context.Fleets[fleet.Id] = fleet;
        }
    }
}
