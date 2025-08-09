namespace SkyHorizont.Domain.Factions
{
    /// <summary>
    /// Command-side service to move characters between factions.
    /// </summary>
    public interface IFactionMembershipService
    {
        Guid GetFactionForCharacter(Guid characterId);
        void MoveCharacterToFaction(Guid characterId, Guid newFactionId);
    }
}
