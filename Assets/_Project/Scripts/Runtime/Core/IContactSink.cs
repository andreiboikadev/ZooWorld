#nullable enable

using ZooWorld.Animals;

namespace ZooWorld.Core
{
    /// <summary>
    /// The <c>Simulation</c>'s inbound collision seam: an <c>Animal</c> enqueues each
    /// <c>OnCollisionEnter</c> contact here. The dumb adapter holds only this back-reference (set
    /// imperatively at registration — not an injected service, not a rule), so resolution stays in the
    /// end-of-step drain (guardrails §2/§6). The <c>Simulation</c> is the production implementer.
    /// </summary>
    public interface IContactSink
    {
        /// <summary>Enqueues the unordered contact pair <paramref name="a"/>/<paramref name="b"/> for the drain.</summary>
        void Enqueue(Animal a, Animal b);
    }
}
