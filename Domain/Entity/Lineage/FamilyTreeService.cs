
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity.Lineage
{
    public class FamilyTreeService : IFamilyTreeService
    {
        private readonly ILineageRepository _repo;

        public FamilyTreeService(ILineageRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public IReadOnlyList<Guid> GetSiblings(Guid characterId)
        {
            var self = _repo.FindByChildId(characterId)
                       ?? throw new DomainException($"Lineage not set for Character {characterId}");

            var parents = self.GetParents(includeAdoptive: true).Select(p => p.ParentId).ToHashSet();

            if (!parents.Any()) return Array.Empty<Guid>();

            var potentialSibs = _repo.FindBySomeParentIds(parents);
            return potentialSibs
                .Where(l => l.CharacterId != characterId && SharesAnyParent(l, parents))
                .Select(l => l.CharacterId)
                .Distinct()
                .ToList();
        }

        private static bool SharesAnyParent(EntityLineage other, HashSet<Guid> parentSet)
        {
            var biop = other.GetParents(includeAdoptive: true).Select(p => p.ParentId);
            return biop.Any(parentSet.Contains);
        }

        public IReadOnlyList<Guid> GetGrandparents(Guid characterId)
        {
            var self = _repo.FindByChildId(characterId)
                         ?? throw new DomainException($"Lineage not set for Character {characterId}");

            var parents = self.GetParents(includeAdoptive: false).Select(p => p.ParentId).ToArray();
            if (!parents.Any()) return Array.Empty<Guid>();

            var gp = new HashSet<Guid>();
            foreach (var pid in parents)
            {
                var parentLine = _repo.FindByChildId(pid);
                if (parentLine == null) continue;

                var gpa = parentLine.GetParents(includeAdoptive: false);
                foreach (var (gpid, _) in gpa)
                    gp.Add(gpid);
            }

            return gp.ToList();
        }

        public IReadOnlyList<Guid> GetAncestors(Guid characterId, int maxDepth = 3)
        {
            var seen = new HashSet<Guid>();
            var queue = new Queue<(Guid id, int depth)>();
            queue.Enqueue((characterId, 0));

            while (queue.Count > 0)
            {
                var (curr, depth) = queue.Dequeue();
                if (depth >= maxDepth) continue;

                var lineage = _repo.FindByChildId(curr);
                if (lineage == null) continue;

                foreach (var (parentId, _) in lineage.GetParents(includeAdoptive: true))
                {
                    if (seen.Add(parentId))
                    {
                        queue.Enqueue((parentId, depth + 1));
                    }
                }
            }

            return seen.ToList();
        }

        public IReadOnlyList<Guid> GetDescendants(Guid characterId, int maxDepth = 3)
        {
            var seen = new HashSet<Guid> { characterId };
            var currentGen = new HashSet<Guid> { characterId };

            for (int depth = 0; depth < maxDepth; depth++)
            {
                var nextGen = new HashSet<Guid>();

                foreach (var lineage in _repo.FindAll())
                {
                    var parents = lineage.GetParents(includeAdoptive: true).Select(p => p.ParentId);
                    if (parents.Any(p => currentGen.Contains(p)))
                    {
                        if (seen.Add(lineage.CharacterId))
                            nextGen.Add(lineage.CharacterId);
                    }
                }

                if (!nextGen.Any()) break;
                currentGen = nextGen;
            }

            seen.Remove(characterId);
            return seen.ToList();
        }
    }
}
