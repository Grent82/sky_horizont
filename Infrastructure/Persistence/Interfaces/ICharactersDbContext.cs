using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ICharactersDbContext : IBaseDbContext
    {
        IDictionary<Guid, Character> Characters { get; }
    }
}