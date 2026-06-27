#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The <see cref="AnimalFactory"/> pool: proportional prewarm, per-index capacity, configured spawn,
    /// instance reuse with no runtime churn, monotonic seq assignment, and cap-exhaustion → null.
    /// </summary>
    public sealed class AnimalFactoryTests
    {
        private Animal? _prefab;
        private AnimalFactory? _factory;

        private static AnimalSpec Spec(Role role, float weight)
        {
            return new AnimalSpec(role, 0, 1f, 1f, Color.white, weight, null,
                new MovementTuning(2.5f, 1.5f, 1.5f, 4f, 0.8f, 1.5f));
        }

        private Animal Prefab()
        {
            var go = new GameObject("AnimalPrefab", typeof(Rigidbody), typeof(SphereCollider), typeof(Animal));
            _prefab = go.GetComponent<Animal>();
            return _prefab;
        }

        [TearDown]
        public void TearDown()
        {
            _factory?.Dispose();
            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab.gameObject);
            }
        }

        private static AnimalSpec[] ThreeSpecies()
        {
            return new[] { Spec(Role.Prey, 0.45f), Spec(Role.Predator, 0.30f), Spec(Role.Prey, 0.25f) };
        }

        [Test]
        public void Prewarm_RoundsPerSpecies()
        {
            _factory = new AnimalFactory(Prefab(), ThreeSpecies(), 120, 1, new FakeSpawnSequence(), null);

            Assert.That(_factory.FreeCount(0), Is.EqualTo(54));
            Assert.That(_factory.FreeCount(1), Is.EqualTo(36));
            Assert.That(_factory.FreeCount(2), Is.EqualTo(30));
            Assert.That(_factory.InstantiatedCount, Is.EqualTo(120));
        }

        [Test]
        public void Capacity_IsMaxPopulationPlusPredatorFloor()
        {
            _factory = new AnimalFactory(Prefab(), ThreeSpecies(), 120, 1, new FakeSpawnSequence(), null);

            Assert.That(_factory.Capacity(0), Is.EqualTo(121));
            Assert.That(_factory.Capacity(1), Is.EqualTo(121));
            Assert.That(_factory.Capacity(2), Is.EqualTo(121));
        }

        [Test]
        public void Spawn_ReturnsConfiguredActiveAnimal()
        {
            _factory = new AnimalFactory(Prefab(), ThreeSpecies(), 120, 1, new FakeSpawnSequence(), null);

            Animal? animal = _factory.Spawn(1, Vector3.zero);

            Assert.That(animal, Is.Not.Null);
            Assert.That(animal!.gameObject.activeSelf, Is.True);
            Assert.That(animal.Role, Is.EqualTo(Role.Predator));
            Assert.That(animal.Seq, Is.EqualTo(1L));
            Assert.That(_factory.InstantiatedCount, Is.EqualTo(120));
            Assert.That(_factory.FreeCount(1), Is.EqualTo(35));
        }

        [Test]
        public void SpawnThenDespawn_ReusesSameInstance()
        {
            _factory = new AnimalFactory(Prefab(), ThreeSpecies(), 120, 1, new FakeSpawnSequence(), null);

            Animal? a = _factory.Spawn(0, Vector3.zero);
            _factory.Despawn(a!);

            Assert.That(a!.gameObject.activeSelf, Is.False);
            Assert.That(_factory.FreeCount(0), Is.EqualTo(54));

            Animal? b = _factory.Spawn(0, Vector3.zero);

            Assert.That(ReferenceEquals(a, b), Is.True);
            Assert.That(_factory.InstantiatedCount, Is.EqualTo(120));
        }

        [Test]
        public void Despawn_ReturnsToOwnPool()
        {
            _factory = new AnimalFactory(Prefab(), ThreeSpecies(), 120, 1, new FakeSpawnSequence(), null);

            Animal? predator = _factory.Spawn(1, Vector3.zero);
            _factory.Despawn(predator!);

            // Routed back to index 1's pool (not index 0) — pins the PoolIndex routing.
            Assert.That(_factory.FreeCount(1), Is.EqualTo(36));
            Assert.That(_factory.FreeCount(0), Is.EqualTo(54));
            Assert.That(_factory.FreeCount(2), Is.EqualTo(30));
        }

        [Test]
        public void Spawn_AssignsMonotonicSeq()
        {
            _factory = new AnimalFactory(Prefab(), ThreeSpecies(), 120, 1, new FakeSpawnSequence(), null);

            Assert.That(_factory.Spawn(0, Vector3.zero)!.Seq, Is.EqualTo(1L));
            Assert.That(_factory.Spawn(1, Vector3.zero)!.Seq, Is.EqualTo(2L));
            Assert.That(_factory.Spawn(2, Vector3.zero)!.Seq, Is.EqualTo(3L));
        }

        [Test]
        public void Spawn_AtCap_ReturnsNull_NoInstantiateBeyondCap()
        {
            var specs = new[] { Spec(Role.Predator, 1f) };
            _factory = new AnimalFactory(Prefab(), specs, 2, 1, new FakeSpawnSequence(), null);

            Assert.That(_factory.Capacity(0), Is.EqualTo(3));
            Assert.That(_factory.Spawn(0, Vector3.zero), Is.Not.Null);
            Assert.That(_factory.Spawn(0, Vector3.zero), Is.Not.Null);
            Assert.That(_factory.Spawn(0, Vector3.zero), Is.Not.Null);
            Assert.That(_factory.Spawn(0, Vector3.zero), Is.Null);
            Assert.That(_factory.InstantiatedCount, Is.EqualTo(3));
        }
    }
}
