namespace SkyHorizont.Domain.Galaxy
{
    public class StarSystem
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        private readonly HashSet<Guid> _planetIds = new();

        /// <summary>
        /// Tracks relative control strength per faction (e.g. fleet/garrison power).
        /// </summary>
        private readonly Dictionary<Guid, double> _factionStrength = new();

        public IReadOnlyCollection<Guid> PlanetIds => _planetIds;
        public IReadOnlyDictionary<Guid, double> FactionStrength => _factionStrength;

        public StarSystem(Guid id, string name)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void AddPlanet(Guid planetId)
        {
            _planetIds.Add(planetId);
        }

        public void RegisterFactionStrength(Guid factionId, double strength)
        {
            // ToDo: based on commanders, fleets, and planets
            _factionStrength[factionId] = strength;
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
