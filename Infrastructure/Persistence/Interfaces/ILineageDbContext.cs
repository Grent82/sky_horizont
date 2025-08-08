using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ILineageDbContext : IBaseDbContext
    {
        Dictionary<Guid, EntityLineage> EntityLineagebyChild  { get; }
    }
}