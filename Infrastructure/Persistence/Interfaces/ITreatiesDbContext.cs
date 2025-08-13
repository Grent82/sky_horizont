using SkyHorizont.Domain.Diplomacy;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ITreatiesDbContext : IBaseDbContext
    {
        Dictionary<Guid, Treaty> Treaties { get; }
    }
}
