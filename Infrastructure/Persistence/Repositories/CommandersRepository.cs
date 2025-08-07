using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class CharactersRepository : ICharacterRepository
    {
        private readonly ICharactersDbContext _context;

        public CharactersRepository(ICharactersDbContext context)
        {
            _context = context;
        }

        public Character? GetById(Guid characterId) =>
            _context.Characters.TryGetValue(characterId, out var cmd) ? cmd : null;

        public void Save(Character character)
        {
            _context.Characters[character.Id] = character;
        }
    }
}
