using SkyHorizont.Domain.Factions;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    /// <summary>
    /// In-memory implementation for IFactionsDbContext.
    /// </summary>
    public class InMemoryFactionsDbContext : IFactionsDbContext
    {
        public Dictionary<Guid, Guid> CharacterFaction { get; } = new();
        public Dictionary<Guid, Guid?> FactionLeaders { get; } = new();

        // Store as normalized unordered pairs (minGuid, maxGuid)
        public HashSet<(Guid a, Guid b)> WarPairs { get; } = new();
        public HashSet<(Guid a, Guid b)> RivalPairs { get; } = new();

        public Dictionary<Guid, Faction?> Factions { get; } = new();

        public void SaveChanges()
        {
            // In-memory no-op
        }
    }
}
