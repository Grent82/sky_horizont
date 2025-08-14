using SkyHorizont.Domain.Entity;

namespace SkyHorizont.Infrastructure.Testing
{
    /// <summary>
    /// Simple helpers to create extreme-archetype characters for simulation/tests.
    /// </summary>
    public static class CharacterFactory
    {
        public static Personality SuperPositivePersonality => new(
            Openness:         100,
            Conscientiousness:100,
            Extraversion:     100,
            Agreeableness:    100,
            Neuroticism:        0);

        public static Personality SuperNegativePersonality => new(
            Openness:           0,
            Conscientiousness:  0,
            Extraversion:       0,
            Agreeableness:      0,
            Neuroticism:      100);

        public static SkillSet MaxSkills => new(
            Research:     100,
            Economy:      100,
            Intelligence: 100,
            Military:     100);

        public static Character CreateSuperPositive(
            Guid id, string name, Sex sex,
            int age, int birthYear, int birthMonth,
            Rank rank = Rank.Civilian)
        {
            return new Character(
                id,
                name,
                age, birthYear, birthMonth,
                sex,
                SuperPositivePersonality,
                MaxSkills,
                rank);
        }

        public static Character CreateSuperNegative(
            Guid id, string name, Sex sex,
            int age, int birthYear, int birthMonth,
            Rank rank = Rank.Civilian)
        {
            return new Character(
                id,
                name,
                age, birthYear, birthMonth,
                sex,
                SuperNegativePersonality,
                MaxSkills,
                rank);
        }

        /// <summary>
        /// Convenience: link the two as lovers (and family links),
        /// so your romance/pregnancy logic can kick in.
        /// </summary>
        public static void LinkAsLovers(Character a, Character b)
        {
            a.AddRelationship(b.Id, RelationshipType.Lover);
            b.AddRelationship(a.Id, RelationshipType.Lover);
            a.LinkFamilyMember(b.Id);
            b.LinkFamilyMember(a.Id);
        }
    }
}
