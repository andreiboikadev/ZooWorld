# T04 — SpawnPlanner (interval, weighting, placement, cap + predator-floor)

| | |
|---|---|
| Milestone | M1 — pure core |
| Depends on | T01 |
| Touches scene/prefabs | no (pure rule + 2 value structs + EditMode tests; no assets/scene) |
| Status | ✅ done |

## Goal

Implement the pure **`SpawnPlanner`** that makes every per-tick spawn decision — the random interval, the
weighted species pick (with the anti-deadlock predator floor), the population cap, and in-bounds
placement via `IOccupancyQuery` — all headless C# over T01's structs/seams, EditMode-tested. It unblocks
the T08 spawner loop (UniTask cadence + pool), which orchestrates these decisions but owns no spawn
*formulas*. (GDD §5/§9; guardrails §4/§6/§9/§10/§14; ADR 0002 §4.)

## Scope decisions (please validate before commit)

GDD/guardrails leave the planner's seam shape and the cap↔floor interaction to settle here. Flag if any
reading is wrong; cheap to flip now.

1. **Two value seams instead of consuming the SOs directly.** `AnimalDefinition` and `SimConfig` are
   `ScriptableObject`s with `[SerializeField] private` fields and **read-only accessors (no setters)**, so a
   pure rule cannot be EditMode-tested over them (a test can't author a definition with a chosen
   weight/role). So the planner consumes plain values, mirroring T01's `MovementTuning`/`PopulationSnapshot`:
   - `readonly struct SpeciesWeight { Role Role; float Weight; }` — one per catalog entry, **in catalog
     order**. Built at composition (T08) from each `AnimalDefinition`'s `Role`/`SpawnWeight`.
   - `readonly struct SpawnTuning { float IntervalMin; float IntervalMax; int MaxPopulation; int
     PredatorFloor; float ClearanceRadius; int MaxPlacementAttempts; }` — built at composition from
     `SimConfig`.
   - The planner **returns an `int` index** into that list. **This is a positional contract:** T08 MUST
     build the `SpeciesWeight` list **1:1 from `AnimalCatalog.Definitions`, in the same order, with no
     filtering/reordering**, and **key the pool by the same index** — index `i` always means `Definitions[i]`
     (there is no id-based remap). A reordered/filtered catalog would silently spawn the **wrong species**
     with no failing planner test (T04 is pure-core and can't see the catalog), so T08 must derive the weight
     list and the pool key from **one** `Definitions` pass and ship a wiring-test (`species.Count ==
     catalog.Definitions.Count`). Both structs live in `ZooWorld.Core` (with `PopulationSnapshot`/
     `MovementTuning`). *Rejected alt:* pass `AnimalCatalog`/`SimConfig` SOs into the rule (untestable) or 6
     positional ctor scalars instead of `SpawnTuning` (error-prone).
2. **Predator-floor is a hard guarantee that OVERRIDES the cap** (GDD §5 anti-deadlock; the §14 contract
   *"predatorCount==0 at/below cap → the next spawn is a predator, no eviction"*). When `PredatorCount <
   PredatorFloor`, the next spawn is a **predator** (weighted among predator species) **even at/above the
   cap** — the planner never evicts (it only picks species/positions; removal is predation-only), so the
   live count may exceed the cap by up to `PredatorFloor` — **peak live = `MaxPopulation + PredatorFloor`**
   (121 at the §9 defaults) — until predation reduces it. The GDD's softer *"weight boosted as count → 0"*
   wording is satisfied by the strongest form (a hard floor). **→ T06/T08 carry-forward (named so it isn't
   lost):** the floor-over-cap path makes the **peak live count `MaxPopulation + PredatorFloor`**, so the
   pool(s) must be sized for that peak. GDD §9's per-pool *"max ≥ maxPopulation"* already permits the
   headroom, but the **predator** pool must not be sized to exactly `MaxPopulation` — a too-small pool ⇒ a
   null take / a guardrails §12-forbidden runtime `Instantiate` on this path. No GDD edit (the predator pool
   absorbs the `+PredatorFloor`). **Hard precondition:** the species list MUST contain **≥1 `Role ==
   Predator`** entry (the catalog always ships Snake); `SpawnPlanner` does **not** guard this — with an unmet
   floor and zero predator candidates the weighted pick degenerates (empty set → `total = 0`,
   `rng.Range(0f, 0f) = 0`, no index satisfies `r < cumulative`), so a prey-only list is **undefined
   behavior**, not a supported mode.
3. **`SelectSpeciesIndex` returns `int?`** — `null` = **pause** (cap reached **and** floor satisfied);
   non-null = the chosen species index. This folds cap + floor + weighting into one cohesive "what to spawn"
   decision. Check order: predator-floor (overrides cap) → cap pause → weighted-over-all.
4. **Placement** = uniform-random candidate in the **full** `FieldBounds` rectangle (`x = rng.Range(Center.x
   − HalfExtents.x, Center.x + HalfExtents.x)`, `z = rng.Range(Center.z − HalfExtents.y, Center.z +
   HalfExtents.y)`, `Y = Center.y`; no inner-margin inset — bounds-return handles edges). Test
   `occupancy.IsClear(candidate, ClearanceRadius)`; the **first clear** candidate within
   `MaxPlacementAttempts` wins; else **skip** (return false). Each attempt draws **two** `Value01` (x then z).
   `ClearanceRadius` is the **query-sphere radius** (production seam: `Physics.CheckSphere(candidate,
   ClearanceRadius, AnimalLayer)`), so the effective **center-to-centre** separation from an existing body is
   `ClearanceRadius + that body's radius` (≈ 2 m for ~1 m animals at `ClearanceRadius` 1) — the intended
   reading of GDD §5's provisional *"≥ ~1 m center distance"*, not a literal 1 m between centres (hence the
   `RetriesUntilClear` test's min-sep 2).
5. **`SpawnPlanner` is an instance `sealed class`** (Lifetime.Singleton DI service, guardrails §9), not a
   `static` rule — constructor-injected `(IReadOnlyList<SpeciesWeight>, in SpawnTuning)`. Pure: no Unity
   statics (only `Vector3`/`Mathf` if needed); the DI binding + seam construction is T08.

## Acceptance criteria

### The rule (`ZooWorld.Spawning`)

**`SpawnPlanner`** — `Assets/_Project/Scripts/Runtime/Spawning/SpawnPlanner.cs`:
- `public sealed class SpawnPlanner` — stateless apart from the two injected immutable inputs (the species
  list + tuning), no mutable fields (DI singleton, §9).
- `public SpawnPlanner(IReadOnlyList<SpeciesWeight> species, in SpawnTuning tuning)` — stores both.
- `public float NextInterval(IRandom rng)` → `rng.Range(tuning.IntervalMin, tuning.IntervalMax)` — a value in
  `[IntervalMin, IntervalMax)`.
- `public int? SelectSpeciesIndex(in PopulationSnapshot pop, IRandom rng)` — top to bottom:
  1. **Predator-floor (overrides cap):** `pop.PredatorCount < tuning.PredatorFloor` → weighted pick among the
     **predator** species (`Role == Predator`), returning the original index.
  2. **Cap pause:** `pop.Total >= tuning.MaxPopulation` → `null`.
  3. **Weighted-over-all:** return a weighted pick among **all** species.
- `public bool TryFindSpawnPosition(in FieldBounds bounds, IOccupancyQuery occupancy, IRandom rng, out
  Vector3 position)` — up to `tuning.MaxPlacementAttempts` candidates (per decision 4); first
  `occupancy.IsClear(candidate, tuning.ClearanceRadius)` wins (`out position`, return `true`); none clear →
  `position = default`, return `false`.
- **Weighted pick** (private helper over a candidate set): `total = Σ Weight`; `r = rng.Range(0f, total)`;
  walk cumulative weights in list order, returning the first original index where `r < cumulative`. One
  `Value01` draw per call. **Terminal clause:** if the walk completes without `r < cumulative` (i.e.
  `r >= total` via a boundary draw where `Value01()` hits its supremum, or a float-summation tail), **return
  the last candidate's original index** — so the method always returns a valid index and the strict `<` walk
  needs no `<=` relaxation.
- No Unity statics (`Time`/`Random`/`Physics`/`Camera`); `UnityEngine` only for `Vector3`. `#nullable
  enable`, `sealed`, Allman, XML docs on the public type + members, C# 9.

### Value structs (`ZooWorld.Core`)

- **`SpeciesWeight`** — `Core/SpeciesWeight.cs`: `readonly struct`; ctor `(Role role, float weight)`;
  read-only `Role Role`, `float Weight`.
- **`SpawnTuning`** — `Core/SpawnTuning.cs`: `readonly struct`; ctor `(float intervalMin, float intervalMax,
  int maxPopulation, int predatorFloor, float clearanceRadius, int maxPlacementAttempts)`; read-only props
  `IntervalMin`, `IntervalMax`, `MaxPopulation`, `PredatorFloor`, `ClearanceRadius`, `MaxPlacementAttempts`.

### EditMode tests (`ZooWorld.Tests.EditMode`) — exact assertions, GDD §9 numbers

**`SpawnPlannerTests`** — `Assets/_Project/Tests/EditMode/SpawnPlannerTests.cs`. Fixtures (the §9 catalog +
config): species `[Prey 0.45 (Frog), Predator 0.30 (Snake), Prey 0.25 (Rabbit)]` (total weight 1.0);
`SpawnTuning(1f, 2f, 120, 1, 1f, 10)`. Helpers: `Prey(float w)`/`Pred(float w)` build `SpeciesWeight`;
`Planner()` = `new SpawnPlanner(<the species list>, <the tuning>)`. (`FakeRandom`/`FakeOccupancy` from T01.)

- **`NextInterval_DrawsWithinConfiguredRange`:** `NextInterval(new FakeRandom(0f))` == `1f`;
  `NextInterval(new FakeRandom(0.5f))` == `1.5f` (both in `[1, 2)`).
- **`SelectSpeciesIndex_BelowCap_WeightedByCumulativeWeight`** (pop `Total 5, PredatorCount 2` — below cap,
  floor met): `FakeRandom(0.1f)` → `0` (Frog); `FakeRandom(0.5f)` → `1` (Snake, cum 0.45→0.75);
  `FakeRandom(0.9f)` → `2` (Rabbit, cum 0.75→1.0).
- **`SelectSpeciesIndex_AtCapacity_FloorSatisfied_ReturnsNull`** (pop `Total 120, PredatorCount 2`):
  result `Is.Null` (cap pause). (Also `Total 121` → null.)
- **`SelectSpeciesIndex_PredatorFloorUnmet_ForcesPredator`** (pop `Total 5, PredatorCount 0`):
  `FakeRandom(0.99f)` → `1` (the predator) — a high draw that would pick prey under weighted-over-all still
  returns the predator, since only Snake is a candidate.
- **`SelectSpeciesIndex_PredatorFloorOverridesCapacity`** (pop `Total 120, PredatorCount 0` — at cap **and**
  no predators): `FakeRandom(0f)` → `1` (predator), **not null** (anti-deadlock; the floor beats the cap, no
  eviction).
- **`TryFindSpawnPosition_ClearSpot_ReturnsInBoundsPosition`** (bounds `Center (0,0,0)`, `HalfExtents
  (10,6)`; `FakeOccupancy.AlwaysClear()`; `FakeRandom(0.5f, 0.5f)`): returns `true`; `position` ==
  `(0,0,0)`; `bounds.Contains(position)` true.
- **`TryFindSpawnPosition_AllBlocked_ReturnsFalse`** (`FakeOccupancy.AlwaysBlocked()`, `new FakeRandom()`):
  returns `false` after `MaxPlacementAttempts` (10) attempts (skip-after-N).
- **`TryFindSpawnPosition_RetriesUntilClear`** (`FakeOccupancy.WithCircles((Vector3.zero, 1f))`,
  `FakeRandom(0.5f, 0.5f, 0.95f, 0.75f)`): attempt 1 `(0,0,0)` is blocked (overlaps the circle; clearance 1 +
  radius 1 = min-sep 2), attempt 2 `(9,0,3)` is clear → returns `true`, `position` == `(9,0,3)`.

(Matches guardrails §14: *"SpawnPlanner — NextInterval range; weighting; placement via a fake
`IOccupancyQuery` + skip-after-N; cap pause; predator-floor (predatorCount==0 at/below cap → next spawn is a
predator, no eviction) over a `PopulationSnapshot`"*.)

## Implementation notes
- **New files:** `Scripts/Runtime/Spawning/SpawnPlanner.cs` (ns `ZooWorld.Spawning` — new folder, auto-covered
  by the `ZooWorld.Runtime` asmdef root), `Scripts/Runtime/Core/SpeciesWeight.cs` + `Core/SpawnTuning.cs` (ns
  `ZooWorld.Core`), `Tests/EditMode/SpawnPlannerTests.cs`. **No new asmdef.**
- `using` outside the namespace (`System.*` first): `System.Collections.Generic` (`IReadOnlyList`),
  `UnityEngine` (`Vector3`), `ZooWorld.Config` (`Role`), `ZooWorld.Core` (`PopulationSnapshot`,
  `FieldBounds`, `IRandom`, `IOccupancyQuery`, `SpeciesWeight`, `SpawnTuning`).
- The weighted pick must return the **original** list index (not the position within a filtered predator
  subset) — iterate with the index and skip non-candidates.
- **Pitfalls:** (a) check **predator-floor before the cap** (the floor overrides the cap — decision 2/3); (b)
  the weighted walk uses `r < cumulative` (not `<=`) and one `Value01` draw; (c) placement draws **x then z**
  per attempt and stops at the **first** clear candidate; (d) `position` is `Center.y` on Y, not 0 hard-coded
  (use `bounds.Center.y`); (e) no Unity statics — `rng`/`occupancy`/`bounds` are seams (guardrails §10/§12).
- Pure rule ⇒ tests ship **in the same change** (DoD, guardrails §14); no scene/Play work in T04.

## Out of scope
- The **T08 spawner**: the UniTask `IAsyncStartable` cadence loop (awaiting `NextInterval` via `IClock`),
  taking from the pool + placing, assigning `ISpawnSequence`, **building the `SpeciesWeight` list (1:1 in
  catalog order, decision 1) + `SpawnTuning`** in `GameLifetimeScope` from the catalog/`SimConfig` (keep the
  positional `SpawnTuning` mapping in sync if `SimConfig`'s spawn block changes — the SO→struct mapping is
  T08-only, not EditMode-covered) + the **T08 wiring-test** (decision 1), the **production `IOccupancyQuery`**
  (`Physics.CheckSphere` on the Animal layer), and **`FieldBounds` from the camera frustum**.
- **On T04 close, propagate the cross-task contracts** (don't leave them in brief prose only — mirrors T03's
  README T07 widen): widen the README **T08** row to name the `SpeciesWeight`/`SpawnTuning` build (catalog-order
  index contract + wiring-test) + production `IOccupancyQuery`; widen the README **T06/T08** rows to name pool
  sizing **`≥ MaxPopulation + PredatorFloor`** (decision 2). (Optionally reconcile the `SimConfig` clearance
  tooltip per decision 4.)
- `Simulation` tick/drain (T07); `Animal` adapter + pooling (T06); `DeathCounters`/events (T05); DI
  registration (T08).
- Eviction / despawn (never — removal is predation-only); multi-tier diets (GDD non-goal).

## Verification
- **EditMode:** `SpawnPlannerTests` green (interval, weighting, cap pause, predator-floor + cap-override,
  placement clear/blocked/retry); **full suite green** (56 prior + T04's new); **0 Console errors**.
  (`Window → Test Runner → EditMode → Run All`, or MCP `run_tests` with `Gameplay.unity` active.)
- `dotnet format --verify-no-changes` green on Runtime; forbidden-API / Unity-statics grep clean in
  `.Spawning` (no `Time`/`Random`/`Physics`/`Camera.main`; `Vector3` only).
- **No Play smoke in T04** — the live cadence / pool / camera bounds / physics occupancy are smoke-verified in
  T08 (vertical slice).

## What was actually done

**Shipped 2026-06-26 on branch `feat/t04-spawn-planner`** — the pure spawn-decision rule, per the approved
brief.

- **Rule** (`ZooWorld.Spawning`): `SpawnPlanner` — `sealed`, stateless instance (DI singleton, §9), ctor
  `(IReadOnlyList<SpeciesWeight>, in SpawnTuning)`. `NextInterval(IRandom)` = `Range(min, max)`;
  `SelectSpeciesIndex(in PopulationSnapshot, IRandom) → int?` (predator-floor → cap pause → weighted-over-all,
  via a private `WeightedPick(predatorsOnly, rng)` with the terminal last-index clause);
  `TryFindSpawnPosition(in FieldBounds, IOccupancyQuery, IRandom, out Vector3)` (≤ `MaxPlacementAttempts`
  full-bounds candidates, first `IsClear(ClearanceRadius)` wins, else false). `Vector3` only; no Unity statics.
- **Value seams** (`ZooWorld.Core`): `SpeciesWeight {Role, Weight}`, `SpawnTuning {IntervalMin/Max,
  MaxPopulation, PredatorFloor, ClearanceRadius, MaxPlacementAttempts}` — both `readonly struct`.
- **Tests** (`ZooWorld.Tests.EditMode`): `SpawnPlannerTests` — 8 tests (interval range, weighted-by-cumulative,
  cap pause, predator-floor force, predator-floor-overrides-cap, placement clear/all-blocked/retry).
- **Doc-close propagation:** README **T06/T08** rows widened (pool max ≥ `MaxPopulation + PredatorFloor`;
  `SpeciesWeight`/`SpawnTuning` build + catalog-order index contract + wiring-test + production
  `IOccupancyQuery`).

**Verification (this session, via MCP):** **EditMode 64/64 green** (56 prior + T04's 8); 0 Console errors
(clean compile after a forced `scope: all` refresh/import — `scope: scripts` compile-only doesn't import new
files, the T03 gotcha); `dotnet format --verify-no-changes` exit 0 on all 4 files; forbidden-API +
Unity-statics grep clean in `.Spawning`. No Play smoke (deferred to T08 per the brief).

**Deviations from the brief:** none. (The decision-4 optional was also applied — the
`SimConfig.ClearanceRadius` tooltip now names it a query-sphere radius instead of a "center-distance check".)

**Commit proposed:** `feat: T04 spawn planner` — _pending human commit_.
