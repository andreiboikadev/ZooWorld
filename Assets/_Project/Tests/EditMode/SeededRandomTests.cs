#nullable enable

using NUnit.Framework;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The production RNG is deterministic per seed and respects its range bounds.</summary>
    public sealed class SeededRandomTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            SeededRandom a = new SeededRandom(12345);
            SeededRandom b = new SeededRandom(12345);

            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.Value01(), Is.EqualTo(b.Value01()));
            }
        }

        [Test]
        public void Value01_StaysInUnitInterval()
        {
            SeededRandom rng = new SeededRandom(7);

            for (int i = 0; i < 1000; i++)
            {
                float v = rng.Value01();
                Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void FloatRange_StaysWithinBounds()
        {
            SeededRandom rng = new SeededRandom(7);

            for (int i = 0; i < 1000; i++)
            {
                float v = rng.Range(2f, 5f);
                Assert.That(v, Is.GreaterThanOrEqualTo(2f).And.LessThan(5f));
            }
        }

        [Test]
        public void IntRange_IsUpperExclusive()
        {
            SeededRandom rng = new SeededRandom(7);

            for (int i = 0; i < 1000; i++)
            {
                int v = rng.Range(0, 3);
                Assert.That(v, Is.InRange(0, 2));
            }
        }
    }
}
