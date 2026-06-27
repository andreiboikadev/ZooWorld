#nullable enable

using UnityEngine;

namespace ZooWorld.UI
{
    /// <summary>
    /// A pooled translucent "death puff" sphere spawned at a victim's position (T09 — the second
    /// <c>AnimalDied</c> subscriber). Driven by the UniTask feedback lerp via <see cref="ApplyProgress"/>:
    /// it scales up and fades out, then returns to the pool. Alpha is set via a shared
    /// <see cref="MaterialPropertyBlock"/> on a transparent URP material (never <c>renderer.material</c> —
    /// guardrails §12 / SRP). No <c>Update</c> (UniTask-driven — guardrails §9).
    /// </summary>
    public sealed class DeathPuff : MonoBehaviour
    {
        private const float StartScale = 0.5f;
        private const float EndScale = 1.5f;

        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock? s_block;

        [SerializeField] private MeshRenderer _renderer = null!;
        [SerializeField] private Color _tint = Color.white;

        /// <summary>Places the puff at <paramref name="worldPosition"/> and resets it for a fresh show (pool take).</summary>
        public void Show(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            ApplyProgress(0f);
        }

        /// <summary>Lerp sink (0 → 1): scales up and fades out (alpha via the shared property block).</summary>
        public void ApplyProgress(float t01)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(StartScale, EndScale, t01);

            s_block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(s_block);
            Color c = _tint;
            c.a = 1f - t01;
            s_block.SetColor(s_baseColorId, c);
            _renderer.SetPropertyBlock(s_block);
        }
    }
}
