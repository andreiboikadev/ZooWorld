#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The strongly-typed delegate for the <see cref="AnimalDied"/> Observer channel
    /// (<see cref="IAnimalDeathSignal"/>). The payload is passed <c>in</c> (by readonly reference) so a
    /// raise neither boxes nor allocates (guardrails §13).
    /// </summary>
    /// <param name="e">The death event payload (victim role + world position).</param>
    public delegate void AnimalDiedHandler(in AnimalDied e);
}
