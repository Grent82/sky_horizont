using SkyHorizont.Domain.Fleets;

namespace SkyHorizont.Domain.Battle
{
    public class BattleResult
    {
        public Guid BattleId { get; }
        public Guid WinningFactionId { get; }
        public Guid LosingFactionId { get; }
        public Fleet? WinnerFleet { get; }
        public Fleet? LoserFleet { get; }
        public int OccupationDurationHours { get; }

        public BattleResult(Guid battleId,
                            Guid winnerFaction,
                            Guid loserFaction,
                            Fleet? winnerFleet,
                            Fleet? loserFleet,
                            int hoursOccupied = 0)
        {
            BattleId = battleId;
            WinningFactionId = winnerFaction;
            LosingFactionId = loserFaction;
            WinnerFleet = winnerFleet;
            LoserFleet = loserFleet;
            OccupationDurationHours = hoursOccupied;
        }
    }
}
