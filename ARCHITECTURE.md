# Zoo World — Architecture

> How the project is engineered, and how to extend it. Design source of truth:
> [`docs/product/game-design.md`](docs/product/game-design.md). Engineering contract:
> [`docs/architecture/implementation-guardrails.md`](docs/architecture/implementation-guardrails.md).
> Locked decisions: [`docs/architecture/adr/`](docs/architecture/adr/).

## The graded axis: animal architecture

The brief grades **how easy it is to add animals** ("assume we will add 1000 different animals — birds,
spiders, fish, crabs"). The answer here: an animal is **composition, not inheritance**, and a new species
is **data, not code**.

An `Animal` is a thin MonoBehaviour adapter holding:

- an **`AnimalDefinition`** (ScriptableObject) — role (Prey/Predator), `strength`, a **movement-strategy
  reference**, spawn weight, colour, size, mass, per-species tuning;
- a plug-in **`MovementBehaviour`** strategy (the Strategy pattern — a *stateless* SO): `WanderMove`,
  `JumpMove`, `LinearMove`;
- per-animal runtime `MovementState`.

All species live in one **`AnimalCatalog`** (a list of definitions). Adding a species that reuses an
existing motion is **pure data**: author one `AnimalDefinition`, drop it in the catalog. A genuinely new
motion is one new `MovementBehaviour` asset — nothing else changes.

## The shape

Thin Unity adapters over pure-C# rules, with **one system that owns the tick**:

```
Simulation (the one FixedUpdate owner) + UI views + the composition root   ── adapt Unity
        drive
Pure rules: FoodChainResolver, SpawnPlanner, BoundsReturn, JumpMath, JumpArc, DeathCounters
        operate over
Plain structs + interface seams (AnimalState, FieldBounds, MoveContext, IClock, IRandom,
IOccupancyQuery, ISpawnSequence) + ScriptableObject config (AnimalDefinition, AnimalCatalog, SimConfig)
```

- **`Simulation`** is the single injected service with a `FixedUpdate` tick. It iterates the active
  animals, ticks each strategy, applies velocity, samples the visual jump arc, drains the end-of-step
  collision queue, and publishes events. **No other gameplay MonoBehaviour has a magic tick.**
- **`Animal` is a dumb adapter** — Rigidbody + state + pool-reset hooks; it only *enqueues* its
  collisions and exposes a visual mesh + a feedback `CancellationToken`. It holds no rules and no service
  dependencies (so it never needs the impossible: constructor injection on a pooled MonoBehaviour).
- **Rules are pure C# over structs behind interface seams**, so they run headless in EditMode (no
  `Time`/`Random`/`Physics`/`Camera` inside a rule). That is what makes the **133-test** suite real.

## Named patterns

- **Strategy** — `MovementBehaviour` (Wander/Jump/Linear): a stateless SO swapped per species; per-animal
  mutable state lives on the `Animal`, per-species constants arrive as an `in MovementTuning`.
- **Factory + Object Pool** — `AnimalFactory` *is* the pool's spawn+configure API; animals, the "Tasty!"
  labels, and the death puffs are all pooled (never `Instantiate`/`Destroy` at runtime).
- **Observer** — typed `readonly struct` channels: `AnimalDied` → counters + death puff; `PredatorAte`
  → the "Tasty!" label. No global event bus.
- **Dependency Injection** — one VContainer composition root (`GameLifetimeScope`); scope-singletons,
  no static singletons, no service locator, no `Resolve` from gameplay.

## Predation

A single pure **`FoodChainResolver`** decides each colliding pair from role + `strength`. Collisions are
**enqueued** in `OnCollisionEnter` and resolved once per `FixedUpdate` in an **end-of-step drain** (eats
before bounces, idempotent on a `dead` flag) — never inside the callback, whose ordering Unity does not
guarantee. Prey×Prey → bounce (physics + an explicit separation impulse, both live); Prey×Predator →
prey dies; Predator×Predator → higher `strength` survives (tie → lower spawn id). One `AnimalDied` per
death drives the counters; one `PredatorAte` per eat drives the "Tasty!" label at the predator.

## Feedback

Feedback is a small **UniTask lerp helper** (`FeedbackLerp`) tied to each pooled object's
`CancellationToken` — cancelled on despawn, so no stale animation fires on a reused object. Colour is set
via a **`MaterialPropertyBlock`** (never `renderer.material`, which would break SRP batching). The frog/
rabbit hop is an `AnimationCurve` sampled in the tick as a **child-mesh Y offset** — the Rigidbody Y
stays frozen on the plane.

## How to add a new animal — worked example: the Rabbit

The Rabbit ships as the **data-only proof**. It is a prey that hops — i.e. it **reuses the Frog's
`JumpMove`**. Adding it took **zero code**:

1. **Author one `AnimalDefinition`** — `Assets/_Project/ScriptableObjects/Rabbit.asset`: role `Prey`,
   `strength 0`, **movement = the existing `JumpMove.asset`** (the same asset the Frog references — reuse,
   not a new strategy), spawn weight `0.25`, a distinct colour, size/mass `1`, jump distance/interval `1.5`.
2. **Add it to the catalog** — append `Rabbit.asset` to `AnimalCatalog.asset`'s list.

That's the whole change. At composition, `CatalogProjection` projects the catalog into the value structs
the rules consume (in catalog order — the 1:1 index contract); `AnimalFactory` auto-creates a third pool
(prewarmed `round(0.25 × 120) = 30`); the spawner weights it into the lottery. **No edits** to the
`Animal`, the resolver, the spawner, the pool, the `Simulation`, or any physics code. An EditMode test —
`CatalogProjectionTests.RealCatalog_Rabbit_ReusesFrogMovement_DataOnly` — pins exactly this: the catalog
has three species and the Rabbit's movement is **reference-equal** to the Frog's.

**A species with genuinely new motion** (a `Fly`, a `Swim`) adds *one* new `MovementBehaviour` SO + its
tuning, then the same two steps — still no edits to existing animals or systems. That is the line the
"1000 animals" brief is really testing, and it stays flat.

## Layout

- First-party code under `Assets/_Project/Scripts/{Runtime,Editor}`; data under `ScriptableObjects/`;
  tests under `Tests/EditMode`. Assemblies `ZooWorld.Runtime` / `.Editor` / `.Tests.EditMode`.
- Namespaces: `ZooWorld.{Animals, Spawning, Predation, Core, UI, Config, Composition}`.
- Libraries (each genuinely exercised): **VContainer** (DI) + **UniTask** (async cadence + feedback).
  No ECS; UI is uGUI.
- Run + test: see [`README.md`](README.md) and
  [`docs/development/build-and-test.md`](docs/development/build-and-test.md).
