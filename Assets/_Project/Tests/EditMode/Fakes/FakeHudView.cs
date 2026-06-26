#nullable enable

using ZooWorld.UI;

namespace ZooWorld.Tests.EditMode.Fakes
{
    /// <summary>
    /// Test double for <see cref="IHudView"/> that records the last string pushed to each counter label,
    /// so presenter tests can assert the exact formatted text.
    /// </summary>
    public sealed class FakeHudView : IHudView
    {
        /// <summary>The last text pushed to the dead-prey label (null until first set).</summary>
        public string? DeadPreyText { get; private set; }

        /// <summary>The last text pushed to the dead-predators label (null until first set).</summary>
        public string? DeadPredatorsText { get; private set; }

        /// <inheritdoc/>
        public void SetDeadPrey(string text)
        {
            DeadPreyText = text;
        }

        /// <inheritdoc/>
        public void SetDeadPredators(string text)
        {
            DeadPredatorsText = text;
        }
    }
}
