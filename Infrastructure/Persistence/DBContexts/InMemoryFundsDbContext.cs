using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryFundsDbContext : IFactionFundsDbContext
    {
        public IDictionary<Guid, int> FactionFunds { get; } = new Dictionary<Guid, int>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
