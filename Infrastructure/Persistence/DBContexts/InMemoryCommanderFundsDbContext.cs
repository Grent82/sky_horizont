using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryCommanderFundsDbContext : ICommanderFundsDbContext
    {
        public IDictionary<Guid, int> CommanderFunds { get; } = new Dictionary<Guid, int>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
