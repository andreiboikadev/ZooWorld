# Current Status
Last updated: 2026-06-26
Updated by: AI session (T02 — movement rules)
Branch/context: `feat/t02-movement-rules` (off `dev`)

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core) progressing** — T01 + T02
done; T03–T05 next (food-chain resolver, spawn planner, counters), all headless-testable.

## Status
**T02 (Movement rules) COMPLETE.** Shipped the movement layer:
- Pure rules (`ZooWorld.Animals`): `JumpMath`, `BoundsReturn`, `MovementHeading`.
- Stateless strategy SOs: `WanderMove`, `LinearMove`, `JumpMove`.
- `MovementTuning` extended (`WanderRerollMin`/`WanderRerollMax`).
- Strategy assets under `ScriptableObjects/Movement/`; wired `Frog.movement → JumpMove`,
  `Snake.movement → LinearMove`.
- 20 new EditMode tests (7 classes).

(T01 — config SOs, core structs/seams, fakes — merged earlier as `c7cc632` / PR #1.)

## Checks Run
- **EditMode: 47/47 green** (T01's 27 + T02's 20; `ZooWorld.Tests.EditMode`).
- 0 Console errors; `dotnet format --verify-no-changes` green on Runtime; forbidden-API + Unity-statics
  grep clean in `.Animals`.

## Decisions Made (T02)
- `LinearMove` = straight (no re-roll), turned only by bounds-return; `WanderMove` re-rolls but is
  unassigned to a shipped species yet.
- `JumpMath.BurstSpeed = jumpDistance × linearDamping` (damp-then-move closed form, dt-independent; = 6
  for 1.5 m @ damping 4).
- `JumpMove` leap = one-shot impulse + physics-damping coast (PINNED for T07; a `Vector3.zero` return
  means "do not drive the body", never "set velocity to zero").
- `MovementTuning` carries the wander-reroll globals.

## Scene infra (NOT a T02 deliverable)
- The MCP test runner refuses to start unless the editor's active scene is saved. Added
  `Assets/_Project/Scenes/Gameplay.unity` (Camera + Directional Light) + Build Settings entry, and
  **removed the junk default `Assets/Scenes/SampleScene.unity`** + the empty `Assets/Scenes/` folder.
  Proposed as a separate `chore:` commit.

## Blockers
- None.

## Open / carry-forward
- **→ T03:** `Outcome.Position` has no source in the resolver input `AnimalState` (from T01) — decide there.
- **→ T06/T07:** the spawn pool-reset must seed a random `Heading` (LinearMove has no self-heal) plus
  `NextLeapTime`/`NextHeadingReroll`; the `JumpMove` leap-coast must be applied as an impulse (not
  velocity-set-then-zero); `JumpMath`'s live leap distance is calibrated in the T07 Play smoke.

## Next Actions
1. **Human (git only):** commit the working tree as 2 commits — `feat: T02 movement rules` (code + tests +
   assets + doc-close) and `chore: replace default scene` (Gameplay + Build Settings − SampleScene) — then
   push + PR → `dev`. **This doc-close is final** — a fresh session starts clean on T03 with no doc records
   left open (git log is the source of truth for what is committed).
2. Start **T03** (FoodChainResolver — pure 2×2 + strength + dead-guard → `Outcome` + EditMode tests).
