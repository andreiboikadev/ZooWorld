#nullable enable

using System.Threading;
using UnityEngine;
using ZooWorld.Config;
using ZooWorld.Core;

namespace ZooWorld.Animals
{
    /// <summary>
    /// The runtime half of an animal — a DUMB adapter: a Rigidbody + per-animal state + pool-reset hooks,
    /// with no magic <c>Update</c>/<c>FixedUpdate</c>, no in-callback collision resolution, and no service
    /// dependencies. The T07 <c>Simulation</c> owns the tick and the collision pipeline; the
    /// <c>AnimalFactory</c> takes this from / returns it to a pool, configuring it from an
    /// <see cref="AnimalSpec"/> at spawn. T09 adds the visual surface (child mesh + colour) and the feedback
    /// <see cref="Token"/>; the Simulation drives the per-animal visuals, so this stays dumb.
    /// </summary>
    public sealed class Animal : MonoBehaviour
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock? s_block;

        [SerializeField] private MeshRenderer _renderer = null!;
        [SerializeField] private Transform _mesh = null!;

        private Rigidbody _rigidbody = null!;
        private MovementState _movementState;
        private IContactSink? _contacts;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// The body, resolved lazily on first access (cached thereafter) — NOT only in <c>Awake</c>, which
        /// does not run for objects built via <c>AddComponent</c>/<c>Instantiate</c> in EditMode tests.
        /// <c>GetComponent</c> fires once, at take/return time — never in the T07 per-frame tick.
        /// </summary>
        public Rigidbody Body => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());

        /// <summary>The child visual mesh — the jump-arc / spawn-pop target (T09); null in headless tests.</summary>
        public Transform Mesh => _mesh;

        /// <summary>The despawn-cancelled feedback token (guardrails §11): fresh on take, cancelled on return.</summary>
        public CancellationToken Token => _cts!.Token;

        /// <summary>Predation role (read by the Simulation to build its resolver state).</summary>
        public Role Role { get; private set; }

        /// <summary>Predator-vs-predator winner key (higher wins).</summary>
        public int Strength { get; private set; }

        /// <summary>Monotonic spawn id, assigned at take-from-pool; breaks predator strength ties.</summary>
        public long Seq { get; private set; }

        /// <summary>True once the Simulation has resolved this animal dead this step (idempotency guard).</summary>
        public bool IsDead { get; private set; }

        /// <summary>The movement strategy this animal ticks (null until T07 wires the tick).</summary>
        public MovementBehaviour? Movement { get; private set; }

        /// <summary>Per-animal movement constants the strategy reads.</summary>
        public MovementTuning Tuning { get; private set; }

        /// <summary>Per-animal mutable movement state, advanced in place by the strategy each tick (T07).</summary>
        public ref MovementState MovementState => ref _movementState;

        /// <summary>The catalog index of the pool this instance belongs to (set by the factory at creation).</summary>
        internal int PoolIndex { get; set; }

        /// <summary>True while idle in the pool — the factory's double-despawn guard (set on create/return, cleared on take).</summary>
        internal bool Pooled { get; set; }

        /// <summary>
        /// Take-from-pool reset + configure: a fresh feedback CTS, the full Rigidbody physics profile, scale,
        /// runtime state, the species colour (via a reused <c>MaterialPropertyBlock</c>), a collapsed mesh for
        /// the spawn scale-in, and a cleared movement state. Idempotent on reuse — a recycled body carries no
        /// stale velocity, dead-flag, heading, grace, or in-flight feedback.
        /// </summary>
        /// <param name="spec">This animal's species spec (role, strength, size, mass, colour, tuning).</param>
        /// <param name="seq">The fresh monotonic spawn id.</param>
        /// <param name="position">The world spawn position.</param>
        public void OnSpawn(in AnimalSpec spec, long seq, Vector3 position)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            Body.useGravity = false;
            Body.sleepThreshold = 0f;
            Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            Body.linearDamping = spec.Tuning.LinearDamping;
            Body.mass = spec.Mass;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;

            transform.localScale = Vector3.one * spec.Size;
            transform.position = position;
            transform.rotation = Quaternion.identity;

            Role = spec.Role;
            Strength = spec.Strength;
            Movement = spec.Movement;
            Tuning = spec.Tuning;
            Seq = seq;
            IsDead = false;

            // Cleared to a known baseline. NextLeapTime = 0 would leap on frame 1 (GDD §11); the T07
            // Simulation seeds NextLeapTime = clock.Now + JumpInterval when it first ticks the animal.
            _movementState = default;

            // Per-species colour via a reused MaterialPropertyBlock (never renderer.material — guardrails §12).
            if (_renderer != null)
            {
                s_block ??= new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(s_block);
                s_block.SetColor(s_baseColorId, spec.Color);
                _renderer.SetPropertyBlock(s_block);
            }

            // The visual mesh starts collapsed at the origin — the Simulation's spawn scale-in pops it to 1.
            if (_mesh != null)
            {
                _mesh.localPosition = Vector3.zero;
                _mesh.localScale = Vector3.zero;
            }
        }

        /// <summary>Return-to-pool reset: zero the body's velocity and cancel any in-flight feedback (guardrails §11).</summary>
        public void OnDespawn()
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            _contacts = null;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>Marks this animal dead (the Simulation calls it on a resolved death, before despawn).</summary>
        public void MarkDead()
        {
            IsDead = true;
        }

        /// <summary>The spawn scale-in sink (0 → 1, T09) — sets the child mesh's uniform scale; null-safe headless.</summary>
        public void SetSpawnScale(float t01)
        {
            if (_mesh != null)
            {
                _mesh.localScale = Vector3.one * t01;
            }
        }

        /// <summary>Wires the collision sink (the Simulation) at registration; cleared to null on despawn.</summary>
        public void SetContactSink(IContactSink? sink)
        {
            _contacts = sink;
        }

        // The dumb adapter's ONLY collision role: enqueue the contact pair into the Simulation's buffer for
        // the end-of-step drain (guardrails §2/§6) — it resolves nothing here. GetComponent fires on a
        // contact event, never in the per-frame tick (§12).
        private void OnCollisionEnter(Collision collision)
        {
            if (_contacts == null || collision.rigidbody == null)
            {
                return;
            }

            Animal? other = collision.rigidbody.GetComponent<Animal>();
            if (other != null)
            {
                _contacts.Enqueue(this, other);
            }
        }
    }
}
