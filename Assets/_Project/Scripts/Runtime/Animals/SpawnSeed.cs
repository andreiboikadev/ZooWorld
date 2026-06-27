#nullable enable

using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Pure spawn-time seeding of a freshly reset <see cref="MovementState"/>: a non-zero unit heading
    /// (<see cref="LinearMove"/> has no self-heal — an unseeded snake would freeze) and the leap /
    /// wander-reroll clocks pushed into the future so a just-spawned animal never leaps or re-rolls on
    /// frame 1 (GDD §11). The factory's <c>OnSpawn</c> is clockless, so the <c>Simulation</c> seeds here at
    /// <c>Register</c>, where it owns the <see cref="IClock"/>/<see cref="IRandom"/> seams.
    /// </summary>
    public static class SpawnSeed
    {
        /// <summary>
        /// Seeds <paramref name="state"/> from the clock + rng. Draw order: the heading angle first, then the
        /// wander-reroll interval (two <c>Value01</c> draws). <see cref="MovementState.GraceUntil"/> is left
        /// at its reset value (0).
        /// </summary>
        public static void Apply(ref MovementState state, IClock clock, IRandom rng, in MovementTuning tuning)
        {
            state.Heading = MovementHeading.RandomXz(rng);
            state.NextLeapTime = clock.Now + tuning.JumpInterval;
            state.NextHeadingReroll = clock.Now + rng.Range(tuning.WanderRerollMin, tuning.WanderRerollMax);
            state.LeapStartTime = float.NegativeInfinity;
        }
    }
}
