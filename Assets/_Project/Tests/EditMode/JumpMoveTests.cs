#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Core;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>Jump bursts on schedule, idles between leaps, and defers a leap during grace.</summary>
    public sealed class JumpMoveTests
    {
        private static MovementTuning Tuning()
        {
            return new MovementTuning(speed: 0f, jumpDistance: 1.5f, jumpInterval: 1.5f, linearDamping: 4f,
                wanderRerollMin: 0f, wanderRerollMax: 0f);
        }

        [Test]
        public void LeapDue_ReturnsBurst_SchedulesNext()
        {
            JumpMove move = ScriptableObject.CreateInstance<JumpMove>();
            FakeRandom rng = new FakeRandom(0f); // angle 0 → heading (1,0,0)
            MoveContext ctx = new MoveContext(0.02f, rng, default, new FakeClock(now: 0f));
            MovementTuning tuning = Tuning();
            MovementState state = default; // NextLeapTime 0, GraceUntil 0

            Vector3 velocity = move.Tick(ref state, in ctx, in tuning);

            Assert.That(velocity.magnitude, Is.EqualTo(6f).Within(1e-4f)); // BurstSpeed(1.5,4)
            Assert.That(state.NextLeapTime, Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(state.Heading.x, Is.EqualTo(1f).Within(1e-5f));

            Object.DestroyImmediate(move);
        }

        [Test]
        public void BetweenLeaps_ReturnsZero()
        {
            JumpMove move = ScriptableObject.CreateInstance<JumpMove>();
            MoveContext ctx = new MoveContext(0.02f, new FakeRandom(), default, new FakeClock(now: 0.5f));
            MovementTuning tuning = Tuning();
            MovementState state = default;
            state.NextLeapTime = 1.5f;

            Vector3 velocity = move.Tick(ref state, in ctx, in tuning);

            Assert.That(velocity, Is.EqualTo(Vector3.zero));
            Assert.That(state.NextLeapTime, Is.EqualTo(1.5f)); // untouched

            Object.DestroyImmediate(move);
        }

        [Test]
        public void DuringGrace_Defers_ThenFiresAfter()
        {
            JumpMove move = ScriptableObject.CreateInstance<JumpMove>();
            FakeRandom rng = new FakeRandom(0f); // consumed only when the leap finally fires
            FakeClock clock = new FakeClock(now: 0.3f);
            MoveContext ctx = new MoveContext(0.02f, rng, default, clock);
            MovementTuning tuning = Tuning();
            MovementState state = default;
            state.NextLeapTime = 0f;
            state.GraceUntil = 0.6f;

            // Inside grace (0.3 < 0.6): defer, do not advance the leap timer.
            Vector3 deferred = move.Tick(ref state, in ctx, in tuning);
            Assert.That(deferred, Is.EqualTo(Vector3.zero));
            Assert.That(state.NextLeapTime, Is.EqualTo(0f));

            // Grace expired (0.6): the deferred leap fires.
            clock.Now = 0.6f;
            Vector3 fired = move.Tick(ref state, in ctx, in tuning);
            Assert.That(fired.magnitude, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(state.NextLeapTime, Is.EqualTo(2.1f).Within(1e-5f));

            Object.DestroyImmediate(move);
        }
    }
}
