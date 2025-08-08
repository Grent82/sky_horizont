namespace SkyHorizont.Domain.Entity.Lineage
{
    public interface ILineageRepository
    {
        EntityLineage? FindByChildId(Guid characterId);
        IEnumerable<EntityLineage> FindBySomeParentIds(IEnumerable<Guid> parentIds);
        IEnumerable<EntityLineage> FindAll();

        /// <summary>
        /// Insert if not exists; otherwise update existing record for this child.
        /// </summary>
        void Upsert(EntityLineage lineage);

        /// <summary>
        /// Returns all children for a given parent (bio or adoptive).
        /// </summary>
        IEnumerable<Guid> FindChildrenOfParent(Guid parentId);

        /// <summary>
        /// For validation/guard-rails (optional): check if "candidateAncestorId" is already
        /// an ancestor of "childId" to prevent cycles/incest-like assignment bugs.
        /// </summary>
        bool IsAncestorOf(Guid candidateAncestorId, Guid childId);
    }
}
