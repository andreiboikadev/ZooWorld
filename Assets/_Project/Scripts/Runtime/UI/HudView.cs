#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace ZooWorld.UI
{
    /// <summary>
    /// The uGUI <see cref="IHudView"/>: two <see cref="Text"/> labels (top-right, GDD §8) the
    /// <see cref="HudPresenter"/> pushes already-formatted counter strings to. A dumb view — no formatting,
    /// no polling, no game logic.
    /// </summary>
    public sealed class HudView : MonoBehaviour, IHudView
    {
        [SerializeField] private Text _deadPreyLabel = null!;
        [SerializeField] private Text _deadPredatorsLabel = null!;

        /// <inheritdoc/>
        public void SetDeadPrey(string text)
        {
            _deadPreyLabel.text = text;
        }

        /// <inheritdoc/>
        public void SetDeadPredators(string text)
        {
            _deadPredatorsLabel.text = text;
        }
    }
}
