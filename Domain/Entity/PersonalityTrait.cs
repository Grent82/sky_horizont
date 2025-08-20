namespace SkyHorizont.Domain.Entity
{
    public enum TraitIntensity
    {
        None = 0,
        Low = 1,
        Moderate = 2,
        High = 3
    }

    public enum TraitKind
    {
        Any = 0,
        Trait = 1,
        Combination = 2
    }

    public record TraitDefinition(
        string Name,
        string Dimension, // Openness, Conscientiousness, Extraversion, Agreeableness, Neuroticism
        Func<Personality, TraitIntensity> IntensityEvaluator,
        string Description,
        Func<Personality, TraitIntensity, double> GameplayEffect
    );

    public record TraitCombination(
        string Name,
        string[] TraitNames,
        TraitIntensity MinimumIntensity,
        string Description,
        Func<Personality, TraitIntensity[], double> CombinedEffect
    );

    public static class PersonalityTraits
    {
        private static readonly List<TraitDefinition> Traits = new()
        {
            // Openness Traits
            new TraitDefinition(
                "HighImagination",
                "Openness",
                p => p.Openness switch
                {
                    > 80 => TraitIntensity.High,
                    > 60 => TraitIntensity.Moderate,
                    > 40 => TraitIntensity.Low,
                    _ => TraitIntensity.None
                },
                "Reflects a vivid imagination and fantasy-driven thinking, influencing creative tasks and espionage.",
                (p, i) => i switch
                {
                    TraitIntensity.High => 15.0,
                    TraitIntensity.Moderate => 10.0,
                    TraitIntensity.Low => 5.0,
                    _ => 0.0
                }
            ),
            new TraitDefinition(
                "AppreciatesArt",
                "Openness",
                p => p.Openness > 60 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Indicates appreciation for aesthetics, boosting diplomatic and courtship interactions.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "EmotionallyAware",
                "Openness",
                p => p.Openness > 50 && p.Agreeableness > 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Enhances empathy, improving family and romantic interactions.",
                (p, i) => i == TraitIntensity.Moderate ? 8.0 : 0.0
            ),
            new TraitDefinition(
                "Adventurous",
                "Openness",
                p => p.Openness > 65 && p.Extraversion > 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Drives risk-taking in espionage and defection, increasing success chance.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "IntellectuallyCurious",
                "Openness",
                p => p.Openness > 75 && p.Conscientiousness > 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Boosts research tasks and espionage intent scores.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "LiberalThinker",
                "Openness",
                p => p.Openness > 70 && p.Agreeableness > 40 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Promotes open-minded diplomacy, aiding negotiation intents.",
                (p, i) => i == TraitIntensity.Moderate ? 7.0 : 0.0
            ),

            // Conscientiousness Traits
            new TraitDefinition(
                "SelfEfficient",
                "Conscientiousness",
                p => p.Conscientiousness > 65 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reflects competence in tasks, boosting task performance.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Orderly",
                "Conscientiousness",
                p => p.Conscientiousness > 60 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Enhances organizational skills, aiding governance tasks.",
                (p, i) => i == TraitIntensity.Moderate ? 8.0 : 0.0
            ),
            new TraitDefinition(
                "Dutiful",
                "Conscientiousness",
                p => p.Conscientiousness > 70 && p.Agreeableness > 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Increases loyalty, reducing defection likelihood.",
                (p, i) => i == TraitIntensity.Moderate ? -10.0 : 0.0
            ),
            new TraitDefinition(
                "AchievementOriented",
                "Conscientiousness",
                p => p.Conscientiousness > 75 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Drives ambition, boosting merit gain and task success.",
                (p, i) => i == TraitIntensity.Moderate ? 12.0 : 0.0
            ),
            new TraitDefinition(
                "Disciplined",
                "Conscientiousness",
                p => p.Conscientiousness > 70 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Improves task reliability, especially in military contexts.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Deliberate",
                "Conscientiousness",
                p => p.Conscientiousness > 65 && p.Neuroticism < 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces impulsive actions, lowering assassination and torture intent scores.",
                (p, i) => i == TraitIntensity.Moderate ? -8.0 : 0.0
            ),

            // Extraversion Traits
            new TraitDefinition(
                "WarmAndFriendly",
                "Extraversion",
                p => p.Extraversion > 70 && p.Agreeableness > 60 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Boosts courtship and family interaction scores.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Sociable",
                "Extraversion",
                p => p.Extraversion > 65 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Increases social interaction success, like recruitment and negotiation.",
                (p, i) => i == TraitIntensity.Moderate ? 8.0 : 0.0
            ),
            new TraitDefinition(
                "Assertive",
                "Extraversion",
                p => p.Extraversion > 70 && p.Conscientiousness > 40 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Enhances negotiation and military intent scores, but may increase coercion risk.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Energetic",
                "Extraversion",
                p => p.Extraversion > 60 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Boosts task performance and intent scores for active roles.",
                (p, i) => i == TraitIntensity.Moderate ? 7.0 : 0.0
            ),
            new TraitDefinition(
                "ThrillSeeker",
                "Extraversion",
                p => p.Extraversion > 80 && p.Openness > 60 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Increases espionage and assassination intent scores.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Cheerful",
                "Extraversion",
                p => p.Extraversion > 65 && p.Neuroticism < 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Improves social interactions, reducing quarrel likelihood.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),

            // Agreeableness Traits
            new TraitDefinition(
                "Trusting",
                "Agreeableness",
                p => p.Agreeableness > 65 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Increases negotiation success but may make character vulnerable to bribes.",
                (p, i) => i == TraitIntensity.Moderate ? 8.0 : 0.0
            ),
            new TraitDefinition(
                "Honest",
                "Agreeableness",
                p => p.Agreeableness > 70 && p.Conscientiousness > 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces espionage and bribery intent scores.",
                (p, i) => i == TraitIntensity.Moderate ? -10.0 : 0.0
            ),
            new TraitDefinition(
                "Altruistic",
                "Agreeableness",
                p => p.Agreeableness > 70 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Boosts family and diplomatic interactions.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Cooperative",
                "Agreeableness",
                p => p.Agreeableness > 70 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Enhances negotiation and recruitment success.",
                (p, i) => i == TraitIntensity.Moderate ? 10.0 : 0.0
            ),
            new TraitDefinition(
                "Modest",
                "Agreeableness",
                p => p.Agreeableness > 60 && p.Extraversion < 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces aggressive intent scores like assassination.",
                (p, i) => i == TraitIntensity.Moderate ? -8.0 : 0.0
            ),
            new TraitDefinition(
                "TenderMinded",
                "Agreeableness",
                p => p.Agreeableness > 65 && p.Openness > 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces torture and rape intent scores, boosts empathy-based interactions.",
                (p, i) => i == TraitIntensity.Moderate ? -10.0 : 0.0
            ),

            // Neuroticism Traits
            new TraitDefinition(
                "Anxious",
                "Neuroticism",
                p => p.Neuroticism > 70 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces task performance under stress, like negotiation or espionage.",
                (p, i) => i == TraitIntensity.Moderate ? -10.0 : 0.0
            ),
            new TraitDefinition(
                "EasilyAngered",
                "Neuroticism",
                p => p.Neuroticism > 65 && p.Agreeableness < 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Increases quarrel, torture, and rape intent scores.",
                (p, i) => i == TraitIntensity.Moderate ? 15.0 : 0.0
            ),
            new TraitDefinition(
                "ProneToSadness",
                "Neuroticism",
                p => p.Neuroticism > 75 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces social interaction success, may increase mortality risk.",
                (p, i) => i == TraitIntensity.Moderate ? -8.0 : 0.0
            ),
            new TraitDefinition(
                "SelfConscious",
                "Neuroticism",
                p => p.Neuroticism > 60 && p.Agreeableness < 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Lowers courtship and recruitment intent scores.",
                (p, i) => i == TraitIntensity.Moderate ? -8.0 : 0.0
            ),
            new TraitDefinition(
                "Impulsive",
                "Neuroticism",
                p => p.Neuroticism > 60 && p.Conscientiousness < 50 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Increases risky actions like assassination, torture, and rape intents.",
                (p, i) => i == TraitIntensity.Moderate ? 15.0 : 0.0
            ),
            new TraitDefinition(
                "StressVulnerable",
                "Neuroticism",
                p => p.Neuroticism > 70 ? TraitIntensity.Moderate : TraitIntensity.None,
                "Reduces performance in high-stakes tasks, increases retreat likelihood.",
                (p, i) => i == TraitIntensity.Moderate ? -10.0 : 0.0
            )
        };

        private static readonly List<TraitCombination> TraitCombinations = new()
        {
            new TraitCombination(
                "ImpulsiveAnger",
                new[] { "Impulsive", "EasilyAngered" },
                TraitIntensity.Moderate,
                "Combines impulsivity and anger, significantly increasing the likelihood of harmful actions like torture or rape.",
                (p, intensities) =>
                {
                    if (intensities.All(i => i >= TraitIntensity.Moderate))
                        return 10.0; // Additional bonus for the combination
                    return 0.0;
                }
            )
            // ToDo: Add more combinations here as needed, e.g., Assertive + EasilyAngered for negotiation aggression
        };

        /// <summary>
        /// Evaluates the intensity of a specific trait for a character's personality.
        /// </summary>
        /// <param name="traitName">The name of the trait (e.g., "Cheerful").</param>
        /// <param name="personality">The character's personality.</param>
        /// <returns>The intensity level of the trait (None, Low, Moderate, High).</returns>
        public static TraitIntensity GetTraitIntensity(string traitName, Personality personality)
        {
            var trait = Traits.Find(t => t.Name == traitName);
            if (trait == null)
                throw new ArgumentException($"Unknown trait: {traitName}", nameof(traitName));
            return trait.IntensityEvaluator(personality);
        }

        /// <summary>
        /// Computes the gameplay effect (e.g., score modifier) of a specific trait.
        /// </summary>
        /// <param name="traitName">The name of the trait.</param>
        /// <param name="personality">The character's personality.</param>
        /// <returns>The gameplay effect value (e.g., score bonus or penalty).</returns>
        public static double GetTraitEffect(string traitName, Personality personality)
        {
            return GetTraitEffect(traitName, personality, TraitKind.Trait);
        }

        /// <summary>
        /// Computes the gameplay effect of a specific trait combination.
        /// </summary>
        /// <param name="combinationName">The name of the trait combination (e.g., "ImpulsiveAnger").</param>
        /// <param name="personality">The character's personality.</param>
        /// <returns>The combined gameplay effect value (e.g., score bonus).</returns>
        public static double GetTraitCombinationEffect(string combinationName, Personality personality)
        {
            return GetTraitEffect(combinationName, personality, TraitKind.Combination);
        }

        /// <summary>
        /// Returns the gameplay effect for a name that can refer to either a Trait or a TraitCombination.
        /// Use 'kind' to force resolution if you have name collisions.
        /// Throws if not found or ambiguous (when kind == Any and both exist).
        /// </summary>
        public static double GetTraitEffect(string name, Personality personality, TraitKind kind = TraitKind.Any)
        {
            if (TryGetTraitEffect(name, personality, out var effect, kind))
                return effect;

            // Build a clear error
            bool traitExists = Traits.Any(t => t.Name == name);
            bool comboExists = TraitCombinations.Any(c => c.Name == name);

            if (!traitExists && !comboExists)
                throw new ArgumentException($"Unknown trait or combination: '{name}'", nameof(name));

            if (kind == TraitKind.Any && traitExists && comboExists)
                throw new InvalidOperationException(
                    $"Name '{name}' exists as both a Trait and a TraitCombination. " +
                    $"Disambiguate using TraitKind.Trait or TraitKind.Combination.");

            // If we got here, it means resolution produced no active effect (e.g., intensities below threshold).
            return 0.0;
        }

        /// <summary>
        /// Safe resolver: returns false if name not found or inactive (effect 0).
        /// </summary>
        public static bool TryGetTraitEffect(string name, Personality personality, out double effect, TraitKind kind = TraitKind.Any)
        {
            effect = 0.0;

            bool traitExists = Traits.Any(t => t.Name == name);
            bool comboExists = TraitCombinations.Any(c => c.Name == name);

            if (kind == TraitKind.Trait || (kind == TraitKind.Any && traitExists && !comboExists))
            {
                var trait = Traits.FirstOrDefault(t => t.Name == name);
                if (trait == null) return false;
                var intensity = trait.IntensityEvaluator(personality);
                if (intensity == TraitIntensity.None) return false;
                effect = trait.GameplayEffect(personality, intensity);
                return effect != 0.0;
            }

            if (kind == TraitKind.Combination || (kind == TraitKind.Any && comboExists && !traitExists))
            {
                var combo = TraitCombinations.FirstOrDefault(c => c.Name == name);
                if (combo == null) return false;
                var intensities = combo.TraitNames.Select(tn => GetTraitIntensity(tn, personality)).ToArray();
                if (!intensities.All(i => i >= combo.MinimumIntensity)) return false;
                effect = combo.CombinedEffect(personality, intensities);
                return effect != 0.0;
            }

            // Ambiguous: both exist and kind == Any → let caller decide (we treat as not resolved here)
            return false;
        }

        /// <summary>
        /// Returns every active (non-zero) effect across traits and combinations with their kind.
        /// Useful for UI/debugging or aggregate scoring.
        /// </summary>
        public static List<(string Name, double Effect, TraitKind Kind)> GetAllActiveEffects(Personality personality)
        {
            var results = new List<(string, double, TraitKind)>(Traits.Count + TraitCombinations.Count);

            // Traits
            foreach (var trait in Traits)
            {
                var intensity = trait.IntensityEvaluator(personality);
                if (intensity == TraitIntensity.None) continue;
                var eff = trait.GameplayEffect(personality, intensity);
                if (eff != 0.0)
                    results.Add((trait.Name, eff, TraitKind.Trait));
            }

            // Combinations
            foreach (var combo in TraitCombinations)
            {
                var intensities = combo.TraitNames.Select(tn => GetTraitIntensity(tn, personality)).ToArray();
                if (!intensities.All(i => i >= combo.MinimumIntensity)) continue;
                var eff = combo.CombinedEffect(personality, intensities);
                if (eff != 0.0)
                    results.Add((combo.Name, eff, TraitKind.Combination));
            }

            return results;
        }

        /// <summary>
        /// Gets all traits with non-zero intensity for a character's personality, grouped by dimension.
        /// </summary>
        /// <param name="personality">The character's personality.</param>
        /// <returns>A dictionary mapping dimension names to lists of (trait name, intensity).</returns>
        public static Dictionary<string, List<(string Name, TraitIntensity Intensity)>> GetActiveTraits(Personality personality)
        {
            var result = new Dictionary<string, List<(string, TraitIntensity)>>();
            foreach (var dimension in new[] { "Openness", "Conscientiousness", "Extraversion", "Agreeableness", "Neuroticism" })
                result[dimension] = new List<(string, TraitIntensity)>();

            foreach (var trait in Traits)
            {
                var intensity = trait.IntensityEvaluator(personality);
                if (intensity != TraitIntensity.None)
                    result[trait.Dimension].Add((trait.Name, intensity));
            }

            return result;
        }

        /// <summary>
        /// Gets all active trait combinations for a character's personality.
        /// </summary>
        /// <param name="personality">The character's personality.</param>
        /// <returns>A list of (combination name, effect value) for active combinations.</returns>
        public static List<(string Name, double Effect)> GetActiveTraitCombinations(Personality personality)
        {
            var result = new List<(string, double)>();
            foreach (var combination in TraitCombinations)
            {
                var intensities = combination.TraitNames
                    .Select(t => GetTraitIntensity(t, personality))
                    .ToArray();
                if (intensities.All(i => i >= combination.MinimumIntensity))
                {
                    var effect = combination.CombinedEffect(personality, intensities);
                    if (effect != 0.0)
                        result.Add((combination.Name, effect));
                }
            }
            return result;
        }

        #region Compatibility Methods for Existing Code
        // Openness
        public static bool HasHighImagination(Personality p) => GetTraitIntensity("HighImagination", p) >= TraitIntensity.Moderate;
        public static bool AppreciatesArt(Personality p) => GetTraitIntensity("AppreciatesArt", p) >= TraitIntensity.Moderate;
        public static bool EmotionallyAware(Personality p) => GetTraitIntensity("EmotionallyAware", p) >= TraitIntensity.Moderate;
        public static bool Adventurous(Personality p) => GetTraitIntensity("Adventurous", p) >= TraitIntensity.Moderate;
        public static bool IntellectuallyCurious(Personality p) => GetTraitIntensity("IntellectuallyCurious", p) >= TraitIntensity.Moderate;
        public static bool LiberalThinker(Personality p) => GetTraitIntensity("LiberalThinker", p) >= TraitIntensity.Moderate;

        // Conscientiousness
        public static bool SelfEfficient(Personality p) => GetTraitIntensity("SelfEfficient", p) >= TraitIntensity.Moderate;
        public static bool Orderly(Personality p) => GetTraitIntensity("Orderly", p) >= TraitIntensity.Moderate;
        public static bool Dutiful(Personality p) => GetTraitIntensity("Dutiful", p) >= TraitIntensity.Moderate;
        public static bool AchievementOriented(Personality p) => GetTraitIntensity("AchievementOriented", p) >= TraitIntensity.Moderate;
        public static bool Disciplined(Personality p) => GetTraitIntensity("Disciplined", p) >= TraitIntensity.Moderate;
        public static bool Deliberate(Personality p) => GetTraitIntensity("Deliberate", p) >= TraitIntensity.Moderate;

        // Extraversion
        public static bool WarmAndFriendly(Personality p) => GetTraitIntensity("WarmAndFriendly", p) >= TraitIntensity.Moderate;
        public static bool Sociable(Personality p) => GetTraitIntensity("Sociable", p) >= TraitIntensity.Moderate;
        public static bool Assertive(Personality p) => GetTraitIntensity("Assertive", p) >= TraitIntensity.Moderate;
        public static bool Energetic(Personality p) => GetTraitIntensity("Energetic", p) >= TraitIntensity.Moderate;
        public static bool ThrillSeeker(Personality p) => GetTraitIntensity("ThrillSeeker", p) >= TraitIntensity.Moderate;
        public static bool Cheerful(Personality p) => GetTraitIntensity("Cheerful", p) >= TraitIntensity.Moderate;

        // Agreeableness
        public static bool Trusting(Personality p) => GetTraitIntensity("Trusting", p) >= TraitIntensity.Moderate;
        public static bool Honest(Personality p) => GetTraitIntensity("Honest", p) >= TraitIntensity.Moderate;
        public static bool Altruistic(Personality p) => GetTraitIntensity("Altruistic", p) >= TraitIntensity.Moderate;
        public static bool Cooperative(Personality p) => GetTraitIntensity("Cooperative", p) >= TraitIntensity.Moderate;
        public static bool Modest(Personality p) => GetTraitIntensity("Modest", p) >= TraitIntensity.Moderate;
        public static bool TenderMinded(Personality p) => GetTraitIntensity("TenderMinded", p) >= TraitIntensity.Moderate;

        // Neuroticism
        public static bool Anxious(Personality p) => GetTraitIntensity("Anxious", p) >= TraitIntensity.Moderate;
        public static bool EasilyAngered(Personality p) => GetTraitIntensity("EasilyAngered", p) >= TraitIntensity.Moderate;
        public static bool ProneToSadness(Personality p) => GetTraitIntensity("ProneToSadness", p) >= TraitIntensity.Moderate;
        public static bool SelfConscious(Personality p) => GetTraitIntensity("SelfConscious", p) >= TraitIntensity.Moderate;
        public static bool Impulsive(Personality p) => GetTraitIntensity("Impulsive", p) >= TraitIntensity.Moderate;
        public static bool StressVulnerable(Personality p) => GetTraitIntensity("StressVulnerable", p) >= TraitIntensity.Moderate;
        #endregion
    }
}