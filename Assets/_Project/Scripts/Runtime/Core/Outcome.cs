#nullable enable

using UnityEngine;
using ZooWorld.Config;

namespace ZooWorld.Core
{
    /// <summary>
    /// The resolved result of one collision pair, returned by the food-chain resolver (T03). It is
    /// exactly one of: <see cref="OutcomeKind.None"/> (e.g. either side already dead),
    /// <see cref="OutcomeKind.Bounce"/> (prey×prey — both live, carries a separation normal), or
    /// <see cref="OutcomeKind.Death"/> (carries the victim's seq/role/position and the "Tasty!" flag).
    /// </summary>
    /// <remarks>
    /// First-cut shape for T01. Resolved in T03: the <c>FoodChainResolver</c> fills only the logical
    /// fields (<see cref="Kind"/>/<see cref="DeadSeq"/>/<see cref="VictimRole"/>/<see cref="RaiseTasty"/>)
    /// and leaves <see cref="Position"/> and <see cref="BounceNormal"/> at <see cref="Vector3.zero"/>; the
    /// <c>Simulation</c> (T07) sources the spatial data (victim/predator positions, contact normal) from
    /// the live bodies at drain time (the resolver's <see cref="AnimalState"/> input carries no position).
    /// </remarks>
    public readonly struct Outcome
    {
        private Outcome(OutcomeKind kind, long deadSeq, Role victimRole, Vector3 position, bool raiseTasty, Vector3 bounceNormal)
        {
            Kind = kind;
            DeadSeq = deadSeq;
            VictimRole = victimRole;
            Position = position;
            RaiseTasty = raiseTasty;
            BounceNormal = bounceNormal;
        }

        /// <summary>Which kind of result this is.</summary>
        public OutcomeKind Kind { get; }

        /// <summary>Spawn id of the animal that died (<see cref="OutcomeKind.Death"/> only).</summary>
        public long DeadSeq { get; }

        /// <summary>Role of the victim — selects which death counter increments.</summary>
        public Role VictimRole { get; }

        /// <summary>World position of the death/eat (for the "Tasty!" label).</summary>
        public Vector3 Position { get; }

        /// <summary>True when a predator ate — the "Tasty!" label is raised.</summary>
        public bool RaiseTasty { get; }

        /// <summary>Separation direction for a prey×prey <see cref="OutcomeKind.Bounce"/>.</summary>
        public Vector3 BounceNormal { get; }

        /// <summary>No interaction (e.g. either side was already dead this step).</summary>
        public static Outcome None { get; } = new Outcome(OutcomeKind.None, 0L, Role.Prey, Vector3.zero, false, Vector3.zero);

        /// <summary>A prey×prey bounce along <paramref name="normal"/> — both animals live.</summary>
        public static Outcome Bounce(Vector3 normal)
        {
            return new Outcome(OutcomeKind.Bounce, 0L, Role.Prey, Vector3.zero, false, normal);
        }

        /// <summary>
        /// A death: animal <paramref name="deadSeq"/> (role <paramref name="victimRole"/>) at
        /// <paramref name="position"/>; <paramref name="raiseTasty"/> is true when a predator ate.
        /// </summary>
        public static Outcome Death(long deadSeq, Role victimRole, Vector3 position, bool raiseTasty)
        {
            return new Outcome(OutcomeKind.Death, deadSeq, victimRole, position, raiseTasty, Vector3.zero);
        }
    }
}
