# Current Status
Last updated: 2026-06-26
Updated by: AI session (T04 — spawn planner)
Branch/context: `feat/t04-spawn-planner` (off `dev`)

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core) nearly done** — T01–T04
done; **T05 (DeathCounters + HUD)** is the last M1 task, then M2 adapters (T06–T08).

## Status
**T04 (SpawnPlanner) COMPLETE.** Shipped the pure spawn-decision rule:
- Rule (`ZooWorld.Spawning`): `SpawnPlanner` — `sealed`, stateless instance (DI singleton); `NextInterval`,
  `SelectSpeciesIndex` (predator-floor → cap pause → weighted-over-all; returns the catalog index or `null`),
  `TryFindSpawnPosition` (N in-bounds candidates, first `IsClear` wins, else skip).
- Value seams (`ZooWorld.Core`): `SpeciesWeight {Role, Weight}`, `SpawnTuning {6 §9 scalars}` — so the rule
  never holds the `AnimalCatalog`/`SimConfig` SOs (built from them at T08).
- 8 new EditMode tests (`SpawnPlannerTests`): interval range, weighted selection, cap pause, predator-floor
  (incl. the cap override / anti-deadlock), placement clear/all-blocked/retry.

(T01–T03 merged to `dev`: `c7cc632`/#1, `51c9948`/#2, `a731a0d`/#3.)

## Checks Run (this session, via MCP)
- **EditMode: 64/64 green** (56 prior + T04's 8; `ZooWorld.Tests.EditMode`).
- 0 Console errors (clean compile after a forced refresh/import).
- `dotnet format --verify-no-changes` exit 0 on all 4 new files; forbidden-API + Unity-statics grep clean
  in `.Spawning`.
- No Play smoke in T04 (live cadence / pool / camera-bounds / physics-occupancy are smoke-verified in T08).

## Decisions Made (T04)
- **Two value seams** (`SpeciesWeight`/`SpawnTuning`) instead of the SOs — `AnimalDefinition`/`SimConfig`
  have no test setters, so the pure rule consumes plain values; the planner returns the **catalog index**.
- **Predator-floor overrides the cap** (anti-deadlock): `PredatorCount < floor` → a predator even at/above
  the cap, no eviction. Peak live = `MaxPopulation + PredatorFloor`.
- **Hard precondition:** the species list has ≥1 predator (the catalog ships Snake); not guarded.

## Blockers
- None.

## Open / carry-forward
- **→ T06/T08 (pooling):** peak live count = `MaxPopulation + PredatorFloor` (the floor-over-cap path), so
  the **predator** pool must be sized for that peak (pool max ≥ `MaxPopulation + PredatorFloor`), not just the
  cap — a too-small pool ⇒ null take / a guardrails §12-forbidden runtime `Instantiate`. (README T06/T08 rows
  widened.)
- **→ T08 (build + wiring):** build the `SpeciesWeight` list **1:1 from `AnimalCatalog.Definitions` in
  catalog order** (the positional index↔catalog contract — a reorder silently spawns the wrong species) and
  key the pool by the same index, from one `Definitions` pass + a wiring-test
  (`species.Count == Definitions.Count`); build `SpawnTuning` from `SimConfig` in `GameLifetimeScope` (keep
  the positional mapping in sync); the production `IOccupancyQuery` (`Physics.CheckSphere`, Animal layer) +
  `FieldBounds` from the camera. (README T08 row widened.) Done in this change: the
  `SimConfig.ClearanceRadius` tooltip now names it a query-sphere radius (decision 4), not a centre distance.
- **→ T07 (predation drain):** (unchanged from T03) the Simulation sources spatial data from live bodies and
  reads `DeadSeq`/`VictimRole` only when `Kind == Death`.

## Next Actions
1. **Human (git only):** commit the working tree as `feat: T04 spawn planner` (planner + 2 structs + tests +
   doc-close: brief Status/What-was-done, matrix row, this doc, README T06/T08 rows) — then push + PR → `dev`.
   This doc-close is final; a fresh session starts clean on T05.
2. Start **T05** (DeathCounters + `AnimalDied` event + HUD presenter/view (uGUI) + test) — the last M1 task.
