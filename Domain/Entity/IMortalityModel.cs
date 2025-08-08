namespace SkyHorizont.Domain.Services
{
    /// <summary>
    /// Returns the probability of death this turn (month) for a character,
    /// given age and optional risk modifiers (combat, disease, etc.).
    /// </summary>
    public interface IMortalityModel
    {
        /// <param name="ageYears">Whole years.</param>
        /// <param name="ageMonthsRemainder">0..11 remainder months.</param>
        /// <param name="riskMultiplier">>1 = riskier turn; <1 = safer.</param>
        double GetMonthlyDeathProbability(int ageYears, int ageMonthsRemainder, double riskMultiplier = 1.0);

        /// <summary>Optional: baseline peacetime mortality at given age.</summary>
        double GetBaselineAnnualHazard(int ageYears);
    }
}
