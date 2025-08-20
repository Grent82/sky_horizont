namespace SkyHorizont.Domain.Travel
{
    public interface ITravelRepository
    {
        void Add(TravelPlan itinerary);
        void Update(TravelPlan itinerary);
        TravelPlan? GetById(Guid id);
        IReadOnlyList<TravelPlan> GetAll();
    }
}