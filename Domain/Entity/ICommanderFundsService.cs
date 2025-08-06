namespace SkyHorizont.Domain.Entity
{
    public interface ICommanderFundsService
    {
        void CreditCommander(Guid commanderId, int amount);
        bool DeductCommander(Guid commanderId, int amount);
    }
}