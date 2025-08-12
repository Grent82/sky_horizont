using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class CharacterFundsService : ICharacterFundsService
    {
        private readonly ICharacterFundsRepository _repo;
        public CharacterFundsService(ICharacterFundsRepository repo) => _repo = repo;

        public void CreditCharacter(Guid characterId, int amount)
        {
            if (amount <= 0) return;
            _repo.AddBalance(characterId, amount);
        }

        public bool DeductCharacter(Guid characterId, int amount)
        {
            var balance = _repo.GetBalance(characterId);
            if (balance < amount) return false;
            _repo.AddBalance(characterId, -amount);
            return true;
        }
    }
}
