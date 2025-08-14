using SkyHorizont.Domain.Diplomacy;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IDiplomacyDbContext : IBaseDbContext
    {
        Dictionary<Guid, Treaty> Treaties { get; }
    }
}
