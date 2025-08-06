using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryCommandersDbContext : ICommandersDbContext
    {
        public IDictionary<Guid, Commander> Commanders { get; } = new Dictionary<Guid, Commander>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
