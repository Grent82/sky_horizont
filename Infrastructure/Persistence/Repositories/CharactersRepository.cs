using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class CharactersRepository : ICharacterRepository
    {
        private readonly ICharactersDbContext _context;

        public CharactersRepository(ICharactersDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<Character> GetAll()
        {
            return _context.Characters.Values.ToList();
        }

        public Character? GetById(Guid characterId)
        {
            if (characterId == Guid.Empty)
                throw new ArgumentException("Invalid character ID", nameof(characterId));
            return _context.Characters.TryGetValue(characterId, out var character) ? character : null;
        }

        public void Save(Character character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (character.Id == Guid.Empty)
                throw new ArgumentException("Character ID cannot be empty", nameof(character.Id));
            _context.Characters[character.Id] = character;
        }

        public IEnumerable<Character> GetByIds(IEnumerable<Guid> characterIds)
        {
            if (characterIds == null)
                throw new ArgumentNullException(nameof(characterIds));
            return characterIds
                .Where(id => id != Guid.Empty)
                .Select(id => _context.Characters.TryGetValue(id, out var character) ? character : null)
                .Where(c => c != null)
                .ToList()!;
        }
    }
}