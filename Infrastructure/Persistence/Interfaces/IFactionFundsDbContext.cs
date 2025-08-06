namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IFactionFundsDbContext : IBaseDbContext
    {
        IDictionary<Guid, int> FactionFunds { get; }
    }
}