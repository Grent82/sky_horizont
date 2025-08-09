using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Intrigue;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ISecretsDbContext : IBaseDbContext
    {
         Dictionary<Guid, Secret> Secrets { get; }
    }
}