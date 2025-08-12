using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryIntelDbContext : IIntelDbContext
    {
        public IList<IntelReport> Reports { get; } = new List<IntelReport>();
        public void SaveChanges() { /* no-op in memory */ }
    }
}
