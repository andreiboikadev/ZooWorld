#nullable enable

using UnityEngine;

namespace ZooWorld.Config
{
    /// <summary>
    /// Designer-authored, runtime-read-only definition of a species — the data half of an
    /// <c>Animal</c> (composition over inheritance). A species that reuses an existing movement
    /// strategy is pure data: author one of these and add it to the <see cref="AnimalCatalog"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Zoo World/Animal Definition", fileName = "AnimalDefinition")]
    public sealed class AnimalDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private Role _role = Role.Prey;
        [Tooltip("Predator-vs-predator winner key (higher wins; tie → lower spawn id survives).")]
        [SerializeField] private int _strength;

        [Header("Movement")]
        [Tooltip("Stateless movement strategy asset (Wander/Jump/Linear). Left null until T02.")]
        [SerializeField] private MovementBehaviour? _movement;
        [Tooltip("Relative weight in the spawn lottery.")]
        [Min(0f)]
        [SerializeField] private float _spawnWeight = 1f;

        [Header("Visuals (placeholder primitives)")]
        [SerializeField] private Color _color = Color.white;
        [Tooltip("Body diameter in metres; also the spawn-clearance basis.")]
        [Min(0f)]
        [SerializeField] private float _size = 1f;
        [Min(0f)]
        [SerializeField] private float _mass = 1f;

        [Header("Tuning")]
        [Tooltip("Cruise speed in m/s (wander/linear).")]
        [Min(0f)]
        [SerializeField] private float _speed = 2.5f;
        [Tooltip("Nominal leap distance in metres (jump strategies).")]
        [Min(0f)]
        [SerializeField] private float _jumpDistance = 1.5f;
        [Tooltip("Seconds between leaps (jump strategies).")]
        [Min(0f)]
        [SerializeField] private float _jumpInterval = 1.5f;

        /// <summary>Stable identifier, e.g. "frog".</summary>
        public string Id => _id;

        /// <summary>Human-readable display name.</summary>
        public string DisplayName => _displayName;

        /// <summary>Predation role.</summary>
        public Role Role => _role;

        /// <summary>Predator-vs-predator winner key (higher wins).</summary>
        public int Strength => _strength;

        /// <summary>The movement strategy this species uses (null until assigned in T02).</summary>
        public MovementBehaviour? Movement => _movement;

        /// <summary>Relative weight in the spawn lottery.</summary>
        public float SpawnWeight => _spawnWeight;

        /// <summary>Body colour (placeholder visual).</summary>
        public Color Color => _color;

        /// <summary>Body diameter in metres; also the spawn-clearance basis.</summary>
        public float Size => _size;

        /// <summary>Rigidbody mass in kilograms.</summary>
        public float Mass => _mass;

        /// <summary>Cruise speed in m/s.</summary>
        public float Speed => _speed;

        /// <summary>Nominal leap distance in metres.</summary>
        public float JumpDistance => _jumpDistance;

        /// <summary>Seconds between leaps.</summary>
        public float JumpInterval => _jumpInterval;
    }
}
