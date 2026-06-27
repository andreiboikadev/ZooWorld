#nullable enable

using UnityEngine;
using ZooWorld.Config;

namespace ZooWorld.Core
{
    /// <summary>
    /// The complete per-species runtime spawn spec the <c>AnimalFactory</c> consumes — built once from an
    /// <c>AnimalDefinition</c> + <c>SimConfig</c> at composition (T08), in catalog order, so the pooling
    /// layer is testable over plain values (the SOs have no test setters, mirroring T04's
    /// <see cref="SpeciesWeight"/>). <see cref="Movement"/> may be null in T06 (the movement tick is T07).
    /// </summary>
    public readonly struct AnimalSpec
    {
        public AnimalSpec(Role role, int strength, float size, float mass, Color color, float spawnWeight,
            MovementBehaviour? movement, in MovementTuning tuning)
        {
            Role = role;
            Strength = strength;
            Size = size;
            Mass = mass;
            Color = color;
            SpawnWeight = spawnWeight;
            Movement = movement;
            Tuning = tuning;
        }

        /// <summary>Predation role.</summary>
        public Role Role { get; }

        /// <summary>Predator-vs-predator winner key (higher wins).</summary>
        public int Strength { get; }

        /// <summary>Body diameter (m) — applied as the spawn transform scale.</summary>
        public float Size { get; }

        /// <summary>Rigidbody mass (kg).</summary>
        public float Mass { get; }

        /// <summary>Body colour, applied via <c>MaterialPropertyBlock</c> at spawn (visual only, T09).</summary>
        public Color Color { get; }

        /// <summary>Relative weight in the spawn lottery — drives the pool prewarm count.</summary>
        public float SpawnWeight { get; }

        /// <summary>The movement strategy (null until the tick lands in T07).</summary>
        public MovementBehaviour? Movement { get; }

        /// <summary>Per-animal movement constants the strategy reads (built with the SimConfig globals).</summary>
        public MovementTuning Tuning { get; }
    }
}
