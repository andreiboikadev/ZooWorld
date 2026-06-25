#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>The production spawn sequence is strictly increasing and never repeats.</summary>
    public sealed class MonotonicSpawnSequenceTests
    {
        [Test]
        public void Next_FirstId_IsOne()
        {
            MonotonicSpawnSequence seq = new MonotonicSpawnSequence();

            Assert.That(seq.Next(), Is.EqualTo(1L));
        }

        [Test]
        public void Next_OverManyCalls_StrictlyIncreasesAndNeverRepeats()
        {
            MonotonicSpawnSequence seq = new MonotonicSpawnSequence();
            HashSet<long> seen = new HashSet<long>();
            long previous = long.MinValue;

            for (int i = 0; i < 1000; i++)
            {
                long id = seq.Next();
                Assert.That(id, Is.GreaterThan(previous), "ids must strictly increase");
                Assert.That(seen.Add(id), Is.True, "ids must never repeat");
                previous = id;
            }
        }
    }
}
