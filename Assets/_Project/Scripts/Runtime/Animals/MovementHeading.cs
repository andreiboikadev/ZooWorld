#nullable enable

using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>Shared pure helper for picking a random horizontal (XZ) movement heading.</summary>
    public static class MovementHeading
    {
        /// <summary>A unit-length XZ heading at a uniform random angle (Y = 0), drawn from <paramref name="rng"/>.</summary>
        public static Vector3 RandomXz(IRandom rng)
        {
            float angle = rng.Range(0f, 2f * Mathf.PI);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
