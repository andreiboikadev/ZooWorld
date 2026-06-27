#nullable enable

using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Spawning
{
    /// <summary>
    /// Production <see cref="IOccupancyQuery"/> over <see cref="Physics"/> — the spawn planner's
    /// spatial-clearance seam in the running sim, abstracting <c>Physics.CheckSphere</c> on the Animal
    /// layer. A DI singleton (T08); the layer mask is injected at composition, never hard-coded.
    /// </summary>
    public sealed class PhysicsOccupancyQuery : IOccupancyQuery
    {
        private readonly LayerMask _animalMask;

        /// <summary>Creates the query against <paramref name="animalMask"/> (the Animal physics layer).</summary>
        public PhysicsOccupancyQuery(LayerMask animalMask)
        {
            _animalMask = animalMask;
        }

        /// <inheritdoc/>
        public bool IsClear(Vector3 position, float radius)
        {
            return !Physics.CheckSphere(position, radius, _animalMask, QueryTriggerInteraction.Ignore);
        }
    }
}
