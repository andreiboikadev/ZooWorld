#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using ZooWorld.Animals;
using ZooWorld.Config;
using ZooWorld.Predation;

namespace ZooWorld.Core
{
    /// <summary>
    /// The single service that owns the FixedUpdate tick (guardrails §2; ADR 0002 §3 KEYSTONE): it ticks
    /// every active animal's movement strategy, applies the resulting velocity (driving continuous movers,
    /// coasting the jumper's burst, suppressing strategy velocity during the post-bounce grace, steering
    /// back at the bounds), then drains the end-of-step collision queue — resolving each pair through the
    /// pure <see cref="FoodChainResolver"/>, despawning the dead, raising <c>AnimalDied</c>, and applying
    /// the prey×prey separation impulse + grace. A plain VContainer scope-singleton (not a MonoBehaviour);
    /// collisions reach it via <see cref="IContactSink"/> from the dumb <c>Animal</c> adapter.
    /// </summary>
    public sealed class Simulation : IFixedTickable, IContactSink, IDisposable
    {
        private readonly IClock _clock;
        private readonly IRandom _random;
        private readonly FieldBounds _bounds;
        private readonly SimulationTuning _tuning;
        private readonly FoodChainResolver _resolver;
        private readonly AnimalDeathSignal _deathSignal;
        private readonly AnimalFactory _factory;
        private readonly List<Animal> _active = new List<Animal>();
        private readonly List<(Animal a, Animal b)> _pendingPairs = new List<(Animal a, Animal b)>();
        private readonly HashSet<(long, long)> _pendingKeys = new HashSet<(long, long)>();

        public Simulation(IClock clock, IRandom random, in FieldBounds bounds, in SimulationTuning tuning,
            FoodChainResolver resolver, AnimalDeathSignal deathSignal, AnimalFactory factory)
        {
            _clock = clock;
            _random = random;
            _bounds = bounds;
            _tuning = tuning;
            _resolver = resolver;
            _deathSignal = deathSignal;
            _factory = factory;
        }

        /// <summary>
        /// Registers a freshly spawned animal: seeds its movement state (heading + leap/reroll clocks), wires
        /// its collision sink, warms the cached body, and adds it to the active list. Called by the spawner
        /// on each take (T08); the tests call it directly.
        /// </summary>
        public void Register(Animal animal)
        {
            SpawnSeed.Apply(ref animal.MovementState, _clock, _random, animal.Tuning);
            animal.SetContactSink(this);

            // Warm the lazy Body cache so the per-frame tick never calls GetComponent (guardrails §12).
            _ = animal.Body;

            _active.Add(animal);
        }

        /// <inheritdoc/>
        void IContactSink.Enqueue(Animal a, Animal b)
        {
            if (ReferenceEquals(a, b) || a.Seq == b.Seq)
            {
                return;
            }

            (long lo, long hi) = a.Seq < b.Seq ? (a.Seq, b.Seq) : (b.Seq, a.Seq);
            if (_pendingKeys.Add((lo, hi)))
            {
                _pendingPairs.Add((a, b));
            }
        }

        /// <inheritdoc/>
        public void FixedTick()
        {
            MoveContext ctx = new MoveContext(_clock.Dt, _random, in _bounds, _clock);
            for (int i = 0; i < _active.Count; i++)
            {
                Animal animal = _active[i];
                MovementBehaviour? movement = animal.Movement;
                if (movement == null)
                {
                    continue;
                }

                Vector3 position = animal.transform.position;
                Vector3 desired = movement.Tick(ref animal.MovementState, in ctx, animal.Tuning);
                bool within = BoundsReturn.IsWithinInner(in _bounds, position);
                Vector3 steer = BoundsReturn.Steer(in _bounds, position, animal.MovementState.Heading);

                if (!within)
                {
                    // Write the steered (toward-centre, unit) heading back so a continuous mover doesn't
                    // re-read its stale outward heading and oscillate at the edge (T02 carry-forward).
                    animal.MovementState.Heading = steer;

                    // An idle out-of-bounds jumper: nudge it to leap inward next tick rather than coast
                    // off-screen (decision 3 — the margin invariant is the primary guard, this the net).
                    if (movement.IsImpulseDriven && desired == Vector3.zero)
                    {
                        animal.MovementState.NextLeapTime = _clock.Now;
                    }
                }

                DriveCommand command = MovementDrive.Decide(desired, movement.IsImpulseDriven, _clock.Now,
                    animal.MovementState.GraceUntil, within, steer);
                switch (command.Mode)
                {
                    case DriveMode.SetVelocity:
                        animal.Body.linearVelocity = command.Velocity;
                        break;
                    case DriveMode.Impulse:
                        animal.Body.AddForce(command.Velocity, ForceMode.VelocityChange);
                        break;
                    case DriveMode.None:
                        break;
                }
            }

            DrainContacts();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].SetContactSink(null);
            }

            _active.Clear();
            _pendingPairs.Clear();
            _pendingKeys.Clear();
        }

        private static AnimalState Snapshot(Animal animal)
        {
            return new AnimalState(animal.Role, animal.Strength, animal.Seq, animal.IsDead);
        }

        private void DrainContacts()
        {
            // Pass A — predator eats + duels first, so a victim is dead before any bounce is considered.
            for (int i = 0; i < _pendingPairs.Count; i++)
            {
                (Animal a, Animal b) = _pendingPairs[i];
                Outcome outcome = _resolver.Resolve(Snapshot(a), Snapshot(b));
                if (outcome.Kind == OutcomeKind.Death)
                {
                    ApplyDeath(in outcome, a, b);
                }
            }

            // Pass B — prey×prey bounces; a pair with a just-eaten member now dead-guards to None.
            for (int i = 0; i < _pendingPairs.Count; i++)
            {
                (Animal a, Animal b) = _pendingPairs[i];
                Outcome outcome = _resolver.Resolve(Snapshot(a), Snapshot(b));
                if (outcome.Kind == OutcomeKind.Bounce)
                {
                    ApplyBounce(a, b);
                }
            }

            _pendingPairs.Clear();
            _pendingKeys.Clear();
        }

        private void ApplyDeath(in Outcome outcome, Animal a, Animal b)
        {
            Animal victim = a.Seq == outcome.DeadSeq ? a : b;

            // Source from transform.position (what OnSpawn writes; physics-synced in Play) before despawn.
            Vector3 deathPosition = victim.transform.position;
            victim.MarkDead();
            _deathSignal.Raise(new AnimalDied(outcome.VictimRole, deathPosition));
            _active.Remove(victim);
            _factory.Despawn(victim);
        }

        private void ApplyBounce(Animal a, Animal b)
        {
            Vector3 delta = a.transform.position - b.transform.position;
            delta.y = 0f;
            Vector3 normal = delta.sqrMagnitude > 1e-6f ? delta.normalized : Vector3.right;

            a.Body.AddForce(normal * _tuning.BounceKick, ForceMode.VelocityChange);
            b.Body.AddForce(-normal * _tuning.BounceKick, ForceMode.VelocityChange);

            float graceUntil = _clock.Now + _tuning.GraceSeconds;
            a.MovementState.GraceUntil = Mathf.Max(a.MovementState.GraceUntil, graceUntil);
            b.MovementState.GraceUntil = Mathf.Max(b.MovementState.GraceUntil, graceUntil);
        }
    }
}
