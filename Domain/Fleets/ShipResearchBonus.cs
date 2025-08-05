namespace SkyHorizont.Domain.Fleets
{
    public class ShipResearchBonus
    {
        public double AttackPercentBonus { get; }
        public double DefensePercentBonus { get; }

        public ShipResearchBonus(double atkPct, double defPct)
        {
            AttackPercentBonus = atkPct;
            DefensePercentBonus = defPct;
        }
    }

}