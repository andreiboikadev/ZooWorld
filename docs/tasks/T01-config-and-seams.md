# T01 — Config & seams (SOs, structs, interfaces + fakes)

| | |
|---|---|
| Milestone | M1 — pure core |
| Depends on | — |
| Touches scene/prefabs | no |
| Status | ▫ not started |

## Goal

Lay the foundation every pure rule and the `Simulation` depend on: the ScriptableObject data model,
the plain value structs, and the interface seams (+ test fakes) that keep Unity statics out of the
rules so they're EditMode-testable headless. After this, T02–T05 can be built and tested with no
scene. Guardrails §5, §10.

## Acceptance criteria

**ScriptableObjects** (`ZooWorld.Config`):
- `Role` enum `{ Prey, Predator }`.
- `MovementBehaviour : ScriptableObject` — **abstract, stateless** base with
  `Vector3 Tick(ref MovementState state, in MoveContext ctx, in MovementTuning tuning)`. (Concretes in T02.) No instance fields.
- `AnimalDefinition : ScriptableObject` — `id`, `displayName`, `Role role`, `int strength`,
  `MovementBehaviour movement`, `float spawnWeight`, visuals (`Color color`, `float size`,
  `float mass`), tuning (`float speed`, `float jumpDistance`, `float jumpInterval`). All
  `[SerializeField] private` with read-only accessors.
- `AnimalCatalog : ScriptableObject` — `IReadOnlyList<AnimalDefinition> Definitions`.
- `SimConfig : ScriptableObject` — the GDD §9 globals: spawn interval `[min,max]`, `maxPopulation`,
  `predatorFloor`, `graceSeconds`, `bounceKick`, `linearDamping`, wander re-roll `[min,max]`,
  `clearanceRadius`, `maxPlacementAttempts`, `tastyLifetime`, `tastyPoolSize`. Pool prewarm is
  **derived** (`round(spawnWeight × maxPopulation)`), not stored.

**Plain structs** (`ZooWorld.Core` / `.Animals`, `readonly` where they're inputs):
- `MovementState` — `Vector3 Heading`, behaviour timers, `float GraceUntil`, leap state (mutated by ref).
  (T02/T07 may add fields — wander timer, leap phase — as the strategies need; this list is the minimum, not a closed set.)
- `readonly struct MoveContext` — `float Dt`, `IRandom Rng`, `FieldBounds Bounds`, `IClock Clock`.
- `readonly struct MovementTuning` — `float Speed`, `float JumpDistance`, `float JumpInterval`,
  `float LinearDamping`: the per-animal movement **constants** the stateless strategy needs, built once at
  spawn from `AnimalDefinition` tuning + `SimConfig.linearDamping` and passed `in` (keeps the shared SO stateless).
- `readonly struct AnimalState` — `Role Role`, `int Strength`, `long Seq`, `bool Dead` (resolver input).
- `readonly struct Outcome` — what the resolver returns (who died (seq), victim `Role`, `Vector3`
  position, `bool RaiseTasty`, plus the bounce flag/normal for prey×prey).
- `readonly struct FieldBounds` — `Vector3 Center`, `Vector2 HalfExtents`, `float InnerMargin`
  (+ a pure `Contains`/`Nearest` helper, no Unity statics).
- `readonly struct PopulationSnapshot` — `int Total`, `int PredatorCount`.
- `readonly struct AnimalDied` — `Role Role`, `Vector3 Position` (the event payload, guardrails §13).

**Interface seams** (`ZooWorld.Core`):
- `IClock { float Now; float Dt; }`, `IRandom { float Value01(); float Range(float a, float b); int Range(int a, int b); }`,
  `ISpawnSequence { long Next(); }`, `IOccupancyQuery { bool IsClear(Vector3 pos, float radius); }`.

**Production impls** (`ZooWorld.Runtime`): `UnityClock` (wraps `Time`), `SeededRandom` (wraps
`System.Random`, seed injected), `MonotonicSpawnSequence` (ever-increasing `long`, never reused).
(`IOccupancyQuery` production impl needs physics → T06/T08; T01 ships only the interface + a fake.)

**Test fakes + EditMode tests** (`ZooWorld.Tests.EditMode`): `FakeClock` (settable `Now`),
`FakeRandom` (scripted sequence), `FakeSpawnSequence`, `FakeOccupancy` (always-clear / always-blocked
/ list-of-circles). Tests assert each fake behaves and `MonotonicSpawnSequence.Next()` is strictly
increasing + never repeats.

## Implementation notes

- Files under `Assets/_Project/Scripts/Runtime/{Config,Core,Animals}` and `Tests/EditMode`.
  Create the `ZooWorld.Runtime`, `ZooWorld.Editor`, `ZooWorld.Tests.EditMode` asmdefs first (ADR 0001)
  and confirm `manifest.json` has **VContainer + UniTask** referenced before this compiles.
- Structs use `UnityEngine.Vector3/Vector2/Color` (fine in EditMode) but **no MonoBehaviour, no
  `Time`/`Random`/`Physics` statics** anywhere in this task's types.
- `MovementBehaviour` is `abstract` here so `AnimalDefinition` can reference it; do not add concretes.
- Author one or two placeholder `AnimalDefinition` assets (Frog, Snake) + an `AnimalCatalog` + a
  `SimConfig` asset **under `Assets/_Project/ScriptableObjects/`** (commit their `.meta`) so later tasks
  have data to load (movement refs left null until T02).

## Out of scope

Concrete movement strategies (T02), `FoodChainResolver` (T03), `SpawnPlanner` (T04), the `Animal`
MonoBehaviour + physics (T06), the production `IOccupancyQuery`/`FieldBounds`-from-camera (T06/T08),
any scene content, DI wiring (T08).

## Verification

- EditMode: the fakes + `MonotonicSpawnSequence` tests pass; the SO types create + serialize in the
  Inspector; the placeholder assets author cleanly.
- Compiles with **no Console errors**; full (new) suite green.

## What was actually done

—
