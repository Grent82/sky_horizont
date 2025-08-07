using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;

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
                    ApplyAffectionChange(captiveId, captorId, isFleet: false);
                }
            }

            foreach (var fleet in _fleetRepo.GetAll())
            {
                foreach (var captiveId in fleet.CapturedCommanderIds)
                {
                    if (fleet.AssignedCommanderId is not Guid captorId) continue;
                    ApplyAffectionChange(captiveId, captorId, isFleet: true);
                }
            }
        }

        private void ApplyAffectionChange(Guid captiveId, Guid captorId, bool isFleet)
        {
            var captor = _cmdRepo.GetById(captorId);
            if (captor is null) return;

            int delta = 0;

            // Increase affection if captor is perceived as kind or humane
            if (PersonalityTraits.TenderMinded(captor.Personality)) delta += 2;
            if (PersonalityTraits.Altruistic(captor.Personality)) delta += 1;
            if (PersonalityTraits.Honest(captor.Personality)) delta += 1;
            if (PersonalityTraits.Trusting(captor.Personality)) delta += 1;

            // Reduce affection for cold or hostile personalities
            if (PersonalityTraits.EasilyAngered(captor.Personality)) delta -= 2;
            if (PersonalityTraits.Impulsive(captor.Personality)) delta -= 1;
            if (PersonalityTraits.StressVulnerable(captor.Personality)) delta -= 1;

            // Unique handling for hostile and cruel captors
            if (PersonalityTraits.SelfConscious(captor.Personality)) delta -= 1;
            if (PersonalityTraits.Anxious(captor.Personality)) delta -= 1;

            // Stockholm-like edge case: admiration for powerful captor
            if (delta < 0 && PersonalityTraits.Assertive(captor.Personality) && PersonalityTraits.IntellectuallyCurious(captor.Personality))
                delta += 1; // confusion or fascination

            _affectionRepo.AdjustAffection(captiveId, captorId, delta);
        }
    }
}
