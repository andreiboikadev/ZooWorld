#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>Each test fake behaves as its production seam would (the shared test infrastructure).</summary>
    public sealed class FakesTests
    {
        [Test]
        public void FakeClock_Advance_MovesNow()
        {
            FakeClock clock = new FakeClock(now: 1f, dt: 0.5f);

            Assert.That(clock.Now, Is.EqualTo(1f));
            Assert.That(clock.Dt, Is.EqualTo(0.5f));

            clock.Advance(2f);
            Assert.That(clock.Now, Is.EqualTo(3f));

            clock.Now = 10f;
            Assert.That(clock.Now, Is.EqualTo(10f));
        }

        [Test]
        public void FakeRandom_Value01_ReturnsScriptedThenZero()
        {
            FakeRandom rng = new FakeRandom(0.25f, 0.75f);

            Assert.That(rng.Value01(), Is.EqualTo(0.25f));
            Assert.That(rng.Value01(), Is.EqualTo(0.75f));
            Assert.That(rng.Value01(), Is.EqualTo(0f));
        }

        [Test]
        public void FakeRandom_FloatRange_DerivesFromValue01()
        {
            FakeRandom rng = new FakeRandom(0.5f);

            Assert.That(rng.Range(0f, 10f), Is.EqualTo(5f));
        }

        [Test]
        public void FakeRandom_IntRange_ReturnsScriptedThenLowerBound()
        {
            FakeRandom rng = new FakeRandom().EnqueueIntRange(2, 7);

            Assert.That(rng.Range(0, 100), Is.EqualTo(2));
            Assert.That(rng.Range(0, 100), Is.EqualTo(7));
            Assert.That(rng.Range(5, 100), Is.EqualTo(5));
        }

        [Test]
        public void FakeSpawnSequence_IncrementsFromStart()
        {
            FakeSpawnSequence seq = new FakeSpawnSequence(start: 10L);

            Assert.That(seq.Next(), Is.EqualTo(10L));
            Assert.That(seq.Next(), Is.EqualTo(11L));
        }

        [Test]
        public void FakeOccupancy_AlwaysClear_IsClearTrue()
        {
            Assert.That(FakeOccupancy.AlwaysClear().IsClear(Vector3.zero, 1f), Is.True);
        }

        [Test]
        public void FakeOccupancy_AlwaysBlocked_IsClearFalse()
        {
            Assert.That(FakeOccupancy.AlwaysBlocked().IsClear(Vector3.zero, 1f), Is.False);
        }

        [Test]
        public void FakeOccupancy_WithCircles_BlocksOverlapClearsElsewhere()
        {
            FakeOccupancy occ = FakeOccupancy.WithCircles((new Vector3(0f, 0f, 0f), 1f));

            Assert.That(occ.IsClear(new Vector3(0.5f, 0f, 0f), 1f), Is.False);
            Assert.That(occ.IsClear(new Vector3(10f, 0f, 0f), 1f), Is.True);
        }
    }
}
