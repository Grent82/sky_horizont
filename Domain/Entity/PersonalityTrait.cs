namespace SkyHorizont.Domain.Entity
{
    public static class PersonalityTraits
    {
        // ——— Openness Facets ———
        public static bool HasHighImagination(Personality p) =>
            p.Openness > 60 && p.Openness < 100;       // Fantasy/Imagination
        public static bool AppreciatesArt(Personality p) =>
            p.Openness > 60;                           // Aesthetics
        public static bool EmotionallyAware(Personality p) =>
            p.Openness > 50 && p.Agreeableness > 50;   // Feelings
        public static bool Adventurous(Personality p) =>
            p.Openness > 65 && p.Extraversion > 50;    // Actions
        public static bool IntellectuallyCurious(Personality p) =>
            p.Openness > 75 && p.Conscientiousness > 50; // Ideas
        public static bool LiberalThinker(Personality p) =>
            p.Openness > 70 && p.Agreeableness > 40;   // Values

        // ——— Conscientiousness Facets ———
        public static bool SelfEfficient(Personality p) =>
            p.Conscientiousness > 65;                  // Competence
        public static bool Orderly(Personality p) =>
            p.Conscientiousness > 60;                  // Order
        public static bool Dutiful(Personality p) =>
            p.Conscientiousness > 70 && p.Agreeableness > 50;  // Dutifulness
        public static bool AchievementOriented(Personality p) =>
            p.Conscientiousness > 75;                  // Achievement Striving
        public static bool Disciplined(Personality p) =>
            p.Conscientiousness > 70;                  // Self-Discipline
        public static bool Deliberate(Personality p) =>
            p.Conscientiousness > 65 && p.Neuroticism < 50; // Deliberation

        // ——— Extraversion Facets ———
        public static bool WarmAndFriendly(Personality p) =>
            p.Extraversion > 70 && p.Agreeableness > 60; // Warmth
        public static bool Sociable(Personality p) =>
            p.Extraversion > 65;                        // Gregariousness
        public static bool Assertive(Personality p) =>
            p.Extraversion > 70 && p.Conscientiousness > 40; // Assertiveness
        public static bool Energetic(Personality p) =>
            p.Extraversion > 60;                        // Activity
        public static bool ThrillSeeker(Personality p) =>
            p.Extraversion > 80 && p.Openness > 60;     // Excitement-Seeking
        public static bool Cheerful(Personality p) =>
            p.Extraversion > 65 && p.Neuroticism < 50;  // Positive Emotions

        // ——— Agreeableness Facets ———
        public static bool Trusting(Personality p) =>
            p.Agreeableness > 65;                       // Trust
        public static bool Honest(Personality p) =>
            p.Agreeableness > 70 && p.Conscientiousness > 50; // Straightforwardness
        public static bool Altruistic(Personality p) =>
            p.Agreeableness > 70;                       // Altruism
        public static bool Cooperative(Personality p) =>
            p.Agreeableness > 70;                       // Compliance
        public static bool Modest(Personality p) =>
            p.Agreeableness > 60 && p.Extraversion < 50; // Modesty
        public static bool TenderMinded(Personality p) =>
            p.Agreeableness > 65 && p.Openness > 50;    // Tender-mindedness

        // ——— Neuroticism (Emotional Stability) Facets ———
        public static bool Anxious(Personality p) =>
            p.Neuroticism > 70;                         // Anxiety
        public static bool EasilyAngered(Personality p) =>
            p.Neuroticism > 65 && p.Agreeableness < 50; // Angry Hostility
        public static bool ProneToSadness(Personality p) =>
            p.Neuroticism > 75;                         // Depression
        public static bool SelfConscious(Personality p) =>
            p.Neuroticism > 60 && p.Agreeableness < 50; // Self-Consciousness
        public static bool Impulsive(Personality p) =>
            p.Neuroticism > 60 && p.Conscientiousness < 50; // Impulsiveness
        public static bool StressVulnerable(Personality p) =>
            p.Neuroticism > 70;                         // Vulnerability
    }
}
