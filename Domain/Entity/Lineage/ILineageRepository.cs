namespace SkyHorizont.Domain.Entity.Lineage
{
    public interface ILineageRepository
    {
        EntityLineage? FindByChildId(Guid characterId);

        IEnumerable<EntityLineage> FindBySomeParentIds(IEnumerable<Guid> parentIds);

        IEnumerable<EntityLineage> FindAll();
    }
}
