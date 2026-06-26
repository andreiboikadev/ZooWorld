#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// The one-layer create + take/configure + return pool for animals (GDD §4 — the factory IS the pool,
    /// not a factory wrapping one). Per-species (catalog-index) free-lists, prewarmed proportionally to
    /// spawn weight and capped at peak live demand; steady state reuses, never churning Instantiate/Destroy.
    /// A DI scope-singleton (binding is T08).
    /// </summary>
    public sealed class AnimalFactory : IDisposable
    {
        private readonly Animal _prefab;
        private readonly IReadOnlyList<AnimalSpec> _specs;
        private readonly ISpawnSequence _sequence;
        private readonly Transform? _parent;
        private readonly Stack<Animal>[] _pools;
        private readonly List<Animal> _allInstances;
        private readonly int[] _countPerIndex;
        private readonly int _capacity;

        public AnimalFactory(Animal prefab, IReadOnlyList<AnimalSpec> specs, int maxPopulation,
            int predatorFloor, ISpawnSequence sequence, Transform? parent)
        {
            _prefab = prefab;
            _specs = specs;
            _sequence = sequence;
            _parent = parent;
            _capacity = maxPopulation + predatorFloor;
            _pools = new Stack<Animal>[specs.Count];
            _allInstances = new List<Animal>();
            _countPerIndex = new int[specs.Count];

            for (int i = 0; i < specs.Count; i++)
            {
                var pool = new Stack<Animal>();
                int prewarm = Mathf.RoundToInt(specs[i].SpawnWeight * maxPopulation);
                for (int n = 0; n < prewarm; n++)
                {
                    pool.Push(CreatePooled(i));
                }

                _pools[i] = pool;
            }
        }

        /// <summary>Total <see cref="Animal"/> instances ever created — a no-runtime-churn probe for tests.</summary>
        public int InstantiatedCount => _allInstances.Count;

        /// <summary>
        /// Takes an animal of species <paramref name="index"/> from its pool (or grows the pool by one,
        /// bounded by the cap), configures it at <paramref name="position"/>, and returns it active.
        /// Returns <c>null</c> if the pool is exhausted at its cap (defensive — the spawn planner gates the
        /// live count upstream, so this should not occur in the wired sim).
        /// </summary>
        public Animal? Spawn(int index, Vector3 position)
        {
            Stack<Animal> pool = _pools[index];
            Animal animal;
            if (pool.Count > 0)
            {
                animal = pool.Pop();
            }
            else if (_countPerIndex[index] < _capacity)
            {
                animal = CreatePooled(index);
            }
            else
            {
                return null;
            }

            animal.gameObject.SetActive(true);
            animal.OnSpawn(_specs[index], _sequence.Next(), position);
            return animal;
        }

        /// <summary>Returns <paramref name="animal"/> to its species pool (reset + deactivated, never destroyed).</summary>
        public void Despawn(Animal animal)
        {
            animal.OnDespawn();
            animal.gameObject.SetActive(false);
            _pools[animal.PoolIndex].Push(animal);
        }

        /// <summary>
        /// Destroys every pooled and active instance. Edit-mode-safe: the EditMode test teardown calls this,
        /// and <c>Object.Destroy</c> is forbidden in edit mode, so it branches on play state.
        /// </summary>
        public void Dispose()
        {
            foreach (Animal animal in _allInstances)
            {
                DestroyInstance(animal);
            }

            _allInstances.Clear();
            for (int i = 0; i < _pools.Length; i++)
            {
                _pools[i].Clear();
            }

            Array.Clear(_countPerIndex, 0, _countPerIndex.Length);
        }

        /// <summary>The per-index pool cap (= MaxPopulation + PredatorFloor).</summary>
        public int Capacity(int index)
        {
            return _capacity;
        }

        /// <summary>The number of idle (pooled) instances for species <paramref name="index"/>.</summary>
        public int FreeCount(int index)
        {
            return _pools[index].Count;
        }

        private Animal CreatePooled(int index)
        {
            Animal animal = UnityEngine.Object.Instantiate(_prefab, _parent);
            animal.PoolIndex = index;
            animal.OnDespawn();
            animal.gameObject.SetActive(false);
            _allInstances.Add(animal);
            _countPerIndex[index]++;
            return animal;
        }

        private static void DestroyInstance(Animal animal)
        {
            if (animal == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(animal.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(animal.gameObject);
            }
        }
    }
}
