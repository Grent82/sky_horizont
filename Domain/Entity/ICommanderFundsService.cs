namespace SkyHorizont.Domain.Entity
{
    public interface ICharacterFundsService
    {
        void CreditCharacter(Guid characterId, int amount);
        bool DeductCharacter(Guid characterId, int amount);
    }
}