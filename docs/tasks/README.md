# Zoo World — Task plan

> The implementation plan from now to a shippable submission. One self-contained brief per task
> (`Tnn-*.md`); a fresh session can read `docs/INDEX.md` + one brief and start. Near-term tasks get
> full briefs; later ones stay matrix rows + a one-line scope, expanded on pickup. Definition of Done
> per task = the per-mechanic test gate ([`implementation-guardrails.md`](../architecture/implementation-guardrails.md) §14):
> pure rules ship EditMode tests in the same change; adapters get a smoke pass; full suite green;
> Console clean. The human owns commits.
>
> Design source of truth: [`game-design.md`](../product/game-design.md). Engineering contract:
> [`implementation-guardrails.md`](../architecture/implementation-guardrails.md). Decisions:
> [`ADR 0001`](../architecture/adr/0001-tech-baseline.md), [`ADR 0002`](../architecture/adr/0002-animal-architecture.md).

## Status legend

`▫ not started` · `🟡 in progress` · `🔴 blocked on <reason>` · `✅ done`.
Brief: `✓` full brief written · `·` matrix-only (expand on start).

## Build strategy

**Pure rules first (headless-testable, no scene), then adapters, then integration, then polish.**
Everything testable is a pure C# rule over structs + interface seams (guardrails §10) so it ships with
EditMode tests before any scene exists. The vertical slice (spawn → move → collide → eat/bounce →
count) is alive at the end of M2; M3 is feedback + the deliverable.

## Matrix

| ID | Title | Milestone | Depends on | Brief | Status |
|---|---|---|---|---|---|
| T01 | Config & seams (SOs, structs, interfaces + fakes) | M1 — pure core | — | ✓ | ✅ |
| T02 | Movement rules (Wander/Jump/Linear SOs, BoundsReturn, JumpMath) + tests | M1 | T01 | ✓ | ✅ |
| T03 | FoodChainResolver (pure 2×2 + strength + dead-guard → Outcome) + tests | M1 | T01 | ✓ | ✅ |
| T04 | SpawnPlanner (interval, weighting, IOccupancyQuery placement, cap + predator-floor) + tests | M1 | T01 | ✓ | ✅ |
| T05 | DeathCounters + `AnimalDied` Observer channel + HUD presenter + `IHudView` seam (uGUI `HudView` → T08) + tests | M1 | T01 | ✓ | ✅ |
| T06 | `Animal` dumb adapter + physics profile + `AnimalFactory` pool (max ≥ MaxPopulation + PredatorFloor) + reset contract | M2 — adapters | T01 | ✓ | ✅ |
| T07 | `Simulation` tick owner: MoveContext + grace + bounds + collision enqueue→drain→resolve→events (raises `AnimalDied` via T05's `AnimalDeathSignal`) + prey-prey impulse + Outcome position/normal sourcing (from live bodies) | M2 | T02, T03, T05, T06 | ✓ | ✅ |
| T08 | Spawner (UniTask `IAsyncStartable`) + `AnimalFactory` DI binding (class ships in T06) + `SpeciesWeight`/`SpawnTuning`/`AnimalSpec` build (1:1 catalog-order index contract + wiring-test over both lists) + production `IOccupancyQuery` (`Physics.CheckSphere`) + `FieldBounds` (camera frustum) + HUD wiring (`HudView` uGUI + Canvas/labels + `UnityEngine.UI` asmdef ref + bind signal/counters/presenter) + `GameLifetimeScope` wiring → **vertical slice runs** | M2 | T04, T05, T07 | ✓ | ✅ |
| T09 | Feedback (UniTask-lerp Tasty!/pop/death billboard + AnimationCurve jump arc) + scene bootstrap + Rabbit (data-only) + `MaterialPropertyBlock` + Windows build + README/ARCHITECTURE | M3 — ship | T08 | · | ▫ |

Pre-task setup (not a task; done/in-flight): Unity 6.3 URP baseline, VContainer, `.editorconfig`,
MCP, the docs. **Before M1 code:** **UniTask** is in `manifest.json` (OpenUPM; DOTween dropped) —
reference it from the `ZooWorld.Runtime` asmdef; create the asmdefs + folder layout (ADR 0001).

## Brief template (`docs/tasks/Tnn-<slug>.md`)

```markdown
# Tnn — <title>
| | |
|---|---|
| Milestone | M? |
| Depends on | — / Tnn |
| Touches scene/prefabs | no / yes — <which> |
| Status | ▫ not started |

## Goal
1–2 sentences: what we build and why; what it unblocks.

## Acceptance criteria
Concrete, testable bullets — exact type/field/file names, exact numbers from the GDD.
For pure rules, list the EditMode-test assertions.

## Implementation notes
Files (new / existing-to-extend); guardrail refs; pitfalls.

## Out of scope
What this task does NOT include.

## Verification
Tests: unit cases (pure) / smoke (adapters). Done = full suite green + Console clean.

## What was actually done
Filled on close: what shipped, deviations, the commit, the date. `—` until done.
```
