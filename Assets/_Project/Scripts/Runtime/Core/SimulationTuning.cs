#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The two §9 combat constants the <c>Simulation</c> needs, passed as plain values rather than the
    /// <c>SimConfig</c> SO so the tick owner is constructable in headless EditMode tests (mirrors T04's
    /// <see cref="SpawnTuning"/>). Built at composition (T08) from <c>SimConfig</c>.
    /// </summary>
    public readonly struct SimulationTuning
    {
        public SimulationTuning(float bounceKick, float graceSeconds)
        {
            BounceKick = bounceKick;
            GraceSeconds = graceSeconds;
        }

        /// <summary>Prey×prey separation impulse magnitude (m/s).</summary>
        public float BounceKick { get; }

        /// <summary>Post-collision control grace window (s).</summary>
        public float GraceSeconds { get; }
    }
}
