namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Handles payments and negotiations for releasing captives.
    /// </summary>
    public interface IRansomService
    {
        /// <summary>
        /// Attempts to settle a ransom for the specified captive.
        /// The service will search for willing payers among family members,
        /// faction mates and other associates, charging the first candidate
        /// that both agrees and has sufficient funds.
        /// </summary>
        /// <param name="captiveId">Captive to receive the funds.</param>
        /// <param name="amount">Ransom amount.</param>
        /// <returns>true if payment succeeded; otherwise false.</returns>
        bool TryResolveRansom(Guid captiveId, int amount);
    }
}
