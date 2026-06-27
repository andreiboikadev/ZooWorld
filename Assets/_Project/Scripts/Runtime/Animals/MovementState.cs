#nullable enable

using UnityEngine;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Per-animal, mutable movement state — advanced <c>ref</c> by the stateless movement strategy
    /// each tick, and stored on the <c>Animal</c> (never on the shared strategy SO). T02/T07 may
    /// extend this as the strategies need; the fields here are the minimum, not a closed set.
    /// </summary>
    /// <remarks>
    /// Deliberately a mutable struct with public fields: it is the by-<c>ref</c> scratch state of a
    /// hot per-tick path, so encapsulation via properties would only cost copies. Reset on pool take.
    /// </remarks>
    public struct MovementState
    {
        /// <summary>Current XZ movement heading (unit-length where it matters).</summary>
        public Vector3 Heading;

        /// <summary>Clock time (s) at which the wander heading is next re-rolled.</summary>
        public float NextHeadingReroll;

        /// <summary>Clock time (s) at which the next leap may begin (jump strategy).</summary>
        public float NextLeapTime;

        /// <summary>
        /// Clock time (s) at which the current leap began — drives the cosmetic visual hop arc (T09).
        /// <see cref="float.NegativeInfinity"/> means grounded (no leap in progress); seeded by
        /// <c>SpawnSeed</c> and set by <c>JumpMove</c> on each burst.
        /// </summary>
        public float LeapStartTime;

        /// <summary>Clock time (s) until which strategy velocity is suppressed (post-bounce grace).</summary>
        public float GraceUntil;
    }
}
