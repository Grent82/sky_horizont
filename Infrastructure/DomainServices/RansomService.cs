using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Prisoners;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Provides operations for settling ransom payments between characters.
    /// Supports multi-round negotiations and dynamic pricing.
    /// </summary>
    public class RansomService : IRansomService
    {
        private readonly ICharacterRepository _cmdRepo;
        private readonly ICharacterFundsService _funds;
        private readonly IRansomDecisionService _decision;
        private readonly IFactionService _factions;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly IRansomMarketplaceService _market;
        private readonly IRansomPricingService _pricing;
        private readonly Dictionary<Guid, PendingRansom> _pending = new();

        public RansomService(
            ICharacterRepository characterRepository,
            ICharacterFundsService characterFundsService,
            IRansomDecisionService decisionService,
            IFactionService factions,
            IFactionFundsRepository factionFunds,
            IRansomMarketplaceService market,
            IRansomPricingService pricing)
        {
            _cmdRepo = characterRepository;
            _funds = characterFundsService;
            _decision = decisionService;
            _factions = factions;
            _factionFunds = factionFunds;
            _market = market;
            _pricing = pricing;
        }

        public void StartRansom(Guid captiveId, Guid captorId)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return;

            var amount = _pricing.EstimateRansomValue(captiveId);
            var candidates = new List<Guid>();
            candidates.AddRange(_cmdRepo.GetFamilyMembers(captiveId).Select(c => c.Id));
            candidates.AddRange(captive.Relationships
                .Where(r => r.Type == RelationshipType.Rival)
                .Select(r => r.TargetCharacterId));
            var factionId = _factions.GetFactionIdForCharacter(captiveId);
            var faction = _factions.GetFaction(factionId);
            candidates.AddRange(faction.CharacterIds.Where(id => id != captiveId));
            candidates.AddRange(captive.Relationships
                .Where(r => r.Type == RelationshipType.Lover)
                .Select(r => r.TargetCharacterId));

            var pending = new PendingRansom(captiveId, captorId, amount, candidates.Distinct());
            _pending[captiveId] = pending;
        }

        public bool ProcessRansomTurn(Guid captiveId)
        {
            if (!_pending.TryGetValue(captiveId, out var pending))
                return false;

            var payerId = pending.NextPayer();
            if (payerId == null)
            {
                HandleUnpaidRansom(pending.CaptiveId, pending.CaptorId, pending.Amount, false);
                _pending.Remove(captiveId);
                return false;
            }

            if (!_decision.WillPayRansom(payerId.Value, captiveId, pending.Amount))
                return pending.NextIndex < pending.CandidatePayers.Count;

            if (_funds.DeductCharacter(payerId.Value, pending.Amount))
            {
                _funds.CreditCharacter(pending.CaptorId, pending.Amount);
                _pending.Remove(captiveId);
                return false;
            }

            var payerFaction = _factions.GetFactionIdForCharacter(payerId.Value);
            if (_factionFunds.GetBalance(payerFaction) >= pending.Amount)
            {
                _factionFunds.AddBalance(payerFaction, -pending.Amount);
                _funds.CreditCharacter(pending.CaptorId, pending.Amount);
                _pending.Remove(captiveId);
                return false;
            }

            return pending.NextIndex < pending.CandidatePayers.Count;
        }

        public void HandleUnpaidRansom(Guid captiveId, Guid captorId, int amount, bool captorIsFaction)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return;

            var listing = new RansomListing(captiveId, captorId, amount, captorIsFaction);
            _market.AddListing(listing);
        }

        public void KeepInHarem(Guid captiveId, Guid captorId)
        {
            var captive = _cmdRepo.GetById(captiveId);
            if (captive == null)
                return;

            captive.Enslave(captorId);
            captive.AddRelationship(captorId, RelationshipType.HaremMember);
        }
    }
}
