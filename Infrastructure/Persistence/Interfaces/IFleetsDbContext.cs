using SkyHorizont.Domain.Fleets;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface IFleetsDbContext : IBaseDbContext
    {
        IDictionary<Guid, Fleet> Fleets { get; }
    }
}