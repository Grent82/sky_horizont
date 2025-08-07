using SkyHorizont.Domain.Entity;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence
{
    public class CharacterFundsRepository : ICharacterFundsRepository
    {
        private readonly ICharacterFundsDbContext _context;

        public CharacterFundsRepository(ICharacterFundsDbContext context)
        {
            _context = context;
        }

        public int GetBalance(Guid characterId) =>
            _context.CharacterFunds.TryGetValue(characterId, out var b) ? b : 0;

        public void AddBalance(Guid characterId, int amount)
        {
            _context.CharacterFunds[characterId] = GetBalance(characterId) + amount;
        }
    }
}