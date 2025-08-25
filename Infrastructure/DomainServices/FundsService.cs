using SkyHorizont.Domain.Factions;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class FundsService : IFundsService
    {
        private readonly IFactionFundsRepository _repo;

        public FundsService(IFactionFundsRepository repo)
        {
            _repo = repo;
        }

        public bool HasFunds(Guid factionId, int amount)
            => _repo.GetBalance(factionId) >= amount;

        public void Deduct(Guid factionId, int amount)
            => _repo.AddBalance(factionId, -amount);

        public void Credit(Guid factionId, int amount)
            => _repo.AddBalance(factionId, amount);

        public int GetBalance(Guid factionId) => _repo.GetBalance(factionId);
    }
}