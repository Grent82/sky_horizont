using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Domain.Travel
{
    public enum TravelStatus { Planned, InProgress, Completed, Cancelled }

    public sealed record TravelPlan(
        Guid Id,
        Guid FleetId,
        Guid OriginPlanetId,
        Guid DestinationPlanetId,
        IReadOnlyList<Guid> RouteSystemIds,
        int DepartYear,
        int DepartMonth,
        int EtaMonths,
        TravelStatus Status,
        double TravelProgress,
        int CurrentLegIndex
    );
}
