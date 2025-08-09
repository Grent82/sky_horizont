using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemorySecretsDbContext : ISecretsDbContext
    {
        public Dictionary<Guid, Secret> Secrets { get; } = new();

        public void SaveChanges()
        {
            // In-memory no-op
        }
    }
}
