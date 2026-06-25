#nullable enable

namespace ZooWorld.Core
{
    /// <summary>The category of a resolved collision <see cref="Outcome"/>.</summary>
    public enum OutcomeKind
    {
        /// <summary>No interaction (e.g. either side already dead).</summary>
        None,

        /// <summary>Prey×prey — both live; a separation impulse is applied.</summary>
        Bounce,

        /// <summary>One animal dies (a predator ate prey, or a predator duel).</summary>
        Death
    }
}
