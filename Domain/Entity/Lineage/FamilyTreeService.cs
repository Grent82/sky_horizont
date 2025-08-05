
using SkyHorizont.Domain.Entity.Task;
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

        public IReadOnlyList<Guid> GetSiblings(Guid commanderId)
        {
            var self = _repo.FindByChildId(commanderId)
                       ?? throw new DomainException($"Lineage not set for Commander {commanderId}");

            var parents = self.GetParents(includeAdoptive: true).Select(p => p.ParentId).ToHashSet();

            if (!parents.Any()) return Array.Empty<Guid>();

            var potentialSibs = _repo.FindBySomeParentIds(parents);
            return potentialSibs
                .Where(l => l.CommanderId != commanderId && SharesAnyParent(l, parents))
                .Select(l => l.CommanderId)
                .Distinct()
                .ToList();
        }

        private static bool SharesAnyParent(EntityLineage other, HashSet<Guid> parentSet)
        {
            var biop = other.GetParents(includeAdoptive: true).Select(p => p.ParentId);
            return biop.Any(parentSet.Contains);
        }

        public IReadOnlyList<Guid> GetGrandparents(Guid commanderId)
        {
            var self = _repo.FindByChildId(commanderId)
                         ?? throw new DomainException($"Lineage not set for Commander {commanderId}");

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

        public IReadOnlyList<Guid> GetAncestors(Guid commanderId, int maxDepth = 3)
        {
            var seen = new HashSet<Guid>();
            var queue = new Queue<(Guid id, int depth)>();
            queue.Enqueue((commanderId, 0));

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

        public IReadOnlyList<Guid> GetDescendants(Guid commanderId, int maxDepth = 3)
        {
            var seen = new HashSet<Guid> { commanderId };
            var currentGen = new HashSet<Guid> { commanderId };

            for (int depth = 0; depth < maxDepth; depth++)
            {
                var nextGen = new HashSet<Guid>();

                foreach (var lineage in _repo.FindAll())
                {
                    var parents = lineage.GetParents(includeAdoptive: true).Select(p => p.ParentId);
                    if (parents.Any(p => currentGen.Contains(p)))
                    {
                        if (seen.Add(lineage.CommanderId))
                            nextGen.Add(lineage.CommanderId);
                    }
                }

                if (!nextGen.Any()) break;
                currentGen = nextGen;
            }

            seen.Remove(commanderId);
            return seen.ToList();
        }
    }
}
