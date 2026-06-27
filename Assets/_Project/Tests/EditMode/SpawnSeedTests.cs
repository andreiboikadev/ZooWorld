#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;
using ZooWorld.Core;
using ZooWorld.Tests.EditMode.Fakes;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure spawn-state seeding (<see cref="SpawnSeed.Apply"/>): a non-zero unit heading + no frame-1
    /// leap/reroll. The angle is drawn as 0 so the heading is the clean exact <c>(1,0,0)</c> (a trig-noisy
    /// heading would defeat an exact <c>Vector3</c> assert — NUnit's <c>Is.EqualTo(Vector3)</c> is
    /// component-exact and <c>.Within</c> does not apply to a whole vector).
    /// </summary>
    public sealed class SpawnSeedTests
    {
        private static MovementTuning Tuning()
        {
            return new MovementTuning(2.5f, 1.5f, 1.5f, 4f, 0.8f, 1.5f);
        }

        [Test]
        public void Apply_SeedsNonZeroHeading()
        {
            MovementState state = default;

            SpawnSeed.Apply(ref state, new FakeClock(5f), new FakeRandom(0f, 0.5f), Tuning());

            Assert.That(state.Heading, Is.EqualTo(new Vector3(1f, 0f, 0f)));
            Assert.That(state.Heading.magnitude, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(state.Heading, Is.Not.EqualTo(Vector3.zero));
        }

        [Test]
        public void Apply_SeedsNextLeapTime_NoFrameOneLeap()
        {
            MovementState state = default;
            FakeClock clock = new FakeClock(5f);

            SpawnSeed.Apply(ref state, clock, new FakeRandom(0f, 0.5f), Tuning());

            Assert.That(state.NextLeapTime, Is.EqualTo(6.5f));
            Assert.That(state.NextLeapTime, Is.GreaterThan(clock.Now));
        }

        [Test]
        public void Apply_SeedsNextHeadingReroll()
        {
            MovementState state = default;

            SpawnSeed.Apply(ref state, new FakeClock(5f), new FakeRandom(0f, 0.5f), Tuning());

            Assert.That(state.NextHeadingReroll, Is.EqualTo(6.15f).Within(1e-4f));
        }

        [Test]
        public void Apply_LeavesGraceZero()
        {
            MovementState state = default;

            SpawnSeed.Apply(ref state, new FakeClock(5f), new FakeRandom(0f, 0.5f), Tuning());

            Assert.That(state.GraceUntil, Is.EqualTo(0f));
        }
    }
}
