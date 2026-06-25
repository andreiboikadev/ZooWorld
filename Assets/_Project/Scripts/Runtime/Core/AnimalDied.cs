#nullable enable

using UnityEngine;
using ZooWorld.Config;

namespace ZooWorld.Core
{
    /// <summary>
    /// The single death-event payload (guardrails §13) — raised once per resolved death by the
    /// <c>Simulation</c>. The death counters subscribe to this; the "Tasty!" label is driven
    /// separately, so a bounce never counts and a predator duel counts exactly once.
    /// </summary>
    public readonly struct AnimalDied
    {
        public AnimalDied(Role role, Vector3 position)
        {
            Role = role;
            Position = position;
        }

        /// <summary>The victim's role — selects which counter increments.</summary>
        public Role Role { get; }

        /// <summary>Where it died, in world space.</summary>
        public Vector3 Position { get; }
    }
}
