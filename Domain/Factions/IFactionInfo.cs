namespace SkyHorizont.Domain.Factions
{
    /// <summary>
    /// Read-only faction info used by planner.
    /// </summary>
    public interface IFactionInfo
    {
        Guid GetFactionIdForCharacter(Guid characterId);
        Guid? GetLeaderId(Guid factionId);
        bool IsAtWar(Guid a, Guid b);
        IEnumerable<Guid> GetAllRivalFactions(Guid forFaction);
    }
}