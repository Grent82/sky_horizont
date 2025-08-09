using SkyHorizont.Domain.Intrigue;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IIntrigueDbContext : IBaseDbContext
    {
        Dictionary<Guid, Plot> Plots { get; }
        Dictionary<Guid, Secret> Secrets { get; }
    }
}
