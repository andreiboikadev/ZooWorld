#nullable enable

using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Pure steering rule that keeps an animal inside the field. The inner margin is a buffer that
    /// suppresses edge-toggling (steer back before the true edge); it is not full two-threshold
    /// hysteresis. <see cref="Steer"/> returns a heading and does not mutate any state — the caller
    /// (the <c>Simulation</c>, T07) applies the result.
    /// </summary>
    public static class BoundsReturn
    {
        /// <summary>
        /// True when <paramref name="position"/> (XZ; Y ignored) is inside the rectangle inset by
        /// <see cref="FieldBounds.InnerMargin"/>.
        /// </summary>
        public static bool IsWithinInner(in FieldBounds bounds, Vector3 position)
        {
            float innerX = bounds.HalfExtents.x - bounds.InnerMargin;
            float innerZ = bounds.HalfExtents.y - bounds.InnerMargin;
            return Mathf.Abs(position.x - bounds.Center.x) <= innerX
                && Mathf.Abs(position.z - bounds.Center.z) <= innerZ;
        }

        /// <summary>
        /// Returns <paramref name="heading"/> unchanged while within the inner rectangle; otherwise a
        /// unit XZ heading from <paramref name="position"/> toward the field centre (Y = 0).
        /// </summary>
        public static Vector3 Steer(in FieldBounds bounds, Vector3 position, Vector3 heading)
        {
            if (IsWithinInner(in bounds, position))
            {
                return heading;
            }

            Vector3 toCenter = new Vector3(bounds.Center.x - position.x, 0f, bounds.Center.z - position.z);
            return toCenter.normalized;
        }
    }
}
