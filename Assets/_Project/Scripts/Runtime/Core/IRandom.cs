#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// Randomness seam for pure rules — abstracts <c>UnityEngine.Random</c>/<c>System.Random</c> so
    /// spawn weighting, wander headings, and intervals are deterministic under a seeded fake.
    /// </summary>
    public interface IRandom
    {
        /// <summary>A uniform value in the range [0, 1).</summary>
        float Value01();

        /// <summary>A uniform float in the range [<paramref name="a"/>, <paramref name="b"/>).</summary>
        float Range(float a, float b);

        /// <summary>A uniform int in the range [<paramref name="a"/>, <paramref name="b"/>) (upper-exclusive).</summary>
        int Range(int a, int b);
    }
}
