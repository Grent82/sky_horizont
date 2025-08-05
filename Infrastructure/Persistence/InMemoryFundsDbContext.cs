namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryFundsDbContext : IFundsDbContext
    {
        public IDictionary<Guid, int> FactionFunds { get; } = new Dictionary<Guid, int>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
