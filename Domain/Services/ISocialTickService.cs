using SkyHorizont.Domain.Social;

namespace SkyHorizont.Domain.Services
{
    public interface ISocialTickService
    {
        void ApplyMonthlyDrift();
        void ApplyEvents(IEnumerable<ISocialEvent> events);
    }

}