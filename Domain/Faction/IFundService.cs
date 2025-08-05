namespace SkyHorizont.Domain.Faction
{
    /// <summary>
    /// A domain-level service interface for checking and deducting faction credits.
    /// </summary>
    public interface IFundsService
    {
        bool HasFunds(Guid factionId, int amount);
        void Deduct(Guid factionId, int amount);
    }
}