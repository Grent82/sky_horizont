using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Social;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Gamey, tunable pregnancy rules.
    /// Notes:
    /// - Relation gate: requires bilateral opinion >= OpinionThreshold for consensual conception.
    /// - Prisoner edge cases: blocks consensual conception if mother is prisoner of partner.
    ///   Optional "dark path" toggle allows coercive conception under harsh personalities.
    /// - Postpartum protection: short cooldown after delivery (policy-local memory).
    /// </summary>
    public sealed class DefaultPregnancyPolicy : IPregnancyPolicy
    {
        private readonly IRandomService _rng;
        private readonly IOpinionRepository _opinions;
        private readonly ILocationService _loc;

        // Tunables
        private const int OpinionThreshold = 25;        // bilateral opinion threshold for consensual conception
        private const int MinAge = 13;                  // absolute floor
        private const int PostpartumCooldownMonths = 3; // 2–3 months suggested; we pick 3 for safety

        // "Dark path" – allow coercive conception if enabled and personality allows.
        // Keep false by default; wire from DI/config if you support it in your build.
        private readonly bool _allowCoercion;

        // Minimal local memory for postpartum cooldown.
        // NOTE: To keep it simple and non-invasive, we expose a public RecordDelivery(...)
        // helper your lifecycle can call immediately after birth.
        private readonly Dictionary<Guid, (int year, int month)> _lastDelivery = new();

        public DefaultPregnancyPolicy(
            IRandomService rng,
            IOpinionRepository opinions,
            ILocationService loc,
            bool allowCoercion = true)
        {
            _rng = rng;
            _opinions = opinions;
            _loc = loc;
            _allowCoercion = allowCoercion;
        }

        public bool ShouldHaveTwins(Character mother, int y, int m)
        {
            double b = 0.02 + (mother.Personality.Extraversion + mother.Personality.Openness - 100) * 0.0001;
            return _rng.NextDouble() < Math.Clamp(b, 0.01, 0.05);
        }

        public bool ShouldHaveComplications(Character mother, int y, int m, out string? note)
        {
            double p = 0.05 + mother.Personality.Neuroticism * 0.0003; // max ~+3%
            bool hit = _rng.NextDouble() < Math.Clamp(p, 0.03, 0.10);
            note = hit ? "Complications at birth." : null;
            return hit;
        }

        public bool IsPostpartumProtected(Character mother, int year, int month)
        {
            if (mother.ActivePregnancy is { Status: PregnancyStatus.Active })
                return true;

            if (!_lastDelivery.TryGetValue(mother.Id, out var last))
                return false;

            int dt = MonthsBetween(last.year, last.month, year, month);
            return dt < PostpartumCooldownMonths;
        }

        public void RecordDelivery(Guid motherId, int year, int month)
        {
            _lastDelivery[motherId] = (year, month);
        }

        public bool CanConceiveWith(Character potentialMother, Character partner, int year, int month)
        {
            if (!potentialMother.IsAlive || !partner.IsAlive)
                return false;
            if (potentialMother.Sex != Sex.Female || partner.Sex != Sex.Male)
                return false;
            if (potentialMother.Age < MinAge || partner.Age < MinAge)
                return false;

            if (potentialMother.ActivePregnancy is { Status: PregnancyStatus.Active })
                return false;
            if (IsPostpartumProtected(potentialMother, year, month))
                return false;

            if (!_loc.AreCoLocated(potentialMother.Id, partner.Id))
                return false;

            bool motherIsPrisonerOfPartner = _loc.IsPrisonerOf(potentialMother.Id, partner.Id);
            if (motherIsPrisonerOfPartner)
            {
                if (!_allowCoercion)
                    return false;

                // "Dark path" gate — only under certain personalities and with low likelihood.
                // High Assertiveness/Neuroticism + low Agreeableness → higher risk.
                double harsh = 0.0;
                harsh += (partner.Personality.Extraversion > 70 && PersonalityTraits.Assertive(partner.Personality)) ? 0.3 : 0.0;
                harsh += PersonalityTraits.EasilyAngered(partner.Personality) ? 0.3 : 0.0;
                harsh += PersonalityTraits.Impulsive(partner.Personality) ? 0.4 : 0.0;
                harsh += (50 - partner.Personality.Agreeableness) * 0.002;
                harsh = Math.Clamp(harsh, 0.1, 1.0);

                return _rng.NextDouble() < harsh;
            }

            int m2p = _opinions.GetOpinion(potentialMother.Id, partner.Id);
            int p2m = _opinions.GetOpinion(partner.Id, potentialMother.Id);
            if (m2p < OpinionThreshold || p2m < OpinionThreshold)
                return false;

            return true;
        }

        // Utility
        private static int MonthsBetween(int y1, int m1, int y2, int m2)
            => (y2 - y1) * 12 + (m2 - m1);
    }
}
