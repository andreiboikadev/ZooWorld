#nullable enable

using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Core;

namespace ZooWorld.Config
{
    /// <summary>
    /// Abstract, STATELESS movement strategy (the Strategy pattern), authored as a shared
    /// <see cref="ScriptableObject"/> asset many definitions can reference. It holds no per-animal
    /// state: mutable state arrives as <c>ref</c> <see cref="MovementState"/>, per-species constants
    /// as <c>in</c> <see cref="MovementTuning"/>. Concretes (Wander/Jump/Linear) land in T02.
    /// </summary>
    public abstract class MovementBehaviour : ScriptableObject
    {
        /// <summary>
        /// Advance <paramref name="state"/> and return the desired XZ velocity (m/s) for this tick.
        /// Pure logic — must not touch Unity statics (<c>Time</c>/<c>Random</c>/<c>Physics</c>);
        /// read time and randomness from <paramref name="ctx"/>.
        /// </summary>
        /// <param name="state">Per-animal movement state, advanced in place.</param>
        /// <param name="ctx">Ambient per-tick inputs (dt, rng, bounds, clock).</param>
        /// <param name="tuning">This animal's movement constants (speed, jump, damping).</param>
        /// <returns>The desired velocity for this tick, in m/s, on the XZ plane.</returns>
        public abstract Vector3 Tick(ref MovementState state, in MoveContext ctx, in MovementTuning tuning);
    }
}
