#nullable enable

using System.Collections.Generic;
using UnityEngine;
using ZooWorld.Core;

namespace ZooWorld.Tests.EditMode.Fakes
{
    /// <summary>
    /// Stub <see cref="IOccupancyQuery"/> — built via the static factories as always-clear,
    /// always-blocked, or a set of occupied circles (a query is blocked when its sphere overlaps any
    /// circle on XZ). Lets the spawn planner be tested without Unity physics.
    /// </summary>
    public sealed class FakeOccupancy : IOccupancyQuery
    {
        private readonly bool _defaultClear;
        private readonly List<(Vector3 center, float radius)> _circles;

        private FakeOccupancy(bool defaultClear, List<(Vector3 center, float radius)> circles)
        {
            _defaultClear = defaultClear;
            _circles = circles;
        }

        /// <summary>Every query is clear.</summary>
        public static FakeOccupancy AlwaysClear()
        {
            return new FakeOccupancy(true, new List<(Vector3 center, float radius)>());
        }

        /// <summary>Every query is blocked.</summary>
        public static FakeOccupancy AlwaysBlocked()
        {
            return new FakeOccupancy(false, new List<(Vector3 center, float radius)>());
        }

        /// <summary>Blocked only where a query sphere overlaps one of <paramref name="circles"/>.</summary>
        public static FakeOccupancy WithCircles(params (Vector3 center, float radius)[] circles)
        {
            return new FakeOccupancy(true, new List<(Vector3 center, float radius)>(circles));
        }

        /// <inheritdoc/>
        public bool IsClear(Vector3 position, float radius)
        {
            if (_circles.Count == 0)
            {
                return _defaultClear;
            }

            foreach ((Vector3 center, float radius) circle in _circles)
            {
                float minSeparation = radius + circle.radius;
                Vector3 delta = position - circle.center;
                delta.y = 0f;
                if (delta.sqrMagnitude < minSeparation * minSeparation)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
