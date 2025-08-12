namespace SkyHorizont.Domain.Services
{
    public interface IResearchService
    {
        void AddProgress(Guid characterId, string technologyName, int points);
        int GetProgress(Guid characterId, string technologyName);
        // Optional: aggregate by faction/academy later
    }
}
