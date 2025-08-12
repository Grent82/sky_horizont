namespace SkyHorizont.Domain.Research
{
    public interface IResearchRepository
    {
        void AddProgress((Guid factionId, string) key, int progress);
        int GetByKey((Guid factionId, string) key);
    }
}