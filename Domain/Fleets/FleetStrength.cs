namespace SkyHorizont.Domain.Fleets
{
    public record FleetStrength(double MilitaryPower, double LogisticsPower)
    {
        public double Total => MilitaryPower + LogisticsPower;
    }
}