#nullable enable

using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// The XZ play rectangle — the camera footprint on the ground plane. A pure value type so no
    /// rule ever reads <c>Camera.main</c>; the production value is computed once at composition.
    /// </summary>
    public readonly struct FieldBounds
    {
        public FieldBounds(Vector3 center, Vector2 halfExtents, float innerMargin)
        {
            Center = center;
            HalfExtents = halfExtents;
            InnerMargin = innerMargin;
        }

        /// <summary>World-space centre of the rectangle (Y is the ground plane).</summary>
        public Vector3 Center { get; }

        /// <summary>Half-width (X→<c>.x</c>) and half-depth (Z→<c>.y</c>) in metres.</summary>
        public Vector2 HalfExtents { get; }

        /// <summary>Inner hysteresis margin (m) — bounds-return engages past this inset edge.</summary>
        public float InnerMargin { get; }

        /// <summary>True if <paramref name="position"/> is inside the rectangle on XZ (Y ignored).</summary>
        public bool Contains(Vector3 position)
        {
            return Mathf.Abs(position.x - Center.x) <= HalfExtents.x
                && Mathf.Abs(position.z - Center.z) <= HalfExtents.y;
        }

        /// <summary>
        /// The nearest point inside the rectangle to <paramref name="position"/> on XZ (each axis
        /// clamped to the bounds); Y is set to the field's <see cref="Center"/> Y.
        /// </summary>
        public Vector3 Nearest(Vector3 position)
        {
            float x = Mathf.Clamp(position.x, Center.x - HalfExtents.x, Center.x + HalfExtents.x);
            float z = Mathf.Clamp(position.z, Center.z - HalfExtents.y, Center.z + HalfExtents.y);
            return new Vector3(x, Center.y, z);
        }
    }
}
