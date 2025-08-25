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
        private readonly IRouteService _route;
        private readonly IStarmapService _starmap;
        private readonly Dictionary<Guid, List<Guid>> _planIdsBySystem;

        public TravelService(IPlanetRepository planets, IFleetRepository fleets, IRandomService rng, ITravelRepository travelRepository, IPiracyService piracy, IGameClockService clock, IRouteService route, IStarmapService starmap)
        {
            _planets = planets ?? throw new ArgumentNullException(nameof(planets));
            _fleets = fleets ?? throw new ArgumentNullException(nameof(fleets));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _travel = travelRepository ?? throw new ArgumentNullException(nameof(travelRepository));
            _piracy = piracy ?? throw new ArgumentNullException(nameof(piracy));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _route = route ?? throw new ArgumentNullException(nameof(route));
            _starmap = starmap ?? throw new ArgumentNullException(nameof(starmap));
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

            var route = _route.FindRoute(origin.SystemId, dest.SystemId);
            double distance = 0.0;
            for (int i = 0; i < route.Count - 1; i++)
                distance += _starmap.GetDistance(route[i], route[i + 1]);
            var eta = _route.EstimateMonths(fleet.AverageFleetSpeed, distance, _clock.MonthsPerYear);
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

        private void IndexPlan(TravelPlan plan)
        {
            foreach (var sid in plan.RouteSystemIds)
            {
                if (!_planIdsBySystem.TryGetValue(sid, out var list)) _planIdsBySystem[sid] = list = new List<Guid>();
                list.Add(plan.Id);
            }
        }

        private void RemovePlan(Guid planId, IReadOnlyList<Guid> route)
        {
            foreach (var sid in route)
            {
                if (_planIdsBySystem.TryGetValue(sid, out var list))
                {
                    list.Remove(planId);
                    if (list.Count == 0) _planIdsBySystem.Remove(sid);
                }
            }
        }

        public void TickTravel(int year, int month)
        {
            var plans = _travel.GetAll().Where(p => p.Status == TravelStatus.InProgress).ToList();
            foreach (var plan in plans)
            {
                var fleet = _fleets.GetById(plan.FleetId);
                if (fleet is null) continue;

                fleet.TickOrders(1.0);
                _fleets.Save(fleet);

                var updated = _travel.GetById(plan.Id);
                if (updated is null) continue;

                if (updated.Status is TravelStatus.Completed or TravelStatus.Cancelled)
                {
                    RemovePlan(plan.Id, plan.RouteSystemIds);
                    continue;
                }

                var currentSystem = fleet.CurrentSystemId;
                var activity = _piracy.GetPirateActivity(currentSystem);
                var traffic = _piracy.GetTrafficLevel(currentSystem);
                var risk = Math.Max(0, activity - traffic) * 0.01;
                if (_rng.NextDouble() < risk)
                {
                    _piracy.RegisterAmbush(_piracy.GetPirateFactionId(), currentSystem, year, month);
                }
            }
        }
    }
}
