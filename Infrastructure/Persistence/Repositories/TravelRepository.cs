using SkyHorizont.Domain.Travel;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class TravelRepository : ITravelRepository
    {
        private readonly ITravelDbContext _context;

        public TravelRepository(ITravelDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Add(TravelPlan itinerary)
        {
            if (itinerary == null)
                throw new ArgumentNullException(nameof(itinerary));
            if (itinerary.Id == Guid.Empty)
                throw new ArgumentException("Itinerary ID cannot be empty", nameof(itinerary.Id));
            _context.Itineraries[itinerary.Id] = itinerary;
            _context.SaveChanges();
        }

        public void Update(TravelPlan itinerary)
        {
            if (itinerary == null)
                throw new ArgumentNullException(nameof(itinerary));
            if (itinerary.Id == Guid.Empty)
                throw new ArgumentException("Itinerary ID cannot be empty", nameof(itinerary.Id));
            if (!_context.Itineraries.ContainsKey(itinerary.Id))
                throw new InvalidOperationException($"Itinerary {itinerary.Id} not found.");
            _context.Itineraries[itinerary.Id] = itinerary;
            _context.SaveChanges();
        }

        public TravelPlan? GetById(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid itinerary ID", nameof(id));
            return _context.Itineraries.TryGetValue(id, out var itinerary) ? itinerary : null;
        }

        public IReadOnlyList<TravelPlan> GetAll()
        {
            return _context.Itineraries.Values.ToList().AsReadOnly();
        }
    }
}