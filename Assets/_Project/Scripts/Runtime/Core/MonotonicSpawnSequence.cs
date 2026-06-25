#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// Production <see cref="ISpawnSequence"/> — a never-reused, strictly increasing <c>long</c>
    /// assigned at take-from-pool. First id is 1; the pre-increment guarantees no two calls collide.
    /// </summary>
    public sealed class MonotonicSpawnSequence : ISpawnSequence
    {
        private long _next;

        /// <inheritdoc/>
        public long Next()
        {
            return ++_next;
        }
    }
}
