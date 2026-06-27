#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The concrete "predator ate" Observer channel: the raise side for the <c>Simulation</c> (T09) and the
    /// subscribe side (<see cref="IPredatorAteSignal"/>) for the "Tasty!" label spawner. Registered as a
    /// scope-singleton in DI (T09) — one instance shared between the publisher and its subscribers.
    /// </summary>
    public sealed class PredatorAteSignal : IPredatorAteSignal
    {
        /// <inheritdoc/>
        public event PredatorAteHandler? Ate;

        /// <summary>Raises <see cref="Ate"/> for one resolved eat (a no-op if there are no subscribers).</summary>
        /// <param name="e">The eat event payload (the predator's world position).</param>
        public void Raise(in PredatorAte e)
        {
            Ate?.Invoke(in e);
        }
    }
}
