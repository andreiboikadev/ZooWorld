#nullable enable

using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Stateless jump strategy (frog/rabbit): a horizontal velocity burst every
    /// <see cref="MovementTuning.JumpInterval"/> seconds, idle between leaps, deferring a new leap during
    /// the post-collision grace window. The burst is a one-shot impulse that coasts under
    /// <c>linearDamping</c>; a <see cref="Vector3.zero"/> return means "do not drive the body this tick"
    /// (the <c>Simulation</c>, T07, must not overwrite the coasting velocity with zero).
    /// </summary>
    [CreateAssetMenu(menuName = "Zoo World/Movement/Jump Move", fileName = "JumpMove")]
    public sealed class JumpMove : MovementBehaviour
    {
        /// <inheritdoc/>
        public override Vector3 Tick(ref MovementState state, in MoveContext ctx, in MovementTuning tuning)
        {
            if (ctx.Clock.Now < state.GraceUntil)
            {
                return Vector3.zero;
            }

            if (ctx.Clock.Now >= state.NextLeapTime)
            {
                state.Heading = MovementHeading.RandomXz(ctx.Rng);
                state.NextLeapTime = ctx.Clock.Now + tuning.JumpInterval;
                return state.Heading * JumpMath.BurstSpeed(tuning.JumpDistance, tuning.LinearDamping);
            }

            return Vector3.zero;
        }
    }
}
