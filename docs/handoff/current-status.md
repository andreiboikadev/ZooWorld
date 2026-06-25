# Current Status
Last updated: 2026-06-25
Updated by: AI session (bootstrap — docs hardened, pre-commit)
Branch/context: `dev`

## Current Objective
Finish the project bootstrap (docs + deps), then implement Zoo World per the task plan
(`docs/tasks/README.md`), starting with T01.

## Status
Bootstrap **doc-set COMPLETE and hardened** — internally consistent, faithful to the task brief,
codestyle enforcement (`.editorconfig` ↔ `csharp-style.md`) intact. Present:
README, GDD, guardrails, csharp-style, ADR 0001/0002, INDEX, build-and-test, setup, agent-verification,
asset-ledger, current-status, task plan + T01; CLAUDE + `.claude/settings.json` + `rules/unity-csharp.md`;
`.editorconfig` + `.gitattributes`. (Reference meta-docs and a `LICENSE` are intentionally omitted —
right-sized for a test task.)

On top of the validated design, this session applied: **(#1)** a `linearDamping ≈ 4` physics constant +
`JumpMath` deriving the jump burst from it; **(#2)** a precise grace-window scope (suppresses only the
strategy velocity; `BoundsReturn` stays active out-of-bounds; a new leap defers); the **Innowise → Sakura
Games** rename; **(B1)** the movement-strategy seam fix — `Tick(ref MovementState, in MoveContext, in
MovementTuning)` so the stateless SO reads per-species constants (speed/jump/damping) without holding them;
and cross-doc polish (#3–#9: pool-prewarm model, Unity version, `linearVelocity`, SimConfig fields, README
license/version, predator-eat brief fidelity, a `§13` qualifier). **All applied but UNCOMMITTED.**

## Checks Run
- Adversarial hypothesis review (7 reviewers, earlier): sound-with-fixes — folded in.
- **8-dimension multi-agent doc audit (this session):** 22 confirmed findings; **1 real blocker — B1, the
  movement-tuning seam — found and FIXED**; the rest applied as polish; a git-deny "issue" refuted as a
  false positive (space-form `Bash(git add *)` is valid, ≡ `:*`).
- Unity Console clean via MCP (0 errors / 0 warnings). No code or EditMode tests yet (none due until T01).

## Decisions Made
- Stack/architecture: ADR 0001 + ADR 0002. Keystone: one `Simulation` tick owner + dumb `Animal`; pure
  rules over structs/seams; **stateless SO strategies fed per-animal constants via `in MovementTuning`**;
  end-of-step collision drain; physics contract incl. `linearDamping`.

## Blockers
- None. (B1 resolved.) DOTween dropped; deps are UPM-only (VContainer + UniTask).

## Next Actions
1. **Human:** review the working-tree diff and make the two proposed commits (deps; bootstrap docs).
2. Create the 3 asmdefs + `Assets/_Project` folder layout (= T01 step 1).
3. Start **T01** (config & seams) with EditMode tests.
