using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Entity.Lineage
{
    public enum LineageType { Biological, Adoptive }

    /// <summary>
    /// Aggregate that stores the parent links for ONE child (CharacterId).
    /// Invariants:
    ///  - At most one biological father and one biological mother.
    ///  - Parent IDs cannot be the child itself.
    ///  - Father and mother cannot be the same person.
    /// </summary>
    public class EntityLineage
    {
        public Guid CharacterId { get; }

        private Guid? _bioFatherId;
        private Guid? _bioMotherId;
        private readonly HashSet<Guid> _adoptiveParentIds = new();

        public Guid? BiologicalFatherId => _bioFatherId;
        public Guid? BiologicalMotherId => _bioMotherId;
        public IReadOnlyCollection<Guid> AdoptiveParentIds => _adoptiveParentIds;

        public EntityLineage(Guid characterId)
        {
            CharacterId = characterId != Guid.Empty
                ? characterId
                : throw new ArgumentException("CharacterId must be non‑empty", nameof(characterId));
        }

        public void SetBiologicalParents(Guid? fatherId, Guid? motherId)
        {
            SetBiologicalFather(fatherId);
            SetBiologicalMother(motherId);
        }

        public void SetBiologicalFather(Guid? fatherId)
        {
            ThrowIfSelf(fatherId);
            if (fatherId.HasValue && _bioMotherId.HasValue && fatherId.Value == _bioMotherId.Value)
                throw new DomainException("Biological father and mother cannot be the same character.");

            _bioFatherId = fatherId;
        }

        public void SetBiologicalMother(Guid? motherId)
        {
            ThrowIfSelf(motherId);
            if (motherId.HasValue && _bioFatherId.HasValue && motherId.Value == _bioFatherId.Value)
                throw new DomainException("Biological father and mother cannot be the same character.");

            _bioMotherId = motherId;
        }

        public void ClearBiologicalParents()
        {
            _bioFatherId = null;
            _bioMotherId = null;
        }

        public void AddAdoptiveParent(Guid parentId)
        {
            ThrowIfSelf(parentId);
            _adoptiveParentIds.Add(parentId);
        }

        public void RemoveAdoptiveParent(Guid parentId) =>
            _adoptiveParentIds.Remove(parentId);

        /// <summary>
        /// Returns bio parents and, optionally, adoptive parents.
        /// </summary>
        public IReadOnlyList<(Guid ParentId, LineageType Type)> GetParents(bool includeAdoptive = false)
        {
            var list = new List<(Guid, LineageType)>(2 + (includeAdoptive ? _adoptiveParentIds.Count : 0));
            if (_bioFatherId.HasValue) list.Add((_bioFatherId.Value, LineageType.Biological));
            if (_bioMotherId.HasValue) list.Add((_bioMotherId.Value, LineageType.Biological));
            if (includeAdoptive)
                list.AddRange(_adoptiveParentIds.Select(id => (id, LineageType.Adoptive)));

            return list;
        }

        private void ThrowIfSelf(Guid? candidate)
        {
            if (candidate.HasValue && candidate.Value == CharacterId)
                throw new DomainException("Cannot assign self as a parent.");
        }

        public override string ToString()
            => $"Lineage[{CharacterId}], BioFather={_bioFatherId}, BioMother={_bioMotherId}, Adopted({string.Join(',', _adoptiveParentIds)})";
    }
}
