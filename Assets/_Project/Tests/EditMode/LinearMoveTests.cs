#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Core;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>Linear cruises straight at species speed and never re-rolls its heading.</summary>
    public sealed class LinearMoveTests
    {
        private static MovementTuning Tuning()
        {
            return new MovementTuning(speed: 2.5f, jumpDistance: 0f, jumpInterval: 0f, linearDamping: 4f,
                wanderRerollMin: 0f, wanderRerollMax: 0f);
        }

        [Test]
        public void ConstantVelocityAlongHeading_NoReroll()
        {
            LinearMove move = ScriptableObject.CreateInstance<LinearMove>();
            MoveContext ctx = new MoveContext(0.02f, new FakeRandom(), default, new FakeClock(now: 0f));
            MovementTuning tuning = Tuning();
            MovementState state = default;
            state.Heading = new Vector3(1f, 0f, 0f);
            state.NextHeadingReroll = 0.8f;
            state.NextLeapTime = 0.5f;

            Vector3 v1 = move.Tick(ref state, in ctx, in tuning);
            Vector3 v2 = move.Tick(ref state, in ctx, in tuning);

            Assert.That(v1, Is.EqualTo(new Vector3(2.5f, 0f, 0f)));
            Assert.That(v2, Is.EqualTo(new Vector3(2.5f, 0f, 0f)));
            Assert.That(state.NextHeadingReroll, Is.EqualTo(0.8f)); // untouched
            Assert.That(state.NextLeapTime, Is.EqualTo(0.5f));      // untouched

            Object.DestroyImmediate(move);
        }

        [Test]
        public void DefaultHeading_ReturnsZero_FrozenSnakeTripwire()
        {
            LinearMove move = ScriptableObject.CreateInstance<LinearMove>();
            MoveContext ctx = new MoveContext(0.02f, new FakeRandom(), default, new FakeClock(now: 0f));
            MovementTuning tuning = Tuning();
            MovementState state = default; // Heading == (0,0,0)

            Vector3 velocity = move.Tick(ref state, in ctx, in tuning);

            // Pins the spawn-seed precondition: an unseeded heading freezes the body.
            Assert.That(velocity, Is.EqualTo(Vector3.zero));

            Object.DestroyImmediate(move);
        }
    }
}
