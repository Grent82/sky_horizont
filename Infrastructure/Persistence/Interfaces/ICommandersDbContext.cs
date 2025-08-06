using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ICommandersDbContext : IBaseDbContext
    {
        IDictionary<Guid, Commander> Commanders { get; }
    }
}