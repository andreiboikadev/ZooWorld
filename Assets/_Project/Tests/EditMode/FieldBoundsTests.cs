#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The pure <see cref="FieldBounds"/> containment/clamp helpers (XZ, Y-agnostic).</summary>
    public sealed class FieldBoundsTests
    {
        private static FieldBounds Make()
        {
            // Centre at origin, half-extents 10 (X) × 6 (Z), inner margin 1.
            return new FieldBounds(Vector3.zero, new Vector2(10f, 6f), 1f);
        }

        [Test]
        public void Contains_PointInside_True()
        {
            Assert.That(Make().Contains(new Vector3(5f, 0f, 3f)), Is.True);
        }

        [Test]
        public void Contains_PointOutsideX_False()
        {
            Assert.That(Make().Contains(new Vector3(11f, 0f, 0f)), Is.False);
        }

        [Test]
        public void Contains_PointOutsideZ_False()
        {
            Assert.That(Make().Contains(new Vector3(0f, 0f, -7f)), Is.False);
        }

        [Test]
        public void Contains_IgnoresY()
        {
            Assert.That(Make().Contains(new Vector3(0f, 999f, 0f)), Is.True);
        }

        [Test]
        public void Nearest_OutsidePoint_ClampsToEdge()
        {
            Vector3 nearest = Make().Nearest(new Vector3(20f, 0f, -20f));

            Assert.That(nearest.x, Is.EqualTo(10f));
            Assert.That(nearest.z, Is.EqualTo(-6f));
        }

        [Test]
        public void Nearest_InsidePoint_Unchanged()
        {
            Vector3 nearest = Make().Nearest(new Vector3(4f, 0f, -2f));

            Assert.That(nearest.x, Is.EqualTo(4f));
            Assert.That(nearest.z, Is.EqualTo(-2f));
        }
    }
}
