namespace SkyHorizont.Domain.Galaxy.Planet
{
    public interface IPlanetRepository
    {
        Planet? GetById(Guid planetId);
        IEnumerable<Planet> GetAll();
        void Save(Planet planet);
        IEnumerable<Planet> GetPlanetsControlledByFaction(Guid factionId);
    }
}