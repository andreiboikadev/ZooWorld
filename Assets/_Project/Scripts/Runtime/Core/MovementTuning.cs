#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The per-animal movement constants a stateless strategy reads — built once at spawn from the
    /// animal's <c>AnimalDefinition</c> tuning plus <c>SimConfig.linearDamping</c>, and passed
    /// <c>in</c> so the shared strategy SO holds no per-species state.
    /// </summary>
    public readonly struct MovementTuning
    {
        public MovementTuning(float speed, float jumpDistance, float jumpInterval, float linearDamping)
        {
            Speed = speed;
            JumpDistance = jumpDistance;
            JumpInterval = jumpInterval;
            LinearDamping = linearDamping;
        }

        /// <summary>Cruise speed (m/s) for wander/linear movement.</summary>
        public float Speed { get; }

        /// <summary>Nominal leap distance (m) under no collision.</summary>
        public float JumpDistance { get; }

        /// <summary>Seconds between leaps.</summary>
        public float JumpInterval { get; }

        /// <summary>Linear damping shared by the live body and <c>JumpMath</c>.</summary>
        public float LinearDamping { get; }
    }
}
