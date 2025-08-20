using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Domain.Fleets
{
    public sealed class MoveOrder : FleetOrder
    {
        public IReadOnlyList<Guid> RouteSystemIds { get; }
        public int CurrentLegIndex { get; private set; }
        public double LegProgress { get; private set; }
        public Guid TravelPlanId { get; }

        private readonly ITravelRepository _travelRepository;

        public MoveOrder(Guid id, Guid travelPlanId, IReadOnlyList<Guid> routeSystemIds, ITravelRepository travelRepository) : base(id)
        {
            if (routeSystemIds is null || routeSystemIds.Count == 0) throw new ArgumentException("Route is empty.", nameof(routeSystemIds));
            TravelPlanId = travelPlanId != Guid.Empty ? travelPlanId : throw new ArgumentException("Travel plan id empty.", nameof(travelPlanId));
            _travelRepository = travelRepository ?? throw new ArgumentNullException(nameof(travelRepository));
            RouteSystemIds = routeSystemIds;
            CurrentLegIndex = 0;
            LegProgress = 0.0;
        }

        public override void Execute(Fleet fleet, double delta)
        {
            ValidateFleet(fleet);

            var plan = _travelRepository.GetById(TravelPlanId);
            if (plan is null || plan.Status is not TravelStatus.InProgress)
            {
                Fail();
                return;
            }

            var targetSystem = RouteSystemIds[Math.Min(CurrentLegIndex, RouteSystemIds.Count - 1)];
            if (fleet.CurrentSystemId == targetSystem)
            {
                CurrentLegIndex++;
                LegProgress = 0.0;

                var advanced = plan with { CurrentLegIndex = CurrentLegIndex, TravelProgress = 0.0 };
                if (CurrentLegIndex >= RouteSystemIds.Count)
                {
                    _travelRepository.Update(advanced with { Status = TravelStatus.Completed });
                    Complete();
                    return;
                }

                _travelRepository.Update(advanced);
                return;
            }

            LegProgress += delta * fleet.AverageFleetSpeed;
            if (LegProgress >= 1.0)
            {
                fleet.CurrentSystemId = targetSystem;
                LegProgress = 0.0;

                var advanced = plan with { CurrentLegIndex = CurrentLegIndex + 1, TravelProgress = 0.0 };
                _travelRepository.Update(advanced);
            }
            else
            {
                _travelRepository.Update(plan with { TravelProgress = LegProgress });
            }
        }
    }
}
