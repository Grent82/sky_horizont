using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class CommandersRepository : ICommanderRepository
    {
        private readonly ICommandersDbContext _context;

        public CommandersRepository(ICommandersDbContext context)
        {
            _context = context;
        }

        public Commander? GetById(Guid commanderId) =>
            _context.Commanders.TryGetValue(commanderId, out var cmd) ? cmd : null;

        public void Save(Commander commander)
        {
            _context.Commanders[commander.Id] = commander;
        }
    }
}
