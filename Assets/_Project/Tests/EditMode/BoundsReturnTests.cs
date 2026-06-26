#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The pure bounds-return steering: inner-margin buffer + steer-to-centre, XZ only.</summary>
    public sealed class BoundsReturnTests
    {
        // Centre 0, half-extents 10 (X) × 6 (Z), inner margin 1 → inner rectangle 9 × 5.
        private static FieldBounds Make()
        {
            return new FieldBounds(Vector3.zero, new Vector2(10f, 6f), 1f);
        }

        [Test]
        public void IsWithinInner_TrueInside_FalseBeyondMargin()
        {
            FieldBounds bounds = Make();

            Assert.That(BoundsReturn.IsWithinInner(in bounds, new Vector3(5f, 0f, 3f)), Is.True);
            Assert.That(BoundsReturn.IsWithinInner(in bounds, new Vector3(9.5f, 0f, 0f)), Is.False);
            Assert.That(BoundsReturn.IsWithinInner(in bounds, new Vector3(0f, 0f, 5.5f)), Is.False);
        }

        [Test]
        public void Steer_Inside_ReturnsHeadingUnchanged()
        {
            FieldBounds bounds = Make();
            Vector3 heading = new Vector3(0.6f, 0f, 0.8f);

            Assert.That(BoundsReturn.Steer(in bounds, new Vector3(5f, 0f, 3f), heading), Is.EqualTo(heading));
        }

        [Test]
        public void Steer_OutsideX_PointsBackTowardCentre()
        {
            FieldBounds bounds = Make();

            Vector3 result = BoundsReturn.Steer(in bounds, new Vector3(9.5f, 0f, 0f), new Vector3(1f, 0f, 0f));

            Assert.That(result.x, Is.LessThan(0f));
            Assert.That(result.z, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(result.magnitude, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Steer_OutsideNegativeZ_PointsPositiveZ()
        {
            FieldBounds bounds = Make();

            Vector3 result = BoundsReturn.Steer(in bounds, new Vector3(0f, 0f, -5.5f), new Vector3(0f, 0f, -1f));

            Assert.That(result.z, Is.GreaterThan(0f));
        }

        [Test]
        public void Steer_IgnoresY()
        {
            FieldBounds bounds = Make();
            Vector3 heading = new Vector3(1f, 0f, 0f);

            // High Y but in-bounds on XZ → treated as inside → unchanged.
            Assert.That(BoundsReturn.Steer(in bounds, new Vector3(0f, 99f, 0f), heading), Is.EqualTo(heading));
        }

        [Test]
        public void Steer_Corner_PointsBackOnBothAxes()
        {
            FieldBounds bounds = Make();

            Vector3 result = BoundsReturn.Steer(in bounds, new Vector3(9.5f, 0f, 5.5f), new Vector3(1f, 0f, 1f));

            Assert.That(result.x, Is.LessThan(0f));
            Assert.That(result.z, Is.LessThan(0f));
        }
    }
}
