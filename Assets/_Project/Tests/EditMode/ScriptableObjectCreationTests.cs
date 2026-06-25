#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Config;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The config ScriptableObjects instantiate with the game-design.md §9 defaults.</summary>
    public sealed class ScriptableObjectCreationTests
    {
        [Test]
        public void AnimalDefinition_CreateInstance_HasSaneDefaults()
        {
            AnimalDefinition def = ScriptableObject.CreateInstance<AnimalDefinition>();

            Assert.That(def, Is.Not.Null);
            Assert.That(def.Role, Is.EqualTo(Role.Prey));
            Assert.That(def.SpawnWeight, Is.GreaterThanOrEqualTo(0f));
            Assert.That(def.Movement, Is.Null, "movement strategy is wired in T02");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void AnimalCatalog_CreateInstance_DefinitionsNonNullEmpty()
        {
            AnimalCatalog catalog = ScriptableObject.CreateInstance<AnimalCatalog>();

            Assert.That(catalog.Definitions, Is.Not.Null);
            Assert.That(catalog.Definitions.Count, Is.EqualTo(0));

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SimConfig_CreateInstance_HasGddDefaults()
        {
            SimConfig config = ScriptableObject.CreateInstance<SimConfig>();

            Assert.That(config.SpawnIntervalMin, Is.EqualTo(1f));
            Assert.That(config.SpawnIntervalMax, Is.EqualTo(2f));
            Assert.That(config.MaxPopulation, Is.EqualTo(120));
            Assert.That(config.PredatorFloor, Is.EqualTo(1));
            Assert.That(config.LinearDamping, Is.EqualTo(4f));
            Assert.That(config.GraceSeconds, Is.EqualTo(0.6f));

            Object.DestroyImmediate(config);
        }
    }
}
