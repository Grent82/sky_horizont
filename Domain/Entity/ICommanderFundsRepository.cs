namespace SkyHorizont.Domain.Entity
{
    public interface ICommanderFundsRepository
    {
        int GetBalance(Guid commanderId);
        void AddBalance(Guid commanderId, int amount);
    }
}