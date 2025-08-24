using System;
using System.Security.Cryptography;
using SkyHorizont.Domain.Services;

namespace SkyHorizont.Infrastructure.DomainServices
{
    public sealed class RandomService : IRandomService
    {
        private readonly object _sync = new();
        private Random _rng;
        public int CurrentSeed { get; private set; }

        public RandomService(int seed = 0)
        {
            CurrentSeed = seed == 0 ? RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue) : seed;
            _rng = new Random(CurrentSeed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            lock (_sync)
            {
                return _rng.Next(minInclusive, maxExclusive);
            }
        }

        public double NextDouble()
        {
            lock (_sync)
            {
                return _rng.NextDouble();
            }
        }

        public void Reseed(int seed)
        {
            lock (_sync)
            {
                CurrentSeed = seed;
                _rng = new Random(seed);
            }
        }
    }
}
