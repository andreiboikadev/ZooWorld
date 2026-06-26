#nullable enable

using ZooWorld.Config;

namespace ZooWorld.Core
{
    /// <summary>
    /// One species' spawn-lottery input for the <c>SpawnPlanner</c> — a plain value projection of an
    /// <c>AnimalDefinition</c>'s role + weight, so the pure rule never touches the ScriptableObject.
    /// Built per catalog entry, in catalog order, at composition (T08).
    /// </summary>
    public readonly struct SpeciesWeight
    {
        public SpeciesWeight(Role role, float weight)
        {
            Role = role;
            Weight = weight;
        }

        /// <summary>Predation role — selects the predator-floor candidate set.</summary>
        public Role Role { get; }

        /// <summary>Relative weight in the spawn lottery.</summary>
        public float Weight { get; }
    }
}
