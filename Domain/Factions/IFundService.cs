namespace SkyHorizont.Domain.Factions
{
    /// <summary>
    /// A domain-level service interface for checking and deducting faction credits.
    /// </summary>
    public interface IFundsService
    {
        bool HasFunds(Guid factionId, int amount);
        void Deduct(Guid factionId, int amount);
        void Credit(Guid factionId, int amount);
        int GetBalance(Guid factionId);
    }
}