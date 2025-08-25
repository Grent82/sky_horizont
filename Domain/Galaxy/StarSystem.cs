namespace SkyHorizont.Domain.Galaxy
{
    public class StarSystem
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        public double X { get; }
        public double Y { get; }

        private readonly HashSet<Guid> _planetIds = new();
        private readonly HashSet<Guid> _adjacentSystems = new();

        /// <summary>
        /// Tracks relative control strength per faction (e.g. fleet/garrison power).
        /// </summary>
        private readonly Dictionary<Guid, double> _factionStrength = new();

        public IReadOnlyCollection<Guid> PlanetIds => _planetIds;
        public IReadOnlyCollection<Guid> AdjacentSystemIds => _adjacentSystems;
        public IReadOnlyDictionary<Guid, double> FactionStrength => _factionStrength;

        public StarSystem(Guid id, string name, double x = 0, double y = 0)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            X = x;
            Y = y;
        }

        public void AddPlanet(Guid planetId)
        {
            _planetIds.Add(planetId);
        }

        public void RegisterFactionStrength(Guid factionId, double strength)
        {
            // ToDo: based on characters, fleets, and planets
            _factionStrength[factionId] = strength;
        }

        public void AddConnection(Guid neighborSystemId) => _adjacentSystems.Add(neighborSystemId);

        public void TickStrengthDecay(double decayRate)
        {
            foreach (var key in _factionStrength.Keys.ToList())
                _factionStrength[key] = Math.Max(0, _factionStrength[key] * (1.0 - decayRate));
        }

        public Guid? CurrentController()
        {
            if (_factionStrength.Count == 0) return null;
            // the faction with the highest strength controls the system
            return _factionStrength
                .OrderByDescending(kv => kv.Value)
                .First().Key;
        }

        public override string ToString() =>
            $"{Name} (Planets: {_planetIds.Count}, Controller: {CurrentController()})";
    }
}
