using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Derives character locations from current Planet/Fleet state.
    /// </summary>
    public sealed class LocationService : ILocationService
    {
        private readonly IPlanetRepository _planets;
        private readonly IFleetRepository _fleets;

        public LocationService(IPlanetRepository planets, IFleetRepository fleets)
        {
            _planets = planets;
            _fleets  = fleets;
        }

        public CharacterLocation? GetCharacterLocation(Guid characterId)
        {
            // Planet governor / planet captives / fleet commanders / fleet captives
            foreach (var p in _planets.GetAll())
            {
                if (p.GovernorId == characterId)
                    return new CharacterLocation(LocationKind.Planet, p.Id, p.SystemId);
                if (p.CapturedCharacterIds.Contains(characterId))
                    return new CharacterLocation(LocationKind.Planet, p.Id, p.SystemId);
            }

            foreach (var f in _fleets.GetAll())
            {
                if (f.AssignedCharacterId == characterId)
                    return new CharacterLocation(LocationKind.Fleet, f.Id, f.CurrentSystemId);
                if (f.CapturedCharacterIds.Contains(characterId))
                    return new CharacterLocation(LocationKind.Fleet, f.Id, f.CurrentSystemId);
            }

            return null;
        }

        public bool AreCoLocated(Guid characterA, Guid characterB)
        {
            var a = GetCharacterLocation(characterA);
            var b = GetCharacterLocation(characterB);
            if (a is null || b is null) return false;
            return a.Kind == b.Kind && a.HostId == b.HostId;
        }

        public bool AreInSameSystem(Guid characterA, Guid characterB)
        {
            var a = GetCharacterLocation(characterA);
            var b = GetCharacterLocation(characterB);
            if (a is null || b is null) return false;
            return a.SystemId == b.SystemId;
        }

        public IEnumerable<Guid> GetCharactersOnPlanet(Guid planetId)
        {
            var p = _planets.GetById(planetId);
            if (p is null) yield break;

            if (p.GovernorId.HasValue && p.GovernorId.Value != Guid.Empty)
                yield return p.GovernorId.Value;

            foreach (var f in p.GetStationedFleets())
                if (f.AssignedCharacterId.HasValue) yield return f.AssignedCharacterId.Value;

            foreach (var c in p.CapturedCharacterIds)
                yield return c;
        }

        public IEnumerable<Guid> GetCharactersOnFleet(Guid fleetId)
        {
            var f = _fleets.GetById(fleetId);
            if (f is null) yield break;

            if (f.AssignedCharacterId.HasValue) yield return f.AssignedCharacterId.Value;
            foreach (var c in f.CapturedCharacterIds)
                yield return c;
        }

        public IEnumerable<Guid> GetCaptivesOnPlanet(Guid planetId)
        {
            var p = _planets.GetById(planetId);
            return p is null ? Enumerable.Empty<Guid>() : p.CapturedCharacterIds;
        }

        public IEnumerable<Guid> GetCaptivesOnFleet(Guid fleetId)
        {
            var f = _fleets.GetById(fleetId);
            return f is null ? Enumerable.Empty<Guid>() : f.CapturedCharacterIds;
        }
    }
}
