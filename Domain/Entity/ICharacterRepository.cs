
namespace SkyHorizont.Domain.Entity
{
    public interface ICharacterRepository
    {
        IEnumerable<Character> GetAll();
        IEnumerable<Character> GetLiving();
        Character? GetById(Guid characterId);
        void Save(Character character);
        IEnumerable<Character> GetByIds(IEnumerable<Guid> characterIds);
        /// <summary>
        /// Returns characters who have notable non-familial ties with the specified
        /// character, such as friends, rivals or lovers.
        /// </summary>
        IEnumerable<Character> GetAssociates(Guid characterId);
    }
}
