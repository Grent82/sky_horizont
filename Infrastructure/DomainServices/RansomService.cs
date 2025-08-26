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
        private readonly IFactionService _factions;

        public RansomService(
            ICharacterRepository characterRepository,
            ICharacterFundsService characterFundsService,
            IFactionFundsRepository fleetRepository,
            IPlanetRepository planetRepo,
            IFleetRepository fleetRepo,
            IRansomDecisionService decisionService,
            IFactionService factionService)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _factionFunds = fleetRepository;
            _planetRepo = planetRepo;
            _fleetRepo = fleetRepo;
            _decision = decisionService;
            _factions = factionService;
        }

        /// <summary>
        /// Attempts to settle a ransom for the specified captive. Potential payers are
        /// considered in order: family members, faction members and then other
        /// associates (friends, rivals, lovers). For each candidate the decision
        /// service is consulted before attempting to deduct funds.
        /// </summary>
        public bool TryResolveRansom(Guid captiveId, int amount)
        {
            if (!_decision.WillPayRansom(payerId, captiveId, amount))
                return _factions.NegotiatePrisonerExchange(payerId, captiveId);
            if (!_funds.DeductCharacter(payerId, amount))
                return _factions.NegotiatePrisonerExchange(payerId, captiveId);
            _funds.CreditCharacter(captiveId, amount);
            return true;
        }
    }
}
