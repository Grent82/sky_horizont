using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

internal class InMemoryCommanderFundsRepository : ICommanderFundsRepository
{
    private readonly ICommanderFundsDbContext _context;

    public InMemoryCommanderFundsRepository(ICommanderFundsDbContext context)
    {
        _context = context;
    }

    public int GetBalance(Guid commanderId) =>
        _context.CommanderFunds.TryGetValue(commanderId, out var b) ? b : 0;
    public void AddBalance(Guid commanderId, int amount)
    {
        _context.CommanderFunds[commanderId] = GetBalance(commanderId) + amount;
    }
}