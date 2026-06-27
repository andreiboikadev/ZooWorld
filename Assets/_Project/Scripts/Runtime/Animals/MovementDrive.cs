#nullable enable

using UnityEngine;

namespace ZooWorld.Animals
{
    /// <summary>
    /// Pure per-tick velocity decision — encodes the whole grace × bounds × coast × impulse interaction so
    /// the <c>Simulation</c> only executes the returned <see cref="DriveCommand"/>. Out-of-bounds overrides
    /// grace (staying on-screen outranks the bounce); an idle jumper or an in-bounds grace tick coasts
    /// (never a zeroed velocity — the T02 §5 freeze bug).
    /// </summary>
    public static class MovementDrive
    {
        /// <summary>
        /// Decides how to drive the body this tick from the strategy's <paramref name="desired"/> velocity,
        /// the grace window (<paramref name="now"/> vs <paramref name="graceUntil"/>), and the bounds state
        /// (<paramref name="isWithinInner"/> + the toward-centre <paramref name="steerToCenter"/>).
        /// </summary>
        public static DriveCommand Decide(Vector3 desired, bool isImpulseDriven, float now, float graceUntil,
            bool isWithinInner, Vector3 steerToCenter)
        {
            if (!isWithinInner)
            {
                if (desired == Vector3.zero)
                {
                    return DriveCommand.None;
                }

                Vector3 returnVelocity = steerToCenter * desired.magnitude;
                return isImpulseDriven
                    ? new DriveCommand(DriveMode.Impulse, returnVelocity)
                    : new DriveCommand(DriveMode.SetVelocity, returnVelocity);
            }

            if (now < graceUntil)
            {
                return DriveCommand.None;
            }

            if (desired == Vector3.zero)
            {
                return DriveCommand.None;
            }

            return isImpulseDriven
                ? new DriveCommand(DriveMode.Impulse, desired)
                : new DriveCommand(DriveMode.SetVelocity, desired);
        }
    }
}
