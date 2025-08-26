using System;
using System.Collections.Generic;
using System.Linq;
using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// In-memory implementation of <see cref="IRansomMarketplaceService"/>.
    /// Allows browsing and purchasing ransom listings.
    /// </summary>
    public class RansomMarketplaceService : IRansomMarketplaceService
    {
        private readonly ICharacterFundsService _characterFunds;
        private readonly IFactionFundsRepository _factionFunds;
        private readonly List<RansomListing> _listings = new();

        public RansomMarketplaceService(
            ICharacterFundsService characterFunds,
            IFactionFundsRepository factionFunds)
        {
            _characterFunds = characterFunds;
            _factionFunds = factionFunds;
        }

        public IEnumerable<RansomListing> GetListings() => _listings.AsReadOnly();

        public void AddListing(RansomListing listing) => _listings.Add(listing);

        public bool TryPurchaseAsCharacter(Guid buyerId, Guid captiveId)
        {
            var listing = _listings.FirstOrDefault(l => l.CaptiveId == captiveId);
            if (listing == null)
                return false;
            if (!_characterFunds.DeductCharacter(buyerId, listing.Amount))
                return false;

            CreditCaptor(listing);
            _listings.Remove(listing);
            return true;
        }

        public bool TryPurchaseAsFaction(Guid factionId, Guid captiveId)
        {
            var listing = _listings.FirstOrDefault(l => l.CaptiveId == captiveId);
            if (listing == null)
                return false;

            if (_factionFunds.GetBalance(factionId) < listing.Amount)
                return false;

            _factionFunds.DeductBalance(factionId, listing.Amount);
            CreditCaptor(listing);
            _listings.Remove(listing);
            return true;
        }

        private void CreditCaptor(RansomListing listing)
        {
            if (listing.CaptorIsFaction)
                _factionFunds.AddBalance(listing.CaptorId, listing.Amount);
            else
                _characterFunds.CreditCharacter(listing.CaptorId, listing.Amount);
        }
    }
}

