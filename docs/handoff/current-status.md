# Current Status
Last updated: 2026-06-26
Updated by: AI session (T06 — brief authoring + adversarial review)
Branch/context: on `feat/t06-animal-adapter-and-pooling` (off `dev`); **T01–T05 merged** (T05 = `b762d5b`/#5).
**T06 brief written, reviewed, validated — not yet implemented.**

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core) COMPLETE — T01–T05 merged.**
**M2 (adapters) started:** T06 (`Animal` dumb adapter + `AnimalFactory` pool) brief authored; implementation
next. Then T07 (`Simulation` tick + collision pipeline) and T08 (DI wiring → vertical slice runs).

## Status
**T06 brief authored, reviewed, validated — code NOT yet written.** Brief:
`docs/tasks/T06-animal-adapter-and-pooling.md` (`Brief ✓`). Scope (per the brief's resolved decisions):
- **In T06 (first adapter task):** the inert `Animal` MonoBehaviour (Rigidbody physics profile, runtime
  state, pool-reset hooks; **no** `Update`/`FixedUpdate`/`OnCollisionEnter`), the `AnimalFactory` (one-layer
  create+pool+configure, per-index pools, prewarm/cap/reuse), the `AnimalSpec` value seam, the GDD §11 reset
  contract; the Animal prefab + `PhysicsMaterial` + Animal layer + Animal×Animal matrix; `AnimalTests` +
  `AnimalFactoryTests`.
- **Deferred:** the collision enqueue + drain + `Simulation` tick → T07; DI wiring + SO→spec build + cadence
  + floor + scene placement + Play smoke → T08; CTS + `MaterialPropertyBlock` colour + child-mesh Y-hop +
  Rabbit → T09.

## Checks Run (this session)
- **Adversarial multi-agent review of the T06 brief** (5 dimensions → per-finding verify → completeness
  critic; 20 agents, Unity-6 facts checked via unity_docs/web). **Fixed in the brief:** 1 **critical** —
  `_rigidbody` cached only in `Awake` would NRE every EditMode test (Awake doesn't run headless) → now a
  lazy `Body` getter; 1 **major** — `Dispose()` used `Object.Destroy` (forbidden in edit-mode `[TearDown]`)
  → now `isPlaying ? Destroy : DestroyImmediate`; **minors** — `Spec()` helper missing `strength`;
  matrix/carry-forward; frame-1-leap (`NextLeapTime=0`) handed to T07; parallel-list (`AnimalSpec`/
  `SpeciesWeight`) wiring-test → T08. (3 verify agents errored on output-serialization, no loss: 2 were the
  same critical confirmed by 3 other dimensions, 1 verified by hand against `JumpMove.cs`.)
- **No code yet** ⇒ no EditMode run; the adapter tests land + run with the T06 implementation.

## Decisions Made (T06 brief)
- See the brief's "Scope decisions (resolved)": inert-adapter / tick split (T07), `AnimalSpec` value seam
  (testable, T04-style; SO→spec build at T08), code-applied physics profile, one-layer factory, pool
  grow-to-cap (prewarm `round(weight×maxPop)`, cap `MaxPopulation + PredatorFloor`), and the deferrals.

## Blockers
- None.

## Open / carry-forward
- **→ T06 implement:** the 3 runtime types + prefab/material/layer/matrix + 2 test files per the brief;
  EditMode green (77 prior + T06's new, 0 Console errors); limited Play smoke (body on-plane, no churn).
- **→ T07:** collision enqueue→drain→resolve→events + the tick; **seed `NextLeapTime = clock.Now +
  JumpInterval`** on spawn (T06's clockless reset leaves it `0` → frame-1 leap, GDD §11).
- **→ T08:** `AnimalFactory` DI binding + build `AnimalSpec` **and** `SpeciesWeight` from one
  `catalog.Definitions` pass + a wiring-test over **both** lists (a reorder otherwise spawns the wrong
  species into the wrong pool); production `IOccupancyQuery`/`FieldBounds`; floor; HUD wiring; slice.

## Next Actions
1. **Human (git only):** commit the T06 brief + plan sync as
   `docs: T06 brief (Animal adapter + physics + AnimalFactory pool + reset) + matrix/status sync`.
2. Implement **T06** per the validated brief.
