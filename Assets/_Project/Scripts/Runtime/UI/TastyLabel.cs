#nullable enable

using UnityEngine;

namespace ZooWorld.UI
{
    /// <summary>
    /// A pooled world-space "Tasty!" label (GDD §8) — lives under a static effects root (not parented to the
    /// poolable predator) and is driven by the UniTask feedback lerp via <see cref="ApplyProgress"/>: it
    /// rises and fades over its lifetime, then returns to the pool. No <c>Update</c> (only the Simulation
    /// ticks; this is UniTask-driven — guardrails §9). Under T08's locked straight-down camera the billboard
    /// is a constant rotation set on take, so there is no per-frame <c>Camera.main</c> (guardrails §12).
    /// </summary>
    public sealed class TastyLabel : MonoBehaviour
    {
        private const float RiseDistance = 1f;

        // Lie flat on the XZ plane, readable from the straight-down camera (decision 4).
        private static readonly Quaternion s_facing = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] private TextMesh _text = null!;

        private float _baseY;
        private Color _color = Color.white;

        /// <summary>Places the label at <paramref name="worldPosition"/> and resets it for a fresh show (pool take).</summary>
        public void Show(Vector3 worldPosition)
        {
            transform.SetPositionAndRotation(worldPosition, s_facing);
            _baseY = worldPosition.y;
            _color = _text.color;
            ApplyProgress(0f);
        }

        /// <summary>Lerp sink (0 → 1): rises by <see cref="RiseDistance"/> and fades the text out.</summary>
        public void ApplyProgress(float t01)
        {
            Vector3 p = transform.position;
            p.y = _baseY + (RiseDistance * t01);
            transform.position = p;

            _color.a = 1f - t01;
            _text.color = _color;
        }
    }
}
