#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The subscribe side of the death Observer channel (GDD §4; guardrails §13 — a typed delegate, not a
    /// global <c>EventBus</c>). Subscribers (the HUD's <c>DeathCounters</c>, the T09 death-puff) attach an
    /// instance-method handler at composition; the <c>Simulation</c> (T07) raises it once per resolved
    /// death via <see cref="AnimalDeathSignal"/>.
    /// </summary>
    public interface IAnimalDeathSignal
    {
        /// <summary>Raised once per resolved death, carrying the victim's role and world position.</summary>
        event AnimalDiedHandler Died;
    }
}
