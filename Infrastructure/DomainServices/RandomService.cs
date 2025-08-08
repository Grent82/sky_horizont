using System;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class RandomService : IRandomService
    {
        private Random _rng;
        public int CurrentSeed { get; private set; }

        public RandomService(int seed = 0)
        {
            CurrentSeed = seed == 0 ? Environment.TickCount : seed;
            _rng = new Random(CurrentSeed);
        }

        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public double NextDouble() => _rng.NextDouble();

        public void Reseed(int seed)
        {
            CurrentSeed = seed;
            _rng = new Random(seed);
        }
    }
}
