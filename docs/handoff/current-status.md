# Current Status
Last updated: 2026-06-27
Updated by: AI session (T07 — Simulation tick owner + collision pipeline)
Branch/context: T01–T06 merged (T06 = `c238d8f`/#6). **T07 implemented & verified in the working tree —
pending human commit.**

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 complete (T01–T05).** **M2 (adapters):
T06 (Animal + factory) merged; T07 (Simulation tick + collision pipeline) done & pending commit.** Next:
**T08** (Spawner + DI wiring + `FieldBounds` + HUD view → the vertical slice runs on its own).

## Status
**T07 (`Simulation` tick owner + end-of-step collision pipeline) DONE — pending commit.** Per the validated
brief (`docs/tasks/T07-simulation-tick-and-collision-pipeline.md`):
- **`ZooWorld.Core`:** `IContactSink`, `SimulationTuning`, **`Simulation`** (plain `IFixedTickable` +
  `IContactSink` + `IDisposable`: `Register`-seed, `FixedTick` = tick→drain, two-pass deaths-then-bounces
  drain, raises `AnimalDied`, prey×prey impulse + grace).
- **`ZooWorld.Animals`:** `SpawnSeed`, `DriveMode`/`DriveCommand`/`MovementDrive` (pure velocity decision);
  the bounds heading-writeback + idle-OOB recovery-leap live in the tick.
- **Merged edits (additive):** `Animal` (`OnCollisionEnter`→enqueue, `SetContactSink`, `Pooled`, sink-clear),
  `AnimalFactory` (double-despawn guard), `MovementBehaviour`/`JumpMove` (`IsImpulseDriven`).
- **23 new EditMode tests** (`MovementDriveTests` 7 + `SpawnSeedTests` 4 + `SimulationTests` 12).

## Checks Run (this session, via MCP)
- **EditMode 112/112 green** (89 prior + 23; re-verified this session; 3.06 s); **0 Console errors** (only the unrelated MCP-bridge
  WebSocket warning).
- `dotnet format --verify-no-changes` exit 0 on all new/edited files; forbidden-API / Unity-statics grep clean
  (the only `GetComponent` is the lazy `Body` getter + the `OnCollisionEnter` contact event — never the tick).
- **Play smoke** (manual `Physics.Simulate` stepping): snake linear ~2.3 m/s + **clean bounds-turn**
  (heading-writeback, maxX 8.04, no escape, Y=0); frog leap finite + Y=0 + no freeze + gravity off; predator
  eats prey → despawn + `AnimalDied(Prey)` → `DeathCounters.DeadPrey == 1`; prey×prey kick separates
  (1.40→3.09 m, isolated from depenetration) + grace opens, both live.

## Decisions / deviations (T07)
- **Velocity model = the `IsImpulseDriven` flag** (the brief's recommended option, not the zero-edit alt).
- **2 mechanical deviations:** dropped the call-site `in` on `animal.Tuning` (CS8156 — `Tuning` is a property
  rvalue; passed as `in` via a temp); added a **`VContainer` reference to the test asmdef** (CS0012 — the
  `SimulationTests` cast the `IFixedTickable` `Simulation`; the brief's *"no asmdef change"* was inaccurate).

## Blockers / open (carry-forward → tuning / T08)
- **JumpMath leap distance (T02):** the live leap ≈ **1.37 m** vs the nominal **1.5 m** (~8% under — Unity
  integrates drag as `v·(1−damping·dt)` damp-then-move; the `distance×damping` form runs ~8% short). **Within
  the provisional GDD §7 tolerance — no fix needed**; an optional ~+9% `JumpMath` tweak is best decided at the
  T08 tuning pass. *(Re-verification correction: an earlier "1.87 m" reading was a smoke-harness artifact —
  the test frog was spawned overlapping the still-active prefab source → depenetration push; confirmed via an
  isolated drag probe + a prefab-deactivated re-measure. Product code is correct — an idle animal does not
  drift.)*
- **`OnCollisionEnter` live-callback firing** is not exercisable under `Physics.Simulate` in Edit mode → the
  drain was driven via manual enqueue (code-verified wiring; enqueue→drain→physics confirmed). The live-collision
  Play smoke is part of **T08**'s vertical slice.

## Next Actions
1. **Human (git only):** commit the working tree as
   `feat: T07 Simulation tick owner + end-of-step collision pipeline` (3 `.Core` + 4 `.Animals` new files + 4
   merged-type edits + the test asmdef + 3 test files + `.meta` + doc-close: the brief What-was-done, the matrix
   row, and this file).
2. Start **T08** (Spawner `IAsyncStartable` + DI wiring + production `IOccupancyQuery` / `FieldBounds`-from-camera
   + HUD `HudView` → the vertical slice runs).
