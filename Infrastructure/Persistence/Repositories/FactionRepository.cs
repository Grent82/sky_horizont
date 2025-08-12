using SkyHorizont.Domain.Factions;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Repository
{
    
    public class FactionRepository : IFactionRepository
    {
        IFactionsDbContext _context;
        public FactionRepository(IFactionsDbContext context) => _context = context;

        public Guid GetFactionIdForCharacter(Guid characterId)
        {
            return _context.CharacterFaction.TryGetValue(characterId, out var fid) ? fid : Guid.Empty;;
        }

        public Guid? GetLeaderId(Guid factionId)
        {
            return _context.FactionLeaders.TryGetValue(factionId, out var leader) ? leader : null;
        }

        public HashSet<(Guid a, Guid b)> GetRivalPairs()
        {
            return _context.RivalPairs;
        }

        public HashSet<(Guid a, Guid b)> GetWarPairs()
        {
            return _context.WarPairs;
        }

        public void MoveCharacterToFaction(Guid characterId, Guid newFactionId)
        {
            _context.CharacterFaction[characterId] = newFactionId;
        }

        public void Save(Faction faction)
        {
            throw new NotImplementedException();
        }
    }
}