#nullable enable

using NUnit.Framework;
using UnityEngine;
using ZooWorld.Animals;

namespace ZooWorld.Tests.EditMode
{
    /// <summary>
    /// The pure per-tick velocity decision (<see cref="MovementDrive.Decide"/>): grace × bounds × coast ×
    /// impulse. All inputs are hand-fed, so the cases are trig-free and assert exact velocities.
    /// </summary>
    public sealed class MovementDriveTests
    {
        [Test]
        public void Continuous_InBounds_NoGrace_SetsDesired()
        {
            DriveCommand command = MovementDrive.Decide(new Vector3(2.5f, 0f, 0f), false, 1f, 0f, true,
                new Vector3(2.5f, 0f, 0f));

            Assert.That(command.Mode, Is.EqualTo(DriveMode.SetVelocity));
            Assert.That(command.Velocity, Is.EqualTo(new Vector3(2.5f, 0f, 0f)));
        }

        [Test]
        public void Continuous_InGrace_Coasts()
        {
            DriveCommand command = MovementDrive.Decide(new Vector3(2.5f, 0f, 0f), false, 1f, 5f, true,
                new Vector3(2.5f, 0f, 0f));

            Assert.That(command.Mode, Is.EqualTo(DriveMode.None));
        }

        [Test]
        public void OutOfBounds_OverridesGrace_ReturnsAtDesiredMagnitude()
        {
            DriveCommand command = MovementDrive.Decide(new Vector3(2.5f, 0f, 0f), false, 1f, 5f, false,
                new Vector3(-1f, 0f, 0f));

            Assert.That(command.Mode, Is.EqualTo(DriveMode.SetVelocity));
            Assert.That(command.Velocity, Is.EqualTo(new Vector3(-2.5f, 0f, 0f)));
        }

        [Test]
        public void Jumper_LaunchInBounds_Impulse()
        {
            DriveCommand command = MovementDrive.Decide(new Vector3(6f, 0f, 0f), true, 2f, 0f, true,
                new Vector3(6f, 0f, 0f));

            Assert.That(command.Mode, Is.EqualTo(DriveMode.Impulse));
            Assert.That(command.Velocity, Is.EqualTo(new Vector3(6f, 0f, 0f)));
        }

        [Test]
        public void Jumper_Idle_Coasts()
        {
            DriveCommand command = MovementDrive.Decide(Vector3.zero, true, 2f, 0f, true, Vector3.zero);

            Assert.That(command.Mode, Is.EqualTo(DriveMode.None));
        }

        [Test]
        public void Jumper_IdleOutOfBounds_Coasts()
        {
            DriveCommand command = MovementDrive.Decide(Vector3.zero, true, 2f, 0f, false,
                new Vector3(-1f, 0f, 0f));

            Assert.That(command.Mode, Is.EqualTo(DriveMode.None));
        }

        [Test]
        public void Jumper_LaunchOutOfBounds_RedirectsBurstToCentre()
        {
            DriveCommand command = MovementDrive.Decide(new Vector3(0f, 0f, 6f), true, 2f, 0f, false,
                new Vector3(-1f, 0f, 0f));

            Assert.That(command.Mode, Is.EqualTo(DriveMode.Impulse));
            Assert.That(command.Velocity, Is.EqualTo(new Vector3(-6f, 0f, 0f)));
        }
    }
}
