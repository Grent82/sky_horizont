namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Handles payments and negotiations for releasing captives.
    /// </summary>
    public interface IRansomService
    {
        /// <summary>
        /// Attempts to settle a ransom by charging the payer and crediting the captive
        /// if the payer is willing and has sufficient funds.
        /// </summary>
        /// <param name="payerId">Character attempting to pay.</param>
        /// <param name="captiveId">Captive to receive the funds.</param>
        /// <param name="amount">Ransom amount.</param>
        /// <returns>true if payment succeeded; otherwise false.</returns>
        bool TryResolveRansom(Guid payerId, Guid captiveId, int amount);

        /// <summary>
        /// Handles a captive whose ransom was not paid in time, determining their fate.
        /// </summary>
        /// <param name="captiveId">The captive in question.</param>
        /// <param name="captorFaction">The faction currently holding the captive.</param>
        void HandleUnpaidRansom(Guid captiveId, Guid captorFaction);
    }
}
