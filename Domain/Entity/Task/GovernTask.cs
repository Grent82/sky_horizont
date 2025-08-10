using SkyHorizont.Domain.Services;

namespace SkyHorizont.Domain.Entity.Task
{
    public class GovernTask : EntityTask
    {
        private Character? _chr;
        public Guid PlanetId => TargetId;

        public GovernTask(Guid id, Guid planetId, int rewardMerit = 25)
            : base(id, TaskType.Govern, planetId, rewardMerit) { }

        protected override double SpeedFactor(IRandomService rng) => 0.03 + rng.NextDouble() * 0.003;

        protected override bool EvaluateSuccess(Character chr, IRandomService rng, out string? failureReason)
        {
            // Economy skill + conscientiousness style defense bonus -> administrative quality
            double roll = chr.Skills.Economy * 1.1
                        + chr.Personality.GetDefenseBonus() * 15.0
                        + rng.NextDouble() * 10.0;

            bool ok = roll >= 45.0;
            failureReason = ok ? null : "Policy gridlock and supply disruptions.";
            return ok;
        }

        protected override TaskEffect? CreateEffect(Character chr, bool success, IGameClockService clock, IRandomService rng)
        {
            if (!success)
            {
                // Optional: slight stability wobble on failure
                return new GovernanceChangeEffect(
                    TaskId: Id,
                    CharacterId: chr.Id,
                    CompletedYear: clock.CurrentYear,
                    CompletedMonth: clock.CurrentMonth,
                    PlanetId: PlanetId,
                    StabilityDelta: -0.01,  // minor unrest
                    CreditsGenerated: 0
                );
            }

            // Success: some credits and a stability bump
            int credits = 50 + (int)(chr.Skills.Economy * 1.25) + (int)(rng.NextDouble() * 40);
            double stab = 0.02 + (chr.Personality.GetDefenseBonus() * 0.02);

            return new GovernanceChangeEffect(
                TaskId: Id,
                CharacterId: chr.Id,
                CompletedYear: clock.CurrentYear,
                CompletedMonth: clock.CurrentMonth,
                PlanetId: PlanetId,
                StabilityDelta: stab,
                CreditsGenerated: credits
            );
        }

        protected override void OnAssigned(Character chr) => _chr = chr;
    }
}
