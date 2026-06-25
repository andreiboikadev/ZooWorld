#nullable enable

using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// Production <see cref="IClock"/> over <see cref="Time"/> — the fixed-step timing source for
    /// the running simulation.
    /// </summary>
    public sealed class UnityClock : IClock
    {
        /// <inheritdoc/>
        public float Now => Time.time;

        /// <inheritdoc/>
        public float Dt => Time.fixedDeltaTime;
    }
}
