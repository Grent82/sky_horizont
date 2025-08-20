namespace SkyHorizont.Domain.Travel
{
    public interface IRouteService
    {
        IReadOnlyList<Guid> FindRoute(Guid originSystemId, Guid destSystemId);
        int EstimateMonths(double avgSpeed, double totalDistance, int monthsPerYear);
    }
}