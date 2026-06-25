#nullable enable

using System;

namespace ZooWorld.Core
{
    /// <summary>
    /// Production <see cref="IRandom"/> over a seeded <see cref="System.Random"/> — deterministic for
    /// a given seed, so a run can be reproduced. Ranges are upper-exclusive, matching the seam.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;

        /// <summary>Creates a generator seeded with <paramref name="seed"/>.</summary>
        public SeededRandom(int seed)
        {
            _random = new Random(seed);
        }

        /// <inheritdoc/>
        public float Value01()
        {
            return (float)_random.NextDouble();
        }

        /// <inheritdoc/>
        public float Range(float a, float b)
        {
            return a + ((float)_random.NextDouble() * (b - a));
        }

        /// <inheritdoc/>
        public int Range(int a, int b)
        {
            return _random.Next(a, b);
        }
    }
}
