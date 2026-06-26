#nullable enable

using NUnit.Framework;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.Predation;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="FoodChainResolver"/> over hand-built <see cref="AnimalState"/> pairs: every
    /// matrix cell, the strength/seq duel tiebreak, order-independence, and dead-guard idempotency.
    /// Spatial fields (<c>Position</c>/<c>BounceNormal</c>) are the Simulation's (T07), not asserted here.
    /// </summary>
    public sealed class FoodChainResolverTests
    {
        private readonly FoodChainResolver _resolver = new();

        private static AnimalState Prey(long seq, bool dead = false)
        {
            return new AnimalState(Role.Prey, 0, seq, dead);
        }

        private static AnimalState Pred(int strength, long seq, bool dead = false)
        {
            return new AnimalState(Role.Predator, strength, seq, dead);
        }

        [Test]
        public void PreyVsPrey_Bounces_NoDeath_NoTasty()
        {
            Outcome outcome = _resolver.Resolve(Prey(1), Prey(2));

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Bounce));
            Assert.That(outcome.RaiseTasty, Is.False);
        }

        [Test]
        public void PreyVsPredator_PreyDies_Tasty()
        {
            Outcome outcome = _resolver.Resolve(Prey(1), Pred(5, 2));

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(outcome.DeadSeq, Is.EqualTo(1L));
            Assert.That(outcome.VictimRole, Is.EqualTo(Role.Prey));
            Assert.That(outcome.RaiseTasty, Is.True);
        }

        [Test]
        public void PredatorVsPrey_PreyDies_OrderIndependent()
        {
            Outcome outcome = _resolver.Resolve(Pred(5, 1), Prey(2));

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(outcome.DeadSeq, Is.EqualTo(2L));
            Assert.That(outcome.VictimRole, Is.EqualTo(Role.Prey));
        }

        [Test]
        public void Duel_HigherStrengthSurvives_LoserDies_Tasty()
        {
            Outcome outcome = _resolver.Resolve(Pred(5, 1), Pred(3, 2));

            Assert.That(outcome.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(outcome.DeadSeq, Is.EqualTo(2L));
            Assert.That(outcome.VictimRole, Is.EqualTo(Role.Predator));
            Assert.That(outcome.RaiseTasty, Is.True);
        }

        [Test]
        public void Duel_StrengthOutranksSeq()
        {
            Outcome outcome = _resolver.Resolve(Pred(3, 1), Pred(5, 2));

            Assert.That(outcome.DeadSeq, Is.EqualTo(1L));
        }

        [Test]
        public void Duel_StrengthTie_LowerSeqSurvives()
        {
            Outcome outcome = _resolver.Resolve(Pred(5, 1), Pred(5, 2));

            Assert.That(outcome.DeadSeq, Is.EqualTo(2L));
            Assert.That(outcome.VictimRole, Is.EqualTo(Role.Predator));
        }

        [Test]
        public void Duel_OrderIndependent()
        {
            Assert.That(
                _resolver.Resolve(Pred(5, 1), Pred(3, 2)).DeadSeq,
                Is.EqualTo(_resolver.Resolve(Pred(3, 2), Pred(5, 1)).DeadSeq));
            Assert.That(
                _resolver.Resolve(Pred(5, 1), Pred(5, 2)).DeadSeq,
                Is.EqualTo(_resolver.Resolve(Pred(5, 2), Pred(5, 1)).DeadSeq));
        }

        [Test]
        public void EitherAlreadyDead_ReturnsNone()
        {
            Outcome deadDuel = _resolver.Resolve(Pred(5, 1, dead: true), Pred(3, 2));
            Assert.That(deadDuel.Kind, Is.EqualTo(OutcomeKind.None));
            Assert.That(deadDuel.RaiseTasty, Is.False);

            // The guard precedes the prey branch too — not only the Pred×Pred arm.
            Assert.That(_resolver.Resolve(Prey(1, dead: true), Pred(5, 2)).Kind, Is.EqualTo(OutcomeKind.None));
            Assert.That(_resolver.Resolve(Pred(5, 2), Prey(1, dead: true)).Kind, Is.EqualTo(OutcomeKind.None));
        }

        [Test]
        public void Death_ExactlyOneVictim_NeverBothDie()
        {
            // One Outcome carries a single DeadSeq — the loser — so a death can never kill both.
            Outcome eat = _resolver.Resolve(Prey(1), Pred(5, 2));
            Assert.That(eat.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(eat.DeadSeq, Is.EqualTo(1L));

            Outcome duel = _resolver.Resolve(Pred(5, 1), Pred(3, 2));
            Assert.That(duel.Kind, Is.EqualTo(OutcomeKind.Death));
            Assert.That(duel.DeadSeq, Is.EqualTo(2L));
        }
    }
}
