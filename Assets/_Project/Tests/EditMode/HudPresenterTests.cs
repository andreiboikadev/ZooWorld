#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.Tests.EditMode.Fakes;
using ZooWorld.UI;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The <see cref="HudPresenter"/> over the full signal → <see cref="DeathCounters"/> → presenter →
    /// <see cref="FakeHudView"/> chain: the initial "0" labels, the exact GDD §8 formatted strings,
    /// independent accumulation, and dispose-stops-updating.
    /// </summary>
    public sealed class HudPresenterTests
    {
        private static AnimalDied Died(Role role)
        {
            return new AnimalDied(role, Vector3.zero);
        }

        [Test]
        public void Start_PushesInitialZeroLabels()
        {
            var signal = new AnimalDeathSignal();
            var counters = new DeathCounters(signal);
            var view = new FakeHudView();
            var presenter = new HudPresenter(counters, view);

            presenter.Start();

            Assert.That(view.DeadPreyText, Is.EqualTo("Dead prey: 0"));
            Assert.That(view.DeadPredatorsText, Is.EqualTo("Dead predators: 0"));
        }

        [Test]
        public void PreyDeath_UpdatesPreyLabel_ExactFormat()
        {
            var signal = new AnimalDeathSignal();
            var counters = new DeathCounters(signal);
            var view = new FakeHudView();
            var presenter = new HudPresenter(counters, view);
            presenter.Start();

            signal.Raise(Died(Role.Prey));

            Assert.That(view.DeadPreyText, Is.EqualTo("Dead prey: 1"));
            Assert.That(view.DeadPredatorsText, Is.EqualTo("Dead predators: 0"));
        }

        [Test]
        public void PredatorDeath_UpdatesPredatorLabel()
        {
            var signal = new AnimalDeathSignal();
            var counters = new DeathCounters(signal);
            var view = new FakeHudView();
            var presenter = new HudPresenter(counters, view);
            presenter.Start();

            signal.Raise(Died(Role.Predator));

            Assert.That(view.DeadPredatorsText, Is.EqualTo("Dead predators: 1"));
        }

        [Test]
        public void BothLabels_AccumulateIndependently()
        {
            var signal = new AnimalDeathSignal();
            var counters = new DeathCounters(signal);
            var view = new FakeHudView();
            var presenter = new HudPresenter(counters, view);
            presenter.Start();

            signal.Raise(Died(Role.Prey));
            signal.Raise(Died(Role.Prey));
            signal.Raise(Died(Role.Predator));
            signal.Raise(Died(Role.Predator));
            signal.Raise(Died(Role.Predator));

            Assert.That(view.DeadPreyText, Is.EqualTo("Dead prey: 2"));
            Assert.That(view.DeadPredatorsText, Is.EqualTo("Dead predators: 3"));
        }

        [Test]
        public void Dispose_StopsUpdatingView()
        {
            var signal = new AnimalDeathSignal();
            var counters = new DeathCounters(signal);
            var view = new FakeHudView();
            var presenter = new HudPresenter(counters, view);
            presenter.Start();

            presenter.Dispose();
            signal.Raise(Died(Role.Prey));

            Assert.That(view.DeadPreyText, Is.EqualTo("Dead prey: 0"));
        }
    }
}
