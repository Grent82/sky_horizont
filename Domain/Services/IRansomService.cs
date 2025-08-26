namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Handles payments and negotiations for releasing captives.
    /// </summary>
    public interface IRansomService
    {
        /// <summary>
        /// Starts a ransom negotiation for the captive. The amount is calculated
        /// by the pricing service and candidate payers are recorded.
        /// </summary>
        void StartRansom(Guid captiveId, Guid captorId);

        /// <summary>
        /// Processes a single negotiation turn for the captive. Exactly one
        /// candidate payer is approached per call. Returns true if the ransom
        /// remains pending after this call; false if negotiation concluded
        /// either by payment or by exhausting all candidates.
        /// </summary>
        bool ProcessRansomTurn(Guid captiveId);

        /// <summary>
        /// Handles a captive whose ransom was not paid in time by listing them on the
        /// ransom marketplace.
        /// </summary>
        void HandleUnpaidRansom(Guid captiveId, Guid captorId, int amount, bool captorIsFaction);

        /// <summary>
        /// Converts the captive into a harem member of the captor, skipping ransom flow.
        /// </summary>
        void KeepInHarem(Guid captiveId, Guid captorId);
    }
}
