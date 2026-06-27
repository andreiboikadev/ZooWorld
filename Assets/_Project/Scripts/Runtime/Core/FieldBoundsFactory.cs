#nullable enable

using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// Builds the production <see cref="FieldBounds"/> from a top-down camera's frustum projected onto the
    /// ground plane — a pure closed form, so the play rectangle is computed once at composition and no rule
    /// ever reads <c>Camera.main</c>. Assumes the camera looks straight down −Y; a tilted camera would
    /// footprint a trapezoid this axis-aligned rectangle cannot represent (the composition root authors the
    /// camera straight down).
    /// </summary>
    public static class FieldBoundsFactory
    {
        /// <summary>
        /// Projects a straight-down perspective camera's frustum onto the ground plane at
        /// <paramref name="groundY"/>. <paramref name="verticalFovDegrees"/> is the camera's vertical field
        /// of view (Unity's <c>Camera.fieldOfView</c>); <paramref name="aspect"/> its width/height ratio.
        /// </summary>
        /// <param name="cameraPosition">World position of the camera (its XZ becomes the rectangle centre).</param>
        /// <param name="verticalFovDegrees">Vertical field of view, degrees.</param>
        /// <param name="aspect">Viewport width / height.</param>
        /// <param name="groundY">World Y of the ground plane the frustum is projected onto.</param>
        /// <param name="innerMargin">Hysteresis inset for bounds-return (game-design.md §9; T07 invariant).</param>
        public static FieldBounds FromTopDownCamera(Vector3 cameraPosition, float verticalFovDegrees,
            float aspect, float groundY, float innerMargin)
        {
            float halfDepth = (cameraPosition.y - groundY) * Mathf.Tan(0.5f * verticalFovDegrees * Mathf.Deg2Rad);
            float halfWidth = halfDepth * aspect;
            Vector3 center = new Vector3(cameraPosition.x, groundY, cameraPosition.z);
            return new FieldBounds(center, new Vector2(halfWidth, halfDepth), innerMargin);
        }
    }
}
