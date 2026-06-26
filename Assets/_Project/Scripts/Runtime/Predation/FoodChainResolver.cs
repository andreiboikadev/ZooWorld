#nullable enable

using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Predation
{
    /// <summary>
    /// Pure, deterministic predation rule for a single collision pair. Decides the
    /// <see cref="Outcome"/> from each animal's role, strength, spawn-sequence, and dead-guard — over
    /// plain structs, with no Unity statics and no physics. The <c>Simulation</c> (T07) drains
    /// collisions, calls this once per unordered pair, and sources the spatial data (death/eat
    /// positions, bounce normal) from the live bodies.
    /// </summary>
    /// <remarks>
    /// Binary food chain (GDD §6): prey×prey bounce (both live); a predator eats any prey; a predator
    /// duel is won by the higher <see cref="AnimalState.Strength"/> (tie → the lower
    /// <see cref="AnimalState.Seq"/> survives). Stateless — registered as a DI singleton (guardrails
    /// §9). Results are built only via the <see cref="Outcome"/> factories; <see cref="Outcome.Position"/>
    /// and <see cref="Outcome.BounceNormal"/> are left <see cref="Vector3.zero"/> for the Simulation.
    /// </remarks>
    public sealed class FoodChainResolver
    {
        /// <summary>
        /// Resolves the predation outcome for the unordered pair <paramref name="a"/>/<paramref name="b"/>.
        /// Order-independent, and idempotent under the dead-guard: if either side is already
        /// <see cref="AnimalState.Dead"/>, the result is <see cref="Outcome.None"/>.
        /// </summary>
        /// <param name="a">One colliding animal's resolver state.</param>
        /// <param name="b">The other colliding animal's resolver state.</param>
        /// <returns>
        /// <see cref="Outcome.None"/> (either already dead), <see cref="Outcome.Bounce"/> (prey×prey;
        /// both live), or <see cref="Outcome.Death"/> (a predator ate prey, or won a duel).
        /// </returns>
        public Outcome Resolve(in AnimalState a, in AnimalState b)
        {
            // Dead-guard first — before any role branch — so a re-drained pair whose member already
            // died this step yields nothing (idempotency; never both-die).
            if (a.Dead || b.Dead)
            {
                return Outcome.None;
            }

            return (a.Role, b.Role) switch
            {
                (Role.Prey, Role.Prey) => Outcome.Bounce(Vector3.zero),
                (Role.Prey, Role.Predator) => Outcome.Death(a.Seq, Role.Prey, Vector3.zero, raiseTasty: true),
                (Role.Predator, Role.Prey) => Outcome.Death(b.Seq, Role.Prey, Vector3.zero, raiseTasty: true),
                _ => ResolveDuel(in a, in b),
            };
        }

        /// <summary>
        /// Predator-vs-predator duel: the higher <see cref="AnimalState.Strength"/> survives; on a tie
        /// the lower <see cref="AnimalState.Seq"/> survives. The loser dies and the winner shows "Tasty!".
        /// </summary>
        private static Outcome ResolveDuel(in AnimalState a, in AnimalState b)
        {
            bool aSurvives = a.Strength != b.Strength
                ? a.Strength > b.Strength
                : a.Seq < b.Seq;
            long loserSeq = aSurvives ? b.Seq : a.Seq;
            return Outcome.Death(loserSeq, Role.Predator, Vector3.zero, raiseTasty: true);
        }
    }
}
