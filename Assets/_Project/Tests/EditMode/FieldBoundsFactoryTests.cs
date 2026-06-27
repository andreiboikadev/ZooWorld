#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="FieldBoundsFactory"/> frustum closed form: a straight-down camera projects to an
    /// XZ rectangle whose half-depth is tan(½ vFov)·height and half-width is that times the aspect, centred
    /// under the camera at the ground plane.
    /// </summary>
    public sealed class FieldBoundsFactoryTests
    {
        [Test]
        public void FromTopDownCamera_DefaultFraming_MatchesGddFootprint()
        {
            // height 10.392, vFov 60°, aspect 1.6667 → halfDepth ≈ 6, halfWidth ≈ 10 → ≈ 20×12 m (GDD §9).
            FieldBounds bounds = FieldBoundsFactory.FromTopDownCamera(
                new Vector3(0f, 10.392f, 0f), 60f, 1.6667f, 0f, 1f);

            Assert.That(bounds.HalfExtents.x, Is.EqualTo(10f).Within(1e-3f));
            Assert.That(bounds.HalfExtents.y, Is.EqualTo(6f).Within(1e-3f));
            Assert.That(bounds.Center, Is.EqualTo(Vector3.zero));
            Assert.That(bounds.InnerMargin, Is.EqualTo(1f));
        }

        [Test]
        public void FromTopDownCamera_CenterTracksCameraXz_AtGroundY()
        {
            FieldBounds bounds = FieldBoundsFactory.FromTopDownCamera(
                new Vector3(3f, 8f, -2f), 60f, 1.5f, 0f, 1.5f);

            Assert.That(bounds.Center, Is.EqualTo(new Vector3(3f, 0f, -2f)));
        }

        [Test]
        public void FromTopDownCamera_HalfWidthIsHalfDepthTimesAspect()
        {
            FieldBounds bounds = FieldBoundsFactory.FromTopDownCamera(
                new Vector3(0f, 10f, 0f), 50f, 1.78f, 0f, 1f);

            Assert.That(bounds.HalfExtents.x, Is.EqualTo(bounds.HalfExtents.y * 1.78f).Within(1e-4f));
        }

        [Test]
        public void FromTopDownCamera_HalfDepthScalesLinearlyWithHeight()
        {
            FieldBounds low = FieldBoundsFactory.FromTopDownCamera(new Vector3(0f, 5f, 0f), 60f, 1.6667f, 0f, 1f);
            FieldBounds high = FieldBoundsFactory.FromTopDownCamera(new Vector3(0f, 10f, 0f), 60f, 1.6667f, 0f, 1f);

            Assert.That(high.HalfExtents.y, Is.EqualTo(low.HalfExtents.y * 2f).Within(1e-4f));
        }
    }
}
