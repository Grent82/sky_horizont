namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ICommanderFundsDbContext : IBaseDbContext
    {
        IDictionary<Guid, int> CommanderFunds { get; }
    }
}