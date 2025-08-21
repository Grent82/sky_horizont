using SkyHorizont.Domain.Services;
using System.Collections.Concurrent;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class InMemoryIntimacyLog : IIntimacyLog
    {
        private readonly ConcurrentDictionary<(int y, int m, Guid mother), ConcurrentDictionary<Guid, byte>> _byMonthMother = new();

        public void RecordIntimacyEncounter(Guid charA, Guid charB, int year, int month)
        {
            if (month < 1 || month > 12)
                return;

            void Add(Guid mother, Guid partner)
            {
                var dict = _byMonthMother.GetOrAdd((year, month, mother), _ => new ConcurrentDictionary<Guid, byte>());
                dict.TryAdd(partner, 0);
            }

            Add(charA, charB);
            Add(charB, charA);
        }

        public IReadOnlyList<Guid> GetPartnersForMother(Guid motherId, int year, int month)
        {
            if (_byMonthMother.TryGetValue((year, month, motherId), out var partners))
                return partners.Keys.ToList();
            return Array.Empty<Guid>();
        }

        public void PurgeOlderThan(int year, int month)
        {
            int cutoff = ToKey(year, month);
            foreach (var key in _byMonthMother.Keys)
            {
                if (ToKey(key.y, key.m) < cutoff)
                    _byMonthMother.TryRemove(key, out _);
            }

            static int ToKey(int y, int m) => y * 100 + m;
        }
    }
}
