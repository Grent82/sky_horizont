using SkyHorizont.Domain.Galaxy;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class StarmapService : IStarmapService
    {
        private readonly Dictionary<Guid, StarSystem> _systems;
        private readonly Dictionary<Guid, Guid> _pirateBases = new();

        public StarmapService(IEnumerable<StarSystem> systems)
        {
            _systems = systems?.ToDictionary(s => s.Id) ?? new Dictionary<Guid, StarSystem>();
        }

        public double GetDistance(Guid systemA, Guid systemB)
        {
            if (!_systems.TryGetValue(systemA, out var a) || !_systems.TryGetValue(systemB, out var b))
                return double.PositiveInfinity;
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Guid? GetNearestPirateFaction(Guid systemId)
        {
            if (!_systems.ContainsKey(systemId) || _pirateBases.Count == 0) return null;
            Guid? nearest = null;
            double best = double.PositiveInfinity;
            foreach (var kv in _pirateBases)
            {
                var dist = GetDistance(systemId, kv.Value);
                if (dist < best)
                {
                    best = dist;
                    nearest = kv.Key;
                }
            }
            return nearest;
        }

        public void RegisterPirateBase(Guid factionId, Guid systemId)
        {
            _pirateBases[factionId] = systemId;
        }

        public void AddSystem(StarSystem system)
        {
            _systems[system.Id] = system;
        }
    }
}
