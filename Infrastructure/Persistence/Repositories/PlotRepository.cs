using SkyHorizont.Domain.Intrigue;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence.Intrigue
{
    public class PlotRepository : IPlotRepository
    {
        private readonly IIntrigueDbContext _ctx;

        public PlotRepository(IIntrigueDbContext ctx)
        {
            _ctx = ctx;
        }

        public Plot? GetById(Guid id)
        {
            _ctx.Plots.TryGetValue(id, out var p);
            return p;
        }

        public IEnumerable<Plot> GetAll() => _ctx.Plots.Values;

        public Plot Create(Guid leaderId, string goal, IEnumerable<Guid> conspirators, IEnumerable<Guid> targets)
        {
            var plot = new Plot(
                PlotId: Guid.NewGuid(),
                LeaderId: leaderId,
                Goal: goal,
                Conspirators: conspirators?.ToList() ?? new List<Guid>(),
                Targets: targets?.ToList() ?? new List<Guid>(),
                Progress: 0,
                Exposed: false
            );
            _ctx.Plots[plot.PlotId] = plot;
            _ctx.SaveChanges();
            return plot;
        }

        public void Save(Plot plot)
        {
            _ctx.Plots[plot.PlotId] = plot;
            _ctx.SaveChanges();
        }

        public void Remove(Guid plotId)
        {
            _ctx.Plots.Remove(plotId);
            _ctx.SaveChanges();
        }
    }
}
