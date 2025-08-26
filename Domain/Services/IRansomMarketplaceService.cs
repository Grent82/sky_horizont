using System;
using System.Collections.Generic;

namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Represents a listing of an unpaid captive and the demanded ransom.
    /// </summary>
    public record RansomListing(Guid CaptiveId, Guid CaptorId, int Amount, bool CaptorIsFaction);

    /// <summary>
    /// Provides a marketplace interface for browsing and purchasing ransom listings.
    /// </summary>
    public interface IRansomMarketplaceService
    {
        /// <summary>
        /// Returns all current ransom listings.
        /// </summary>
        IEnumerable<RansomListing> GetListings();

        /// <summary>
        /// Adds a new listing to the marketplace.
        /// </summary>
        void AddListing(RansomListing listing);

        /// <summary>
        /// Attempts to purchase a ransom as an individual character.
        /// </summary>
        bool TryPurchaseAsCharacter(Guid buyerId, Guid captiveId);

        /// <summary>
        /// Attempts to purchase a ransom as a faction.
        /// </summary>
        bool TryPurchaseAsFaction(Guid factionId, Guid captiveId);
    }
}

