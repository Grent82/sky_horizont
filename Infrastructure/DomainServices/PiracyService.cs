using SkyHorizont.Domain.Factions;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class PiracyService : IPiracyService
    {
        private readonly IFactionService _factions;
        private readonly IRandomService _rng;
        private readonly Guid _pirateFactionId;

        // simple in-memory state
        private readonly Dictionary<Guid, int> _pirateActivityBySystem = new(); // 0..100
        private readonly Dictionary<Guid, int> _trafficBySystem = new();        // 0..100
        private readonly HashSet<string> _ambushKeys = new(); // $"{actor}:{system}:{year}:{month}"

        // ToDo: multiple pirate faction
        public PiracyService(IFactionService factions, IRandomService rng, Guid pirateFactionId)
        {
            _factions = factions ?? throw new ArgumentNullException(nameof(factions));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (pirateFactionId == Guid.Empty) throw new ArgumentException("Pirate faction id cannot be empty.", nameof(pirateFactionId));
            _pirateFactionId = pirateFactionId;
        }

        public bool IsPirateFaction(Guid factionId) => factionId == _pirateFactionId;

        public int GetPirateActivity(Guid systemId)
        {
            if (systemId == Guid.Empty) return 0;
            if (_pirateActivityBySystem.TryGetValue(systemId, out var v)) return v;

            // lazy seed with moderate activity + noise
            var seeded = Math.Clamp(30 + _rng.NextInt(-10, 25), 0, 100);
            _pirateActivityBySystem[systemId] = seeded;
            return seeded;
        }

        public int GetTrafficLevel(Guid systemId)
        {
            if (systemId == Guid.Empty) return 0;
            if (_trafficBySystem.TryGetValue(systemId, out var v)) return v;

            // lazy seed with medium traffic + noise
            var seeded = Math.Clamp(50 + _rng.NextInt(-20, 30), 0, 100);
            _trafficBySystem[systemId] = seeded;
            return seeded;
        }

        public bool BecomePirate(Guid characterId)
        {
            if (characterId == Guid.Empty) return false;
            // If the character is already in the pirate faction, this is a no-op success.
            var currentFaction = _factions.GetFactionIdForCharacter(characterId);
            if (currentFaction == _pirateFactionId) return true;

            _factions.MoveCharacterToFaction(characterId, _pirateFactionId);
            return true;
        }

        public bool RegisterAmbush(Guid pirateActorId, Guid systemId, int year, int month)
        {
            if (pirateActorId == Guid.Empty || systemId == Guid.Empty) return false;
            var key = $"{pirateActorId:D}:{systemId:D}:{year}:{month}";
            // Disallow duplicates for the same window
            if (_ambushKeys.Contains(key)) return false;

            _ambushKeys.Add(key);

            // Slightly increase pirate activity in that system when an ambush is staged
            var current = GetPirateActivity(systemId);
            _pirateActivityBySystem[systemId] = Math.Clamp(current + 3 + _rng.NextInt(0, 4), 0, 100);
            return true;
        }

        public Guid GetPirateFactionId() => _pirateFactionId;

        public void RegisterPirateFaction(Guid id)
        {
            // current implementation supports only a single pirate faction
            // additional factions can be registered in future enhancements
        }
    }
}
