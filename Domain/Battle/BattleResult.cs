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
        public int OutcomeMerit { get; }
        public int LootCredits { get; }
        public int PlanetCaptureBonus { get; }

        public BattleResult(
            Guid battleId,
            Guid winningFactionId,
            Guid losingFactionId,
            Fleet? winnerFleet,
            Fleet? loserFleet,
            int occupationDurationHours,
            int outcomeMerit,
            int lootCredits,
            int planetCaptureBonus)
        {
            BattleId = battleId;
            WinningFactionId = winningFactionId;
            LosingFactionId = losingFactionId;
            WinnerFleet = winnerFleet;
            LoserFleet = loserFleet;
            OccupationDurationHours = occupationDurationHours;
            OutcomeMerit = outcomeMerit;
            LootCredits = lootCredits;
            PlanetCaptureBonus = planetCaptureBonus;
        }
    }
}
