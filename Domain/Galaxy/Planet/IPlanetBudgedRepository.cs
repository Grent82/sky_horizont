namespace SkyHorizont.Domain.Galaxy.Planet
{
    public interface IPlanetBudgedRepository
    {
        void AddBudget(Guid planetId, int credits);
        int GetPlanetBudget(Guid planetId);
    }
}