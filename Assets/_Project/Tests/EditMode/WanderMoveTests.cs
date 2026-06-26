#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Core;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>Wander re-rolls its heading on the schedule and cruises at species speed.</summary>
    public sealed class WanderMoveTests
    {
        private static MovementTuning Tuning()
        {
            return new MovementTuning(speed: 2f, jumpDistance: 0f, jumpInterval: 0f, linearDamping: 4f,
                wanderRerollMin: 0.8f, wanderRerollMax: 1.5f);
        }

        [Test]
        public void FirstTick_RerollsHeading_SchedulesNext_ReturnsSpeed()
        {
            WanderMove move = ScriptableObject.CreateInstance<WanderMove>();
            // Draw order per Tick: angle (RandomXz) then interval (Range). Value01 0 → angle 0 → (1,0,0); 0 → 0.8.
            FakeRandom rng = new FakeRandom(0f, 0f);
            MoveContext ctx = new MoveContext(0.02f, rng, default, new FakeClock(now: 0f));
            MovementTuning tuning = Tuning();
            MovementState state = default;

            Vector3 velocity = move.Tick(ref state, in ctx, in tuning);

            Assert.That(state.Heading.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(state.Heading.z, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(state.NextHeadingReroll, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(velocity.magnitude, Is.EqualTo(2f).Within(1e-5f));

            Object.DestroyImmediate(move);
        }

        [Test]
        public void WithinInterval_DoesNotReroll()
        {
            WanderMove move = ScriptableObject.CreateInstance<WanderMove>();
            FakeRandom rng = new FakeRandom(); // no draws expected
            MoveContext ctx = new MoveContext(0.02f, rng, default, new FakeClock(now: 0.5f));
            MovementTuning tuning = Tuning();
            MovementState state = default;
            state.Heading = new Vector3(1f, 0f, 0f);
            state.NextHeadingReroll = 0.8f;

            Vector3 velocity = move.Tick(ref state, in ctx, in tuning);

            Assert.That(state.Heading, Is.EqualTo(new Vector3(1f, 0f, 0f)));
            Assert.That(state.NextHeadingReroll, Is.EqualTo(0.8f));
            Assert.That(velocity.magnitude, Is.EqualTo(2f).Within(1e-5f));

            Object.DestroyImmediate(move);
        }

        [Test]
        public void AfterInterval_RerollsAgain()
        {
            WanderMove move = ScriptableObject.CreateInstance<WanderMove>();
            // angle Value01 0.25 → π/2 → (0,0,1); interval 0 → 0.8.
            FakeRandom rng = new FakeRandom(0.25f, 0f);
            MoveContext ctx = new MoveContext(0.02f, rng, default, new FakeClock(now: 1f));
            MovementTuning tuning = Tuning();
            MovementState state = default;
            state.Heading = new Vector3(1f, 0f, 0f);
            state.NextHeadingReroll = 0.8f;

            move.Tick(ref state, in ctx, in tuning);

            Assert.That(state.Heading.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(state.Heading.z, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(state.NextHeadingReroll, Is.EqualTo(1.8f).Within(1e-5f));

            Object.DestroyImmediate(move);
        }
    }
}
