namespace SkyHorizont.Domain.Entity
{
    public record Personality(
        int Openness,
        int Conscientiousness,
        int Extraversion,
        int Agreeableness,
        int Neuroticism)
    {
        public int CheckCompatibility(Personality other)
        {
            int diffSum = Math.Abs(Openness - other.Openness)
                        + Math.Abs(Conscientiousness - other.Conscientiousness)
                        + Math.Abs(Extraversion - other.Extraversion)
                        + Math.Abs(Agreeableness - other.Agreeableness)
                        + Math.Abs(Neuroticism - other.Neuroticism);

            int score = 100 - (diffSum / 5);
            return Math.Clamp(score, 0, 100);
        }

        public double GetAffectionModifier() =>
            Agreeableness * 0.01 - Neuroticism * 0.005;

        public double GetRetreatModifier() =>
            (50 - Extraversion) * 0.002;

        public double GetAttackBonus() =>
            Openness * 0.001 + Extraversion * 0.001;

        public double GetDefenseBonus() =>
            Conscientiousness * 0.001 + Agreeableness * 0.001;
    }
}
