namespace SkyHorizont.Domain.Factions
{
    public interface IFactionRepository
    {
        Guid GetFactionIdForCharacter(Guid characterId);
        Guid? GetLeaderId(Guid factionId);

        HashSet<(Guid a, Guid b)> GetWarPairs();
        HashSet<(Guid a, Guid b)> GetRivalPairs();


        void Save(Faction faction);
    }
}