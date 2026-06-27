#nullable enable

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Composition;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure <see cref="CatalogProjection"/>: the 1:1 catalog-order index contract over the parallel
    /// spec/weight lists, the per-field projection (Definition + SimConfig → value structs), and the
    /// SpawnTuning/SimulationTuning copies. Hermetic cases build the getter-only SOs via reflection (the
    /// repo idiom for SO privates); a real-asset case round-trips the authored AnimalCatalog.
    /// </summary>
    public sealed class CatalogProjectionTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Build_ProjectsEachDefinitionInCatalogOrder()
        {
            SimConfig config = Track(ScriptableObject.CreateInstance<SimConfig>());
            MovementBehaviour preyMove = Track(ScriptableObject.CreateInstance<JumpMove>());
            MovementBehaviour predMove = Track(ScriptableObject.CreateInstance<LinearMove>());
            AnimalDefinition frog = Definition(Role.Prey, 0, preyMove, 0.45f, 1f, 1f, 0f, 1.5f, 1.5f, Color.green);
            AnimalDefinition snake = Definition(Role.Predator, 5, predMove, 0.30f, 1.2f, 2f, 2.5f, 0f, 0f, Color.red);
            AnimalCatalog catalog = Catalog(frog, snake);

            (AnimalSpec[] specs, SpeciesWeight[] weights) = CatalogProjection.Build(catalog, config);

            Assert.That(specs.Length, Is.EqualTo(2));
            Assert.That(weights.Length, Is.EqualTo(catalog.Definitions.Count));

            // index 0 — frog (Prey): full field round-trip + tuning from the SimConfig globals.
            Assert.That(weights[0].Role, Is.EqualTo(Role.Prey));
            Assert.That(weights[0].Weight, Is.EqualTo(0.45f));
            Assert.That(specs[0].Role, Is.EqualTo(Role.Prey));
            Assert.That(specs[0].Strength, Is.EqualTo(0));
            Assert.That(specs[0].Size, Is.EqualTo(1f));
            Assert.That(specs[0].Mass, Is.EqualTo(1f));
            Assert.That(specs[0].SpawnWeight, Is.EqualTo(0.45f));
            Assert.That(specs[0].Movement, Is.SameAs(preyMove));
            Assert.That(specs[0].Color, Is.EqualTo(Color.green));
            Assert.That(specs[0].Tuning.Speed, Is.EqualTo(0f));
            Assert.That(specs[0].Tuning.JumpDistance, Is.EqualTo(1.5f));
            Assert.That(specs[0].Tuning.JumpInterval, Is.EqualTo(1.5f));
            Assert.That(specs[0].Tuning.LinearDamping, Is.EqualTo(config.LinearDamping));
            Assert.That(specs[0].Tuning.WanderRerollMin, Is.EqualTo(config.WanderRerollMin));
            Assert.That(specs[0].Tuning.WanderRerollMax, Is.EqualTo(config.WanderRerollMax));

            // index 1 — snake (Predator) lands at its catalog index.
            Assert.That(weights[1].Role, Is.EqualTo(Role.Predator));
            Assert.That(specs[1].Role, Is.EqualTo(Role.Predator));
            Assert.That(specs[1].Strength, Is.EqualTo(5));
            Assert.That(specs[1].Size, Is.EqualTo(1.2f));
            Assert.That(specs[1].Mass, Is.EqualTo(2f));
            Assert.That(specs[1].Movement, Is.SameAs(predMove));
            Assert.That(specs[1].Color, Is.EqualTo(Color.red));
            Assert.That(specs[1].Tuning.Speed, Is.EqualTo(2.5f));
        }

        [Test]
        public void BuildSpawnTuning_CopiesSpawnConstants()
        {
            SimConfig config = Track(ScriptableObject.CreateInstance<SimConfig>());

            SpawnTuning tuning = CatalogProjection.BuildSpawnTuning(config);

            Assert.That(tuning.IntervalMin, Is.EqualTo(1f));
            Assert.That(tuning.IntervalMax, Is.EqualTo(2f));
            Assert.That(tuning.MaxPopulation, Is.EqualTo(120));
            Assert.That(tuning.PredatorFloor, Is.EqualTo(1));
            Assert.That(tuning.ClearanceRadius, Is.EqualTo(1f));
            Assert.That(tuning.MaxPlacementAttempts, Is.EqualTo(10));
        }

        [Test]
        public void BuildSimulationTuning_CopiesCombatConstants()
        {
            SimConfig config = Track(ScriptableObject.CreateInstance<SimConfig>());

            SimulationTuning tuning = CatalogProjection.BuildSimulationTuning(config);

            Assert.That(tuning.BounceKick, Is.EqualTo(4f));
            Assert.That(tuning.GraceSeconds, Is.EqualTo(0.6f));
        }

        [Test]
        public void BuildFeedbackTuning_CopiesFeedbackConstants()
        {
            SimConfig config = Track(ScriptableObject.CreateInstance<SimConfig>());

            FeedbackTuning tuning = CatalogProjection.BuildFeedbackTuning(config);

            Assert.That(tuning.JumpArcHeight, Is.EqualTo(0.5f));
            Assert.That(tuning.JumpArcDuration, Is.EqualTo(0.45f));
            Assert.That(tuning.SpawnPopDuration, Is.EqualTo(0.2f));
            Assert.That(tuning.JumpArc, Is.Not.Null);
            Assert.That(tuning.PopEase, Is.Not.Null);
        }

        [Test]
        public void Build_RealCatalogAsset_RoundTripsAndContainsPredator()
        {
            AnimalCatalog catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(
                "Assets/_Project/ScriptableObjects/AnimalCatalog.asset");
            SimConfig config = AssetDatabase.LoadAssetAtPath<SimConfig>(
                "Assets/_Project/ScriptableObjects/SimConfig.asset");
            Assert.That(catalog, Is.Not.Null, "AnimalCatalog.asset must exist (slice precondition)");
            Assert.That(config, Is.Not.Null, "SimConfig.asset must exist");

            (AnimalSpec[] specs, SpeciesWeight[] weights) = CatalogProjection.Build(catalog, config);

            Assert.That(specs.Length, Is.EqualTo(catalog.Definitions.Count));
            Assert.That(weights.Length, Is.EqualTo(catalog.Definitions.Count));

            bool hasPredator = false;
            for (int i = 0; i < specs.Length; i++)
            {
                Assert.That(specs[i].Role, Is.EqualTo(catalog.Definitions[i].Role));
                Assert.That(weights[i].Weight, Is.EqualTo(catalog.Definitions[i].SpawnWeight));
                Assert.That(specs[i].Color, Is.EqualTo(catalog.Definitions[i].Color));
                if (specs[i].Role == Role.Predator)
                {
                    hasPredator = true;
                }
            }

            Assert.That(hasPredator, Is.True, "the catalog needs >= 1 Predator (SpawnPlanner floor precondition)");
        }

        [Test]
        public void RealCatalog_Rabbit_ReusesFrogMovement_DataOnly()
        {
            AnimalCatalog catalog = AssetDatabase.LoadAssetAtPath<AnimalCatalog>(
                "Assets/_Project/ScriptableObjects/AnimalCatalog.asset");
            Assert.That(catalog, Is.Not.Null, "AnimalCatalog.asset must exist");

            // The data-only extension proof (GDD §4 / ADR 0002 §1): Rabbit is the 3rd entry and reuses the
            // SHARED JumpMove SO — no new strategy authored, no code touched.
            Assert.That(catalog.Definitions.Count, Is.EqualTo(3));
            Assert.That(catalog.Definitions[0].Id, Is.EqualTo("frog"));
            Assert.That(catalog.Definitions[1].Id, Is.EqualTo("snake"));
            Assert.That(catalog.Definitions[2].Id, Is.EqualTo("rabbit"));
            Assert.That(catalog.Definitions[2].Movement, Is.SameAs(catalog.Definitions[0].Movement));
            Assert.That(catalog.Definitions[0].SpawnWeight, Is.EqualTo(0.45f));
            Assert.That(catalog.Definitions[1].SpawnWeight, Is.EqualTo(0.30f));
            Assert.That(catalog.Definitions[2].SpawnWeight, Is.EqualTo(0.25f));
        }

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        private AnimalDefinition Definition(Role role, int strength, MovementBehaviour? movement,
            float spawnWeight, float size, float mass, float speed, float jumpDistance, float jumpInterval,
            Color color)
        {
            AnimalDefinition def = Track(ScriptableObject.CreateInstance<AnimalDefinition>());
            SetField(def, "_role", role);
            SetField(def, "_strength", strength);
            SetField(def, "_movement", movement);
            SetField(def, "_spawnWeight", spawnWeight);
            SetField(def, "_size", size);
            SetField(def, "_mass", mass);
            SetField(def, "_speed", speed);
            SetField(def, "_jumpDistance", jumpDistance);
            SetField(def, "_jumpInterval", jumpInterval);
            SetField(def, "_color", color);
            return def;
        }

        private AnimalCatalog Catalog(params AnimalDefinition[] definitions)
        {
            AnimalCatalog catalog = Track(ScriptableObject.CreateInstance<AnimalCatalog>());
            SetField(catalog, "_definitions", definitions);
            return catalog;
        }

        private static void SetField(object target, string field, object? value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!;
            info.SetValue(target, value);
        }
    }
}
