using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryCharacterFundsDbContext : ICharacterFundsDbContext
    {
        public IDictionary<Guid, int> CharacterFunds { get; } = new Dictionary<Guid, int>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}
