namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IAffectionDbContext : IBaseDbContext
    {
        Dictionary<(Guid source, Guid target), int> Affection { get; }
    }
}