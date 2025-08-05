namespace SkyHorizont.Domain.Galaxy.Planet
{
    public interface IPlanetRepository
    {
        Planet? GetById(Guid planetId);
        void Save(Planet planet);
    }
}