# Current Status
Last updated: 2026-06-26
Updated by: AI session (T05 — DeathCounters + AnimalDied Observer + HUD presenter)
Branch/context: on `dev`; T01–T04 merged. **T05 implemented & verified — pending human commit.**

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core) is COMPLETE — T01–T05
done** (T05 verified, pending commit). Next: **M2 adapters** — T06 (`Animal` dumb adapter + physics +
pooling), T07 (`Simulation` tick/drain), T08 (spawner + DI wiring + HUD scene → vertical slice runs).

## Status
**T05 (DeathCounters + `AnimalDied` Observer + HUD presenter) DONE — pending commit.** Shipped the headless
death-accounting subsystem per the validated brief (`docs/tasks/T05-death-counters-and-hud.md`):
- **Observer channel** (`ZooWorld.Core`): `AnimalDiedHandler` (delegate over `in AnimalDied`),
  `IAnimalDeathSignal` (subscribe), `AnimalDeathSignal.Raise(in AnimalDied)` (the raise side for T07).
- **Pure HUD** (`ZooWorld.UI`): `DeathCounters : IDisposable` (one increment per death by `Role`, `event
  Action Changed`, unsubscribe on dispose); `IHudView` seam; `HudPresenter : IStartable, IDisposable`
  (exact GDD §8 strings → `IHudView`, initial 0/0 in `Start()`).
- **13 EditMode tests** (`DeathCountersTests` 7 + `DeathCountersAllocationTests` 1 + `HudPresenterTests` 5)
  with `FakeHudView`.
- Deferred to **T08** (per brief decision 1): the concrete uGUI `HudView`, `UnityEngine.UI` asmdef ref,
  Canvas/labels, DI wiring, Play smoke.

T01–T04 merged to `dev`: `c7cc632`/#1, `51c9948`/#2, `a731a0d`/#3, `35126c2`/#4.

## Checks Run (this session, via MCP)
- **EditMode 77/77 green** (64 prior + T05's 13; 2.47 s; `ZooWorld.Tests.EditMode`).
- **0 Console errors** (only an unrelated MCP-bridge WebSocket warning).
- `dotnet format --verify-no-changes` **exit 0** on the Runtime + Tests new files; forbidden-API /
  Unity-statics grep **clean** in `.UI` + the `.Core` signal.
- **No Play smoke** in T05 (headless by design — the live HUD tick is verified in T08).

## Decisions Made (T05)
- See the brief's "Scope decisions (resolved)": view↔T08 split, Observer seam (concrete `Raise` /
  interface subscribe), `event Action Changed` (lean MVP), legacy `Text` chosen in T08, `.UI` namespace,
  no GDD §9 tunables.
- **One deviation:** the zero-alloc test is in its own file `DeathCountersAllocationTests.cs` — the brief's
  "fully-qualify the `Is` constraint" guidance doesn't compile (`AllocatingGCMemory` is an extension method,
  needs the namespace `using` + an `Is` alias that would shadow NUnit's `Is`). Same assertion, isolated.

## Blockers
- None.

## Open / carry-forward
- **→ T07:** `Simulation` raises `AnimalDied` via `AnimalDeathSignal.Raise`, sourcing the victim position
  from the live body (matrix T07 deps include T05).
- **→ T08:** HUD view + Canvas + `UnityEngine.UI` asmdef ref + DI bindings (`AnimalDeathSignal` as
  `IAnimalDeathSignal` + concrete raiser; `DeathCounters`; `HudPresenter` via `RegisterEntryPoint` —
  `IStartable`/`IDisposable`) + Play smoke (counters tick on screen).

## Next Actions
1. **Human (git only):** commit the working tree as
   `feat: T05 death counters + AnimalDied Observer + HUD presenter` (channel + counters + presenter + seam +
   13 tests + `.meta` + doc-close: brief Status/What-was-done, matrix row, this doc). The brief + matrix/status
   sync from the prior step are uncommitted in the same tree, so this single `feat` commit covers them.
2. Start **T06** (`Animal` dumb adapter + physics profile + pooling) — first M2 task.
