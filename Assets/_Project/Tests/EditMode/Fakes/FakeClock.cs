#nullable enable

using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode.Fakes
{
    /// <summary>Settable <see cref="IClock"/> for headless tests — advance time by hand.</summary>
    public sealed class FakeClock : IClock
    {
        /// <summary>Creates a clock at <paramref name="now"/> with fixed step <paramref name="dt"/>.</summary>
        public FakeClock(float now = 0f, float dt = 0.02f)
        {
            Now = now;
            Dt = dt;
        }

        /// <inheritdoc/>
        public float Now { get; set; }

        /// <inheritdoc/>
        public float Dt { get; set; }

        /// <summary>Advances <see cref="Now"/> by <paramref name="seconds"/>.</summary>
        public void Advance(float seconds)
        {
            Now += seconds;
        }
    }
}
