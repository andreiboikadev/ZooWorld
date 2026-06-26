#nullable enable

using System;
using VContainer.Unity;

namespace ZooWorld.UI
{
    /// <summary>
    /// Formats the two death counts (<see cref="DeathCounters"/>) into the GDD §8 label strings and
    /// pushes them to the <see cref="IHudView"/> on every change — the presenter in the HUD's
    /// model→presenter→view chain. Pure (no Unity statics; not a MonoBehaviour) so it runs headless in
    /// EditMode. Implements <see cref="IStartable"/> so VContainer pushes the initial "0" labels at
    /// startup (T08); a unit test calls <see cref="Start"/> directly.
    /// </summary>
    public sealed class HudPresenter : IStartable, IDisposable
    {
        private readonly DeathCounters _counters;
        private readonly IHudView _view;

        public HudPresenter(DeathCounters counters, IHudView view)
        {
            _counters = counters;
            _view = view;
            _counters.Changed += OnCountersChanged;
        }

        /// <summary>
        /// Pushes the current counts to the view — the initial "Dead prey: 0" / "Dead predators: 0".
        /// VContainer's entry-point dispatcher invokes this because the class implements
        /// <see cref="IStartable"/>.
        /// </summary>
        public void Start()
        {
            OnCountersChanged();
        }

        /// <summary>Unsubscribes from the model — call on scope teardown (guardrails §13).</summary>
        public void Dispose()
        {
            _counters.Changed -= OnCountersChanged;
        }

        private void OnCountersChanged()
        {
            _view.SetDeadPrey($"Dead prey: {_counters.DeadPrey}");
            _view.SetDeadPredators($"Dead predators: {_counters.DeadPredators}");
        }
    }
}
