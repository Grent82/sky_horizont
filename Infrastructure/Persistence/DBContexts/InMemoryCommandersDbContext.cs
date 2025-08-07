using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryCharactersDbContext : ICharactersDbContext
    {
        public IDictionary<Guid, Character> Characters { get; } = new Dictionary<Guid, Character>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
