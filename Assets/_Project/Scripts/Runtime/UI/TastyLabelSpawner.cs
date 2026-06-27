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
    /// Subscribes the "Tasty!" label to the <see cref="IPredatorAteSignal"/> Observer channel and pools the
    /// labels (GDD §8; guardrails §9/§11). On each eat it takes a label, shows it at the predator's
    /// position, and runs the UniTask rise/fade lerp, returning it to the pool on completion. Prewarmed to
    /// <see cref="SimConfig.TastyPoolSize"/>; grows by one when exhausted (never pops an empty stack).
    /// Edit-mode-safe teardown — destroys instances like <c>AnimalFactory.Dispose</c>.
    /// </summary>
    public sealed class TastyLabelSpawner : IStartable, IDisposable
    {
        private readonly IPredatorAteSignal _signal;
        private readonly TastyLabel _prefab;
        private readonly SimConfig _config;
        private readonly Transform? _parent;
        private readonly Stack<TastyLabel> _pool = new Stack<TastyLabel>();
        private readonly List<TastyLabel> _all = new List<TastyLabel>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public TastyLabelSpawner(IPredatorAteSignal signal, TastyLabel prefab, SimConfig config, Transform? parent)
        {
            _signal = signal;
            _prefab = prefab;
            _config = config;
            _parent = parent;
        }

        /// <summary>Prewarms the pool and subscribes (the VContainer entry point — guardrails §9/§13).</summary>
        public void Start()
        {
            for (int i = 0; i < _config.TastyPoolSize; i++)
            {
                _pool.Push(Create());
            }

            _signal.Ate += OnAte;
        }

        /// <summary>Unsubscribes, cancels in-flight lerps, and destroys every pooled instance (edit-mode-safe).</summary>
        public void Dispose()
        {
            _signal.Ate -= OnAte;
            _cts.Cancel();
            _cts.Dispose();

            foreach (TastyLabel label in _all)
            {
                if (label != null)
                {
                    DestroyInstance(label.gameObject);
                }
            }

            _all.Clear();
            _pool.Clear();
        }

        private void OnAte(in PredatorAte e)
        {
            TastyLabel label = _pool.Count > 0 ? _pool.Pop() : Create();
            label.gameObject.SetActive(true);
            label.Show(e.Position);
            AnimateAsync(label).Forget();
        }

        private async UniTaskVoid AnimateAsync(TastyLabel label)
        {
            await FeedbackLerp.RunAsync(_config.TastyLifetime, _config.RiseEase, label.ApplyProgress, _cts.Token);
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            label.gameObject.SetActive(false);
            _pool.Push(label);
        }

        private TastyLabel Create()
        {
            TastyLabel label = UnityEngine.Object.Instantiate(_prefab, _parent);
            label.gameObject.SetActive(false);
            _all.Add(label);
            return label;
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
