#nullable enable

using System.Collections.Generic;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode.Fakes
{
    /// <summary>
    /// Scripted <see cref="IRandom"/> — dequeues queued draws in order so a test pins exact values.
    /// <see cref="Range(float,float)"/> derives from <see cref="Value01"/>; integer ranges have their
    /// own queue. An exhausted float queue yields 0; an exhausted int queue yields the lower bound.
    /// </summary>
    public sealed class FakeRandom : IRandom
    {
        private readonly Queue<float> _value01;
        private readonly Queue<int> _intRange;

        /// <summary>Creates a generator that returns <paramref name="value01Sequence"/> from <see cref="Value01"/>.</summary>
        public FakeRandom(params float[] value01Sequence)
        {
            _value01 = new Queue<float>(value01Sequence);
            _intRange = new Queue<int>();
        }

        /// <summary>Queues more <see cref="Value01"/> draws; returns <c>this</c> for chaining.</summary>
        public FakeRandom EnqueueValue01(params float[] values)
        {
            foreach (float v in values)
            {
                _value01.Enqueue(v);
            }

            return this;
        }

        /// <summary>Queues integer <see cref="Range(int,int)"/> results; returns <c>this</c> for chaining.</summary>
        public FakeRandom EnqueueIntRange(params int[] values)
        {
            foreach (int v in values)
            {
                _intRange.Enqueue(v);
            }

            return this;
        }

        /// <inheritdoc/>
        public float Value01()
        {
            return _value01.Count > 0 ? _value01.Dequeue() : 0f;
        }

        /// <inheritdoc/>
        public float Range(float a, float b)
        {
            return a + (Value01() * (b - a));
        }

        /// <inheritdoc/>
        public int Range(int a, int b)
        {
            return _intRange.Count > 0 ? _intRange.Dequeue() : a;
        }
    }
}
