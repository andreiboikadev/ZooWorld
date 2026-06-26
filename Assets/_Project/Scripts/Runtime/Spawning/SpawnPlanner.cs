#nullable enable

using System.Collections.Generic;
using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Spawning
{
    /// <summary>
    /// Pure, deterministic per-tick spawn decisions: the random interval, the weighted species pick
    /// (with the anti-deadlock predator floor), the population cap, and in-bounds placement via an
    /// <see cref="IOccupancyQuery"/> — all over plain structs/seams, no Unity statics and no physics.
    /// The T08 spawner loop awaits/applies these; it owns no spawn formulas.
    /// </summary>
    /// <remarks>
    /// Stateless apart from its two immutable inputs (the per-catalog <see cref="SpeciesWeight"/> list,
    /// in catalog order, and the <see cref="SpawnTuning"/>); registered as a DI singleton (guardrails
    /// §9). <see cref="SelectSpeciesIndex"/> returns the index into that list (= the catalog index).
    /// Precondition: the list contains at least one <see cref="Role.Predator"/> entry — the predator
    /// floor is not guarded against a prey-only list.
    /// </remarks>
    public sealed class SpawnPlanner
    {
        private readonly IReadOnlyList<SpeciesWeight> _species;
        private readonly SpawnTuning _tuning;

        public SpawnPlanner(IReadOnlyList<SpeciesWeight> species, in SpawnTuning tuning)
        {
            _species = species;
            _tuning = tuning;
        }

        /// <summary>
        /// The next random spawn interval (s), uniform in
        /// [<see cref="SpawnTuning.IntervalMin"/>, <see cref="SpawnTuning.IntervalMax"/>).
        /// </summary>
        public float NextInterval(IRandom rng)
        {
            return rng.Range(_tuning.IntervalMin, _tuning.IntervalMax);
        }

        /// <summary>
        /// The species to spawn this tick (index into the catalog-order list), or <c>null</c> to pause.
        /// Predator-floor first (forces a predator even at/above the cap — anti-deadlock, no eviction),
        /// then the cap pause, then a weighted pick over all species.
        /// </summary>
        public int? SelectSpeciesIndex(in PopulationSnapshot pop, IRandom rng)
        {
            // Predator-floor overrides the cap (GDD §5 anti-deadlock; the planner never evicts).
            if (pop.PredatorCount < _tuning.PredatorFloor)
            {
                return WeightedPick(predatorsOnly: true, rng);
            }

            if (pop.Total >= _tuning.MaxPopulation)
            {
                return null;
            }

            return WeightedPick(predatorsOnly: false, rng);
        }

        /// <summary>
        /// Finds a clear in-bounds spawn position: up to <see cref="SpawnTuning.MaxPlacementAttempts"/>
        /// uniform-random candidates in <paramref name="bounds"/> (XZ; Y = the field's centre), the
        /// first <see cref="IOccupancyQuery.IsClear"/> one winning. Returns <c>false</c> (skip the tick)
        /// if none is clear.
        /// </summary>
        public bool TryFindSpawnPosition(in FieldBounds bounds, IOccupancyQuery occupancy, IRandom rng, out Vector3 position)
        {
            for (int attempt = 0; attempt < _tuning.MaxPlacementAttempts; attempt++)
            {
                float x = rng.Range(bounds.Center.x - bounds.HalfExtents.x, bounds.Center.x + bounds.HalfExtents.x);
                float z = rng.Range(bounds.Center.z - bounds.HalfExtents.y, bounds.Center.z + bounds.HalfExtents.y);
                Vector3 candidate = new Vector3(x, bounds.Center.y, z);
                if (occupancy.IsClear(candidate, _tuning.ClearanceRadius))
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// Weighted-random index over a candidate set (all species, or predators only). One
        /// <see cref="IRandom.Value01"/> draw via <c>Range(0, total)</c>, then a cumulative walk;
        /// the terminal clause returns the last candidate's index if the draw lands at the top.
        /// </summary>
        private int WeightedPick(bool predatorsOnly, IRandom rng)
        {
            float total = 0f;
            for (int i = 0; i < _species.Count; i++)
            {
                if (!predatorsOnly || _species[i].Role == Role.Predator)
                {
                    total += _species[i].Weight;
                }
            }

            float r = rng.Range(0f, total);
            float cumulative = 0f;
            int lastIndex = -1;
            for (int i = 0; i < _species.Count; i++)
            {
                if (predatorsOnly && _species[i].Role != Role.Predator)
                {
                    continue;
                }

                lastIndex = i;
                cumulative += _species[i].Weight;
                if (r < cumulative)
                {
                    return i;
                }
            }

            return lastIndex;
        }
    }
}
