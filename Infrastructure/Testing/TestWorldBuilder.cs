// SkyHorizont.Infrastructure.Testing/TestWorldBuilder.cs
using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Galaxy.Planet;

namespace SkyHorizont.Infrastructure.Testing
{
    public static class TestWorldBuilder
    {
        /// <summary>
        /// Drops both characters onto the same planet (co-location),
        /// optionally assigns the positive one as governor to make
        /// tax/economy effects visible too.
        /// </summary>
        public static Planet SeedSinglePlanetWithCouple(
            Planet planet,
            Character positive,
            Character negative,
            bool makePositiveGovernor = true)
        {
            if (makePositiveGovernor)
                planet.AssignGovernor(positive.Id);

            // family/romance links to encourage conception paths
            CharacterFactory.LinkAsLovers(positive, negative);

            planet.AddCitizen(positive.Id);
            planet.AddCitizen(negative.Id);

            return planet;
        }
    }
}
