#nullable enable

namespace ZooWorld.Animals
{
    /// <summary>
    /// Pure closed form for the jump leap burst. Unity PhysX applies <c>linearDamping</c> before
    /// integrating position each step (damp-then-move), so a body launched at <c>v₀</c> coasts a total
    /// <c>v₀ / linearDamping</c> (dt-independent); the burst that lands a nominal distance is therefore
    /// <c>distance × linearDamping</c>. The live distance is confirmed by the T07 Play smoke.
    /// </summary>
    public static class JumpMath
    {
        /// <summary>
        /// The initial horizontal burst speed (m/s) that coasts a nominal <paramref name="jumpDistance"/>
        /// (m) under <paramref name="linearDamping"/>. dt-independent under damp-then-move.
        /// </summary>
        public static float BurstSpeed(float jumpDistance, float linearDamping)
        {
            return jumpDistance * linearDamping;
        }
    }
}
