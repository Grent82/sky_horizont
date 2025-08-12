using SkyHorizont.Domain.Services;

namespace SkyHorizont.Domain.Intrigue
{
    public interface IIntelRepository
    {
        void AddReport(IntelReport report);
        IEnumerable<IntelReport> GetReportsByFactionId(Guid targetFactionId);
    }
}
