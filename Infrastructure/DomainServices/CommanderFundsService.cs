using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.DomainServices
{
    internal class CommanderFundsService : ICommanderFundsService
    {
        private readonly ICommanderFundsRepository _repo;
        public CommanderFundsService(ICommanderFundsRepository repo) => _repo = repo;

        public void CreditCommander(Guid commanderId, int amount)
        {
            if (amount <= 0) return;
            _repo.AddBalance(commanderId, amount);
        }

        public bool DeductCommander(Guid commanderId, int amount)
        {
            var balance = _repo.GetBalance(commanderId);
            if (balance < amount) return false;
            _repo.AddBalance(commanderId, -amount);
            return true;
        }
    }
}
