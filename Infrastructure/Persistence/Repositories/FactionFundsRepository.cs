using SkyHorizont.Domain.Factions;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class FactionFundsRepository : IFactionFundsRepository
    {
        private readonly IFactionFundsDbContext _context;

        public FactionFundsRepository(IFactionFundsDbContext context)
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

        public void DeductBalance(Guid factionId, int delta)
        {
            _context.FactionFunds[factionId] = GetBalance(factionId) - delta;
            _context.SaveChanges();
        }
    }

}