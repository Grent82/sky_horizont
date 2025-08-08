using SkyHorizont.Domain.Entity.Lineage;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Repository
{
    /// <summary>Simple in-memory repo; replace with EF/DB later.</summary>
    public class LineageRepository : ILineageRepository
    {
        private readonly ILineageDbContext _context;

        public LineageRepository(ILineageDbContext context)
        {
            _context = context;
        }

        public EntityLineage? FindByChildId(Guid characterId)
            => _context.EntityLineagebyChild.TryGetValue(characterId, out var l) ? l : null;

        public IEnumerable<EntityLineage> FindBySomeParentIds(IEnumerable<Guid> parentIds)
        {
            var set = new HashSet<Guid>(parentIds);
            foreach (var kv in _context.EntityLineagebyChild.Values)
            {
                var parents = kv.GetParents(includeAdoptive: true).Select(p => p.ParentId);
                if (parents.Any(set.Contains))
                    yield return kv;
            }
        }

        public IEnumerable<EntityLineage> FindAll() => _context.EntityLineagebyChild.Values;

        public void Upsert(EntityLineage lineage)
        {
            _context.EntityLineagebyChild[lineage.CharacterId] = lineage;
        }

        public IEnumerable<Guid> FindChildrenOfParent(Guid parentId)
        {
            foreach (var l in _context.EntityLineagebyChild.Values)
            {
                foreach (var (pid, _) in l.GetParents(includeAdoptive: true))
                    if (pid == parentId)
                        yield return l.CharacterId;
            }
        }

        public bool IsAncestorOf(Guid candidateAncestorId, Guid childId)
        {
            // BFS up the tree
            var visited = new HashSet<Guid>();
            var q = new Queue<Guid>();
            q.Enqueue(childId);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (!visited.Add(cur)) continue;

                var curLine = FindByChildId(cur);
                if (curLine == null) continue;

                foreach (var (pid, _) in curLine.GetParents(includeAdoptive: true))
                {
                    if (pid == candidateAncestorId) return true;
                    q.Enqueue(pid);
                }
            }
            return false;
        }
    }
}
