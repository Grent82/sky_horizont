namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IOpinionsDbContext : IBaseDbContext
    {
        // Opinion score: -100..+100
        Dictionary<(Guid source, Guid target), int> Opinions { get; }

        // Optional: simple audit log per opinion edge
        Dictionary<(Guid source, Guid target), List<string>> OpinionReasons { get; }
    }
}