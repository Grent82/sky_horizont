using SkyHorizont.Domain.Galaxy.Planet;
using SkyHorizont.Domain.Fleets;
using SkyHorizont.Domain.Travel;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class TravelService : ITravelService
    {
        private readonly IPlanetRepository _planets;
        private readonly IFleetRepository _fleets;
        private readonly IRandomService _rng;
        private readonly ITravelRepository _travel;
        private readonly IPiracyService _piracy;
        private readonly IGameClockService _clock;
        private readonly Dictionary<Guid, List<Guid>> _planIdsBySystem;

        public TravelService(IPlanetRepository planets, IFleetRepository fleets, IRandomService rng, ITravelRepository travelRepository, IPiracyService piracy, IGameClockService clock)
        {
            _planets = planets ?? throw new ArgumentNullException(nameof(planets));
            _fleets = fleets ?? throw new ArgumentNullException(nameof(fleets));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _travel = travelRepository ?? throw new ArgumentNullException(nameof(travelRepository));
            _piracy = piracy ?? throw new ArgumentNullException(nameof(piracy));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _planIdsBySystem = new();
        }

        public Guid PlanFleetTravel(Guid fleetId, Guid originPlanetId, Guid destPlanetId, Resources? cargo = null, IReadOnlyList<Guid>? passengerIds = null)
        {
            var origin = _planets.GetById(originPlanetId) ?? throw new DomainException($"Invalid origin {originPlanetId}");
            var dest = _planets.GetById(destPlanetId) ?? throw new DomainException($"Invalid destination {destPlanetId}");
            var fleet = _fleets.GetById(fleetId) ?? throw new DomainException($"Fleet {fleetId} not found");
            if (fleet.IsAssigned) throw new DomainException($"Fleet {fleetId} is already assigned");

            if (cargo.HasValue && !cargo.Value.IsEmpty) fleet.AddCargo(cargo.Value);
            if (passengerIds is not null) foreach (var pid in passengerIds) fleet.AddPassenger(pid);

            var route = CalculateRoute(origin, dest);
            var eta = CalculateEtaMonths(route, 1.0);
            var plan = new TravelPlan(Guid.NewGuid(), fleetId, originPlanetId, destPlanetId, route, _clock.CurrentYear, _clock.CurrentMonth, eta, TravelStatus.Planned, 0.0, 0);
            _travel.Add(plan);
            IndexPlan(plan);
            var order = new MoveOrder(Guid.NewGuid(), plan.Id, route, _travel);
            fleet.EnqueueOrder(order);
            _fleets.Save(fleet);
            _travel.Update(plan with { Status = TravelStatus.InProgress });
            return plan.Id;
        }

        public IEnumerable<TravelPlan> GetPlansInSystem(Guid systemId)
        {
            if (!_planIdsBySystem.TryGetValue(systemId, out var ids)) return Array.Empty<TravelPlan>();
            var plans = new List<TravelPlan>(ids.Count);
            foreach (var id in ids)
            {
                var p = _travel.GetById(id);
                if (p is not null && p.Status == TravelStatus.InProgress) plans.Add(p);
            }
            return plans;
        }

        private IReadOnlyList<Guid> CalculateRoute(Planet origin, Planet dest)
        {
            var systems = new List<Guid> { origin.SystemId };
            if (origin.SystemId != dest.SystemId) systems.Add(dest.SystemId);
            return systems.AsReadOnly();
        }

        private int CalculateEtaMonths(IReadOnlyList<Guid> route, double speedFactor) => Math.Max(1, (int)(route.Count / speedFactor));

        private void IndexPlan(TravelPlan plan)
        {
            foreach (var sid in plan.RouteSystemIds)
            {
                if (!_planIdsBySystem.TryGetValue(sid, out var list)) _planIdsBySystem[sid] = list = new List<Guid>();
                list.Add(plan.Id);
            }
        }

        public void TickTravel(int year, int month)
        {
            throw new NotImplementedException();
        }
    }
}
