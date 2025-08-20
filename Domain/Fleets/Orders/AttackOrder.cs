using SkyHorizont.Domain.Battle;
using SkyHorizont.Domain.Travel;

namespace SkyHorizont.Domain.Fleets
{
    public sealed class AttackOrder : FleetOrder
    {
        public Guid TargetFleetId { get; }
        private readonly IFleetRepository _fleets;
        private readonly IRiskService _risk;
        private readonly IBattleOutcomeService _outcome;

        public AttackOrder(Guid id, Guid targetFleetId, IFleetRepository fleets, IRiskService riskService, IBattleOutcomeService battleOutcomeService) : base(id)
        {
            TargetFleetId = targetFleetId != Guid.Empty ? targetFleetId : throw new ArgumentException("Target empty.", nameof(targetFleetId));
            _fleets = fleets ?? throw new ArgumentNullException(nameof(fleets));
            _risk = riskService ?? throw new ArgumentNullException(nameof(riskService));
            _outcome = battleOutcomeService ?? throw new ArgumentNullException(nameof(battleOutcomeService));
        }

        public override void Execute(Fleet fleet, double delta)
        {
            ValidateFleet(fleet);
            var target = _fleets.GetById(TargetFleetId);
            if (target == null || !target.Ships.Any()) { Fail(); return; }

            var result = _risk.ResolveBattle(fleet, target);
            foreach (var shipId in result.LoserFleet!.ComputeLostShips(result.LoserFleet.CalculateStrength().MilitaryPower * 0.5, result.DefenseRetreated))
                result.LoserFleet.DestroyShip(shipId);

            if (result.WinnerFleet?.Id == fleet.Id)
            {
                fleet.RewardAfterBattle(result, _outcome);
                Complete();
            }
            else
            {
                foreach (var shipId in fleet.ComputeLostShips(fleet.CalculateStrength().MilitaryPower * 0.5, result.DefenseRetreated))
                    fleet.DestroyShip(shipId);
                Fail();
            }

            _fleets.Save(fleet);
            _fleets.Save(target);
        }
    }
}
