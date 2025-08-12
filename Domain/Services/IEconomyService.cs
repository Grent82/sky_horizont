namespace SkyHorizont.Domain.Services
{
    public interface IEconomyService
    {
        void CreditPlanetBudget(Guid planetId, int amount);
        // ToDo: add Debit, Transfer, etc. later
    }
}
