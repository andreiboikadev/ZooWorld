#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The concrete death Observer channel: the raise side for the <c>Simulation</c> (T07) and the
    /// subscribe side (<see cref="IAnimalDeathSignal"/>) for the counters/effects. Registered as a
    /// scope-singleton in DI (T08) — one instance shared between the publisher and its subscribers.
    /// </summary>
    public sealed class AnimalDeathSignal : IAnimalDeathSignal
    {
        /// <inheritdoc/>
        public event AnimalDiedHandler? Died;

        /// <summary>Raises <see cref="Died"/> for one resolved death (a no-op if there are no subscribers).</summary>
        /// <param name="e">The death event payload (victim role + world position).</param>
        public void Raise(in AnimalDied e)
        {
            Died?.Invoke(in e);
        }
    }
}
