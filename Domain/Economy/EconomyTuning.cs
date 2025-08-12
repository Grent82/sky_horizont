namespace SkyHorizont.Domain.Economy
{
    /// <summary>
    /// Balance knobs for economy-related calculations.
    /// Keep defaults matching your current behavior; tweak in DI if desired.
    /// </summary>
    public sealed record EconomyTuning(
        // Base rates (match your previous constants)
        double BaseShipMaintPct = 0.02,
        int BaseInfraUpkeepPerLvl = 3,

        // Commander/governor modifiers (0.0..1.0 is typical)
        double FleetMaintSkillReductionPer100 = 0.25,   // up to -25% maint at Military=100
        double FleetMaintConscReductionPer100 = 0.10,   // up to -10% maint at Conscientiousness=100
        double GovInfraSkillReductionPer100 = 0.30,     // up to -30% infra upkeep at Economy=100
        double GovConscReductionPer100 = 0.10           // up to -10% infra upkeep at Conscientiousness=100
    );
}
