#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using ZooWorld.Animals;
using ZooWorld.Core;

namespace ZooWorld.Spawning
{
    /// <summary>
    /// The spawn-cadence loop: a VContainer <see cref="IAsyncStartable"/> that, every random interval, asks
    /// the pure <see cref="SpawnPlanner"/> for a species (cap + predator floor) and a clear in-bounds
    /// position, then takes an animal from the <see cref="AnimalFactory"/> pool and hands it to the
    /// <see cref="Simulation"/> to register. It owns no spawn formulas (those are the planner's) and no
    /// timing rule (the interval is the planner's; UniTask only awaits it).
    /// </summary>
    public sealed class Spawner : IAsyncStartable
    {
        private readonly SpawnPlanner _planner;
        private readonly AnimalFactory _factory;
        private readonly Simulation _simulation;
        private readonly IOccupancyQuery _occupancy;
        private readonly IRandom _random;
        private readonly FieldBounds _bounds;

        public Spawner(SpawnPlanner planner, AnimalFactory factory, Simulation simulation,
            IOccupancyQuery occupancy, IRandom random, in FieldBounds bounds)
        {
            _planner = planner;
            _factory = factory;
            _simulation = simulation;
            _occupancy = occupancy;
            _random = random;
            _bounds = bounds;
        }

        /// <summary>
        /// Runs the cadence until the scope-tied <paramref name="cancellation"/> fires (on scope teardown).
        /// The delay rides UniTask; on cancellation the await returns canceled and the loop exits cleanly —
        /// no leaked task, no spawn after teardown.
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                float interval = _planner.NextInterval(_random);
                bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: cancellation)
                    .SuppressCancellationThrow();
                if (canceled)
                {
                    return;
                }

                int? index = _planner.SelectSpeciesIndex(_simulation.Population, _random);
                if (index == null)
                {
                    // At the cap with the predator floor satisfied — pause this tick.
                    continue;
                }

                if (!_planner.TryFindSpawnPosition(in _bounds, _occupancy, _random, out Vector3 position))
                {
                    // No clear placement within the attempt budget — skip this tick (no force-place).
                    continue;
                }

                Animal? animal = _factory.Spawn(index.Value, position);
                if (animal != null)
                {
                    _simulation.Register(animal);
                }
            }
        }
    }
}
