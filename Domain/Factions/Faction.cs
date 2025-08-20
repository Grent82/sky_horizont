using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Factions
{
    public class Faction
    {
        private readonly HashSet<Guid> _characterIds = new();
        private readonly HashSet<Guid> _planetIds = new();
        private readonly Dictionary<Guid, DiplomaticStanding> _diplomacy = new();

        public Guid Id { get; }
        public string Name { get; private set; }
        public Guid LeaderId { get; private set; }
        public IReadOnlyCollection<Guid> CharacterIds => _characterIds.ToList().AsReadOnly();
        public IReadOnlyCollection<Guid> PlanetIds => _planetIds.ToList().AsReadOnly();
        public IReadOnlyDictionary<Guid, DiplomaticStanding> Diplomacy => _diplomacy;

        public Faction(Guid id, string name, Guid leaderId)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LeaderId = leaderId;
            _characterIds = new();
            _planetIds = new();
        }

        public void AddCharacter(Guid characterId)
        {
            if (!_characterIds.Contains(characterId))
                _characterIds.Add(characterId);
        }

        public void RemoveCharacter(Guid characterId)
        {
            _characterIds.Remove(characterId);
        }

        public void ProposeDiplomacy(Faction other, int proposedChange)
        {
            if (other.Id == Id)
                throw new DomainException("Cannot modify diplomacy with self.");

            var newVal = _diplomacy.TryGetValue(other.Id, out var current)
                ? current.Adjust(proposedChange)
                : new DiplomaticStanding(proposedChange);
            _diplomacy[other.Id] = newVal;
        }

        public void UpdateDiplomacy(Guid otherFactionId, DiplomaticStanding standing)
        {
            _diplomacy[otherFactionId] = standing;
        }

        public DiplomaticStanding GetStandingWith(Faction other)
        {
            return _diplomacy.TryGetValue(other.Id, out var val)
                ? val
                : new DiplomaticStanding(0);
        }

        public void ConquerPlanet(Guid planetId)
        {
            if (!_planetIds.Add(planetId))
                throw new DomainException($"Faction {Name} already owns planet {planetId}.");
        }

        public void LosePlanet(Guid planetId)
        {
            _planetIds.Remove(planetId);
        }

        public void ChangeLeader(Guid newLeader)
        {
            if (!_characterIds.Contains(newLeader))
                throw new DomainException($"Character {newLeader} is not part of faction {Name}.");
            LeaderId = newLeader;
        }
    }
}