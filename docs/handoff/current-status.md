# Current Status
Last updated: 2026-06-26
Updated by: AI session (T06 — Animal adapter + AnimalFactory pool)
Branch/context: on `feat/t06-animal-adapter-and-pooling` (off `dev`); T01–T05 merged (T05 = `b762d5b`/#5).
**T06 implemented & verified — pending human commit.**

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 complete (T01–T05 merged).** **M2
(adapters) in progress:** T06 (`Animal` + `AnimalFactory`) done, pending commit. Next: **T07** (`Simulation`
tick + collision pipeline), then **T08** (DI wiring + spawn cadence → vertical slice runs).

## Status
**T06 (Animal dumb adapter + physics + AnimalFactory pool + reset) DONE — pending commit.** Per the
validated brief (`docs/tasks/T06-animal-adapter-and-pooling.md`):
- **`ZooWorld.Core`:** `AnimalSpec` (readonly-struct value seam for the factory).
- **`ZooWorld.Animals`:** `Animal` (sealed MonoBehaviour dumb adapter — lazy `Body`, runtime state +
  accessors, `OnSpawn`/`OnDespawn`/`MarkDead`, no `Update`/`FixedUpdate`/`OnCollisionEnter`);
  `AnimalFactory` (one-layer create+pool+configure, per-index prewarm/cap/reuse, edit-mode-safe `Dispose`).
- **Assets:** Animal layer (slot 8); `Prefabs/Animal.prefab`; `Physics/AnimalPhysicsMaterial.asset`.
- **12 EditMode tests** (`AnimalTests` 5 + `AnimalFactoryTests` 7; incl. post-audit hardening).
- Deferred per brief: collision enqueue/drain/tick + frame-1-leap `NextLeapTime` seed → T07; DI wiring +
  SO→`AnimalSpec` build + cadence + floor + scene placement → T08; CTS + MPB colour + Rabbit → T09.

## Checks Run (this session, via MCP)
- **EditMode 89/89 green** (77 prior + T06's 12; re-run after the asset/Play work + post-audit hardening; ~2.6 s).
- **0 Console errors/warnings.**
- `dotnet format --verify-no-changes` exit 0 on the new/changed files; forbidden-API / Unity-statics grep
  clean in `.Animals`.
- **Post-implementation file-audit** (8 agents; code/scope/docs **clean**): 5 minor findings (test-coverage
  gaps + 1 latent `Dispose` invariant) — cheap hardenings applied, the `Despawn` double-despawn guard
  deferred to T07 (its first real caller).
- **Play smoke:** factory-spawned animal under a downward+lateral velocity → Y stayed 0 (FreezePositionY),
  moved ~0.68 m on XZ, gravity off, never slept; despawn→respawn reused the same instance, no Instantiate
  churn.

## Decisions Made (T06)
- See the brief's "Scope decisions (resolved)": inert-adapter / tick split (T07), `AnimalSpec` value seam,
  code-applied physics profile, one-layer factory, pool grow-to-cap, deferrals.
- **2 mechanical deviations (intent unchanged):** PhysicsMaterial saved as `.asset` (the brief's
  `.physicsMaterial` + `CreateAsset` raised a Unity console error; Unity's suggested fix is `.asset`);
  `Animal._rigidbody = null!` (the brief's bare field is CS8618 under `#nullable enable`; `= null!` + the
  lazy `Body` getter is the standard warning-free idiom).

## Blockers
- None.

## Open / carry-forward
- **→ T07:** `OnCollisionEnter` enqueue + drain + the `Simulation.FixedTick` (ticks `Animal.Movement`,
  applies velocity); **seed `NextLeapTime = clock.Now + JumpInterval`** on spawn (T06's reset leaves it 0 →
  frame-1 leap, GDD §11); raise `AnimalDied` via the T05 `AnimalDeathSignal`; prey×prey impulse + grace.
- **→ T08:** `AnimalFactory` DI binding + build `AnimalSpec` **and** `SpeciesWeight` from one
  `catalog.Definitions` pass + a wiring-test over **both** lists; production `IOccupancyQuery`/`FieldBounds`;
  the floor collider + Floor layer + Animal×Floor; scene placement; HUD wiring; vertical-slice smoke.

## Next Actions
1. **Human (git only):** commit the working tree as
   `feat: T06 animal adapter + physics + AnimalFactory pool` (3 runtime types + prefab/material/layer + 11
   tests + `.meta` + doc-close: brief Status/What-was-done, matrix row, this doc). The brief + matrix/status
   sync from the prior step are uncommitted in the same tree, so this single `feat` commit covers them.
2. Start **T07** (`Simulation` tick owner + collision pipeline).
