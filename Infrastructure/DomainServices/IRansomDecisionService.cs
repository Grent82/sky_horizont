using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Evaluates whether a character is willing to pay a ransom for a captive.
    /// </summary>
    public interface IRansomDecisionService
    {
        /// <summary>
        /// Returns true if the payer is willing to cover the ransom amount for the captive.
        /// Used by <see cref="RansomService"/> prior to charging any funds.
        /// </summary>
        bool WillPayRansom(Guid payerId, Guid captiveId, int amount);
    }
}
