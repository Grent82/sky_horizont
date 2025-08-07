namespace SkyHorizont.Domain.Entity.Lineage
{
    public interface IFamilyTreeService
    {
        IReadOnlyList<Guid> GetSiblings(Guid characterId);

        IReadOnlyList<Guid> GetGrandparents(Guid characterId);

        IReadOnlyList<Guid> GetAncestors(Guid characterId, int maxDepth = 3);

        IReadOnlyList<Guid> GetDescendants(Guid characterId, int maxDepth = 3);
    }
}