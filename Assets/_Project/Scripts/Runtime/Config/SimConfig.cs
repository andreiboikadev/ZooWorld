#nullable enable

using UnityEngine;

namespace ZooWorld.Config
{
    /// <summary>
    /// Global simulation tunables (game-design.md §9), authored once and read-only at runtime.
    /// Pool prewarm is intentionally NOT stored: it is derived per definition
    /// (round(spawnWeight × maxPopulation)) so adding a species needs no pool re-tuning.
    /// </summary>
    [CreateAssetMenu(menuName = "Zoo World/Sim Config", fileName = "SimConfig")]
    public sealed class SimConfig : ScriptableObject
    {
        [Header("Spawning")]
        [Tooltip("Lower bound of the random spawn interval, seconds (brief: 1–2 s).")]
        [Min(0f)]
        [SerializeField] private float _spawnIntervalMin = 1f;
        [Tooltip("Upper bound of the random spawn interval, seconds (brief: 1–2 s).")]
        [Min(0f)]
        [SerializeField] private float _spawnIntervalMax = 2f;
        [Tooltip("Population safeguard: spawner pauses at this live count.")]
        [Min(1)]
        [SerializeField] private int _maxPopulation = 120;
        [Tooltip("Minimum predators kept on screen (anti-deadlock floor).")]
        [Min(0)]
        [SerializeField] private int _predatorFloor = 1;
        [Tooltip("Placement clearance query-sphere radius (m); effective centre separation = this + body radius.")]
        [Min(0f)]
        [SerializeField] private float _clearanceRadius = 1f;
        [Tooltip("Placement attempts before skipping a spawn tick.")]
        [Min(1)]
        [SerializeField] private int _maxPlacementAttempts = 10;

        [Header("Movement & physics")]
        [Tooltip("Post-collision control grace window, seconds (blend-back).")]
        [Min(0f)]
        [SerializeField] private float _graceSeconds = 0.6f;
        [Tooltip("Prey×prey separation kick, m/s.")]
        [Min(0f)]
        [SerializeField] private float _bounceKick = 4f;
        [Tooltip("Linear damping shared by every animal body and JumpMath.")]
        [Min(0f)]
        [SerializeField] private float _linearDamping = 4f;
        [Tooltip("Lower bound of the wander heading re-roll interval, seconds.")]
        [Min(0f)]
        [SerializeField] private float _wanderRerollMin = 0.8f;
        [Tooltip("Upper bound of the wander heading re-roll interval, seconds.")]
        [Min(0f)]
        [SerializeField] private float _wanderRerollMax = 1.5f;
        [Tooltip("Bounds-return inner margin (m). T07 invariant: >= JumpDistance, so a jumper leaping from the inner edge can't clear the true edge.")]
        [Min(0f)]
        [SerializeField] private float _fieldInnerMargin = 1.5f;

        [Header("Feedback")]
        [Tooltip("\"Tasty!\" label lifetime, seconds.")]
        [Min(0f)]
        [SerializeField] private float _tastyLifetime = 1f;
        [Tooltip("\"Tasty!\" label pool size.")]
        [Min(1)]
        [SerializeField] private int _tastyPoolSize = 16;

        /// <summary>Lower bound of the random spawn interval (s).</summary>
        public float SpawnIntervalMin => _spawnIntervalMin;

        /// <summary>Upper bound of the random spawn interval (s).</summary>
        public float SpawnIntervalMax => _spawnIntervalMax;

        /// <summary>Population safeguard — the spawner pauses at this live count.</summary>
        public int MaxPopulation => _maxPopulation;

        /// <summary>Minimum predators kept on screen (anti-deadlock floor).</summary>
        public int PredatorFloor => _predatorFloor;

        /// <summary>Spawn clearance radius (m).</summary>
        public float ClearanceRadius => _clearanceRadius;

        /// <summary>Placement attempts before skipping a spawn tick.</summary>
        public int MaxPlacementAttempts => _maxPlacementAttempts;

        /// <summary>Post-collision control grace window (s).</summary>
        public float GraceSeconds => _graceSeconds;

        /// <summary>Prey×prey separation kick (m/s).</summary>
        public float BounceKick => _bounceKick;

        /// <summary>Linear damping shared by every animal body and JumpMath.</summary>
        public float LinearDamping => _linearDamping;

        /// <summary>Lower bound of the wander heading re-roll interval (s).</summary>
        public float WanderRerollMin => _wanderRerollMin;

        /// <summary>Upper bound of the wander heading re-roll interval (s).</summary>
        public float WanderRerollMax => _wanderRerollMax;

        /// <summary>Bounds-return inner hysteresis margin (m); T07 invariant: ≥ JumpDistance.</summary>
        public float FieldInnerMargin => _fieldInnerMargin;

        /// <summary>"Tasty!" label lifetime (s).</summary>
        public float TastyLifetime => _tastyLifetime;

        /// <summary>"Tasty!" label pool size.</summary>
        public int TastyPoolSize => _tastyPoolSize;
    }
}
