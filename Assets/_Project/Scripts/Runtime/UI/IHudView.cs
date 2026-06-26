#nullable enable

namespace ZooWorld.UI
{
    /// <summary>
    /// The HUD view seam: the presenter pushes formatted counter strings here, decoupled from the
    /// concrete uGUI widget (the <c>HudView</c> MonoBehaviour, T08). Keeps <see cref="HudPresenter"/>
    /// headless and unit-testable against a fake view.
    /// </summary>
    public interface IHudView
    {
        /// <summary>Sets the dead-prey counter label text.</summary>
        void SetDeadPrey(string text);

        /// <summary>Sets the dead-predators counter label text.</summary>
        void SetDeadPredators(string text);
    }
}
