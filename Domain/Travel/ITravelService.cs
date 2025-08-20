using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Travel
{
    public interface ITravelService
    {
        Guid PlanFleetTravel(Guid fleetId, Guid originPlanetId, Guid destPlanetId, Resources? cargo = null, IReadOnlyList<Guid>? passengerIds = null);
        IEnumerable<TravelPlan> GetPlansInSystem(Guid systemId);
        void TickTravel(int year, int month);
    }
}