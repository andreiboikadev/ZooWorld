#nullable enable

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using ZooWorld.Config;
using ZooWorld.Core;
using ZooWorld.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// Allocation guard for the <see cref="DeathCounters"/> counting path (guardrails §14 — "zero-alloc").
    /// Isolated in its own file because <c>Is.Not.AllocatingGCMemory()</c> needs the
    /// <c>UnityEngine.TestTools.Constraints</c> namespace import (the constraint is an extension method) plus
    /// an <c>Is</c> alias — which would shadow NUnit's <c>Is</c> used by the other counter tests.
    /// </summary>
    public sealed class DeathCountersAllocationTests
    {
        [Test]
        public void OnAnimalDied_DoesNotAllocate()
        {
            var signal = new AnimalDeathSignal();

            // Counters subscribed, but nothing subscribes to Changed (a formatting presenter would
            // allocate) — this measures DeathCounters in isolation.
            _ = new DeathCounters(signal);

            // Warm the path once so first-call JIT does not register as an allocation.
            signal.Raise(new AnimalDied(Role.Prey, Vector3.zero));

            Assert.That(
                () => signal.Raise(new AnimalDied(Role.Prey, Vector3.zero)),
                Is.Not.AllocatingGCMemory());
        }
    }
}
