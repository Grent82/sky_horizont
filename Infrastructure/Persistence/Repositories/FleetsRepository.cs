using SkyHorizont.Domain.Fleets;
using SkyHorizont.Infrastructure.Persistence.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class FleetsRepository : IFleetRepository
    {
        private readonly IFleetsDbContext _context;

        public FleetsRepository(IFleetsDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<Fleet> GetAll()
        {
            return _context.Fleets.Values.ToList();
        }

        public Fleet? GetById(Guid fleetId)
        {
            if (fleetId == Guid.Empty)
                throw new ArgumentException("Invalid fleet ID", nameof(fleetId));
            return _context.Fleets.TryGetValue(fleetId, out var fleet) ? fleet : null;
        }

        public void Save(Fleet fleet)
        {
            if (fleet == null)
                throw new ArgumentNullException(nameof(fleet));
            if (fleet.Id == Guid.Empty)
                throw new ArgumentException("Fleet ID cannot be empty", nameof(fleet.Id));
            _context.Fleets[fleet.Id] = fleet;
        }

        public IEnumerable<Fleet> GetFleetsForFaction(Guid factionId)
        {
            if (factionId == Guid.Empty)
                return Enumerable.Empty<Fleet>();
            return _context.Fleets.Values
                .Where(f => f.FactionId == factionId)
                .ToList();
        }
    }
}