namespace SkyHorizont.Domain.Fleets
{
    public interface IFleetRepository
        {
            Fleet? GetById(Guid attackerFleetId);
            void Save(Fleet fleet);
        }
}