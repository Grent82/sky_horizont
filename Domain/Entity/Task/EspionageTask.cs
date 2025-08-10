using SkyHorizont.Domain.Services;

namespace SkyHorizont.Domain.Entity.Task
{
    public class EspionageTask : EntityTask
    {
        private Character? _chr;
        public Guid TargetFactionId => TargetId;

        public EspionageTask(Guid id, Guid targetFactionId, int rewardMerit = 30)
            : base(id, TaskType.Espionage, targetFactionId, rewardMerit) { }

        protected override double SpeedFactor(IRandomService rng) => 0.015 + rng.NextDouble() * 0.004;

        protected override bool EvaluateSuccess(Character chr, IRandomService rng, out string? failureReason)
        {
            // Intelligence-driven, with a mild “risk appetite” from Extraversion.
            double roll = chr.Skills.Intelligence * 1.1
                        + chr.Personality.GetAttackBonus() * 10.0
                        + rng.NextDouble() * 20.0;

            bool ok = roll >= 65.0;
            failureReason = ok ? null : "Network went cold; handlers spooked.";
            return ok;
        }

        protected override TaskEffect? CreateEffect(Character chr, bool success, IGameClockService clock, IRandomService rng)
        {
            // Even on failure, you might still produce a tiny, noisy rumor (optional).
            if (!success)
            {
                if (rng.NextDouble() < 0.25)
                {
                    return new IntelReportEffect(
                        TaskId: Id,
                        CharacterId: chr.Id,
                        CompletedYear: clock.CurrentYear,
                        CompletedMonth: clock.CurrentMonth,
                        TargetFactionId: TargetFactionId,
                        Summary: "Rumors of fleet repositioning, low confidence.",
                        IntelValue: 5
                    );
                }
                return null;
            }

            int intel = 20 + (int)(chr.Skills.Intelligence * 1.2) + (int)(rng.NextDouble() * 25);
            string summary = "Secured asset dump: garrisons, patrol routes, requisitions.";

            return new IntelReportEffect(
                TaskId: Id,
                CharacterId: chr.Id,
                CompletedYear: clock.CurrentYear,
                CompletedMonth: clock.CurrentMonth,
                TargetFactionId: TargetFactionId,
                Summary: summary,
                IntelValue: intel
            );
        }

        protected override void OnAssigned(Character chr) => _chr = chr;
    }
}
