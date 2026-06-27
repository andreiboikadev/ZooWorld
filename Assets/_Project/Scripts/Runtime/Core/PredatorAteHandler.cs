#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The strongly-typed delegate for the <see cref="PredatorAte"/> Observer channel
    /// (<see cref="IPredatorAteSignal"/>). The payload is passed <c>in</c> (by readonly reference) so a
    /// raise neither boxes nor allocates (guardrails §13).
    /// </summary>
    /// <param name="e">The eat event payload (the predator's world position).</param>
    public delegate void PredatorAteHandler(in PredatorAte e);
}
