#nullable enable

namespace ZooWorld.Config
{
    /// <summary>
    /// The predation role of an animal. Binary by design — a predator eats any non-predator;
    /// predator-vs-predator is settled by strength (the food-chain resolver, T03).
    /// </summary>
    public enum Role
    {
        /// <summary>Eaten by predators; prey-vs-prey only bounces.</summary>
        Prey,

        /// <summary>Eats non-predators; duels other predators by strength.</summary>
        Predator
    }
}
