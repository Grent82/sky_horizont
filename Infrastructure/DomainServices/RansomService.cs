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
        private readonly IRansomDecisionService _decision;
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;
        private readonly IRansomMarketplaceService _market;

        public RansomService(
            ICharacterRepository characterRepository,
            ICharacterFundsService characterFundsService,
            IRansomDecisionService decisionService,
            IFactionService factions,
            IRandomService rng,
            IRansomMarketplaceService market)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _decision = decisionService;
            _factions = factions;
            _rng = rng;
            _market = market;
        }

        /// <summary>
        /// Attempts to settle a ransom for the specified captive. Potential payers are
        /// considered in order: family members, faction members and then other
        /// associates (friends, rivals, lovers). For each candidate the decision
        /// service is consulted before attempting to deduct funds.
        /// </summary>
        public bool TryResolveRansom(Guid payerId, Guid captiveId, int amount)
        {
            if (!_decision.WillPayRansom(payerId, captiveId, amount))
                return _factions.NegotiatePrisonerExchange(payerId, captiveId);
            if (!_funds.DeductCharacter(payerId, amount))
                return _factions.NegotiatePrisonerExchange(payerId, captiveId);
            _funds.CreditCharacter(captiveId, amount);
            return true;
        }

        public void HandleUnpaidRansom(Guid captiveId, Guid captorFaction)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return;

            var outcome = _rng.NextInt(0, 3);
            switch (outcome)
            {
                case 0:
                    // Sold to a slavery market; no specific owner.
                    captive.Enslave(null);
                    break;
                case 1:
                    // Transferred to captor's harem/crew.
                    var owner = _factions.GetLeaderId(captorFaction);
                    captive.Enslave(owner);
                    _factions.MoveCharacterToFaction(captiveId, captorFaction);
                    break;
                default:
                    // Execution.
                    captive.MarkDead();
                    break;
            }

            _cmdRepo.Save(captive);
        }
    }
}
