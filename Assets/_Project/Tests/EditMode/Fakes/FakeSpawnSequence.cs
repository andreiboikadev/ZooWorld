#nullable enable

using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode.Fakes
{
    /// <summary>Hand-set <see cref="ISpawnSequence"/> — returns ascending ids from a start value.</summary>
    public sealed class FakeSpawnSequence : ISpawnSequence
    {
        private long _next;

        /// <summary>Creates a sequence whose first <see cref="Next"/> returns <paramref name="start"/>.</summary>
        public FakeSpawnSequence(long start = 1L)
        {
            _next = start;
        }

        /// <inheritdoc/>
        public long Next()
        {
            return _next++;
        }
    }
}
