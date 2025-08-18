
namespace SkyHorizont.Domain.Entity
{
    public interface ICharacterRepository
    {
        IEnumerable<Character> GetAll();
        Character? GetById(Guid characterId);
        void Save(Character character);
        IEnumerable<Character> GetByIds(IEnumerable<Guid> characterIds);
    }
}
