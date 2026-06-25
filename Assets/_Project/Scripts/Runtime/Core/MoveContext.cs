#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// Ambient, per-tick inputs the <c>Simulation</c> builds and passes (by <c>in</c>) to a movement
    /// strategy. Carries the seams (clock, rng) and the field bounds — never Unity statics, so the
    /// strategy stays headless-testable.
    /// </summary>
    public readonly struct MoveContext
    {
        public MoveContext(float dt, IRandom rng, in FieldBounds bounds, IClock clock)
        {
            Dt = dt;
            Rng = rng;
            Bounds = bounds;
            Clock = clock;
        }

        /// <summary>Fixed step length (s) for this tick.</summary>
        public float Dt { get; }

        /// <summary>Randomness seam (wander headings, intervals).</summary>
        public IRandom Rng { get; }

        /// <summary>The XZ play rectangle.</summary>
        public FieldBounds Bounds { get; }

        /// <summary>Time seam (grace-window comparisons).</summary>
        public IClock Clock { get; }
    }
}
