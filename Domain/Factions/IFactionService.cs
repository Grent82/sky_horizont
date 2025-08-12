namespace SkyHorizont.Domain.Factions
{
    /// <summary>
    /// Read-only faction info used by planner.
    /// </summary>
    public interface IFactionService
    {
        Guid GetFactionIdForCharacter(Guid characterId);
        Guid? GetLeaderId(Guid factionId);
        bool IsAtWar(Guid a, Guid b);
        IEnumerable<Guid> GetAllRivalFactions(Guid forFaction);

        void MoveCharacterToFaction(Guid characterId, Guid newFactionId);
    }
}