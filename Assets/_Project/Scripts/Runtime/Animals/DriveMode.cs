#nullable enable

namespace ZooWorld.Animals
{
    /// <summary>How the <c>Simulation</c> applies a tick's <see cref="DriveCommand"/> to the Rigidbody.</summary>
    public enum DriveMode
    {
        /// <summary>Leave the body coasting under linear damping — never zero its velocity (T02 §5).</summary>
        None,

        /// <summary>Set <c>rb.linearVelocity</c> directly (continuous movers).</summary>
        SetVelocity,

        /// <summary>Apply <c>rb.AddForce(v, ForceMode.VelocityChange)</c> (the jump burst).</summary>
        Impulse
    }
}
