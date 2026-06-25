# Claude Code Instructions — Zoo World

## Project snapshot
A 3D top-down primitives sim (Senior Unity test task) graded on **animal architecture** —
clear, easy to extend ("1000 animals"), SOLID, DI, named patterns. **No ECS; uGUI.** Unity 6.3
LTS + URP; VContainer (DI) + UniTask. Feedback = a small UniTask lerp helper (no DOTween).
Design: `docs/product/game-design.md`. Engineering: `docs/architecture/implementation-guardrails.md`.

## Start every session
1. Read `docs/INDEX.md` and `docs/handoff/current-status.md`.
2. Read the task-relevant docs (the active `docs/tasks/Tnn-*.md`, the GDD section, guardrails).
3. Run read-only `git status --short` before editing.
4. State what you're about to change before broad edits.

## Must-read docs
- `docs/product/game-design.md` — what we build (source of truth).
- `docs/architecture/implementation-guardrails.md` — how it's engineered (the contract).
- `docs/architecture/adr/0001-tech-baseline.md`, `…/0002-animal-architecture.md` — locked decisions.
- `docs/tasks/README.md` — the task plan; the active brief.
- `docs/handoff/current-status.md` — current state.

## Repository rules
- First-party code/content under `Assets/_Project/`; commit `.meta` files with assets.
- Don't edit generated `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, or build output.
- Add package deps by editing `Packages/manifest.json` (OpenUPM scoped registry); document why.
- Update docs in the same change when behavior, setup, architecture, tests, or workflow change.

## Architecture rules (follow `implementation-guardrails.md`)
- **One `Simulation` service owns the FixedUpdate tick; `Animal` is a dumb adapter** (no magic
  `Update`/`FixedUpdate`, no service deps).
- **Rules are pure C# over plain structs + interface seams** (`IClock`/`IRandom`/`ISpawnSequence`/
  `IOccupancyQuery`/`FieldBounds`/`AnimalState`/…) — no `Time`/`Random`/`Physics`/`Camera.main`
  inside a rule. EditMode-tested headless.
- **VContainer composition root**; scope-singletons OK, **no static singletons, no service locator,
  no `Resolve` from gameplay**. Object pooling for animals + labels (cancel UniTask tokens on
  despawn — stops in-flight feedback).
- Collisions resolved in the end-of-step drain (not inside `OnCollisionEnter`). No ECS. UI = uGUI.

## Verification
- `docs/development/build-and-test.md` is the source of truth for commands.
- Never claim a test passed unless it was run this session, with cited evidence.

## Git policy
- Read-only git only: `git status`, `git diff`, `git log`. (Enforced by `.claude/settings.json`.)
- Never `git add`/`commit`/`push`/destructive git. Propose a Conventional Commit message; the human commits.

## End every session
Update `docs/handoff/current-status.md`: outcome, files changed, checks run, blockers, next actions.
