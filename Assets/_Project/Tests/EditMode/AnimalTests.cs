#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The <see cref="Animal"/> dumb adapter: the <c>OnSpawn</c> physics profile + transform + runtime
    /// state, <c>MarkDead</c>, and the reset contract on reuse. Physics is not simulated headless —
    /// configured values are asserted, not motion.
    /// </summary>
    public sealed class AnimalTests
    {
        private GameObject? _go;

        private static AnimalSpec Spec(Role role, int strength = 0, float size = 1f, float mass = 1f, float weight = 1f)
        {
            return new AnimalSpec(role, strength, size, mass, Color.white, weight, null,
                new MovementTuning(2.5f, 1.5f, 1.5f, 4f, 0.8f, 1.5f));
        }

        private Animal MakeAnimal()
        {
            _go = new GameObject("Animal", typeof(Rigidbody), typeof(SphereCollider), typeof(Animal));
            return _go.GetComponent<Animal>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void OnSpawn_AppliesPhysicsProfile()
        {
            Animal animal = MakeAnimal();

            animal.OnSpawn(Spec(Role.Prey, mass: 2f), 1L, Vector3.zero);

            Assert.That(animal.Body.useGravity, Is.False);
            Assert.That(animal.Body.sleepThreshold, Is.EqualTo(0f));
            Assert.That(animal.Body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Discrete));
            Assert.That(
                animal.Body.constraints,
                Is.EqualTo(RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX
                    | RigidbodyConstraints.FreezeRotationZ));
            Assert.That(animal.Body.linearDamping, Is.EqualTo(4f));
            Assert.That(animal.Body.mass, Is.EqualTo(2f));
        }

        [Test]
        public void OnSpawn_AppliesTransform()
        {
            Animal animal = MakeAnimal();

            animal.OnSpawn(Spec(Role.Prey, size: 1.5f), 1L, new Vector3(3f, 0f, 4f));

            Assert.That(animal.transform.localScale, Is.EqualTo(Vector3.one * 1.5f));
            Assert.That(animal.transform.position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
            Assert.That(animal.transform.rotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void OnSpawn_SetsRuntimeState()
        {
            Animal animal = MakeAnimal();

            animal.OnSpawn(Spec(Role.Predator, strength: 5), 7L, Vector3.zero);

            Assert.That(animal.Role, Is.EqualTo(Role.Predator));
            Assert.That(animal.Strength, Is.EqualTo(5));
            Assert.That(animal.Seq, Is.EqualTo(7L));
            Assert.That(animal.IsDead, Is.False);
            Assert.That(animal.Tuning.LinearDamping, Is.EqualTo(4f));
        }

        [Test]
        public void MarkDead_SetsIsDead()
        {
            Animal animal = MakeAnimal();
            animal.OnSpawn(Spec(Role.Prey), 1L, Vector3.zero);

            animal.MarkDead();

            Assert.That(animal.IsDead, Is.True);
        }

        [Test]
        public void Reuse_ClearsStaleState()
        {
            Animal animal = MakeAnimal();
            animal.OnSpawn(Spec(Role.Prey), 1L, new Vector3(1f, 0f, 1f));
            animal.Body.linearVelocity = new Vector3(5f, 0f, 0f);
            animal.MarkDead();
            animal.MovementState.Heading = Vector3.right;
            animal.MovementState.GraceUntil = 99f;
            animal.MovementState.NextLeapTime = 99f;
            animal.MovementState.NextHeadingReroll = 99f;
            animal.OnDespawn();

            // OnDespawn independently zeroes velocity (not masked by the next OnSpawn).
            Assert.That(animal.Body.linearVelocity, Is.EqualTo(Vector3.zero));

            animal.OnSpawn(Spec(Role.Predator), 2L, new Vector3(2f, 0f, 2f));

            Assert.That(animal.Body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(animal.IsDead, Is.False);
            Assert.That(animal.Seq, Is.EqualTo(2L));
            Assert.That(animal.Role, Is.EqualTo(Role.Predator));
            Assert.That(animal.MovementState.Heading, Is.EqualTo(Vector3.zero));
            Assert.That(animal.MovementState.GraceUntil, Is.EqualTo(0f));
            Assert.That(animal.MovementState.NextLeapTime, Is.EqualTo(0f));
            Assert.That(animal.MovementState.NextHeadingReroll, Is.EqualTo(0f));
        }
    }
}
