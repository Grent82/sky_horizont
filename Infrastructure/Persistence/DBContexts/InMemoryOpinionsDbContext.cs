using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryOpinionsDbContext : IOpinionsDbContext
    {
        public Dictionary<(Guid source, Guid target), int> Opinions { get; } = new();
        public Dictionary<(Guid source, Guid target), List<string>> OpinionReasons { get; } = new();

        public void SaveChanges()
        {
            // In-memory no-op
        }
    }
}
