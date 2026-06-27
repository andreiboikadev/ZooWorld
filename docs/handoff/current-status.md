# Current Status
Last updated: 2026-06-27
Updated by: AI session (T08 — spawner + DI wiring + production seams + uGUI HUD + scene)
Branch/context: T01–T07 merged. **T08 implemented & verified in the working tree — pending human commit**
(intended on branch `feat/t08-spawner-di-wiring-and-vertical-slice`; two planned commits — the T08 brief
`docs:` then the implementation `feat:`).

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 complete (T01–T05).** **M2 complete:
T06 + T07 merged; T08 (spawner + DI + production seams + scene) done & pending commit → the vertical slice
runs.** Next: **T09** (M3 ship — feedback visuals Tasty!/pop/death + `AnimationCurve` jump arc; Rabbit
data-only; `MaterialPropertyBlock` colour; Windows build; README/ARCHITECTURE).

## Status
**T08 DONE — pending commit.** Per the validated, adversarially-reviewed brief
(`docs/tasks/T08-spawner-di-wiring-and-vertical-slice.md`):
- **`ZooWorld.Composition`:** `CatalogProjection` (pure SO→struct projection); **`GameLifetimeScope`** (the one
  composition root — every service bound via the §G factory-lambda recipe, a source-verified VContainer
  wiring: `in`-struct/array/primitive ctor args captured as locals; `RegisterEntryPoint` factory overload +
  `.AsSelf()` for `Simulation`; `AnimalDeathSignal` dual-exposed).
- **New seams/loop:** `FieldBoundsFactory` (pure camera frustum), `PhysicsOccupancyQuery` (`Physics.CheckSphere`),
  `Spawner` (UniTask `IAsyncStartable`), `HudView` (uGUI).
- **Additive edits:** `Simulation.Population`; `SimConfig._fieldInnerMargin` = 1.5 m (= `JumpDistance`, T07
  invariant); `ZooWorld.Runtime.asmdef` +`UnityEngine.UI`.
- **Scene `Gameplay.unity`:** composition root + all refs; straight-down camera (≈ 20×12 m); uGUI HUD Canvas
  (two top-right counters); `Animals` root; thin static `Floor` on a new **Floor** layer; collision matrix =
  **Animal×Animal + Animal×Floor**.
- **+10 EditMode tests** (CatalogProjection 4 + FieldBoundsFactory 4 + Population 2).

## Checks Run (this session, via MCP)
- **EditMode 122/122 green** (112 prior + 10 new; re-run in the final state, 3.10 s); **0 Console errors**
  across compile + the Play smoke.
- **Play smoke** (`Gameplay.unity`): spawn cadence + cap + predator-floor live; animals move (snake ~2.3 m/s)
  and stay in the field (maxX 4.76, maxZ 4.47 ≪ bounds); **live predation** (`OnCollisionEnter`→drain→
  `AnimalDied`) → HUD "Dead prey: 1" / "Dead predators: 1" (full counters→presenter→view chain, incl. a
  predator duel); camera straight down (Dot 1.000). The live-collision smoke T07 deferred to T08 is confirmed.

## Decisions / deviations (T08)
- **InnerMargin = 1.5 m (= `JumpDistance`)** — honours T07 decision-3's `InnerMargin ≥ JumpDistance` invariant
  (an earlier brief draft's 1.0 m would have let a jumper leak off-screen); shipped as `SimConfig._fieldInnerMargin`.
- **No `EventSystem`** on the HUD Canvas — display-only, and the project's Input System package makes the legacy
  `StandaloneInputModule` throw; the unused EventSystem was removed (keeps the Console clean).
- **VContainer wiring = §G factory lambdas** (not `RegisterInstance`) — required: `in`-struct/array/primitive
  ctor args can't be reflection-injected (established by the brief's adversarial review, re-confirmed: the
  container builds and the slice runs).

## Blockers / open
- **None — the slice runs.** Docs are consistent: the `game-design.md §9` bounds-inner-margin row
  (`≥ jump distance ≈ 1.5 m`) now mirrors `SimConfig._fieldInnerMargin` (re-serialized into `SimConfig.asset`),
  and `dotnet format --verify-no-changes` is clean on both the runtime and test projects.

## Next Actions
1. **Human (git only):** on `feat/t08-spawner-di-wiring-and-vertical-slice`, two commits —
   `docs: T08 brief — Spawner + DI wiring + vertical slice` (the brief), then
   `feat: T08 spawner + DI wiring + production seams + uGUI HUD → vertical slice` (the implementation + tests +
   scene + the doc close-out: the brief's *What was actually done*, the matrix row, and this file).
2. Start **T09** (feedback visuals + Rabbit data-only + `MaterialPropertyBlock` colour + Windows build +
   README/ARCHITECTURE) — the M3 ship milestone.
