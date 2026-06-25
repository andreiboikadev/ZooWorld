#nullable enable

using UnityEngine;

namespace ZooWorld.Core
{
    /// <summary>
    /// Spatial-clearance seam for spawn placement — abstracts <c>Physics.CheckSphere</c> so the
    /// spawn planner is testable against a stub (list-of-circles / always-clear / always-blocked).
    /// </summary>
    public interface IOccupancyQuery
    {
        /// <summary>
        /// True if no active animal overlaps a sphere of <paramref name="radius"/> centred at
        /// <paramref name="position"/>.
        /// </summary>
        bool IsClear(Vector3 position, float radius);
    }
}
