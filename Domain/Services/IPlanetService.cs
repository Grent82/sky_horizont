namespace SkyHorizont.Domain.Services
{
    public interface IPlanetService
    {
        void AdjustStability(Guid planetId, double delta);
    }
}
