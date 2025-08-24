
namespace SkyHorizont.Domain.Entity
{
    public interface ICharacterRepository
    {
        IEnumerable<Character> GetAll();
        IEnumerable<Character> GetLiving();
        Character? GetById(Guid characterId);
        void Save(Character character);
        IEnumerable<Character> GetByIds(IEnumerable<Guid> characterIds);
    }
}
