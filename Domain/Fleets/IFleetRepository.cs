namespace SkyHorizont.Domain.Fleets
{
    public interface IFleetRepository
    {
        Fleet? GetById(Guid attackerFleetId);
        IEnumerable<Fleet> GetAll();
        void Save(Fleet fleet);
        IEnumerable<Fleet> GetFleetsForFaction(Guid factionId);
    }
}