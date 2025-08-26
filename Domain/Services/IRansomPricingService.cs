using System;

namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Provides dynamic ransom price estimates based on character status.
    /// </summary>
    public interface IRansomPricingService
    {
        /// <summary>
        /// Estimates the ransom value for the specified captive.
        /// </summary>
        int EstimateRansomValue(Guid captiveId);
    }
}
