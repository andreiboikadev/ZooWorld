#nullable enable

using UnityEngine;

namespace ZooWorld.Animals
{
    /// <summary>
    /// The result of the per-tick velocity decision: an apply <see cref="Mode"/> plus the velocity it
    /// carries. Returned by <see cref="MovementDrive.Decide"/> and executed by the <c>Simulation</c>.
    /// </summary>
    public readonly struct DriveCommand
    {
        public DriveCommand(DriveMode mode, Vector3 velocity)
        {
            Mode = mode;
            Velocity = velocity;
        }

        /// <summary>How to apply <see cref="Velocity"/> (or <see cref="DriveMode.None"/> to coast).</summary>
        public DriveMode Mode { get; }

        /// <summary>The velocity to set or impulse (ignored when <see cref="Mode"/> is <see cref="DriveMode.None"/>).</summary>
        public Vector3 Velocity { get; }

        /// <summary>The "leave the body coasting this tick" command.</summary>
        public static DriveCommand None { get; } = new DriveCommand(DriveMode.None, Vector3.zero);
    }
}
