# ADR 0002: Animal architecture (composition, the Simulation tick, libraries)

Status: Accepted
Date: 2026-06-25
Decision owner: Andrei Boika

## Context

The graded axis is the **animal architecture** — "clear, easy to extend… 1000 animals". The design
was distilled into an implementation hypothesis and put through a 7-lens adversarial review
(physics, architecture, DI, libraries, testability, performance, completeness). The review confirmed
the approach is sound with no architectural dead-ends, but surfaced real fixes — chiefly that
ScriptableObject/`[SerializeReference]` strategies and pooled `Animal` MonoBehaviours **cannot be
constructor-injected**, and that several "pure" rules secretly leaked Unity (`Physics.CheckSphere`,
mutating MonoBehaviours, in-callback collision resolution). This ADR records the resulting decisions.
Detailed rules live in [`implementation-guardrails.md`](../implementation-guardrails.md); the design
in [`game-design.md`](../../product/game-design.md). This ADR **refines ADR 0001's** one-line
strategy mention (which said `[SerializeReference]`).

## Decision

1. **Composition over inheritance.** `Animal` = `AnimalDefinition` (SO: role, `strength`, a movement
   strategy ref, spawnWeight, tuning, visuals) + a `MovementBehaviour` strategy + runtime
   `MovementState`. A new species = **add one `AnimalDefinition` SO to the `AnimalCatalog`** (+ a new
   strategy SO only for genuinely new motion).

2. **Movement strategies are `MovementBehaviour : ScriptableObject`, STATELESS** — *chosen over
   `[SerializeReference]` inline instances.* Rationale: refactor-safe (no managed-reference loss on
   rename), designer-drags-an-asset (no custom property drawer), and statelessness is structurally
   enforced (a shared asset can hold no per-animal state). Cost — a few strategy assets — is trivial.
   Per-species **tuning lives on the Definition**; per-animal **state lives on `Animal`**; the strategy
   is pure logic `Tick(ref MovementState, in MoveContext, in MovementTuning) → desired velocity` — it reads
   its species' constants (speed, jump distance/interval, damping) from the `in MovementTuning` the
   `Simulation` builds at spawn, so the shared SO stays stateless (and advances per-animal `MovementState`).

3. **One `Simulation` service owns the tick; `Animal` is a dumb adapter** (KEYSTONE). `Simulation`
   (a VContainer scope-singleton) holds `IClock`/`IRandom`/`FieldBounds`/catalog/factory/events; in a
   single `FixedUpdate` it ticks every strategy, applies velocity, drains the collision queue, and
   publishes events. `Animal` holds only Rigidbody + state + collision-enqueue and has **no magic
   `Update`/`FixedUpdate` and no service dependencies**. This dissolves the un-injectable-pooled-
   MonoBehaviour / un-injectable-strategy problem (nothing needs injection that can't get it),
   removes 120 per-object `FixedUpdate` callbacks, and makes the tick deterministic.

4. **Rules are pure over plain structs behind interface seams** so they are genuinely
   EditMode-testable headless: `FoodChainResolver(in AnimalState, in AnimalState) → Outcome`;
   `SpawnPlanner` over `IRandom` + `IOccupancyQuery` + `PopulationSnapshot`; `BoundsReturn` over a
   `FieldBounds` struct; `JumpMath` closed-form; `DeathCounters` off the `AnimalDied` struct. Unity
   (`Physics.CheckSphere`, the camera, `Time`) stays behind the seams in adapters.

5. **Predation is resolved deterministically end-of-step, not in `OnCollisionEnter`.** Collisions are
   enqueued as unordered `(minSeq, maxSeq)` pairs; `Simulation` drains them once per step (eats before
   bounces), idempotency keyed per-pair + `dead` flag. This removes dependence on Unity's
   non-guaranteed callback ordering and makes the live path match the pure-resolver tests. Predation
   stays **binary** (role + scalar `strength`); multi-tier diets are an explicit, un-built seam.

6. **Physics correctness contract:** dynamic Rigidbody, freeze posY + rotX/Z, gravity off,
   `sleepThreshold = 0` (else bodies freeze), **Discrete** collision, a thin static floor collider,
   a low-restitution material, **low linear damping** (≈ 4 — gives the leap a finite nominal
   distance); prey×prey "fly apart" uses an **explicit separation impulse** +
   an `IClock` **float grace window** (blend-back + heading bias), never passive restitution and
   never a per-collision UniTask await.

7. **Libraries — each genuinely used:** **VContainer** (one composition root; scope-singletons, not
   static singletons; `IAsyncStartable` spawn loop with a scope-tied token; `RegisterInstance` for SO
   assets). **UniTask** (spawn cadence + label lifetime; zero-alloc; rule timing stays on `IClock`).
   Feedback animations are a **small UniTask lerp helper** (Tasty!/spawn-pop/death; the jump arc is an
   `AnimationCurve`), tied to the object's `CancellationToken` (cancel-on-despawn). **DOTween was
   dropped** — not on OpenUPM (Asset-Store-only + source-in-repo) and the review's weakest-justified
   lib. No ECS.

## Consequences

- **Easier:** new species are data; rules are pure and unit-testable without the engine; the
  composition root is the one wiring place; the tick is deterministic and centrally profileable.
- **Constrained:** predation is binary by design (multi-tier is documented, not built); population is
  capped (~100–120) for stability; the `Simulation` is the one component allowed a magic tick.
- **Risks (mitigated in the guardrails):** the physics "fly apart" + no-freeze + crowding behaviours
  must be smoke-verified on a Player build; feedback animations must be cancelled on despawn (UniTask
  token) or they fire on a reused pooled object.
- **Supersede** this ADR with a new one rather than editing once accepted.
