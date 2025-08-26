using System.Linq;
using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public class RansomDecisionService : IRansomDecisionService
    {
        private readonly ICharacterRepository _characterRepo;

        public RansomDecisionService(ICharacterRepository characterRepo)
        {
            _characterRepo = characterRepo;
        }

        public bool WillPayRansom(Guid payerId, Guid captiveId, int amount)
        {
            var payer = _characterRepo.GetById(payerId);
            var captive = _characterRepo.GetById(captiveId);
            if (payer == null || captive == null) return false;

            bool hasRelationship = payer.Relationships.Any(r => r.TargetCharacterId == captiveId);
            int compatibility = payer.Personality.CheckCompatibility(captive.Personality);

            double score = payer.Personality.Agreeableness + compatibility * 0.5;
            if (hasRelationship) score += 30;
            score -= amount / 100.0;
            return score >= 100;
        }
    }
}
