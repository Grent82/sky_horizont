
using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Domain.Services;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class IntelRepository : IIntelRepository
    {
        private readonly IIntelDbContext _ctx;

        public IntelRepository(IIntelDbContext ctx)
        {
            _ctx = ctx;
        }

        public void AddReport(IntelReport report)
        {
            _ctx.Reports.Add(report);
            _ctx.SaveChanges();
        }

        public IEnumerable<IntelReport> GetReportsByFactionId(Guid targetFactionId) => _ctx.Reports.Where(r => r.TargetFactionId == targetFactionId).ToList();
    }
}
