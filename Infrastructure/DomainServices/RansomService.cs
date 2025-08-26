using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Provides operations for settling ransom payments between characters.
    /// </summary>
    public class RansomService : IRansomService
    {
        private readonly ICharacterRepository _cmdRepo;
        private readonly ICharacterFundsService _funds;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IPlanetRepository _planetRepo;
        private readonly IFleetRepository _fleetRepo;
        private readonly IRansomDecisionService _decision;

        public RansomService(
            ICharacterRepository characterRepository,
            ICharacterFundsService characterFundsService,
            IFactionFundsRepository fleetRepository,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IRansomDecisionService decisionService)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _factionFunds = fleetRepository;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _decision = decisionService;
        }

        /// <summary>
        /// Attempts to settle a ransom for the specified captive. Potential payers are
        /// considered in order: family members, faction members and then other
        /// associates (friends, rivals, lovers). For each candidate the decision
        /// service is consulted before attempting to deduct funds.
        /// </summary>
        public bool TryResolveRansom(Guid captiveId, int amount)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return false;

            bool Attempt(Guid payerId)
            {
                if (!_decision.WillPayRansom(payerId, captiveId, amount))
                    return false;
                if (!_funds.DeductCharacter(payerId, amount))
                    return false;
                _funds.CreditCharacter(captiveId, amount);
                return true;
            }

            // 1) Family
            foreach (var familyId in captive.FamilyLinkIds)
                if (Attempt(familyId)) return true;

            // 2) Faction members - Placeholder: not yet implemented
            // (retains previous behaviour where no faction payment occurs)

            // 3) Associates
            foreach (var associate in _cmdRepo.GetAssociates(captiveId))
                if (Attempt(associate.Id)) return true;

            return false;
        }
    }
}
