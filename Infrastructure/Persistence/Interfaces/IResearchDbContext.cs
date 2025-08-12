namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IResearchDbContext : IBaseDbContext
    {
        Dictionary<(Guid factionId, string tech), int> ResearchProgress { get; }
    }
}
