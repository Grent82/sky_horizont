using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Factions
{
    public sealed class FactionService : IFactionService
    {
        private readonly IFactionRepository _factionRepository;
        private readonly IPlanetRepository _planetRepository;

        public FactionService(IFactionRepository factionRepository, IPlanetRepository planetRepository)
        {
            _factionRepository = factionRepository ?? throw new ArgumentNullException(nameof(factionRepository));
            _planetRepository = planetRepository ?? throw new ArgumentNullException(nameof(planetRepository));
        }

        public Guid GetFactionIdForCharacter(Guid characterId)
        {
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

        public bool HasAlliance(Guid factionA, Guid factionB)
        {
            if (factionA == Guid.Empty || factionB == Guid.Empty || factionA == factionB) return false;
            var faction = _factionRepository.GetFaction(factionA);
            return faction != null && faction.Diplomacy.TryGetValue(factionB, out var standing) && standing.Value >= 50;
        }

        public Guid GetFactionIdForPlanet(Guid planetId)
        {
            var planet = _planetRepository.GetById(planetId);
            return planet?.FactionId ?? Guid.Empty;
        }

        public Guid GetFactionIdForSystem(Guid systemId)
        {
            var planets = _planetRepository.GetAll().Where(p => p.SystemId == systemId).ToList();
            if (!planets.Any()) return Guid.Empty;
            var factionCounts = planets.GroupBy(p => p.FactionId)
                                      .Select(g => new { FactionId = g.Key, Count = g.Count() })
                                      .OrderByDescending(g => g.Count)
                                      .First();
            return factionCounts.FactionId;
        }

        public int GetEconomicStrength(Guid factionId)
        {
            var planets = _planetRepository.GetPlanetsControlledByFaction(factionId);
            var totalInfrastructure = planets.Sum(p => p.InfrastructureLevel);
            var totalStability = planets.Sum(p => p.Stability * 100);
            var planetCount = planets.Count();
            return planetCount > 0 ? (int)((totalInfrastructure + totalStability) / (2 * planetCount)) : 0;
        }

        public IEnumerable<Guid> GetAllRivalFactions(Guid forFaction)
        {
            if (forFaction == Guid.Empty) return Enumerable.Empty<Guid>();

            IEnumerable<Guid> rivalsFromRivalPairs = _factionRepository.GetRivalPairs()
                .Where(p => p.a == forFaction || p.b == forFaction)
                .Select(p => p.a == forFaction ? p.b : p.a);

            IEnumerable<Guid> rivalsFromWars = _factionRepository.GetWarPairs()
                .Where(p => p.a == forFaction || p.b == forFaction)
                .Select(p => p.a == forFaction ? p.b : p.a);

            return rivalsFromRivalPairs.Concat(rivalsFromWars).Distinct();
        }

        public void MoveCharacterToFaction(Guid characterId, Guid newFactionId)
        {
            _factionRepository.MoveCharacterToFaction(characterId, newFactionId);
        }

        private static (Guid a, Guid b) Normalize(Guid x, Guid y)
        {
            return x.CompareTo(y) <= 0 ? (x, y) : (y, x);
        }

        public void Save(Faction faction)
        {
            _factionRepository.Save(faction);
        }

        public Faction? GetFaction(Guid factionId)
        {
            return _factionRepository.GetFaction(factionId);
        }
    }
}