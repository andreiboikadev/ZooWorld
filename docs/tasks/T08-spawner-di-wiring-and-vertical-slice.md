# T08 — Spawner + DI wiring + production seams (FieldBounds / occupancy / HUD view) → vertical slice runs

| | |
|---|---|
| Milestone | M2 — adapters / integration |
| Depends on | T04 (SpawnPlanner), T05 (DeathCounters/HudPresenter/IHudView), T07 (Simulation) — also consumes T02 movement SOs + T06 `Animal` prefab/factory |
| Touches scene/prefabs | **yes** — `Assets/_Project/Scenes/Gameplay.unity` (composition root, camera, HUD canvas, floor, animals root); verifies `Assets/_Project/Prefabs/Animal.prefab`; ProjectSettings physics matrix |
| Status | ▫ not started |

## Goal

Wire the composition root that turns the unit-tested rules + adapters (T01–T07) into a **running
vertical slice**: a `GameLifetimeScope` projects the `AnimalCatalog` + `SimConfig` into the value
structs the rules consume, binds every service, and starts a UniTask spawn loop. Adds the three
production seams still missing (`Physics.CheckSphere` occupancy, camera-frustum `FieldBounds`, the
uGUI `HudView`) and the population accessor the spawner needs. On Play, `Gameplay.unity` spawns →
moves → collides → eats/bounces → counts, Console clean. This is the **M2 exit** (the slice is
alive); feedback visuals + Rabbit + build are T09.

## Decisions to confirm (flagged for validation — affect the criteria below)

