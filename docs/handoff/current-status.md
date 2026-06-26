# Current Status
Last updated: 2026-06-26
Updated by: AI session (T05 — brief authoring + adversarial review)
Branch/context: on `dev` (clean before this change); T01–T04 merged. **T05 brief written & validated — not
yet implemented.**

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core): T01–T04 done & merged**
(`35126c2`/#4 latest). **T05 (DeathCounters + `AnimalDied` Observer + HUD presenter)** is the last M1 task —
its brief is now authored; implementation next. Then M2 adapters (T06–T08).

## Status
**T05 brief authored, reviewed, validated — code NOT yet written.** Brief:
`docs/tasks/T05-death-counters-and-hud.md` (`Brief ✓`). Resolved scope:
- **In T05 (headless, EditMode-tested):** the `AnimalDied` Observer channel — `AnimalDiedHandler` /
  `IAnimalDeathSignal` / `AnimalDeathSignal` (`.Core`); the pure `DeathCounters` + `HudPresenter` +
  `IHudView` seam (`.UI`); `FakeHudView` + `DeathCountersTests` + `HudPresenterTests`.
- **Deferred to T08:** the concrete uGUI `HudView`, the `UnityEngine.UI` asmdef ref, the Canvas/labels, DI
  wiring, and the Play smoke (M1 stays headless; nothing raises `AnimalDied` until T07, no scene until T08).
- **Key calls:** `event Action Changed` (lean MVP, not a struct payload — §13 struct-payload rule is for
  domain events); `HudPresenter : IStartable` (so VContainer calls `Start()`); counts never reset;
  `DeathCounters` lives in `.UI` (its HUD subsystem).

T01–T04 merged to `dev`: `c7cc632`/#1, `51c9948`/#2, `a731a0d`/#3, `35126c2`/#4.

## Checks Run (this session, via MCP)
- Unity MCP on (`ZooWorld@…`, 6000.3.16f1); Console **0 errors / 0 warnings**; `git status` clean (pre-edit).
- **Adversarial multi-agent review of the T05 brief** (5 dimensions → per-finding verify → completeness
  critic): **2 issues fixed** in the brief — ARCH-01 (`HudPresenter` must implement `IStartable` or
  VContainer never calls `Start()` → blank HUD) and the T07 depends-on gap (now `T02, T03, T05, T06`);
  **1 resolved** (`DeathCounters` in `.UI`); the rest refuted (incl. alloc-in-EditMode verified viable,
  nullability clean). No code yet ⇒ no EditMode run this session; the pure-rule tests land with the T05 impl.

## Decisions Made (T05 brief)
- See the brief's "Scope decisions (resolved)": view↔T08 split, Observer seam shape, `Action` notification,
  legacy `Text` (chosen in T08), `.UI` namespace, no GDD §9 tunables.

## Blockers
- None.

## Open / carry-forward
- **→ T05 implement:** write the 6 runtime types + 3 test files per the brief; EditMode green (64 prior +
  T05's new, 0 Console errors) via MCP `run_tests` with `Gameplay.unity` active; `dotnet format` +
  forbidden-API grep clean. No Play smoke in T05 (headless).
- **→ T07:** `Simulation` raises `AnimalDied` via `AnimalDeathSignal.Raise`, sourcing the victim position
  from the live body (matrix T07 deps now include T05).
- **→ T08:** HUD view + Canvas + `UnityEngine.UI` ref + DI wiring + smoke (see the widened T08 matrix row).

## Next Actions
1. **Human (git only):** commit the T05 brief + plan sync as
   `docs: T05 brief (DeathCounters + AnimalDied Observer + HUD presenter) + matrix/status sync`.
2. Implement **T05** per the validated brief (pure rules + tests, headless).
