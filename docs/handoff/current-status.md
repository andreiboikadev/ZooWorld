# Current Status
Last updated: 2026-06-25
Updated by: AI session (T01 — config & seams)
Branch/context: `feat/t01-config-and-seams` (off `dev`)

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core) in progress** —
T01 done; T02–T05 next (movement rules, food-chain resolver, spawn planner, counters), all
headless-testable before any scene exists.

## Status
**T01 (Config & seams) COMPLETE.** Shipped the headless core:
- **Config SOs** (`ZooWorld.Config`): `Role`, abstract stateless `MovementBehaviour`,
  `AnimalDefinition`, `AnimalCatalog`, `SimConfig` (GDD §9 defaults).
- **Core structs + seams** (`ZooWorld.Core`): `MoveContext`, `MovementTuning`, `AnimalState`,
  `Outcome` (+ `OutcomeKind`), `FieldBounds` (`Contains`/`Nearest`), `PopulationSnapshot`,
  `AnimalDied`; `IClock`/`IRandom`/`ISpawnSequence`/`IOccupancyQuery` +
  `UnityClock`/`SeededRandom`/`MonotonicSpawnSequence`. `MovementState` in `ZooWorld.Animals`.
- **Tests** (`ZooWorld.Tests.EditMode`): the four fakes + 6 test classes (incl. `OutcomeTests`).
- **Placeholder assets** (`Assets/_Project/ScriptableObjects/`): `Frog`, `Snake`, `AnimalCatalog`,
  `SimConfig` — movement refs null until T02.
- 3 asmdefs (`ZooWorld.Runtime`/`.Editor`/`.Tests.EditMode`) + `Assets/_Project` layout in place.

## Checks Run
- **EditMode: 27/27 green** (`ZooWorld.Tests.EditMode`; re-run after asset authoring and after adding
  `OutcomeTests` — no regression).
- Compiles with **0 Console errors** (Runtime + Tests + asset authoring).
- Placeholder assets round-trip from disk cleanly (accessors read role/weight/strength/count/maxPop back).
- Forbidden-API + Unity-statics grep clean (only `UnityClock` uses `Time`; no `Physics`/`Camera.main`/
  `GameObject.Find`/`.material`/DOTween anywhere).
- **Ultracode verification:** 4-agent static audit (criteria/style/adversarial/docs) + main-session MCP
  cross-check — pass; surfaced & fixed an `OutcomeTests` coverage gap; `Frog._speed=0` reviewed → kept
  (intentional, GDD §7 — see Decisions).
- **Codestyle gate:** `dotnet format --verify-no-changes` → **green** on `ZooWorld.Runtime` (0 `IDE1006`,
  formatting/whitespace clean) after exempting `IDE0044` for Unity `[SerializeField]` fields
  (`.editorconfig` + `csharp-style.md`) — they cannot be `readonly` without breaking serialization.

## Decisions Made
- Namespaces (per brief): `Role`→`.Config`, `MovementState`→`.Animals`, structs/seams/impls→`.Core`.
- Added a small `OutcomeKind` discriminator (None/Bounce/Death) for `Outcome`. `#nullable enable` per file.
- Stack/architecture per ADR 0001/0002 unchanged.
- `Frog._speed = 0` is **intentional** (not a placeholder): GDD §7 — jumpers idle between leaps, so a
  frog has no cruise speed; `JumpMove` reads jump distance/interval, not `Speed`. T02 just wires the SO.

## Blockers
- None.

## Open / carry-forward
- **→ T03:** `Outcome` carries `Position` but the resolver input `AnimalState` has no position, so
  `FoodChainResolver` can't fill it. Decide in T03: add a position to `AnimalState`, **or** have
  `Simulation` source it from the live body by `DeadSeq` at drain (so `Outcome.Position` would no
  longer need filling by the resolver). Recorded in `Outcome.cs` `<remarks>` + the T01 brief.

## Next Actions
1. **Human:** commit the placeholder assets + these doc updates (closes T01); push branch + open PR → `dev`.
2. Start **T02** (movement rules: `WanderMove`/`JumpMove`/`LinearMove` SOs + `BoundsReturn` + `JumpMath`)
   with EditMode tests; wire the now-null movement refs on the `Frog`/`Snake` assets.

## Commits (this branch)
- `2ac736c` build: asmdefs + folder layout
- `72bdb88` feat(core): config, structs, seams + EditMode tests
- _pending (human):_ (a) `.editorconfig` + `csharp-style.md` — IDE0044/`[SerializeField]` fix;
  (b) T01 assets + `OutcomeTests` + doc-close
