namespace SkyHorizont.Domain.Factions
{
    /// <summary>
    /// Read-only faction info used by planner.
    /// </summary>
    public interface IFactionService
    {
        Guid GetFactionIdForCharacter(Guid characterId);
        Guid GetFactionIdForPlanet(Guid planetId);
        Guid GetFactionIdForSystem(Guid systemId);
        Guid? GetLeaderId(Guid factionId);
        bool IsAtWar(Guid a, Guid b);
        IEnumerable<Guid> GetAllRivalFactions(Guid forFaction);
        bool HasAlliance(Guid factionA, Guid factionB);
        int GetEconomicStrength(Guid factionId);

        /// <summary>
        /// Attempt to negotiate a prisoner exchange between factions of the payer and captive.
        /// Returns true if an exchange or deferment was arranged.
        /// </summary>
        bool NegotiatePrisonerExchange(Guid payerId, Guid captiveId);

        void MoveCharacterToFaction(Guid characterId, Guid newFactionId);
        void Save(Faction faction);
        Faction GetFaction(Guid factionId);
    }
}