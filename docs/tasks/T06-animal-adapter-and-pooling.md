# T06 — `Animal` dumb adapter + physics profile + object pooling (factory) + reset contract

| | |
|---|---|
| Milestone | M2 — adapters |
| Depends on | T01 |
| Touches scene/prefabs | **yes** — the `Animal` prefab + a `PhysicsMaterial` asset + the **Animal** physics layer + the Animal×Animal collision-matrix entry (no scene edits; the prefab is instantiated by the pool, placed into the scene only at T08) |
| Status | ✅ done |

## Goal

Build the runtime half of an animal and its lifecycle: the **`Animal`** MonoBehaviour — a *dumb adapter*
(Rigidbody + collider + per-animal state + pool-reset hooks, **no** `Update`/`FixedUpdate`, **no** service
deps, **no** rules) — plus the **`AnimalFactory`** (the pool's create + take/configure + return layer, GDD §4
"one layer") with the GDD §11 **reset contract** and the §5/§9 pool sizing. This is the body the T07
`Simulation` ticks/collides and the T08 spawner takes from. (GDD §4/§5/§7/§11; guardrails §4/§7/§11/§12;
ADR 0001 physics baseline; ADR 0002 §1/§3/§6.)

## Scope decisions (resolved — chosen as the best fit; flag at review to override)

T06 is the **first adapter task**, so the testing model shifts: pure rules are done (M1); here the
EditMode tests instantiate the `Animal` in the test and assert **component config + reset bookkeeping**
(physics is not *simulated* headless, but every value the profile sets and every field the reset clears
**is** checkable), and the *live* physics behaviour is smoke-verified at T08. These decisions pin the
boundaries.

1. **T06 ships the *inert* adapter + the pool; the tick and the collision pipeline are T07.** Per the
   matrix, "collision enqueue→drain→resolve→events" is **all T07**. So T06's `Animal` has **no
   `OnCollisionEnter`** and **no movement tick** — it is a configured, pooled body that just *sits* until
   T07 drives it. T06 delivers: the physics profile, the per-animal state + accessors the T07 `Simulation`
   reads/writes (`Role`/`Strength`/`Seq`/`IsDead`, `ref MovementState`, `MovementTuning`, the `Rigidbody`),
   `MarkDead()`, and the pool reset. (Carry-forward: T07 adds `OnCollisionEnter` → enqueue + the drain, and
   the `Simulation.FixedTick` that ticks `Movement` and applies velocity.)

2. **The factory consumes a value-spec list (`AnimalSpec`), not the SO catalog directly** — so its
   pooling/sizing/configure logic is EditMode-testable with hand-built specs. This mirrors **T04**
   (`SpeciesWeight`/`SpawnTuning`): `AnimalDefinition`/`SimConfig` are SOs with `[SerializeField] private`
   fields and **no setters**, so a test can't author one with a chosen role/size/weight. The
   **`AnimalDefinition` + `SimConfig` → `AnimalSpec` build is T08** (composition), **1:1 in catalog order**
   (the same positional index↔catalog contract as T04 — `specs[i]` ⇔ `Definitions[i]` ⇔ `SpeciesWeight[i]`;
   the pool is keyed by that index). *Rejected:* read the SOs in the factory (untestable headless) or
   reflection-author SOs in tests (the project rejected this in T04).

3. **The physics profile is applied in code at `OnSpawn` (authoritative + testable); the prefab carries
   the components.** The prefab holds the GameObject + mesh + `SphereCollider` (+ the `PhysicsMaterial`) +
   `Rigidbody` + `Animal` on the **Animal** layer; `Animal.OnSpawn` then **sets the full Rigidbody profile
   every take** (constraints/gravity/sleep/discrete/damping/mass) + scale, so the profile is code-enforced
   (an EditMode test asserts it, and a mis-edited prefab self-heals). *Rejected:* rely only on prefab-baked
   values (not unit-checkable headless — `Awake` does not run in EditMode tests; the profile could silently
   drift).

4. **`AnimalFactory` is ONE layer (GDD §4: "not a factory wrapping a pool").** It owns the per-index
   free-lists internally (`Stack<Animal>[]`) — **no** separate `AnimalPool`/`PoolSizing` types. Sizing is a
   private helper, covered by `AnimalFactoryTests` via prewarm-count + capacity assertions.

