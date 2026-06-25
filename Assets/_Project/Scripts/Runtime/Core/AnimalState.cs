#nullable enable

using ZooWorld.Config;

namespace ZooWorld.Core
{
    /// <summary>
    /// The food-chain resolver's per-animal input — rebuilt from the live <c>Animal</c> at drain
    /// time so the <see cref="Dead"/> guard reads current state, not a stale enqueue-time value.
    /// </summary>
    public readonly struct AnimalState
    {
        public AnimalState(Role role, int strength, long seq, bool dead)
        {
            Role = role;
            Strength = strength;
            Seq = seq;
            Dead = dead;
        }

        /// <summary>Predation role.</summary>
        public Role Role { get; }

        /// <summary>Predator-vs-predator winner key (higher wins).</summary>
        public int Strength { get; }

        /// <summary>Monotonic spawn id — breaks strength ties (lower seq survives).</summary>
        public long Seq { get; }

        /// <summary>True once already resolved dead this step (idempotency guard).</summary>
        public bool Dead { get; }
    }
}
