namespace SkyHorizont.Domain.Travel
{
    public interface IPiracyService
    {
        bool IsPirateFaction(Guid factionId);
        int GetPirateActivity(Guid systemId);
        int GetTrafficLevel(Guid systemId);
        bool BecomePirate(Guid characterId);
        bool RegisterAmbush(Guid pirateActorId, Guid systemId, int year, int month);
        Guid GetPirateFactionId();
    }
}