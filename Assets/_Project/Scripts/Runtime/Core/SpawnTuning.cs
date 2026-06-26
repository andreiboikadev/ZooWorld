#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The global spawn constants the stateless <c>SpawnPlanner</c> reads — built once at composition
    /// (T08) from <c>SimConfig</c>, so the pure rule never holds the ScriptableObject. (game-design.md §9.)
    /// </summary>
    public readonly struct SpawnTuning
    {
        public SpawnTuning(float intervalMin, float intervalMax, int maxPopulation, int predatorFloor, float clearanceRadius, int maxPlacementAttempts)
        {
            IntervalMin = intervalMin;
            IntervalMax = intervalMax;
            MaxPopulation = maxPopulation;
            PredatorFloor = predatorFloor;
            ClearanceRadius = clearanceRadius;
            MaxPlacementAttempts = maxPlacementAttempts;
        }

        /// <summary>Lower bound of the random spawn interval (s).</summary>
        public float IntervalMin { get; }

        /// <summary>Upper bound of the random spawn interval (s).</summary>
        public float IntervalMax { get; }

        /// <summary>Population safeguard — the spawner pauses at this live count.</summary>
        public int MaxPopulation { get; }

        /// <summary>Minimum predators kept on screen (anti-deadlock floor).</summary>
        public int PredatorFloor { get; }

        /// <summary>Spawn clearance query-sphere radius (m); effective centre separation = this + body radius.</summary>
        public float ClearanceRadius { get; }

        /// <summary>Placement attempts before skipping a spawn tick.</summary>
        public int MaxPlacementAttempts { get; }
    }
}
