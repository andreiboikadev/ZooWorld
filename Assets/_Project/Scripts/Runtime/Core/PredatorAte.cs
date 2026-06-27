#nullable enable

using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// The "a predator ate" event payload (guardrails §13) — raised once per resolved eat by the
    /// <c>Simulation</c>, carrying the predator's (survivor's) world position so the "Tasty!" label spawns
    /// there. Driven separately from <see cref="AnimalDied"/> (the victim's death, which the counters
    /// subscribe to) so the label lands on the predator, never the victim (GDD §6/§8).
    /// </summary>
    public readonly struct PredatorAte
    {
        public PredatorAte(Vector3 position)
        {
            Position = position;
        }

        /// <summary>The predator's world position — where the "Tasty!" label appears.</summary>
        public Vector3 Position { get; }
    }
}
