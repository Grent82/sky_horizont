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
        /// rivals, faction mates and secret lovers, charging the first candidate
        /// that both agrees and has sufficient funds.
        /// </summary>
        /// <param name="captiveId">Captive to receive the funds.</param>
        /// <param name="amount">Ransom amount.</param>
        /// <param name="captorId">Identifier of the captor character.</param>
        /// <returns>true if payment succeeded; otherwise false.</returns>
        bool TryResolveRansom(Guid captiveId, int amount, Guid captorId);

        /// <summary>
        /// Handles a captive whose ransom was not paid in time, determining their fate.
        /// </summary>
        /// <param name="captiveId">The captive in question.</param>
        /// <param name="captorFaction">The faction currently holding the captive.</param>
        void HandleUnpaidRansom(Guid captiveId, Guid captorFaction);
    }
}
