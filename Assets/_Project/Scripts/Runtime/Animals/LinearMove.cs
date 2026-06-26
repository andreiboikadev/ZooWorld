#nullable enable

using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Stateless linear strategy (snake): constant <see cref="MovementTuning.Speed"/> along a fixed
    /// heading that is seeded at spawn and redirected only by bounds-return — no periodic re-roll.
    /// Relies on a non-zero spawned <see cref="MovementState.Heading"/> (no self-heal): a zero heading
    /// yields zero velocity (a frozen body). See the spawn-seed obligation in the T02 brief.
    /// </summary>
    [CreateAssetMenu(menuName = "Zoo World/Movement/Linear Move", fileName = "LinearMove")]
    public sealed class LinearMove : MovementBehaviour
    {
        /// <inheritdoc/>
        public override Vector3 Tick(ref MovementState state, in MoveContext ctx, in MovementTuning tuning)
        {
            return state.Heading * tuning.Speed;
        }
    }
}
