#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// Monotonic spawn-sequence seam — hands out a never-reused, strictly increasing id at
    /// take-from-pool. The id breaks predator-vs-predator strength ties deterministically.
    /// </summary>
    public interface ISpawnSequence
    {
        /// <summary>The next id; strictly greater than every previously returned value.</summary>
        long Next();
    }
}
