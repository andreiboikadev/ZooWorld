#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZooWorld.Config
{
    /// <summary>
    /// The single list of all <see cref="AnimalDefinition"/>s — drives spawn weighting, keys the
    /// object pool, and is bound in DI. Adding a species = add one entry here (plus a new movement
    /// strategy SO only for genuinely new motion).
    /// </summary>
    [CreateAssetMenu(menuName = "Zoo World/Animal Catalog", fileName = "AnimalCatalog")]
    public sealed class AnimalCatalog : ScriptableObject
    {
        [SerializeField] private AnimalDefinition[] _definitions = Array.Empty<AnimalDefinition>();

        /// <summary>All authored species definitions, in catalog order.</summary>
        public IReadOnlyList<AnimalDefinition> Definitions => _definitions;
    }
}
