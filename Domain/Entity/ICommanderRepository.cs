namespace SkyHorizont.Domain.Entity
{
    public interface ICharacterRepository
    {
        Character? GetById(Guid characterId);
        void Save(Character character);
    }
}
