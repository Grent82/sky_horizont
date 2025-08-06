using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryCommanderRepository : ICommanderRepository
    {
        private readonly Dictionary<Guid, Commander> _storage = new();

        public Commander? GetById(Guid commanderId) =>
            _storage.TryGetValue(commanderId, out var cmd) ? cmd : null;

        public void Save(Commander commander)
        {
            _storage[commander.Id] = commander;
        }
    }
}
