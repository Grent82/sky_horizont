using SkyHorizont.Domain.Travel;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class InMemoryTravelDbContext : ITravelDbContext
    {
        public IDictionary<Guid, TravelPlan> Itineraries { get; } = new Dictionary<Guid, TravelPlan>();

        public void SaveChanges()
        {
            // No action needed for in-memory
        }
    }
}