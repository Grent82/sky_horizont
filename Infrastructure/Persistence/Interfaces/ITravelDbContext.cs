using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ITravelDbContext : IBaseDbContext
    {
        IDictionary<Guid, TravelPlan> Itineraries { get; }
    }
}