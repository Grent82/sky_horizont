using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyHorizont.Domain.Prisoners
{
    /// <summary>
    /// Represents an ongoing ransom negotiation handled over multiple turns.
    /// </summary>
    public class PendingRansom
    {
        private readonly List<Guid> _candidates;

        public Guid CaptiveId { get; }
        public Guid CaptorId { get; }
        public int Amount { get; }
        public int NextIndex { get; private set; }
        public IReadOnlyList<Guid> CandidatePayers => _candidates.AsReadOnly();

        public PendingRansom(Guid captiveId, Guid captorId, int amount, IEnumerable<Guid> candidates)
        {
            CaptiveId = captiveId;
            CaptorId = captorId;
            Amount = amount;
            _candidates = candidates.ToList();
            NextIndex = 0;
        }

        /// <summary>
        /// Returns the next candidate to ask for payment, or null if no more remain.
        /// Advances the internal index when a candidate is returned.
        /// </summary>
        public Guid? NextPayer()
        {
            if (NextIndex >= _candidates.Count)
                return null;
            return _candidates[NextIndex++];
        }
    }
}
