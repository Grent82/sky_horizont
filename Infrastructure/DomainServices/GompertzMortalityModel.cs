using System;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    /// <summary>
    /// Gompertz-Makeham style monthly mortality approximation.
    /// You can tune alpha/beta to match your universe's baseline longevity.
    /// </summary>
    public sealed class GompertzMortalityModel : IMortalityModel
    {
        // Approximate adult hazard doubling every ~8 years; tweak freely for your setting.
        // h(age) = A * exp(B * ageYears)  (Makeham term omitted by default; add if you want accidents/background)
        private readonly double _A; // base hazard at age 0–1
        private readonly double _B; // growth rate with age
        private readonly double _makeham; // age-independent term (accidents)

        public GompertzMortalityModel(double a = 1e-5, double b = 0.085, double makeham = 0.0)
        {
            _A = a;
            _B = b;
            _makeham = makeham;
        }

        public double GetMonthlyDeathProbability(int ageYears, int ageMonthsRemainder, double riskMultiplier = 1.0)
        {
            var age = ageYears + ageMonthsRemainder / 12.0;
            var annualHazard = GetBaselineAnnualHazard(ageYears);
            // Convert annual hazard λ to monthly probability p ≈ 1 - exp(-λ/12)
            var monthly = 1.0 - Math.Exp(-(annualHazard) / 12.0);
            monthly *= riskMultiplier;
            return Clamp01(monthly);
        }

        public double GetBaselineAnnualHazard(int ageYears)
        {
            var gompertz = _A * Math.Exp(_B * ageYears);
            return gompertz + _makeham;
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
