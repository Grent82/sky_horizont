using SkyHorizont.Domain.Services;

namespace SkyHorizont.Domain.Entity.Task
{
    public class ResearchTask : EntityTask
    {
        private Character? _chr;
        public string TechFocus { get; }

        public ResearchTask(Guid id, Guid planetId, string techFocus, int rewardMerit = 20)
            : base(id, TaskType.Research, planetId, rewardMerit)
        {
            TechFocus = techFocus;
        }

        protected override double SpeedFactor(IRandomService rng)
        {
            // Slight noise so parallel researchers don’t finish in lockstep.
            return 0.02 + rng.NextDouble() * 0.005;
        }

        protected override bool EvaluateSuccess(Character chr, IRandomService rng, out string? failureReason)
        {
            // Simple model using Research skill + personality bonus + randomness
            double roll = chr.Skills.Research * 1.0
                        + chr.Personality.GetAttackBonus() * 20.0 // Openness/Extraversion bonus repurposed as creativity
                        + rng.NextDouble() * 15.0;

            bool ok = roll >= 60.0;
            failureReason = ok ? null : "Insufficient breakthroughs this month.";
            return ok;
        }

        protected override TaskEffect? CreateEffect(Character chr, bool success, IGameClockService clock, IRandomService rng)
        {
            if (!success) return null;

            int points = 150 + (int)(chr.Skills.Research * 1.5) + (int)(rng.NextDouble() * 50);
            return new ResearchUnlockEffect(
                TaskId: Id,
                CharacterId: chr.Id,
                CompletedYear: clock.CurrentYear,
                CompletedMonth: clock.CurrentMonth,
                TechnologyName: TechFocus,
                ResearchPoints: points
            );
        }

        protected override void OnAssigned(Character chr) => _chr = chr;
    }
}
