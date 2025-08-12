using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Thin service over Planet aggregate to enforce clamping and persistence.
    /// </summary>
    public class PlanetService : IPlanetService
    {
        private readonly IPlanetRepository _repository;

        public PlanetService(IPlanetRepository repository)
        {
            _repository = repository;
        }

        public void AdjustStability(Guid planetId, double delta)
        {
            var planet = _repository.GetById(planetId);
            if (planet is null) return;

            var stability = Math.Clamp(planet.Stability + delta, 0.0, 1.0);
            planet.Stability = stability;

            _repository.Save(planet);
        }
    }
}
