#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// The small UniTask lerp helper for feedback animations (guardrails §9; ADR 0002 §7 — DOTween was
    /// dropped). Drives a 0 → 1 progress over <paramref name="duration"/> real seconds, eased by an
    /// <see cref="AnimationCurve"/>, calling <paramref name="apply"/> each frame and a final
    /// <c>apply(1f)</c>. Tied to the caller's <see cref="CancellationToken"/> — a despawn/teardown cancel
    /// stops it cleanly (no per-tween bookkeeping, no stale-animation-on-reuse). <paramref name="apply"/> is
    /// a method group at every call site (no per-frame closure; the per-event spike path, guardrails §13).
    /// </summary>
    public static class FeedbackLerp
    {
        /// <summary>
        /// Eases <paramref name="apply"/> from 0 to 1 over <paramref name="duration"/> seconds, then settles
        /// it at 1. A cancel (despawn/teardown) returns early without throwing and without the final settle.
        /// </summary>
        /// <param name="duration">Animation length (s). Non-positive ⇒ a single <c>apply(1f)</c>.</param>
        /// <param name="ease">The 0 → 1 ease curve.</param>
        /// <param name="apply">The per-frame sink (e.g. a label's rise/fade), called with the eased value.</param>
        /// <param name="ct">Cancelled on despawn/teardown — stops the lerp cleanly.</param>
        public static async UniTask RunAsync(float duration, AnimationCurve ease, Action<float> apply,
            CancellationToken ct)
        {
            if (duration <= 0f)
            {
                apply(1f);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                bool cancelled = await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
                if (cancelled)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                apply(ease.Evaluate(Mathf.Clamp01(elapsed / duration)));
            }

            apply(1f);
        }
    }
}
