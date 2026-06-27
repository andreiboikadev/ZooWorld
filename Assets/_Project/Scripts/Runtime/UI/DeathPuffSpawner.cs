#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.UI
{
    /// <summary>
    /// The second <see cref="IAnimalDeathSignal"/> subscriber (T09): a pooled translucent "death puff" at
    /// each victim's position. Mirrors <see cref="TastyLabelSpawner"/> — a prewarmed pool, a UniTask
    /// scale/fade lerp, grow-on-exhaustion, and edit-mode-safe teardown. A separate pooled effect because
    /// the dying animal returns to its own pool at once (guardrails §9).
    /// </summary>
    public sealed class DeathPuffSpawner : IStartable, IDisposable
    {
        private const int PrewarmCount = 8;

        private readonly IAnimalDeathSignal _signal;
        private readonly DeathPuff _prefab;
        private readonly SimConfig _config;
        private readonly Transform? _parent;
        private readonly Stack<DeathPuff> _pool = new Stack<DeathPuff>();
        private readonly List<DeathPuff> _all = new List<DeathPuff>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public DeathPuffSpawner(IAnimalDeathSignal signal, DeathPuff prefab, SimConfig config, Transform? parent)
        {
            _signal = signal;
            _prefab = prefab;
            _config = config;
            _parent = parent;
        }

        /// <summary>Prewarms the pool and subscribes (the VContainer entry point — guardrails §9/§13).</summary>
        public void Start()
        {
            for (int i = 0; i < PrewarmCount; i++)
            {
                _pool.Push(Create());
            }

            _signal.Died += OnDied;
        }

        /// <summary>Unsubscribes, cancels in-flight lerps, and destroys every pooled instance (edit-mode-safe).</summary>
        public void Dispose()
        {
            _signal.Died -= OnDied;
            _cts.Cancel();
            _cts.Dispose();

            foreach (DeathPuff puff in _all)
            {
                if (puff != null)
                {
                    DestroyInstance(puff.gameObject);
                }
            }

            _all.Clear();
            _pool.Clear();
        }

        private void OnDied(in AnimalDied e)
        {
            DeathPuff puff = _pool.Count > 0 ? _pool.Pop() : Create();
            puff.gameObject.SetActive(true);
            puff.Show(e.Position);
            AnimateAsync(puff).Forget();
        }

        private async UniTaskVoid AnimateAsync(DeathPuff puff)
        {
            await FeedbackLerp.RunAsync(_config.DeathPuffLifetime, _config.RiseEase, puff.ApplyProgress, _cts.Token);
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            puff.gameObject.SetActive(false);
            _pool.Push(puff);
        }

        private DeathPuff Create()
        {
            DeathPuff puff = UnityEngine.Object.Instantiate(_prefab, _parent);
            puff.gameObject.SetActive(false);
            _all.Add(puff);
            return puff;
        }

        private static void DestroyInstance(GameObject go)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
