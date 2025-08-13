using SkyHorizont.Domain.Diplomacy;
using SkyHorizont.Infrastructure.Persistence.Interfaces;

namespace SkyHorizont.Infrastructure.Persistence.Diplomacy
{
    public sealed class TreatiesRepository : IDiplomacyRepository
    {
        private readonly ITreatiesDbContext _ctx;

        public TreatiesRepository(ITreatiesDbContext ctx) => _ctx = ctx;

        public IEnumerable<Treaty> GetAll() => _ctx.Treaties.Values;

        public IEnumerable<Treaty> FindBetween(Guid factionA, Guid factionB)
        {
            var (a, b) = Normalize(factionA, factionB);
            return _ctx.Treaties.Values.Where(t =>
                (t.FactionA, t.FactionB) == (a, b) || (t.FactionA, t.FactionB) == (b, a));
        }

        public Treaty? GetById(Guid id)
        {
            _ctx.Treaties.TryGetValue(id, out var t);
            return t;
        }

        public Treaty Add(Treaty treaty)
        {
            _ctx.Treaties[treaty.Id] = treaty;
            _ctx.SaveChanges();
            return treaty;
        }

        public void Save(Treaty treaty)
        {
            _ctx.Treaties[treaty.Id] = treaty;
            _ctx.SaveChanges();
        }

        public void Remove(Guid id)
        {
            _ctx.Treaties.Remove(id);
            _ctx.SaveChanges();
        }

        private static (Guid a, Guid b) Normalize(Guid x, Guid y)
            => x.CompareTo(y) <= 0 ? (x, y) : (y, x);
    }
}
