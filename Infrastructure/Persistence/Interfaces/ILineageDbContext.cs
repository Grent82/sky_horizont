using SkyHorizont.Domain.Entity.Lineage;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ILineageDbContext : IBaseDbContext
    {
        Dictionary<Guid, EntityLineage> EntityLineagebyChild  { get; }
    }
}