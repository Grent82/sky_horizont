using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class RouteService : IRouteService
    {
        private readonly IStarmapService _starmap;

        public RouteService(IStarmapService starmap)
        {
            _starmap = starmap ?? throw new ArgumentNullException(nameof(starmap));
        }

        public IReadOnlyList<Guid> FindRoute(Guid originSystemId, Guid destSystemId)
        {
            if (originSystemId == destSystemId)
                return new List<Guid> { originSystemId }.AsReadOnly();
            return new List<Guid> { originSystemId, destSystemId }.AsReadOnly();
        }

        public int EstimateMonths(double avgSpeed, double totalDistance, int monthsPerYear)
        {
            if (avgSpeed <= 0) return monthsPerYear;
            var years = totalDistance / avgSpeed;
            var months = (int)Math.Ceiling(years * monthsPerYear);
            return Math.Max(1, months);
        }
    }
}
