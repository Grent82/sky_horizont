namespace SkyHorizont.Infrastructure.Persistence.Interfaces
{
    public interface ICharacterFundsDbContext : IBaseDbContext
    {
        IDictionary<Guid, int> CharacterFunds { get; }
    }
}