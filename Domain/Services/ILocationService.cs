namespace SkyHorizont.Domain.Services
{
    public enum LocationKind { Planet, Fleet }

    /// <summary>
    /// Lightweight location queries derived from existing aggregates.
    /// </summary>
    public record CharacterLocation(LocationKind Kind, Guid HostId, Guid SystemId);

    public interface ILocationService
    {
        // High-level
        CharacterLocation? GetCharacterLocation(Guid characterId);
        bool AreCoLocated(Guid characterA, Guid characterB);
        bool AreInSameSystem(Guid characterA, Guid characterB);

        // Convenience listings (used by various systems)
        IEnumerable<Guid> GetCharactersOnPlanet(Guid planetId);
        IEnumerable<Guid> GetCharactersOnFleet(Guid fleetId);

        IEnumerable<Guid> GetCaptivesOnPlanet(Guid planetId);
        IEnumerable<Guid> GetCaptivesOnFleet(Guid fleetId);

        void AddCitizenToPlanet(Guid character, Guid locationId);
        void AddPassengerToFleet(Guid character, Guid fleetId);
        void StageAtHolding(Guid character, Guid locationId);
        void KeepInHarem(Guid captiveId, Guid captorId);

        bool IsPrisonerOf(Guid prisonerId, Guid captorId);
    }
}
