#nullable enable

namespace ZooWorld.Core
{
    /// <summary>
    /// Time seam for pure rules — abstracts <c>UnityEngine.Time</c> so movement and grace-window
    /// logic is headless-testable against a settable fake clock.
    /// </summary>
    public interface IClock
    {
        /// <summary>Seconds since start (production: <c>Time.time</c>).</summary>
        float Now { get; }

        /// <summary>Fixed step length in seconds (production: <c>Time.fixedDeltaTime</c>).</summary>
        float Dt { get; }
    }
}
