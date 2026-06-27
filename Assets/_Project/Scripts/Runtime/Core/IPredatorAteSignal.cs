#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// The subscribe side of the "predator ate" Observer channel (GDD §6/§8; guardrails §13 — a typed
    /// delegate, not a global <c>EventBus</c>). The T09 "Tasty!" label spawner attaches an instance-method
    /// handler at composition; the <c>Simulation</c> raises it once per resolved eat via
    /// <see cref="PredatorAteSignal"/>.
    /// </summary>
    public interface IPredatorAteSignal
    {
        /// <summary>Raised once per resolved eat, carrying the predator's world position.</summary>
        event PredatorAteHandler Ate;
    }
}
