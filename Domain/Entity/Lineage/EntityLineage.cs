using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity.Lineage
{
    public enum LineageType { Biological, Adoptive }

    public class EntityLineage
    {
        public Guid CommanderId { get; }

        private Guid? _bioFatherId;
        private Guid? _bioMotherId;
        private readonly HashSet<Guid> _adoptiveParentIds = new();

        public Guid? BiologicalFatherId => _bioFatherId;
        public Guid? BiologicalMotherId => _bioMotherId;
        public IReadOnlyCollection<Guid> AdoptiveParentIds => _adoptiveParentIds;

        public EntityLineage(Guid commanderId)
        {
            CommanderId = commanderId != Guid.Empty
                ? commanderId
                : throw new ArgumentException("CommanderId must be non‑empty", nameof(commanderId));
        }

        public void SetBiologicalParents(Guid? fatherId, Guid? motherId)
        {
            ThrowIfSelf(fatherId);
            ThrowIfSelf(motherId);

            _bioFatherId = fatherId;
            _bioMotherId = motherId;
        }

        public void AddAdoptiveParent(Guid parentId)
        {
            ThrowIfSelf(parentId);
            if (_adoptiveParentIds.Contains(parentId)) return;
            _adoptiveParentIds.Add(parentId);
        }

        public void RemoveAdoptiveParent(Guid parentId) =>
            _adoptiveParentIds.Remove(parentId);

        public IReadOnlyList<(Guid ParentId, LineageType Type)> GetParents(
            bool includeAdoptive = false)
        {
            var list = new List<(Guid, LineageType)>();
            if (_bioFatherId.HasValue) list.Add((_bioFatherId.Value, LineageType.Biological));
            if (_bioMotherId.HasValue) list.Add((_bioMotherId.Value, LineageType.Biological));
            if (includeAdoptive)
                list.AddRange(_adoptiveParentIds.Select(id => (id, LineageType.Adoptive)));

            return list;
        }

        private void ThrowIfSelf(Guid? candidate)
        {
            if (candidate.HasValue && candidate.Value == CommanderId)
                throw new DomainException("Cannot assign self as a parent.");
        }

        public override string ToString()
            => $"Lineage[{CommanderId}], BioFather = {_bioFatherId}, BioMother = {_bioMotherId}, Adopted({string.Join(',', _adoptiveParentIds)})";
    }
}
