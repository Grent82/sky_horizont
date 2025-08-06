using SkyHorizont.Domain.Factions;
using SkyHorizont.Infrastructure.Persistence;

namespace SkyHorizont.Infrastruture.Repositories
{
    public class FactionFundsRepository : IFactionFundsRepository
    {
        private readonly IFundsDbContext _context;

        public FactionFundsRepository(IFundsDbContext context)
        {
            _context = context;
        }

        public int GetBalance(Guid factionId)
            => _context.FactionFunds.TryGetValue(factionId, out var credits) ? credits : 0;

        public void AddBalance(Guid factionId, int delta)
        {
            _context.FactionFunds[factionId] = GetBalance(factionId) + delta;
            _context.SaveChanges();
        }
    }

}