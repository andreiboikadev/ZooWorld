#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.Predation;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// Headless integration of the <see cref="Simulation"/> tick + drain: the real <c>FixedTick</c> driven
    /// with no Play (physics is not simulated, so state the tick changes is asserted, not motion). Contacts
    /// are enqueued by hand via <see cref="IContactSink"/>. The <c>FieldBounds</c> inner margin is set to 1
    /// (below <c>JumpDistance</c>) deliberately, to reach the out-of-bounds safety-net paths.
    /// </summary>
    public sealed class SimulationTests
    {
        private readonly List<MovementBehaviour> _strategySos = new List<MovementBehaviour>();
        private readonly List<AnimalDied> _deaths = new List<AnimalDied>();

        private GameObject _prefabGo = null!;
        private Animal _prefab = null!;
        private FakeClock _clock = null!;
        private FakeRandom _random = null!;
        private AnimalDeathSignal _signal = null!;
        private Simulation? _sim;
        private AnimalFactory? _factory;

        [SetUp]
        public void SetUp()
        {
            _prefabGo = new GameObject("AnimalPrefab", typeof(Rigidbody), typeof(SphereCollider), typeof(Animal));
            _prefab = _prefabGo.GetComponent<Animal>();
            _clock = new FakeClock();
            _random = new FakeRandom();
            _signal = new AnimalDeathSignal();
            _signal.Died += OnDied;
            _deaths.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _sim?.Dispose();
            _factory?.Dispose();
            _sim = null;
            _factory = null;

            foreach (MovementBehaviour so in _strategySos)
            {
                if (so != null)
                {
                    Object.DestroyImmediate(so);
                }
            }

            _strategySos.Clear();

            if (_prefabGo != null)
            {
                Object.DestroyImmediate(_prefabGo);
            }
        }

        [Test]
        public void Register_SeedsState()
        {
            AnimalFactory factory = MakeFactory(PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);

            Animal frog = SpawnRegister(factory, sim, 0, Vector3.zero);

            Assert.That(frog.MovementState.Heading, Is.Not.EqualTo(Vector3.zero));
            Assert.That(frog.MovementState.NextLeapTime, Is.EqualTo(_clock.Now + 1.5f));
        }

        [Test]
        public void FixedTick_DrivesLinearMover()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()));
            Simulation sim = MakeSim(factory);
            Animal snake = SpawnRegister(factory, sim, 0, Vector3.zero);

            sim.FixedTick();

            Assert.That(snake.Body.linearVelocity, Is.EqualTo(new Vector3(2.5f, 0f, 0f)));
        }

        [Test]
        public void FixedTick_OutOfBounds_WritesSteeredHeadingBack()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()));
            Simulation sim = MakeSim(factory);
            Animal snake = SpawnRegister(factory, sim, 0, Vector3.zero);
            snake.MovementState.Heading = new Vector3(1f, 0f, 0f);
            snake.transform.position = new Vector3(9.5f, 0f, 0f);

            sim.FixedTick();

            Assert.That(snake.MovementState.Heading.x, Is.LessThan(0f));
            Assert.That(snake.Body.linearVelocity.x, Is.LessThan(0f));
        }

        [Test]
        public void FixedTick_JumperAdvancesLeapState()
        {
            AnimalFactory factory = MakeFactory(PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal frog = SpawnRegister(factory, sim, 0, Vector3.zero);
            frog.MovementState.NextLeapTime = 1.5f;
            _clock.Now = 1.5f;

            sim.FixedTick();

            Assert.That(frog.MovementState.NextLeapTime, Is.EqualTo(3.0f));
            Assert.That(frog.MovementState.Heading, Is.Not.EqualTo(Vector3.zero));
        }

        [Test]
        public void FixedTick_OutOfBoundsIdleJumper_ForcesRecoveryLeap()
        {
            AnimalFactory factory = MakeFactory(PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal frog = SpawnRegister(factory, sim, 0, Vector3.zero);
            frog.transform.position = new Vector3(9.5f, 0f, 0f);

            sim.FixedTick();

            Assert.That(frog.MovementState.NextLeapTime, Is.EqualTo(_clock.Now));
        }

        [Test]
        public void FixedTick_InBoundsIdleJumper_DoesNotNudge()
        {
            AnimalFactory factory = MakeFactory(PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal frog = SpawnRegister(factory, sim, 0, Vector3.zero);

            sim.FixedTick();

            // The recovery-leap nudge is gated on out-of-bounds — an in-bounds idle jumper keeps its seed.
            Assert.That(frog.MovementState.NextLeapTime, Is.EqualTo(_clock.Now + 1.5f));
        }

        [Test]
        public void Drain_PredatorEatsPrey_RaisesDeath_Despawns()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()), PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal predator = SpawnRegister(factory, sim, 0, new Vector3(1f, 0f, 1f));
            Animal prey = SpawnRegister(factory, sim, 1, new Vector3(3f, 0f, 4f));
            Enqueue(sim, predator, prey);

            sim.FixedTick();

            Assert.That(prey.IsDead, Is.True);
            Assert.That(predator.IsDead, Is.False);
            Assert.That(prey.gameObject.activeSelf, Is.False);
            Assert.That(_deaths.Count, Is.EqualTo(1));
            Assert.That(_deaths[0].Role, Is.EqualTo(Role.Prey));
            Assert.That(_deaths[0].Position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
        }

        [Test]
        public void Drain_PreyVsPrey_BothLive_OpensGrace_NoDeath()
        {
            AnimalFactory factory = MakeFactory(PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal a = SpawnRegister(factory, sim, 0, new Vector3(1f, 0f, 0f));
            Animal b = SpawnRegister(factory, sim, 0, new Vector3(2f, 0f, 0f));
            Enqueue(sim, a, b);

            sim.FixedTick();

            Assert.That(a.IsDead, Is.False);
            Assert.That(b.IsDead, Is.False);
            Assert.That(a.MovementState.GraceUntil, Is.EqualTo(0.6f));
            Assert.That(b.MovementState.GraceUntil, Is.EqualTo(0.6f));
            Assert.That(_deaths.Count, Is.EqualTo(0));
        }

        [Test]
        public void Drain_DuplicateContacts_ResolveOnce()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()), PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal predator = SpawnRegister(factory, sim, 0, new Vector3(1f, 0f, 0f));
            Animal prey = SpawnRegister(factory, sim, 1, new Vector3(2f, 0f, 0f));
            Enqueue(sim, predator, prey);
            Enqueue(sim, prey, predator);
            Enqueue(sim, predator, prey);

            sim.FixedTick();

            Assert.That(_deaths.Count, Is.EqualTo(1));
            Assert.That(prey.IsDead, Is.True);
        }

        [Test]
        public void Drain_EatsBeforeBounces_DeadPreyDoesNotBounce()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()), PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal predator = SpawnRegister(factory, sim, 0, new Vector3(0f, 0f, 0f));
            Animal preyA = SpawnRegister(factory, sim, 1, new Vector3(1f, 0f, 0f));
            Animal preyB = SpawnRegister(factory, sim, 1, new Vector3(2f, 0f, 0f));
            // Enqueue the BOUNCE pair FIRST: only a two-pass (deaths-before-bounces) drain leaves preyB
            // un-bounced. A one-pass loop iterating in enqueue order would open preyB's grace on (preyA,
            // preyB) BEFORE preyA is eaten — so this order genuinely discriminates the ordering.
            Enqueue(sim, preyA, preyB);
            Enqueue(sim, predator, preyA);

            sim.FixedTick();

            Assert.That(preyA.IsDead, Is.True);
            Assert.That(preyB.IsDead, Is.False);
            Assert.That(preyB.MovementState.GraceUntil, Is.EqualTo(0f));
            Assert.That(_deaths.Count, Is.EqualTo(1));
            Assert.That(_deaths[0].Role, Is.EqualTo(Role.Prey));
        }

        [Test]
        public void Drain_PredatorDuel_HigherStrengthSurvives()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()), PredatorSpec(3, Strategy<LinearMove>()));
            Simulation sim = MakeSim(factory);
            Animal strong = SpawnRegister(factory, sim, 0, new Vector3(1f, 0f, 0f));
            Animal weak = SpawnRegister(factory, sim, 1, new Vector3(2f, 0f, 0f));
            Enqueue(sim, strong, weak);

            sim.FixedTick();

            Assert.That(weak.IsDead, Is.True);
            Assert.That(strong.IsDead, Is.False);
            Assert.That(_deaths.Count, Is.EqualTo(1));
            Assert.That(_deaths[0].Role, Is.EqualTo(Role.Predator));
        }

        [Test]
        public void Despawn_Twice_NoDoublePush()
        {
            AnimalFactory factory = MakeFactory(PreySpec(Strategy<JumpMove>()));
            Animal a = factory.Spawn(0, Vector3.zero)!;
            factory.Despawn(a);
            int free = factory.FreeCount(0);

            factory.Despawn(a);

            Assert.That(factory.FreeCount(0), Is.EqualTo(free));
        }

        [Test]
        public void Population_CountsActiveByRole()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()), PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            SpawnRegister(factory, sim, 0, new Vector3(1f, 0f, 0f));
            SpawnRegister(factory, sim, 0, new Vector3(2f, 0f, 0f));
            SpawnRegister(factory, sim, 1, new Vector3(3f, 0f, 0f));

            PopulationSnapshot pop = sim.Population;

            Assert.That(pop.Total, Is.EqualTo(3));
            Assert.That(pop.PredatorCount, Is.EqualTo(2));
        }

        [Test]
        public void Population_AfterPreyDeath_TotalDrops_PredatorCountHolds()
        {
            AnimalFactory factory = MakeFactory(PredatorSpec(5, Strategy<LinearMove>()), PreySpec(Strategy<JumpMove>()));
            Simulation sim = MakeSim(factory);
            Animal predator = SpawnRegister(factory, sim, 0, new Vector3(1f, 0f, 0f));
            Animal prey = SpawnRegister(factory, sim, 1, new Vector3(2f, 0f, 0f));
            Enqueue(sim, predator, prey);

            sim.FixedTick();

            PopulationSnapshot pop = sim.Population;
            Assert.That(pop.Total, Is.EqualTo(1));
            Assert.That(pop.PredatorCount, Is.EqualTo(1));
        }

        private static MovementTuning MoveTuning()
        {
            return new MovementTuning(2.5f, 1.5f, 1.5f, 4f, 0.8f, 1.5f);
        }

        private AnimalSpec PreySpec(MovementBehaviour movement)
        {
            return new AnimalSpec(Role.Prey, 0, 1f, 1f, 0.5f, movement, MoveTuning());
        }

        private AnimalSpec PredatorSpec(int strength, MovementBehaviour movement)
        {
            return new AnimalSpec(Role.Predator, strength, 1f, 1f, 0.5f, movement, MoveTuning());
        }

        private T Strategy<T>() where T : MovementBehaviour
        {
            T so = ScriptableObject.CreateInstance<T>();
            _strategySos.Add(so);
            return so;
        }

        private AnimalFactory MakeFactory(params AnimalSpec[] specs)
        {
            _factory = new AnimalFactory(_prefab, specs, 2, 1, new FakeSpawnSequence(), null);
            return _factory;
        }

        private Simulation MakeSim(AnimalFactory factory)
        {
            _sim = new Simulation(_clock, _random, new FieldBounds(Vector3.zero, new Vector2(10f, 6f), 1f),
                new SimulationTuning(4f, 0.6f), new FoodChainResolver(), _signal, factory);
            return _sim;
        }

        private Animal SpawnRegister(AnimalFactory factory, Simulation sim, int index, Vector3 position)
        {
            Animal animal = factory.Spawn(index, position)!;
            sim.Register(animal);
            return animal;
        }

        private void Enqueue(Simulation sim, Animal a, Animal b)
        {
            ((IContactSink)sim).Enqueue(a, b);
        }

        private void OnDied(in AnimalDied e)
        {
            _deaths.Add(e);
        }
    }
}
