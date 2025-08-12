namespace SkyHorizont.Domain.Factions
{
    public interface IFactionRepository
    {
        Guid GetFactionIdForCharacter(Guid characterId);
        Guid? GetLeaderId(Guid factionId);

        HashSet<(Guid a, Guid b)> GetWarPairs();
        HashSet<(Guid a, Guid b)> GetRivalPairs();

        void MoveCharacterToFaction(Guid characterId, Guid newFactionId);

        void Save(Faction faction);
    }
}