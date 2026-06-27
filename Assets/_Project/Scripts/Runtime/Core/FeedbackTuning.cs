#nullable enable

using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// The feedback constants the <c>Simulation</c> samples in the tick (game-design.md §9, T09), passed as
    /// plain values + shared curve refs rather than the <c>SimConfig</c> SO so the tick owner stays
    /// constructable in headless EditMode tests (mirrors T04's <see cref="SpawnTuning"/>). Built at
    /// composition from <c>SimConfig</c>. Only the in-tick / animal-lifetime visuals live here — the
    /// "Tasty!" label + death-puff are Observer subscribers that read <c>SimConfig</c> directly.
    /// </summary>
    public readonly struct FeedbackTuning
    {
        public FeedbackTuning(float jumpArcHeight, float jumpArcDuration, AnimationCurve jumpArc,
            float spawnPopDuration, AnimationCurve popEase)
        {
            JumpArcHeight = jumpArcHeight;
            JumpArcDuration = jumpArcDuration;
            JumpArc = jumpArc;
            SpawnPopDuration = spawnPopDuration;
            PopEase = popEase;
        }

        /// <summary>Jump-arc peak height (m).</summary>
        public float JumpArcHeight { get; }

        /// <summary>Jump-arc duration (s) — cosmetic, independent of the physics coast.</summary>
        public float JumpArcDuration { get; }

        /// <summary>Jump-arc shape (a hump 0 → 1 → 0), sampled by <c>JumpArc</c>.</summary>
        public AnimationCurve JumpArc { get; }

        /// <summary>Spawn scale-in duration (s).</summary>
        public float SpawnPopDuration { get; }

        /// <summary>Shared 0 → 1 ease for the spawn-pop.</summary>
        public AnimationCurve PopEase { get; }
    }
}
