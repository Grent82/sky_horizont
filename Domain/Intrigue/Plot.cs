namespace SkyHorizont.Domain.Intrigue
{
    public record Plot(Guid PlotId, Guid LeaderId, string Goal, List<Guid> Conspirators,
                    List<Guid> Targets, int Progress, bool Exposed);
}