1. **`FieldBounds.InnerMargin` has no GDD §9 number — but T07 already locked the invariant.** T07
   decision 3 (and its §9 line) require **`InnerMargin ≥ JumpDistance`** (≈ **1.5–2 m**) so a jumper
   leaping from the inner edge cannot clear the *true* edge (off-screen leak). Proposal: add a provisional
   `SimConfig._fieldInnerMargin` (getter `FieldInnerMargin`, default **1.5 m = `JumpDistance`**, honouring
   T07's invariant — **not** the 1.0 m an earlier draft used) and a §9 row worded
   `InnerMargin ≥ JumpDistance (≈1.5–2 m)`. The composition root reads it into `FieldBoundsFactory` (never
   a literal). *(Alt: derive as a fraction of the half-extents — rejected as less designer-legible.)*
   Confirm the value; it adds a §9 row.
2. **Population source for the cap/predator-floor.** Proposal: an additive read-only
   `Simulation.Population` (`PopulationSnapshot`) computed from the active list the Simulation already
   owns (single source of truth). *(Alt: the Spawner tracks counts via `IAnimalDeathSignal` + its own
   spawn increments — rejected as a second, drift-prone count.)* This is a small additive edit to the
   merged T07 `Simulation`.
3. **T08/T09 scene boundary.** T08 ships **one functional `Gameplay.unity`** (scope + camera + HUD +
   floor) — enough for the slice to run. The polished Boot→Running framing, the "Tasty!" label, the
   spawn/death feedback, per-species `MaterialPropertyBlock` colour, Rabbit, and the Windows build stay
   in **T09** (matrix). T08 animals may share the prefab's placeholder material.
4. **Camera model:** top-down **perspective** looking straight down −Y (GDD §3); `FieldBounds` from its
   frustum on the ground plane. Authored so the footprint ≈ **20×12 m** (GDD §9).

## Acceptance criteria

Exact names; numbers cite game-design.md §9. New runtime code under
`Assets/_Project/Scripts/Runtime/`. Pure rules ship EditMode tests in the same change.

### A. Catalog projection (pure) — `Composition/CatalogProjection.cs` (`ZooWorld.Composition`)
A static pure projection from the config SOs to the value structs the rules consume (no Unity statics
beyond reading SO getters), in **catalog order**:
- `static (AnimalSpec[] specs, SpeciesWeight[] weights) Build(AnimalCatalog catalog, SimConfig config)`
  — for each `catalog.Definitions[i]` (i in order) produces `specs[i]` and `weights[i]` from that same
  definition. `weights[i] = new SpeciesWeight(def.Role, def.SpawnWeight)`;
  `specs[i] = new AnimalSpec(def.Role, def.Strength, def.Size, def.Mass, def.SpawnWeight, def.Movement,
  in tuning)` with `tuning = new MovementTuning(def.Speed, def.JumpDistance, def.JumpInterval,
  config.LinearDamping, config.WanderRerollMin, config.WanderRerollMax)`.
- `static SpawnTuning BuildSpawnTuning(SimConfig config)` and
  `static SimulationTuning BuildSimulationTuning(SimConfig config)` — direct field copies.
- **Index contract:** `weights[i]`, `specs[i]`, the `AnimalFactory` pool index `i`, and the index
  `SpawnPlanner.SelectSpeciesIndex` returns all address the **same** catalog species.

### B. Production occupancy seam — `Spawning/PhysicsOccupancyQuery.cs` (`ZooWorld.Spawning`)
- `sealed class PhysicsOccupancyQuery : IOccupancyQuery`; `IsClear(Vector3 position, float radius)`
  returns `!Physics.CheckSphere(position, radius, _animalMask, QueryTriggerInteraction.Ignore)`.
- `_animalMask` is the **Animal** layer mask (layer 8), injected at composition — never hard-coded.
- It is a Unity adapter (touches `Physics`) → **smoke-verified**, not unit-tested (guardrails §10/§14).

### C. Camera-frustum bounds — `Core/FieldBoundsFactory.cs` (pure) + composition read
- `static FieldBounds FromTopDownCamera(Vector3 cameraPosition, float verticalFovDegrees, float aspect,
  float groundY, float innerMargin)`: `halfDepth = (cameraPosition.y - groundY) *
  Mathf.Tan(0.5f * verticalFovDegrees * Mathf.Deg2Rad)`, `halfWidth = halfDepth * aspect`,
  `Center = (cameraPosition.x, groundY, cameraPosition.z)`, `HalfExtents = (halfWidth, halfDepth)`.
  Pure math (`Mathf` only) → EditMode-tested.
- **Precondition (straight-down only):** the closed form assumes the camera looks straight down −Y; it is
  invalid for any tilt (a tilted camera projects a *trapezoid* offset from the camera XZ, which the pure
  factory cannot detect). `GameLifetimeScope` must author the camera straight down (§H).
- `GameLifetimeScope` reads the scene `Camera` **once at composition** (`camera.fieldOfView` — always the
  *vertical* FOV regardless of FOV-axis mode; `camera.aspect`; `transform.position`) and the margin from
  `config.FieldInnerMargin` (decision 1), then calls the factory. Reading the camera once at composition is
  allowed; `Camera.main`/frustum reads **per frame** remain forbidden (guardrails §12).

### D. Spawner loop — `Spawning/Spawner.cs` (`ZooWorld.Spawning`, `VContainer.Unity.IAsyncStartable`)
- Ctor injects `SpawnPlanner`, `AnimalFactory`, `Simulation`, `IOccupancyQuery`, `IRandom`, and the
  `FieldBounds` (in). `async UniTask StartAsync(CancellationToken ct)` loops until cancelled:
  1. `float interval = _planner.NextInterval(_random);`
     `await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);`
     (rule timing stays on the pure `NextInterval`; UniTask only awaits — guardrails §9).
  2. `int? idx = _planner.SelectSpeciesIndex(_simulation.Population, _random);`
     `if (idx == null) continue;` (cap pause).
  3. `if (!_planner.TryFindSpawnPosition(in _bounds, _occupancy, _random, out var pos)) continue;`
     (skip-tick on no clear placement).
  4. `Animal? a = _factory.Spawn(idx.Value, pos); if (a != null) _simulation.Register(a);`
- Teardown clean: wrap with `SuppressCancellationThrow()` / catch `OperationCanceledException`; no
  per-tick allocation in the steady loop (no LINQ, no closures — guardrails §12/§13).

### E. Population accessor (additive to T07) — `Core/Simulation.cs`
- `public PopulationSnapshot Population { get; }` computed from the active list:
  `Total = _active.Count`, `PredatorCount = count of _active where Role == Role.Predator`. O(n),
  n ≤ MaxPopulation, read once per spawn interval — negligible (guardrails §15). No other T07 behaviour
  changes.

### F. uGUI HUD view — `UI/HudView.cs` (`ZooWorld.UI`, `MonoBehaviour : IHudView`)
- Two serialized `UnityEngine.UI.Text` fields (`_deadPreyLabel`, `_deadPredatorsLabel`);
  `SetDeadPrey(string)` / `SetDeadPredators(string)` assign `.text`. No formatting in the view (the
  `HudPresenter` already formats the GDD §8 strings `"Dead prey: N"` / `"Dead predators: N"`).
- `ZooWorld.Runtime.asmdef` gains a **`"UnityEngine.UI"`** reference (uGUI; GDD §8 — no TMP).

### G. Composition root — `Composition/GameLifetimeScope.cs` (`ZooWorld.Composition : LifetimeScope`)
Serialized refs: `AnimalCatalog`, `SimConfig`, `Animal` prefab, `Camera`, `HudView`, animals-root
`Transform`, `LayerMask _animalMask` (the Animal layer), `int _randomSeed` (prov.). `Configure(
IContainerBuilder builder)` (VContainer; scope-singletons, no static singletons, no `Resolve` from
gameplay — guardrails §9). **The registration mechanics below are source-verified against VContainer
1.18.0 — they are load-bearing (see the VContainer-wiring pitfall); the slice will not build/run if a
service with a non-injectable ctor arg is registered with a bare `Register<T>()`.**

**Build the value inputs as composition locals first** — do **not** `RegisterInstance` the structs/arrays
(that path silently breaks `in`-params and `IReadOnlyList<>` consumers): `var (specs, weights) =
CatalogProjection.Build(catalog, config);` plus `spawnTuning`/`simTuning` (via `CatalogProjection`) and
`bounds = FieldBoundsFactory.FromTopDownCamera(_camera.transform.position, _camera.fieldOfView,
_camera.aspect, groundY, config.FieldInnerMargin)` (`groundY` = the floor plane Y). Then register:
- **Seams** (factory lambdas where a ctor arg isn't DI-resolvable): `Register<IClock, UnityClock>(Singleton)`;
  `Register<ISpawnSequence, MonotonicSpawnSequence>(Singleton)`; `Register<IRandom>(_ => new
  SeededRandom(_randomSeed), Singleton)` (seeded — no parameterless ctor); `Register<IOccupancyQuery>(_ =>
  new PhysicsOccupancyQuery(_animalMask), Singleton)`.
- **Death channel — ONE registration, both contracts:** `Register<AnimalDeathSignal>(Singleton)
  .AsSelf().As<IAnimalDeathSignal>()` (Simulation raises the concrete; DeathCounters subscribes the
  interface — they **must** share the instance; two registrations = a silently dead HUD).
- **Rules/services** (factory lambdas capture the locals, so `in`-struct / `IReadOnlyList<>` / primitive
  args never go through reflection injection): `Register<FoodChainResolver>(Singleton)`;
  `Register<SpawnPlanner>(_ => new SpawnPlanner(weights, in spawnTuning), Singleton)`;
  `Register<AnimalFactory>(c => new AnimalFactory(_animalPrefab, specs, config.MaxPopulation,
  config.PredatorFloor, c.Resolve<ISpawnSequence>(), _animalsRoot), Singleton)`;
  `Register<DeathCounters>(Singleton)`.
- **View:** `RegisterComponent<IHudView>(_hudView)` (binds the scene `HudView` under `IHudView`, which is
  all `HudPresenter` injects).
- **Entry points** (the dispatcher is auto-registered by the first `RegisterEntryPoint`; it then drives
  **every** service exposed as a marker interface — `IFixedTickable`/`IStartable`/`IAsyncStartable` —
  however it was registered, factory lambda included):
  `RegisterEntryPoint<HudPresenter>(Singleton)` (`IStartable` — pushes the initial "0" labels);
  `RegisterEntryPoint<Simulation>(c => new Simulation(c.Resolve<IClock>(), c.Resolve<IRandom>(), in bounds,
  in simTuning, c.Resolve<FoodChainResolver>(), c.Resolve<AnimalDeathSignal>(), c.Resolve<AnimalFactory>()),
  Singleton).AsSelf()` — the factory overload exposes `IFixedTickable`+`IContactSink`+`IDisposable` via
  `AsImplementedInterfaces`, **plus `.AsSelf()`** so the Spawner can inject the concrete `Simulation` for
  `Register`/`Population`; one registration = one instance;
  `RegisterEntryPoint<Spawner>(c => new Spawner(c.Resolve<SpawnPlanner>(), c.Resolve<AnimalFactory>(),
  c.Resolve<Simulation>(), c.Resolve<IOccupancyQuery>(), c.Resolve<IRandom>(), in bounds), Singleton)`
  (`IAsyncStartable`).
- **Disposal is automatic:** the container disposes any singleton whose instance is `IDisposable`
  (`Simulation`, `AnimalFactory`, `DeathCounters`, `HudPresenter`) on scope teardown — no explicit
  `As<IDisposable>()` needed.

### H. Scene + project wiring — `Gameplay.unity`, `Animal.prefab`, physics matrix
- `Gameplay.unity` contains: the `GameLifetimeScope` GameObject (all serialized refs assigned to the
  real `AnimalCatalog.asset` / `SimConfig.asset` / `Animal.prefab` / scene `Camera` / `HudView` /
  animals root); a **top-down perspective Camera looking straight down −Y** (rotation ≈ (90,0,0)) framing
  ≈ **20×12 m** (GDD §3/§9) — **re-author** the current placeholder camera (identity rotation at
  `(0,1,-10)`, i.e. horizontal — wrong for the frustum formula) + a Directional Light; a **uGUI Canvas**
  (top-right) with the two `Text` labels + `HudView`; a **thin static floor
  collider** at the ground plane (guardrails §7); an animals-root empty.
- `Animal.prefab` verified: `Rigidbody` + primitive `Collider` + `Animal` + layer **Animal** +
  `AnimalPhysicsMaterial`. (The runtime profile — gravity off, `sleepThreshold 0`, Discrete, freeze
  posY/rotX-Z, damping — is applied by `Animal.OnSpawn`; the prefab only carries the static setup.)
- Physics collision matrix limited to **Animal×Animal + Animal×Floor** (guardrails §7).
- `AnimalCatalog.asset` precondition: contains Frog (Prey, Jump) + Snake (Predator, Linear) with
  movement strategies assigned (T02/T06) and **≥ 1 Predator** (the `SpawnPlanner` predator-floor
  precondition).

## Pure-rule EditMode test assertions (ship in the same change)

**`CatalogProjectionTests`** — the "wiring test over both lists" (the index contract):
- Length: `specs.Length == weights.Length == catalog.Definitions.Count`.
- Per index `i` (order preserved): `weights[i].Role == def[i].Role` and `weights[i].Weight ==
  def[i].SpawnWeight`; `specs[i].Role/Strength/Size/Mass/SpawnWeight == def[i].*`;
  `specs[i].Movement` is **reference-equal** to `def[i].Movement`;
  `specs[i].Tuning.Speed/JumpDistance/JumpInterval == def[i].*`; and
  `specs[i].Tuning.LinearDamping/WanderRerollMin/WanderRerollMax == config.*`.
- Hermetic case: build a 2-entry catalog of `ScriptableObject.CreateInstance<AnimalDefinition>()` and set
  each definition's role to `{Prey, Predator}` (and `AnimalCatalog._definitions`) **via reflection over the
  private `[SerializeField]` fields** — the repo's established idiom (`MovementBehaviourStatelessTests` uses
  `BindingFlags.NonPublic`) or a `SerializedObject` pass (the test asmdef is Editor-only) — since the SOs
  are getter-only with no setters, so plain `CreateInstance` yields all-`Prey`/all-default. Assert order
  preserved + the Predator lands at its catalog index. *(Or drop this case and rely on the real-asset case
  below — it already carries a real `Predator`, Snake.)*
- Real-asset case (EditMode `AssetDatabase`): `AssetDatabase.LoadAssetAtPath<AnimalCatalog>(
  "Assets/_Project/ScriptableObjects/AnimalCatalog.asset")` → projection round-trips Frog/Snake fields 1:1
  and the result contains ≥ 1 `Role.Predator` (the **first** AssetDatabase-backed test — Editor-only).
- `BuildSpawnTuning`: `IntervalMin 1`, `IntervalMax 2`, `MaxPopulation 120`, `PredatorFloor 1`,
  `ClearanceRadius 1`, `MaxPlacementAttempts 10` (from a default `SimConfig`).
- `BuildSimulationTuning`: `BounceKick 4`, `GraceSeconds 0.6`.

**`FieldBoundsFactoryTests`** — the frustum closed-form:
- `cameraPosition (0, 10.392, 0)`, `vFov 60`, `aspect 1.6667`, `groundY 0`, `margin 1` →
  `HalfExtents ≈ (10, 6)` (full ≈ **20×12 m**, GDD §9), `Center == (0,0,0)`, `InnerMargin == 1`
  (tolerance 1e-3). (`margin` is an opaque pass-through in the unit test; the **production** value is
  `config.FieldInnerMargin ≥ JumpDistance` ≈ 1.5 m — decision 1, not 1.)
- Camera offset `(3, h, -2)` → `Center == (3, groundY, -2)` (XZ tracks the camera, Y = groundY).
- `halfWidth == halfDepth * aspect` for a second aspect; `halfDepth` scales linearly with camera height.

**`SimulationTests` (extend)** — the population snapshot:
- Register K animals (specs with mixed roles) → `Population.Total == K`,
  `Population.PredatorCount == #predators`.
- After a resolved predator-eats-prey death drains → `Total` and (if the victim was a predator)
  `PredatorCount` decremented; no double count.

*(The Spawner loop, `PhysicsOccupancyQuery`, `HudView`, the camera read, and the whole container/scene
are integration/adapters → covered by the smoke pass, not unit tests — guardrails §14.)*

## Implementation notes

**New files** — `Composition/GameLifetimeScope.cs`, `Composition/CatalogProjection.cs`,
`Spawning/Spawner.cs`, `Spawning/PhysicsOccupancyQuery.cs`, `Core/FieldBoundsFactory.cs`,
`UI/HudView.cs`; tests `CatalogProjectionTests.cs`, `FieldBoundsFactoryTests.cs`. **Existing-to-extend**
— `Core/Simulation.cs` (`Population`), `Config/SimConfig.cs` (`_fieldInnerMargin` ≈ 1.5 m = `JumpDistance`, getter `FieldInnerMargin` — decision 1),
`ZooWorld.Runtime.asmdef` (`UnityEngine.UI`), `SimulationTests.cs`, `Gameplay.unity`, `Animal.prefab`,
ProjectSettings (physics matrix), `SimConfig.asset` (margin). Commit `.meta` files with new assets.

**Guardrail refs** — composition root + entry points + value structs built at composition and passed via
factory-lambda registrations (§9/§10); one `Simulation` tick owner, `Animal` stays dumb (§2); no `Resolve` from gameplay, no static
singletons (§9); occupancy/camera behind seams, read once at composition (§10/§12); pooled spawn path
`factory.Spawn` → `simulation.Register` (§11); struct events, non-capturing handlers (§13).

**Pitfalls** —
- **`Spawn` then `Register` are two steps:** `AnimalFactory.Spawn` returns active-but-unregistered; the
  Spawner must call `Simulation.Register` (seeds movement state via `SpawnSeed`, wires the contact sink,
  warms `Body`, adds to the active list). Missing it = a frozen, collision-deaf animal.
- **`Spawn` may return `null`** at the pool cap — guard before `Register` (the planner gates the live
  count upstream, but keep the defensive null-check).
- **VContainer registration traps (source-verified; the slice won't build/run if missed):**
  (a) **`in` struct ctor params are ByRef types** (`Simulation(in FieldBounds/in SimulationTuning)`,
  `SpawnPlanner(in SpawnTuning)`, `Spawner(in FieldBounds)`) — the reflection injector resolves
  `FieldBounds&`, which is unregistered, and **throws** at build. Construct these via **factory lambdas**
  (§G), never `RegisterInstance(value)` + auto-injection.
  (b) **`RegisterInstance(AnimalSpec[])` registers only `AnimalSpec[]`, not `IReadOnlyList<AnimalSpec>`** —
  the consumer then **silently** gets an *empty* array (VContainer's single-element-collection fallback),
  so `AnimalFactory` builds 0 pools and `Spawn` throws `IndexOutOfRange`. §G passes the arrays as captured
  locals, avoiding this (or add `.As<IReadOnlyList<AnimalSpec>>()` if you do register them).
  (c) **`AnimalFactory`'s two `int` params (`maxPopulation`, `predatorFloor`) collide by type** — bind by
  parameter NAME, or (preferred) use the §G factory lambda.
  (d) **Single-instance dual-exposure** for `Simulation` and `AnimalDeathSignal`: expose both contracts
  from ONE registration (`.AsSelf()` / `.As<IAnimalDeathSignal>()` chained). `RegisterEntryPoint` alone
  exposes only the *interfaces*, not the concrete type the Spawner injects; a second separate `Register`
  makes a SECOND instance — the Spawner then `Register`s animals into a **non-ticked** `Simulation` (every
  animal frozen, **no Console error**), or deaths never reach the counters.
- **One shared `IRandom`** singleton (`SeededRandom(_randomSeed)` — it has no parameterless ctor) feeds
  both `SpawnPlanner` (interval/species/placement) and `Simulation` (wander); don't bind two. *Species &
  placement selection is deterministic given the seed; the inter-spawn delay rides real time via
  `UniTask.Delay` (fine at M2) — the spawn schedule is not wall-clock reproducible.*
- **UniTask cancellation** — await the `StartAsync` token; on scope dispose the loop must exit without a
  leaked task or a `Spawn` after teardown (guardrails §9).
- **Placement Y** — `SpawnPlanner.TryFindSpawnPosition` writes `bounds.Center.y`; keep the camera/ground
  plane and `FieldBounds.Center.y` at the same ground `Y` so spawns land on the floor.

## Out of scope (→ T09)

The "Tasty!" label + pool, spawn scale-in / death-puff feedback, the `AnimationCurve` jump arc, the
UniTask lerp helper; per-species `MaterialPropertyBlock` colour (prey/predator distinction); the Rabbit
species (data-only extension proof); the polished Boot→Running scene framing; the standalone Windows
build; README/ARCHITECTURE. No new gameplay rules — T08 is wiring + adapters only.

## Verification

- **EditMode:** the new/extended tests above pass; **full suite green** (no regression on the T01–T07
  ~112 tests), **0 Console errors** (build-and-test.md). Run via MCP `run_tests` (active scene saved)
  or Test Runner.
- **Smoke (Play / MCP):** enter Play on `Gameplay.unity` — first confirm the composition read sees a
  straight-down camera (`Vector3.Dot(camera.transform.forward, Vector3.down) ≈ 1`) so `FieldBounds` matches
  the framed view; then: animals spawn on the 1–2 s cadence and move (none frozen); an animal reaching the
  edge turns back (bounds-return, role-agnostic); prey×prey visibly fly apart and both live; a predator
  eats prey → the prey despawns and **"Dead prey"** increments by exactly one (the **HUD counters are the
  role oracle** — animals share one placeholder colour until T09); the population holds under the 120 cap
  with ≥ 1 predator present; Console clean. `dotnet format --verify-no-changes` + the forbidden-API grep
  (guardrails §12) clean on the new/edited files.
- **Done** = full suite green + Console clean + the smoke flow observed **this session** with cited
  evidence (build-and-test.md; agent-verification.md §2).

## What was actually done

— *(filled on close: what shipped, deviations, the commit, the date.)*
