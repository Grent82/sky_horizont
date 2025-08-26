namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Handles payments and negotiations for releasing captives.
    /// </summary>
    public interface IRansomService
    {
        /// <summary>
        /// Attempts to settle a ransom for the specified captive.
        /// The service searches for willing payers among family members,
        /// rivals, faction mates and secret lovers, charging the first candidate
        /// that both agrees and has sufficient funds and crediting the captor.
        /// </summary>
        /// <param name="captiveId">Identifier of the captive whose release is negotiated.</param>
        /// <param name="captorId">Identifier of the captor character to receive payment.</param>
        /// <param name="amount">Ransom amount.</param>
        /// <returns>true if payment succeeded; otherwise false.</returns>
        bool TryResolveRansom(Guid captiveId, Guid captorId, int amount);

        /// <summary>
        /// Handles a captive whose ransom was not paid in time by listing them on the
        /// ransom marketplace.
        /// </summary>
        /// <param name="captiveId">Identifier of the captive being listed.</param>
        /// <param name="captorId">Identifier of the captor (character or faction).
        /// This entity will receive payment when the ransom is purchased.</param>
        /// <param name="amount">Ransom amount to demand for the captive.</param>
        /// <param name="captorIsFaction">True if the captor identifier refers to a faction;
        /// otherwise it refers to an individual character.</param>
        void HandleUnpaidRansom(Guid captiveId, Guid captorId, int amount, bool captorIsFaction);
    }
}
