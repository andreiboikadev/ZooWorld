#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="Outcome"/> factories model exactly one of None / Bounce / Death and set
    /// the right fields. A regression anchor for the type that carries the T03 position carry-forward.
    /// </summary>
    public sealed class OutcomeTests
    {
        [Test]
        public void None_IsKindNone_NoTasty()
        {
            Outcome outcome = Outcome.None;

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.None));
            Assert.That(outcome.RaiseTasty, Is.False);
        }

        [Test]
        public void Bounce_IsKindBounce_CarriesNormal_NoDeath()
        {
            Vector3 normal = new Vector3(1f, 0f, 0f);

            Outcome outcome = Outcome.Bounce(normal);

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Bounce));
            Assert.That(outcome.BounceNormal, Is.EqualTo(normal));
            Assert.That(outcome.RaiseTasty, Is.False);
        }

        [Test]
        public void Death_CarriesVictimSeqRolePosition_AndTastyFlag()
        {
            Vector3 position = new Vector3(2f, 0f, -3f);

            Outcome outcome = Outcome.Death(deadSeq: 7L, victimRole: Role.Predator, position: position, raiseTasty: true);

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(outcome.DeadSeq, Is.EqualTo(7L));
            Assert.That(outcome.VictimRole, Is.EqualTo(Role.Predator));
            Assert.That(outcome.Position, Is.EqualTo(position));
            Assert.That(outcome.RaiseTasty, Is.True);
        }

        [Test]
        public void Death_WithoutTasty_DoesNotRaise()
        {
            Outcome outcome = Outcome.Death(deadSeq: 1L, victimRole: Role.Prey, position: Vector3.zero, raiseTasty: false);

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(outcome.RaiseTasty, Is.False);
        }
    }
}