5. **Pool growth model:** prewarm each index to `round(SpawnWeight × MaxPopulation)`; on a take past the
   free-list, **lazily `Instantiate` up to the per-index cap** (one-time bounded growth, never `Destroy`);
   steady state = pure reuse. "Never `Instantiate`/`Destroy` at runtime" (GDD §5 / guardrails §12) means
   **no per-spawn churn** — bounded one-time growth to the cap is the pool *filling*, not churn. Cap =
   `MaxPopulation + PredatorFloor` for **all** pools (uniform safe upper bound — satisfies the T04
   carry-forward "predator pool ≥ MaxPopulation + PredatorFloor" and GDD §9 "max ≥ maxPopulation"; a cap is
   only an upper bound, pools grow to real demand). A take at the cap returns **`null`** (defensive; the
   T04 `SpawnPlanner` already gates the live count, so this should not occur in the wired sim).

6. **Deferred (named so they aren't lost):** the **`CancellationTokenSource`** in the reset contract (GDD
   §11) → **T09** (nothing has an in-flight UniTask feedback to cancel until then); **colour via
   `MaterialPropertyBlock`** (GDD §13) → **T09**; the **floor collider + Floor layer + Animal×Floor** → the
   **T08** scene (gravity is off, so the inert T06 body doesn't fall — no floor needed yet); the **child
   mesh for the visual Y-hop** → **T09** (jump arc); **DI registration + the SO→`AnimalSpec` build + the
   spawn cadence loop + scene placement** → **T08**.

## Acceptance criteria

### Value seam (`ZooWorld.Core`)

**`AnimalSpec`** — `Core/AnimalSpec.cs`: `public readonly struct AnimalSpec`; ctor `(Role role, int
strength, float size, float mass, float spawnWeight, MovementBehaviour? movement, in MovementTuning
tuning)`; read-only props `Role Role`, `int Strength`, `float Size`, `float Mass`, `float SpawnWeight`,
`MovementBehaviour? Movement`, `MovementTuning Tuning`. The complete per-species runtime spec the factory
needs (built from `AnimalDefinition` + `SimConfig` at T08). `Movement` may be `null` in T06 (the tick is
T07).

### The adapter (`ZooWorld.Animals`)

**`Animal`** — `Assets/_Project/Scripts/Runtime/Animals/Animal.cs`:
- `public sealed class Animal : MonoBehaviour` — **no `Update`/`FixedUpdate`/`OnCollisionEnter`** (T07);
  **no service dependencies / no `Resolve`**; ≤ ~150 lines (guardrails red-flag).
- Cached body: `private Rigidbody _rigidbody;` exposed via a **lazy, self-caching** getter
  `public Rigidbody Body => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());` —
  `OnSpawn`/`OnDespawn` and the T07 `Simulation` use `Body`. **Lazy, NOT Awake-only:** `Awake` does **not**
  run for `AddComponent`/`Instantiate` in EditMode tests (decision 3), so an Awake-only cache would be
  `null` and NRE every test + the factory prewarm. `GetComponent` fires **once** (first `Body` access,
  then cached), at take/return time — never in the T07 per-frame tick.
- Runtime state (read by the T07 `Simulation` to build `AnimalState`): `public Role Role { get; private
  set; }`, `public int Strength { get; private set; }`, `public long Seq { get; private set; }`,
  `public bool IsDead { get; private set; }`; `public MovementBehaviour? Movement { get; private set; }`,
  `public MovementTuning Tuning { get; private set; }`; `private MovementState _movementState;` with
  `public ref MovementState MovementState => ref _movementState;` (by-ref so the T07 strategy advances it
  in place).
- `public void OnSpawn(in AnimalSpec spec, long seq, Vector3 position)` — the take-from-pool reset +
  configure. Applies, in order:
  - **Physics profile (every take):** `Body.useGravity = false`; `Body.sleepThreshold = 0f`;
    `Body.collisionDetectionMode = CollisionDetectionMode.Discrete`; `Body.constraints =
    RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX |
    RigidbodyConstraints.FreezeRotationZ`; `Body.linearDamping = spec.Tuning.LinearDamping`;
    `Body.mass = spec.Mass`.
  - **Velocity clear:** `Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;`.
  - **Transform:** `transform.localScale = Vector3.one * spec.Size;` `transform.position = position;`
    `transform.rotation = Quaternion.identity;`.
  - **State:** `Role = spec.Role; Strength = spec.Strength; Movement = spec.Movement; Tuning =
    spec.Tuning; Seq = seq; IsDead = false;`.
  - **Movement state reset:** `_movementState = default;` (Heading/`NextHeadingReroll`/`NextLeapTime`/
    `GraceUntil` cleared to a known baseline — no stale heading or leftover grace). **Caveat (→ T07):**
    `default` leaves `NextLeapTime = 0`, and `JumpMove.Tick` leaps when `Clock.Now >= NextLeapTime`, so a
    just-reset jumper would leap on its **first tick** — a GDD §11 frame-1 jump. T06's `OnSpawn` is
    clockless and cannot seed it; **T07's `Simulation` MUST set `NextLeapTime = clock.Now + JumpInterval`
    when it registers/first-ticks a spawned animal** (it owns `IClock`). The no-frame-1-leap guarantee
    completes at T07.
- `public void OnDespawn()` — return-to-pool: `Body.linearVelocity = Vector3.zero;
  Body.angularVelocity = Vector3.zero;` (idempotent with `OnSpawn`; a returned body carries no
  velocity into its dormant period).
- `public void MarkDead()` — `IsDead = true;` (the T07 `Simulation` calls this on a resolved death,
  before despawn; the dead-guard reads it).
- No Unity statics beyond the body's own components; `#nullable enable`, `sealed`, Allman, XML docs, C# 9.

### The pool/factory (`ZooWorld.Animals`)

**`AnimalFactory`** — `Assets/_Project/Scripts/Runtime/Animals/AnimalFactory.cs`:
- `public sealed class AnimalFactory : IDisposable` — the spawn+configure+pool layer (GDD §4, one layer);
  a DI scope-singleton (binding is T08).
- `public AnimalFactory(Animal prefab, IReadOnlyList<AnimalSpec> specs, int maxPopulation, int
  predatorFloor, ISpawnSequence sequence, Transform? parent)` — builds `specs.Count` internal pools
  (`Stack<Animal>`), **prewarms** pool `i` with `round(specs[i].SpawnWeight × maxPopulation)` instances
  (instantiated under `parent`, `OnDespawn`-reset, `SetActive(false)`), records the per-index **cap** =
  `maxPopulation + predatorFloor`.
- `public Animal? Spawn(int index, Vector3 position)` — take from pool `index` (pop the stack, or
  `Instantiate` one more if the stack is empty **and** the pool is below its cap, else **return `null`**);
  `SetActive(true)`; `animal.OnSpawn(specs[index], sequence.Next(), position)`; return it. The taken
  animal remembers its index (private field, set here) so `Despawn` returns it to the right pool.
- `public void Despawn(Animal animal)` — `animal.OnDespawn()`; `SetActive(false)`; push to its index's
  stack. (Never `Destroy`.)
- `public void Dispose()` — destroy all pooled + active instances, **edit-mode-safe**:
  `Application.isPlaying ? Object.Destroy(go) : Object.DestroyImmediate(go)`. The EditMode `[TearDown]`
  calls `Dispose()`, and `Object.Destroy` is forbidden in edit mode (logs an error + leaks) — so it must
  branch on play state.
- **Test-facing read-only accessors:** `public int Capacity(int index)`; `public int FreeCount(int
  index)`; `public int InstantiatedCount { get; }` (total ever instantiated — the no-runtime-churn probe).
- No LINQ in any hot path; `Instantiate`/`Destroy` only at prewarm/Dispose/bounded-growth (never per
  steady-state spawn).

### Assets & project settings

- **`Animal` prefab** — `Assets/_Project/Prefabs/Animal.prefab`: a sphere-primitive GameObject with
  `MeshFilter` (built-in Sphere) + `MeshRenderer` (default URP material — colour via MPB is T09) +
  `SphereCollider` (radius `0.5`, `sharedMaterial` = the `PhysicsMaterial` below) + `Rigidbody` + the
  `Animal` component, on the **Animal** layer. (Per-animal scale/mass/damping are code-set at `OnSpawn`.)
- **`PhysicsMaterial`** — `Assets/_Project/Physics/AnimalPhysicsMaterial.physicsMaterial`: `bounciness 0`,
  `dynamicFriction 0`, `staticFriction 0`, `bounceCombine`/`frictionCombine` = `Minimum` (low-restitution,
  low-friction; GDD §7). *(Unity 6 type is `PhysicsMaterial`, not the deprecated `PhysicMaterial`.)*
- **Animal layer** (TagManager) + **Animal×Animal** enabled in the Physics collision matrix
  (DynamicsManager). (Floor layer + Animal×Floor + the floor collider are T08.) Commit the `.meta` for the
  prefab + material and the changed `ProjectSettings/*.asset`.

### EditMode tests (`ZooWorld.Tests.EditMode`)

Both fixtures build `Animal`s in code (no prefab asset / no `AssetDatabase`): a `[SetUp]` makes a stub
`Animal` source GameObject (`new GameObject` + `AddComponent<Rigidbody>` + `AddComponent<SphereCollider>` +
`AddComponent<Animal>`); a `[TearDown]` `Object.DestroyImmediate`s every created object (and
`factory.Dispose()`). Helper `Spec(Role role, int strength = 0, float size = 1f, float mass = 1f, float weight = 1f)`
builds an `AnimalSpec` with `MovementTuning(2.5f, 1.5f, 1.5f, 4f, 0.8f, 1.5f)` and `movement: null`. Physics is
**not simulated** headless — assert *configured values*, not motion.

**`AnimalTests`** — `Tests/EditMode/AnimalTests.cs`:
- **`OnSpawn_AppliesPhysicsProfile`:** after `OnSpawn(Spec(Role.Prey, mass: 2f), 1L, Vector3.zero)` →
  `Body.useGravity` is `false`; `Body.sleepThreshold == 0f`; `Body.collisionDetectionMode ==
  CollisionDetectionMode.Discrete`; `Body.constraints == (FreezePositionY | FreezeRotationX |
  FreezeRotationZ)`; `Body.linearDamping == 4f`; `Body.mass == 2f`.
- **`OnSpawn_AppliesTransform`:** `Spec(size: 1.5f)`, `position (3,0,4)` → `transform.localScale ==
  Vector3.one * 1.5f`; `transform.position == (3,0,4)`; `transform.rotation == Quaternion.identity`.
- **`OnSpawn_SetsRuntimeState`:** `OnSpawn(Spec(Role.Predator, strength: 5), 7L, Vector3.zero)` → `Role ==
  Predator`; `Strength == 5`; `Seq == 7L`; `IsDead` is `false`; `Tuning.LinearDamping == 4f`.
- **`MarkDead_SetsIsDead`:** after `MarkDead()` → `IsDead` is `true`.
- **`Reuse_ClearsStaleState`** (the core reset contract): `OnSpawn(Spec(Prey), 1L, posA)`;
  `Body.linearVelocity = new Vector3(5,0,0)`; `MarkDead()`; mutate `MovementState` (e.g.
  `MovementState.Heading = Vector3.right; MovementState.GraceUntil = 99f`); `OnDespawn()`; then
  `OnSpawn(Spec(Predator), 2L, posB)` → `Body.linearVelocity == Vector3.zero`; `IsDead` is `false`; `Seq
  == 2L`; `Role == Predator`; `MovementState.Heading == Vector3.zero` and `MovementState.GraceUntil ==
  0f` (no stale velocity, stuck dead-flag, or leftover grace/heading).

**`AnimalFactoryTests`** — `Tests/EditMode/AnimalFactoryTests.cs`. Specs `[Prey 0.45, Predator 0.30, Prey
0.25]`, `maxPopulation 120`, `predatorFloor 1`, `FakeSpawnSequence` (from T01); `parent: null`.
- **`Prewarm_RoundsPerSpecies`** (GDD §9): `FreeCount(0) == 54`, `FreeCount(1) == 36`, `FreeCount(2) ==
  30` (`round(0.45×120)`, `round(0.30×120)`, `round(0.25×120)`); `InstantiatedCount == 120`.
- **`Capacity_IsMaxPopulationPlusPredatorFloor`:** `Capacity(i) == 121` for every `i` (decision 5).
- **`Spawn_ReturnsConfiguredActiveAnimal`:** `Spawn(1, pos)` → non-null, `gameObject.activeSelf` is
  `true`, `Role == Predator`, `Seq == 1L`; `InstantiatedCount == 120` (taken from the prewarm, **no new
  instantiate**); `FreeCount(1) == 35`.
- **`SpawnThenDespawn_ReusesSameInstance`:** `a = Spawn(0,…)`; `Despawn(a)` → `a.gameObject.activeSelf`
  is `false`, `FreeCount(0)` back to `54`; `b = Spawn(0,…)` → `ReferenceEquals(a, b)` and
  `InstantiatedCount == 120` (pure reuse, no churn).
- **`Spawn_AssignsMonotonicSeq`:** three spawns → `Seq` `1L, 2L, 3L` (the `FakeSpawnSequence` order).
- **`Spawn_AtCap_ReturnsNull_NoInstantiateBeyondCap`** (small numbers for a cheap exhaustion): a factory
  with specs `[Predator 1.0]`, `maxPopulation 2`, `predatorFloor 1` → `Capacity(0) == 3`; spawn 3 → all
  non-null; the **4th** `Spawn(0,…)` returns `null`; `InstantiatedCount == 3` (never instantiated past the
  cap).

(Guardrails §14: pure rules ship EditMode tests in the same change; the adapter's testable contract — the
profile, the reset, the pool bookkeeping — is unit-tested here; the *live* physics is the T08 smoke.)

## Implementation notes
- **New files:** `Scripts/Runtime/Core/AnimalSpec.cs`; `Scripts/Runtime/Animals/{Animal,AnimalFactory}.cs`;
  `Prefabs/Animal.prefab`; `Physics/AnimalPhysicsMaterial.physicsMaterial`; `Tests/EditMode/{AnimalTests,
  AnimalFactoryTests}.cs`. **Project settings:** Animal layer + Animal×Animal matrix. **No asmdef change**
  (`Animals/` is under the `ZooWorld.Runtime` root; no VContainer/UniTask needed in T06 — DI + cadence are
  T08).
- **`using` outside the namespace** (`System.*` first): `System` (`IDisposable`),
  `System.Collections.Generic` (`IReadOnlyList`, `Stack`), `UnityEngine`, `ZooWorld.Config`
  (`Role`, `MovementBehaviour`), `ZooWorld.Core` (`AnimalSpec`, `MovementTuning`, `ISpawnSequence`).
  `Animal` uses `ZooWorld.Animals` (`MovementState`) — same namespace. Tests add `NUnit.Framework`,
  `ZooWorld.Tests.EditMode.Fakes` (`FakeSpawnSequence`), `ZooWorld.Core`, `ZooWorld.Config`.
- Use MCP for the prefab/material/layer/matrix (`manage_gameobject`/`manage_prefabs`/`manage_asset`/
  `manage_editor`(add_layer)/`manage_physics`); verify with `read_console` (0 errors) after a
  `scope: all` refresh — new files need a full import (the T03/T05 gotcha).
- **Pitfalls:** (a) `sleepThreshold = 0` is mandatory — a gravity-off body otherwise sleeps and silently
  ignores velocity sets (guardrails §7 "a real bug"); (b) constraints freeze **position-Y + rotation-X/Z**
  only (X/Z move, Y-rotation turns); (c) `OnSpawn` resets on **take**, `OnDespawn` on **return** — a
  reused body must never carry stale velocity or a stuck `IsDead`; the **frame-1-leap** half of GDD §11
  needs `NextLeapTime` seeded with the clock → **T07** (see the reset caveat); (d) the cap check guards the
  bounded growth — never `Instantiate` past `Capacity`; (e) resolve the body **lazily** via `Body` (cached
  on first use), NOT Awake-only, or the EditMode tests + prewarm NRE (`Awake` doesn't run headless);
  `GetComponent` stays out of the T07 tick; (f) **no** `Update`/`FixedUpdate`/`OnCollisionEnter` on
  `Animal` (T07 owns the tick + the collision pipeline — guardrails §2/§17); (g) EditMode `[TearDown]`
  must `DestroyImmediate` (not `Destroy`, which errors + leaks in edit mode), and since the tests call
  `factory.Dispose()`, `Dispose()` itself branches `isPlaying ? Destroy : DestroyImmediate`.

## Out of scope
- **`OnCollisionEnter` → enqueue, the drain, the `Simulation` tick** (MoveContext, grace, bounds-return,
  velocity apply, prey×prey impulse, `AnimalDied`/"Tasty!") — **T07**.
- **DI wiring + SO→`AnimalSpec`/`SpeciesWeight` build (catalog-order) + the UniTask spawn cadence + the
  production `IOccupancyQuery`/`FieldBounds` + scene placement + the floor collider + Play smoke** —
  **T08**.
- **`CancellationTokenSource` in the reset, colour via `MaterialPropertyBlock`, the child-mesh visual
  Y-hop, the Rabbit definition asset** — **T09** (decision 6).
- Per-species *prefabs* (birds/fish/crabs) — the keyed-by-index pool is the seam; not built now (3 shipped
  species share the one primitive prefab). Multi-tier diets — GDD non-goal.

## Verification
- **EditMode:** `AnimalTests` + `AnimalFactoryTests` green (profile, transform, state, `MarkDead`, reuse
  reset; prewarm rounding, capacity, configured spawn, reuse-no-churn, monotonic seq, cap→null); **full
  suite green** (77 prior + T06's new); **0 Console errors**. (`Window → Test Runner → EditMode → Run
  All`, or MCP `run_tests` with `Gameplay.unity` active.)
- `dotnet format --verify-no-changes` green on the new files; forbidden-API / Unity-statics grep clean in
  `.Animals` (no `GameObject.Find`/`Camera.main`/`Time`/`Random`/`.material`/LINQ-in-loops; `GetComponent`
  only at take/return via the lazy `Body` getter, never in the tick).
- **Smoke (limited, this task):** enter Play with one factory-spawned `Animal` in the scene → it stays on
  the XZ plane (Y frozen, gravity off), does **not** sleep/freeze, and no `Instantiate`/`Destroy` fires on
  take/return (Profiler/Console). Full movement + collision + crowd smoke is **T08** (needs the T07 tick).

## Carry-forward (propagate on close)
- **README matrix:** T06 row → name the **`AnimalFactory`** class + pools (max ≥ MaxPopulation +
  PredatorFloor) + reset contract; T08 row → change "factory + pools" to "factory **DI binding** +
  SO→`AnimalSpec`/`SpeciesWeight` build + cadence" (the factory **class** ships in T06; T08 wires it). Flip
  the T06 `Brief` flag `·→✓`.
- **→ T08 (parallel-list desync):** build the `AnimalSpec` list **and** T04's `SpeciesWeight` list from
  **one** `catalog.Definitions` pass (same index keys the pool, the planner, and the spec), and extend the
  T04 wiring-test to assert `specs.Count == speciesWeights.Count == Definitions.Count` and that each entry
  matches `Definitions[i]` by `Id` — a same-count catalog reorder otherwise silently spawns the wrong
  species into the wrong pool, with no failing test.
- **→ T07 (frame-1 leap):** the `Simulation` seeds `NextLeapTime = clock.Now + JumpInterval` when an animal
  is spawned/first-ticked (T06's clockless reset leaves it `0`, which would leap on frame 1 — GDD §11).
- **→ T07 (deferred from the post-implementation file-audit):** add a **double-despawn guard** to `Despawn`
  (e.g. an internal `_pooled` flag set in `CreatePooled`/`Despawn`, cleared in `Spawn`, early-returning if
  already pooled) — `Despawn` has no liveness/ownership check, so a repeated `Despawn(a)` would double-push
  and alias. Deferred because **T07 is `Despawn`'s first real caller** (the drain despawns each death once
  via the dead-guard, so it can't manifest in T06). (The audit's other items — `Dispose` `Array.Clear` and
  the test hardening — were applied at T06 close; the audit was clean on code/scope/docs.)

## What was actually done

**Implemented 2026-06-26** — the Animal adapter + the AnimalFactory pool + the reset contract, per the
validated brief.

- **`AnimalSpec`** (`ZooWorld.Core`): the `readonly struct` value seam (Role/Strength/Size/Mass/SpawnWeight/
  `MovementBehaviour?`/`MovementTuning`) the factory consumes (built from the SOs at T08).
- **`Animal`** (`ZooWorld.Animals`): a sealed MonoBehaviour dumb adapter — lazy `Body` getter;
  `Role`/`Strength`/`Seq`/`IsDead`/`Movement`/`Tuning`/`ref MovementState` accessors; `OnSpawn` (full
  Rigidbody profile + scale + state + movement-state reset), `OnDespawn`, `MarkDead`; `internal PoolIndex`.
  No `Update`/`FixedUpdate`/`OnCollisionEnter`, no service deps.
- **`AnimalFactory`** (`ZooWorld.Animals`): the one-layer create+pool+configure — `Stack<Animal>[]` per
  index, prewarm `round(weight×maxPop)`, cap `MaxPopulation + PredatorFloor`, lazy bounded growth, reuse;
  `Spawn`/`Despawn`/`Dispose`/`Capacity`/`FreeCount`/`InstantiatedCount`; `IDisposable`, edit-mode-safe
  `Dispose`.
- **Assets:** the **Animal** layer (slot 8); `Prefabs/Animal.prefab` (sphere + Rigidbody + SphereCollider +
  the material + `Animal`, on the Animal layer); `Physics/AnimalPhysicsMaterial.asset` (bounciness/friction
  0, combine Minimum). Animal×Animal collides by default (no matrix edit; the Floor pairing is T08).
- **Tests** (`ZooWorld.Tests.EditMode`): `AnimalTests` (5) + `AnimalFactoryTests` (7) — **12 new** (incl. a
  post-audit cross-index `Despawn` routing test; `Reuse_ClearsStaleState` also gained an independent
  `OnDespawn` velocity assertion + full 4-field `MovementState` reset coverage).

**Deviations from the brief (2, both mechanical — intent unchanged):**
1. **`PhysicsMaterial` saved as `.asset`, not `.physicsMaterial`.** The brief's `.physicsMaterial` +
   `AssetDatabase.CreateAsset` raised a Unity Exception-level console message (*"CreateAsset() should not be
   used to create a file of type 'physicsMaterial' … change the file type to '*.asset'"*). Applied Unity's
   own suggested fix (`.asset`); the prefab's collider references it by GUID (extension-agnostic), so it's
   functionally identical and warning-free.
2. **`Animal._rigidbody` declared `= null!`** (not the brief's bare `private Rigidbody _rigidbody;`). Under
   `#nullable enable` a bare non-nullable uninitialized field is CS8618; `= null!` + the brief's exact lazy
   `Body` getter (works via Unity's overloaded `!=`) is the standard warning-free idiom. Same behaviour.

**Post-audit hardening (applied at close):** a post-implementation file-audit (clean on code/scope/docs;
all 5 findings minor, code verified correct) surfaced test-coverage gaps + one latent `Dispose` invariant;
applied — `Array.Clear(_countPerIndex)` in `Dispose`, the cross-index `Despawn` routing test, and the
`OnDespawn`/4-field-`MovementState` assertions noted above. The `Despawn` double-despawn guard is deferred
to **T07** (its first real caller).

**Verification (this session, via MCP):** **EditMode 89/89 green** (77 prior + T06's 12; re-run after the
asset/Play work and the post-audit hardening; ~2.6 s); **0 Console errors/warnings** (after recreating the material as `.asset`);
`dotnet format --verify-no-changes` exit 0 on all 5 files; forbidden-API / Unity-statics grep clean in
`.Animals` (the only hit is the XML-doc comment naming the forbidden methods). **Play smoke:** one
factory-spawned animal under a downward+lateral velocity → `y` stayed `0.0000` (FreezePositionY), moved
~0.68 m on XZ (free), gravity off, never slept (sleepThreshold 0); despawn→respawn reused the same instance
with no Instantiate churn.

**Commit proposed:** `feat: T06 animal adapter + physics + AnimalFactory pool` — _pending human commit_.
