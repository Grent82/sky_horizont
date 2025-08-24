using SkyHorizont.Domain.Social;
using System.Collections.Concurrent;

namespace SkyHorizont.Infrastructure.Social
{
    /// <summary>
    /// Simple append-only in-memory event log usable by UI and tests.
    /// </summary>
    public class InMemorySocialEventLog : ISocialEventLog
    {
        private readonly ConcurrentQueue<ISocialEvent> _events = new();

        public void Append(ISocialEvent ev) => _events.Enqueue(ev);

        public IReadOnlyList<ISocialEvent> GetAll() => _events.ToArray();

        public void Clear()
        {
            while (_events.TryDequeue(out _)) { }
        }
    }
}
