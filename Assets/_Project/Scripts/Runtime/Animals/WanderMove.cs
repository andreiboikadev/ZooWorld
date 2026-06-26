#nullable enable

using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Stateless wander strategy: roams at species <see cref="MovementTuning.Speed"/>, re-rolling its
    /// heading to a new random XZ direction every <c>[WanderRerollMin, WanderRerollMax]</c> seconds.
    /// </summary>
    [CreateAssetMenu(menuName = "Zoo World/Movement/Wander Move", fileName = "WanderMove")]
    public sealed class WanderMove : MovementBehaviour
    {
        /// <inheritdoc/>
        public override Vector3 Tick(ref MovementState state, in MoveContext ctx, in MovementTuning tuning)
        {
            if (ctx.Clock.Now >= state.NextHeadingReroll)
            {
                state.Heading = MovementHeading.RandomXz(ctx.Rng);
                state.NextHeadingReroll = ctx.Clock.Now + ctx.Rng.Range(tuning.WanderRerollMin, tuning.WanderRerollMax);
            }

            return state.Heading * tuning.Speed;
        }
    }
}
