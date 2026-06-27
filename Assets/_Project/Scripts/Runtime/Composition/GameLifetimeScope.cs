#nullable enable

using UnityEngine;
using VContainer;
using VContainer.Unity;
using ZooWorld.Animals;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.Predation;
using ZooWorld.Spawning;
using ZooWorld.UI;

namespace ZooWorld.Composition
{
    /// <summary>
    /// The single composition root (guardrails §9): projects the config SOs into value structs, builds the
    /// camera-derived <see cref="FieldBounds"/>, and binds every service as a scope-singleton. Services
    /// whose constructors take non-injectable args (<c>in</c> structs, the spec/weight arrays, primitives,
    /// the prefab/Transform) are registered via factory lambdas that capture the composition locals — so
    /// nothing relies on reflection injection for a value the container can't resolve. No static singletons,
    /// no service locator, no <c>Resolve</c> from gameplay.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Config")]
        [SerializeField] private AnimalCatalog _catalog = null!;
        [SerializeField] private SimConfig _config = null!;

        [Header("Scene refs")]
        [SerializeField] private Animal _animalPrefab = null!;
        [SerializeField] private Camera _camera = null!;
        [SerializeField] private HudView _hudView = null!;
        [SerializeField] private Transform _animalsRoot = null!;

        [Header("Spawn placement")]
        [Tooltip("The Animal physics layer queried for spawn clearance.")]
        [SerializeField] private LayerMask _animalMask;
        [Tooltip("World Y of the ground plane the camera frustum is projected onto.")]
        [SerializeField] private float _groundY;
        [Tooltip("Seed for the deterministic IRandom (spawn + wander draws).")]
        [SerializeField] private int _randomSeed = 12345;

        /// <inheritdoc/>
        protected override void Configure(IContainerBuilder builder)
        {
            // Build the value inputs as composition locals; factory lambdas below capture them, so the
            // in-struct / IReadOnlyList<> / primitive ctor args never go through reflection injection.
            (AnimalSpec[] specs, SpeciesWeight[] weights) = CatalogProjection.Build(_catalog, _config);
            SpawnTuning spawnTuning = CatalogProjection.BuildSpawnTuning(_config);
            SimulationTuning simTuning = CatalogProjection.BuildSimulationTuning(_config);
            FieldBounds bounds = FieldBoundsFactory.FromTopDownCamera(_camera.transform.position,
                _camera.fieldOfView, _camera.aspect, _groundY, _config.FieldInnerMargin);

            // Seams (factory lambdas where a ctor arg isn't DI-resolvable).
            builder.Register<IClock, UnityClock>(Lifetime.Singleton);
            builder.Register<ISpawnSequence, MonotonicSpawnSequence>(Lifetime.Singleton);
            builder.Register<IRandom>(_ => new SeededRandom(_randomSeed), Lifetime.Singleton);
            builder.Register<IOccupancyQuery>(_ => new PhysicsOccupancyQuery(_animalMask), Lifetime.Singleton);

            // Death channel — ONE registration, both contracts (publisher + subscribers share the instance).
            builder.Register<AnimalDeathSignal>(Lifetime.Singleton).AsSelf().As<IAnimalDeathSignal>();

            // Rules / services.
            builder.Register<FoodChainResolver>(Lifetime.Singleton);
            builder.Register<SpawnPlanner>(_ => new SpawnPlanner(weights, in spawnTuning), Lifetime.Singleton);
            builder.Register<AnimalFactory>(c => new AnimalFactory(_animalPrefab, specs, _config.MaxPopulation,
                _config.PredatorFloor, c.Resolve<ISpawnSequence>(), _animalsRoot), Lifetime.Singleton);
            builder.Register<DeathCounters>(Lifetime.Singleton);

            // uGUI view (the scene component, bound under IHudView — all the presenter injects).
            builder.RegisterComponent<IHudView>(_hudView);

            // Entry points — the dispatcher is auto-registered by the first RegisterEntryPoint and then
            // drives every service exposed as a marker interface, factory lambda included.
            builder.RegisterEntryPoint<HudPresenter>(Lifetime.Singleton);
            builder.RegisterEntryPoint<Simulation>(c => new Simulation(c.Resolve<IClock>(), c.Resolve<IRandom>(),
                in bounds, in simTuning, c.Resolve<FoodChainResolver>(), c.Resolve<AnimalDeathSignal>(),
                c.Resolve<AnimalFactory>()), Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<Spawner>(c => new Spawner(c.Resolve<SpawnPlanner>(),
                c.Resolve<AnimalFactory>(), c.Resolve<Simulation>(), c.Resolve<IOccupancyQuery>(),
                c.Resolve<IRandom>(), in bounds), Lifetime.Singleton);
        }
    }
}
