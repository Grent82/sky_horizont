
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Domain.Faction
{
    public class Faction
    {
        private readonly HashSet<Guid> _commanderIds = new();
        private readonly HashSet<Guid> _planetIds = new();

        public Guid Id { get; }
        public string Name { get; private set; }
        public Guid LeaderId { get; private set; }
        public IReadOnlyCollection<Guid> CommanderIds => _commanderIds.ToList().AsReadOnly();
        public IReadOnlyCollection<Guid> PlanetIds => _planetIds.ToList().AsReadOnly();

        // Diplomacy with others: key = other‑faction ID
        private readonly Dictionary<Guid, DiplomaticStanding> _diplomacy = new();
        public IReadOnlyDictionary<Guid, DiplomaticStanding> Diplomacy => _diplomacy;

        public Faction(Guid id, string name, Guid leaderId)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LeaderId = leaderId;
            _commanderIds = new();
            _planetIds = new();
        }

        public void AddCommander(Guid commanderId)
        {
            if (!_commanderIds.Contains(commanderId))
                _commanderIds.Add(commanderId);
        }

        public void RemoveCommander(Guid commanderId)
        {
            _commanderIds.Remove(commanderId);
        }

        public void ProposeDiplomacy(Faction other, int proposedChange)
        {
            if (other.Id == Id)
                throw new DomainException("Cannot modify diplomacy with self.");

            var newVal = _diplomacy.TryGetValue(other.Id, out var current)
                ? current.Adjust(proposedChange)
                : new DiplomaticStanding(proposedChange);

            _diplomacy[other.Id] = newVal;
            // ToDo: Optionally: signal domain event or auto-copy reciprocal with dampening
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
            if (! _commanderIds.Contains(newLeader))
                throw new DomainException(
                    $"Commander {newLeader} is not part of faction {Name}.");

            LeaderId = newLeader;
        }
    }
}
