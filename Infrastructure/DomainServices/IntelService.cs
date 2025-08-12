using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Stores intel reports verbatim. A future version could aggregate and decay intel.
    /// </summary>
    public class IntelService : IIntelService
    {
        private readonly IIntelRepository _repository;

        public IntelService(IIntelRepository repository)
        {
            _repository = repository;
        }

        public void RecordReport(Guid characterId, Guid targetFactionId, string summary, int intelValue, int year, int month)
        {
            intelValue = Math.Max(0, intelValue);
            var report = new IntelReport(
                ReportId: Guid.NewGuid(),
                SourceCharacterId: characterId,
                TargetFactionId: targetFactionId,
                Summary: summary ?? string.Empty,
                IntelValue: intelValue,
                Year: year,
                Month: month
            );
            _repository.AddReport(report);
        }

        public IEnumerable<IntelReport> GetReportsForFaction(Guid targetFactionId) =>
            _repository.GetReportsByFactionId(targetFactionId);

    }
}
