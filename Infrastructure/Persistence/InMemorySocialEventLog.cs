using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.Social
{
    /// <summary>
    /// Simple append-only in-memory event log usable by UI and tests.
    /// </summary>
    public class InMemorySocialEventLog : ISocialEventLog
    {
        private readonly List<ISocialEvent> _events = new();

        public void Append(ISocialEvent ev) => _events.Add(ev);

        public IReadOnlyList<ISocialEvent> GetAll() => _events.AsReadOnly();

        public void Clear() => _events.Clear();
    }
}
