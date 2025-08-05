namespace SkyHorizont.Domain.Faction
{
    public interface IFactionFundsRepository
    {
        int GetBalance(Guid factionId);
        void AddBalance(Guid factionId, int delta);
    }
}