using SkyHorizont.Domain.Factions;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Read-only service that implements IFactionInfo from an IFactionsDbContext.
    /// Stores war/rival relations as undirected pairs of faction IDs.
    /// </summary>
    public sealed class FactionInfoService : IFactionInfo
    {
        private readonly IFactionRepository _factionRepository;

        public FactionInfoService(IFactionRepository factionRepository) =>_factionRepository = factionRepository;

        public Guid GetFactionIdForCharacter(Guid characterId)
        {
            // If unknown, return Guid.Empty to indicate "no faction".
            // Swap to throw NotFoundException if you prefer strictness.
            return _factionRepository.GetFactionIdForCharacter(characterId);
        }

        public Guid? GetLeaderId(Guid factionId)
        {
            return _factionRepository.GetLeaderId(factionId);
        }

        public bool IsAtWar(Guid factionA, Guid factionB)
        {
            if (factionA == Guid.Empty || factionB == Guid.Empty || factionA == factionB) return false;
            var key = Normalize(factionA, factionB);
            return _factionRepository.GetWarPairs().Contains(key);
        }

        public IEnumerable<Guid> GetAllRivalFactions(Guid forFaction)
        {
            if (forFaction == Guid.Empty) return Enumerable.Empty<Guid>();

            // Rivals are all opponents from RivalPairs ∪ WarPairs (planner may treat both as "rivals").
            IEnumerable<Guid> rivalsFromRivalPairs = _factionRepository.GetRivalPairs()
                .Where(p => p.a == forFaction || p.b == forFaction)
                .Select(p => p.a == forFaction ? p.b : p.a);

            IEnumerable<Guid> rivalsFromWars = _factionRepository.GetWarPairs()
                .Where(p => p.a == forFaction || p.b == forFaction)
                .Select(p => p.a == forFaction ? p.b : p.a);

            return rivalsFromRivalPairs
                .Concat(rivalsFromWars)
                .Distinct();
        }

        #region Helper

        private static (Guid a, Guid b) Normalize(Guid x, Guid y)
        {
            // Store undirected relations canonically: (min, max)
            return x.CompareTo(y) <= 0 ? (x, y) : (y, x);
        }

        #endregion
    }
}
