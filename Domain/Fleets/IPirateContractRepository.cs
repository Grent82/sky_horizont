namespace SkyHorizont.Domain.Fleets
{
    public interface IPirateContractRepository
    {
        Fleet GeneratePirateFleet(Guid pirateFactionId, int shipCount);
        void TransferPirateFleetToFaction(Guid fleetId, Guid nonPirateFactionId);
    }
}