#nullable enable

using UnityEngine;

namespace ZooWorld.Animals
{
    /// <summary>
    /// The pure visual jump-arc sample (T09): given the current clock time and the leap's start time,
    /// returns the child-mesh local Y offset for the hop, sampled from an <see cref="AnimationCurve"/> and
    /// scaled by a peak height. Cosmetic only — decoupled from the physics coast (guardrails §8). Pure
    /// (no Unity statics; <see cref="AnimationCurve.Evaluate(float)"/> is headless-safe), so the
    /// <c>Simulation</c> samples it in the tick and the EditMode test pins the closed form (like
    /// <see cref="JumpMath"/>).
    /// </summary>
    public static class JumpArc
    {
        /// <summary>
        /// The hop height at <paramref name="now"/> for a leap that began at <paramref name="leapStart"/>.
        /// Returns 0 while grounded (before the leap — <c>now &lt; leapStart</c> — or after it lands —
        /// <c>t &gt;= 1</c>) and for a non-positive <paramref name="duration"/>; otherwise
        /// <c>curve.Evaluate(t) * height</c> where <c>t = (now - leapStart) / duration</c>.
        /// </summary>
        /// <param name="now">Current clock time (s).</param>
        /// <param name="leapStart">Clock time the current leap began (s); <see cref="float.NegativeInfinity"/> = grounded.</param>
        /// <param name="duration">Cosmetic hop duration (s).</param>
        /// <param name="height">Peak hop height (m).</param>
        /// <param name="curve">The hop shape (a hump: 0 → 1 → 0).</param>
        /// <returns>The child-mesh local Y offset (m).</returns>
        public static float Height(float now, float leapStart, float duration, float height, AnimationCurve curve)
        {
            if (duration <= 0f || now < leapStart)
            {
                return 0f;
            }

            float t = (now - leapStart) / duration;
            if (t >= 1f)
            {
                return 0f;
            }

            return curve.Evaluate(t) * height;
        }
    }
}
