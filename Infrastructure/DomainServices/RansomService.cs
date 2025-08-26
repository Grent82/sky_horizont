using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using System.Collections.Generic;
using System.Linq;

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
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IRansomMarketplaceService _market;

        public RansomService(
            ICharacterRepository characterRepository,
            ICharacterFundsService characterFundsService,
            IRansomDecisionService decisionService,
            IFactionService factions,
            IFactionFundsRepository factionFunds,
            IRansomMarketplaceService market)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _decision = decisionService;
            _factions = factions;
            _factionFunds = factionFunds;
            _market = market;
        }

        /// <summary>
        /// Attempts to settle a ransom for the specified captive. Potential payers are
        /// considered in order: family members, rival characters, faction members
        /// and secret lovers. For each candidate the decision service is consulted
        /// before attempting to deduct funds from either personal or faction treasuries.
        /// When payment succeeds, the amount is credited to the captor.
        /// </summary>
        public bool TryResolveRansom(Guid captiveId, Guid captorId, int amount)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return false;

            var candidates = new List<Guid>();

            // Family members first
            candidates.AddRange(_cmdRepo.GetFamilyMembers(captiveId).Select(c => c.Id));

            // Rivals
            candidates.AddRange(captive.Relationships
                .Where(r => r.Type == RelationshipType.Rival)
                .Select(r => r.TargetCharacterId));

            // Faction members
            var factionId = _factions.GetFactionIdForCharacter(captiveId);
            var faction = _factions.GetFaction(factionId);
            candidates.AddRange(faction.CharacterIds.Where(id => id != captiveId));

            // Secret lovers
            candidates.AddRange(captive.Relationships
                .Where(r => r.Type == RelationshipType.Lover)
                .Select(r => r.TargetCharacterId));

            foreach (var payerId in candidates.Distinct())
            {
                if (!_decision.WillPayRansom(payerId, captiveId, amount))
                    continue;

                if (_funds.DeductCharacter(payerId, amount))
                {
                    _funds.CreditCharacter(captorId, amount);
                    return true;
                }

                var payerFaction = _factions.GetFactionIdForCharacter(payerId);
                if (_factionFunds.GetBalance(payerFaction) >= amount)
                {
                    _factionFunds.AddBalance(payerFaction, -amount);
                    _funds.CreditCharacter(captorId, amount);
                    return true;
                }
            }

            return false;
        }

        public void HandleUnpaidRansom(Guid captiveId, Guid captorId, int amount, bool captorIsFaction)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return;

            var listing = new RansomListing(captiveId, captorId, amount, captorIsFaction);
            _market.AddListing(listing);
        }
    }
}
