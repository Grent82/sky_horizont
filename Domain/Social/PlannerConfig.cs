namespace SkyHorizont.Domain.Social
{
    public sealed class PlannerConfig
    {
        public int MaxIntentsPerMonth { get; init; } = 2;
        public double ScoreNoiseMax { get; init; } = 5.0;
        public double RomanceWeight { get; init; } = 0.9;
        public double FamilyWeight { get; init; } = 0.7;
        public double LoverVisitWeight { get; init; } = 1.0;
        public double SpyWeight { get; init; } = 1.0;
        public double BribeWeight { get; init; } = 0.8;
        public double RecruitWeight { get; init; } = 0.9;
        public double DefectionWeight { get; init; } = 1.0;
        public double NegotiateWeight { get; init; } = 0.7;
        public double QuarrelWeight { get; init; } = 0.6;
        public double AssassinateWeight { get; init; } = 0.65;
        public double TortureWeight { get; init; } = 0.75;
        public double RapeWeight { get; init; } = 0.6;
        public double TravelWeight { get; init; } = 0.8;
        public double BecomePirateWeight { get; init; } = 0.9;
        public double RaidConvoyWeight { get; init; } = 1.0;
        public int MinBribeBudget { get; init; } = 200;
        public double AssassinateFrequency { get; init; } = 0.05;
        public int MaxCandidatePool { get; init; } = 60;
        public int MaxCrossFactionPool { get; init; } = 40;
        public int QuarrelOpinionThreshold { get; init; } = -25;
        public int AssassinationOpinionThreshold { get; init; } = -50;
        public int ConflictBuffer { get; init; } = 8;

        public static PlannerConfig Default => new();
    }
}
