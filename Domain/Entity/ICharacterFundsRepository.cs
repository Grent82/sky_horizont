namespace SkyHorizont.Domain.Entity
{
    public interface ICharacterFundsRepository
    {
        int GetBalance(Guid characterId);
        void AddBalance(Guid characterId, int amount);
    }
}