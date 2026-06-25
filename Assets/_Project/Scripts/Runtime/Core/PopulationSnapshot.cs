#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// A point-in-time population summary the spawn planner reads to apply the cap and the predator
    /// floor — hand-built in tests, built from the active animal list in production.
    /// </summary>
    public readonly struct PopulationSnapshot
    {
        public PopulationSnapshot(int total, int predatorCount)
        {
            Total = total;
            PredatorCount = predatorCount;
        }

        /// <summary>Total live animals.</summary>
        public int Total { get; }

        /// <summary>Live predators (drives the predator-floor boost).</summary>
        public int PredatorCount { get; }
    }
}
