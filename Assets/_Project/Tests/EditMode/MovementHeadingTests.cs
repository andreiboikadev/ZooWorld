#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The shared random-heading helper yields unit-length XZ vectors.</summary>
    public sealed class MovementHeadingTests
    {
        [Test]
        public void RandomXz_IsUnitLength_OnXZPlane()
        {
            FakeRandom rng = new FakeRandom(0f, 0.25f, 0.5f, 0.875f);

            for (int i = 0; i < 4; i++)
            {
                Vector3 heading = MovementHeading.RandomXz(rng);
                Assert.That(heading.y, Is.EqualTo(0f));
                Assert.That(heading.magnitude, Is.EqualTo(1f).Within(1e-5f));
            }
        }

        [Test]
        public void RandomXz_AngleZero_PointsPositiveX()
        {
            FakeRandom rng = new FakeRandom(0f); // angle 0 → (cos 0, 0, sin 0) = (1,0,0)

            Vector3 heading = MovementHeading.RandomXz(rng);

            Assert.That(heading.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(heading.z, Is.EqualTo(0f).Within(1e-5f));
        }
    }
}
