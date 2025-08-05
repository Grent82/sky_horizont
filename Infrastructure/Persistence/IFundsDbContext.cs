namespace SkyHorizont.Infrastructure.Persistence
{
    public interface IFundsDbContext
    {
        // Mimics a table of FactionAccount-like records in memory or database
        IDictionary<Guid, int> FactionFunds { get; }

        void SaveChanges();
    }
}