#nullable enable

using NUnit.Framework;
using ZooWorld.Animals;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The pure jump-burst closed form lands the nominal distance under the damping model.</summary>
    public sealed class JumpMathTests
    {
        [Test]
        public void BurstSpeed_IsDistanceTimesDamping()
        {
            Assert.That(JumpMath.BurstSpeed(1.5f, 4f), Is.EqualTo(6f));
        }

        [Test]
        public void BurstSpeed_RoundTrips_ToNominalDistance_DampThenMove()
        {
            const float dt = 0.02f;
            const float damping = 4f;
            float v = JumpMath.BurstSpeed(1.5f, damping);
            float dist = 0f;

            for (int i = 0; i < 2000; i++)
            {
                v /= 1f + (damping * dt); // damp, then...
                dist += v * dt;           // ...move (the order the live body uses)
            }

            Assert.That(dist, Is.EqualTo(1.5f).Within(1e-3f));
        }

        [Test]
        public void BurstSpeed_IsMonotonic_InDistanceAndDamping()
        {
            Assert.That(JumpMath.BurstSpeed(2f, 4f), Is.GreaterThan(JumpMath.BurstSpeed(1.5f, 4f)));
            Assert.That(JumpMath.BurstSpeed(1.5f, 5f), Is.GreaterThan(JumpMath.BurstSpeed(1.5f, 4f)));
        }
    }
}
