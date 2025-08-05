namespace SkyHorizont.Domain.Entity.Lineage
{
    public interface IFamilyTreeService
    {
        IReadOnlyList<Guid> GetSiblings(Guid commanderId);

        IReadOnlyList<Guid> GetGrandparents(Guid commanderId);

        IReadOnlyList<Guid> GetAncestors(Guid commanderId, int maxDepth = 3);

        IReadOnlyList<Guid> GetDescendants(Guid commanderId, int maxDepth = 3);
    }
}