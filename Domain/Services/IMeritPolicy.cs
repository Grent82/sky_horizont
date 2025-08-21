using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Domain.Services
{
    public interface IMeritPolicy
    {
        /// <summary>
        /// Returns a merit delta for the given action and context.
        /// </summary>
        int Compute(MeritAction action, MeritContext ctx);
    }

    public enum MeritAction
    {
        Courtship,
        FamilyVisit,
        LoverVisit,
        Spy,
        Bribe,
        Recruit,
        Defect,
        Negotiate,
        Quarrel,
        Assassinate,
        Torture,
        Rape,
        TravelBooked,
        BecomePirate,
        RaidConvoy,
        BattleSmallWin,
        BattleMajorWin,
        Legendary
    }

    public sealed class MeritContext
    {
        public bool Success { get; init; }
        public CharacterAmbition? Ambition { get; init; }
        public bool ProducedIntel { get; init; }
        public int IntelSeverity { get; init; }
        public int EnemyStrength { get; init; }
        public int Loot { get; init; }
        public bool AtWar { get; init; }

        public static MeritContext Succeeded(CharacterAmbition? amb = null) => new() { Success = true, Ambition = amb };
        public static MeritContext Failed(CharacterAmbition? amb = null) => new() { Success = false, Ambition = amb };
    }
}
