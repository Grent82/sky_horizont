using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IPlanetsDbContext : IBaseDbContext
    {
        IDictionary<Guid, Planet> Planets { get; }
    }
}