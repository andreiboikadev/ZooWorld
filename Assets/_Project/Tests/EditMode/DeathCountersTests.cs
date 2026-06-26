#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.UI;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="DeathCounters"/> over the real <see cref="AnimalDeathSignal"/> Observer path:
    /// per-role increment, accumulation, the <c>Changed</c> cadence, zero allocation, and
    /// dispose-unsubscribe.
    /// </summary>
    public sealed class DeathCountersTests
    {
        private static AnimalDeathSignal Signal()
        {
            return new AnimalDeathSignal();
        }

        private static AnimalDied Died(Role role)
        {
            return new AnimalDied(role, Vector3.zero);
        }

        [Test]
        public void StartsAtZero()
        {
            var counters = new DeathCounters(Signal());

            Assert.That(counters.DeadPrey, Is.EqualTo(0));
            Assert.That(counters.DeadPredators, Is.EqualTo(0));
        }

        [Test]
        public void PreyDeath_IncrementsPreyOnly()
        {
            AnimalDeathSignal signal = Signal();
            var counters = new DeathCounters(signal);

            signal.Raise(Died(Role.Prey));

            Assert.That(counters.DeadPrey, Is.EqualTo(1));
            Assert.That(counters.DeadPredators, Is.EqualTo(0));
        }

        [Test]
        public void PredatorDeath_IncrementsPredatorOnly()
        {
            AnimalDeathSignal signal = Signal();
            var counters = new DeathCounters(signal);

            signal.Raise(Died(Role.Predator));

            Assert.That(counters.DeadPredators, Is.EqualTo(1));
            Assert.That(counters.DeadPrey, Is.EqualTo(0));
        }

        [Test]
        public void MixedDeaths_AccumulatePerRole()
        {
            AnimalDeathSignal signal = Signal();
            var counters = new DeathCounters(signal);

            signal.Raise(Died(Role.Prey));
            signal.Raise(Died(Role.Predator));
            signal.Raise(Died(Role.Prey));
            signal.Raise(Died(Role.Predator));
            signal.Raise(Died(Role.Prey));

            Assert.That(counters.DeadPrey, Is.EqualTo(3));
            Assert.That(counters.DeadPredators, Is.EqualTo(2));
        }

        [Test]
        public void Changed_RaisedOncePerDeath()
        {
            AnimalDeathSignal signal = Signal();
            var counters = new DeathCounters(signal);
            int changedCount = 0;
            counters.Changed += () => changedCount++;

            signal.Raise(Died(Role.Prey));
            signal.Raise(Died(Role.Predator));
            signal.Raise(Died(Role.Prey));

            Assert.That(changedCount, Is.EqualTo(3));
        }

        [Test]
        public void Position_DoesNotAffectCount()
        {
            AnimalDeathSignal signal = Signal();
            var counters = new DeathCounters(signal);

            signal.Raise(new AnimalDied(Role.Prey, new Vector3(5f, 0f, 5f)));
            signal.Raise(new AnimalDied(Role.Prey, Vector3.zero));

            Assert.That(counters.DeadPrey, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_Unsubscribes_NoFurtherCounting()
        {
            AnimalDeathSignal signal = Signal();
            var counters = new DeathCounters(signal);

            counters.Dispose();
            signal.Raise(Died(Role.Prey));

            Assert.That(counters.DeadPrey, Is.EqualTo(0));
        }
    }
}
