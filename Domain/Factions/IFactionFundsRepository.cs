namespace SkyHorizont.Domain.Factions
{
    public interface IFactionFundsRepository
    {
        int GetBalance(Guid factionId);
        void AddBalance(Guid factionId, int delta);
        void DeductBalance(Guid factionId, int upkeep);
    }
}