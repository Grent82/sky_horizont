using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class AffectionService : IAffectionService
    {
        private readonly ICommanderRepository _cmdRepo;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;
        private readonly IAffectionRepository _affectionRepo;

        public AffectionService(
            ICommanderRepository cmdRepo,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IAffectionRepository affectionRepo)
        {
            _cmdRepo = cmdRepo;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _affectionRepo = affectionRepo;
        }

        public void UpdateAffection()
        {
            foreach (var planet in _planetRepo.GetAll())
            {
                foreach (var captiveId in planet.CapturedCommanderIds)
                {
                    if (planet.GovernorId is not Guid captorId) continue;

                    ApplyAffectionChange(captiveId, captorId);
                }
            }

            foreach (var fleet in _fleetRepo.GetAll())
            {
                foreach (var captiveId in fleet.CapturedCommanderIds)
                {
                    if (fleet.AssignedCommanderId is not Guid captorId) continue;

                    ApplyAffectionChange(captiveId, captorId);
                }
            }
        }

        private void ApplyAffectionChange(Guid captiveId, Guid captorId)
        {
            var captor = _cmdRepo.GetById(captorId);
            if (captor is null) return;

            int delta = captor.Personality.Type switch
            {
                PersonalityType.FamilyOriented => +2,
                PersonalityType.Honorable => +1,
                PersonalityType.Aggressive => -2,
                PersonalityType.Greedy => -1,
                PersonalityType.Cold => -3,
                _ => 0
            };

            _affectionRepo.AdjustAffection(captiveId, captorId, delta);
        }
    }
}
