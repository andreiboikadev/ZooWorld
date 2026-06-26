#nullable enable

using System;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.UI
{
    /// <summary>
    /// The HUD's death model: subscribes to the <see cref="IAnimalDeathSignal"/> Observer channel and
    /// keeps one running count per <see cref="Role"/>. Exactly one counter increments per resolved death
    /// (GDD §6); a prey×prey bounce raises no <see cref="AnimalDied"/>, so it never reaches here. Pure
    /// (no Unity statics) — runs headless in EditMode. Counts never reset (a single endless session).
    /// </summary>
    public sealed class DeathCounters : IDisposable
    {
        private readonly IAnimalDeathSignal _signal;

        public DeathCounters(IAnimalDeathSignal signal)
        {
            _signal = signal;
            _signal.Died += OnAnimalDied;
        }

        /// <summary>Raised after each death is counted, so the presenter can refresh the view.</summary>
        public event Action? Changed;

        /// <summary>The running count of dead prey.</summary>
        public int DeadPrey { get; private set; }

        /// <summary>The running count of dead predators.</summary>
        public int DeadPredators { get; private set; }

        /// <summary>Unsubscribes from the signal — call on scope teardown (guardrails §13).</summary>
        public void Dispose()
        {
            _signal.Died -= OnAnimalDied;
        }

        private void OnAnimalDied(in AnimalDied e)
        {
            // Exactly one counter moves, keyed by the victim's role; the death position is irrelevant here.
            switch (e.Role)
            {
                case Role.Prey:
                    DeadPrey++;
                    break;
                case Role.Predator:
                    DeadPredators++;
                    break;
            }

            Changed?.Invoke();
        }
    }
}
