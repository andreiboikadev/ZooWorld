# Current Status
Last updated: 2026-06-27
Updated by: AI session (T09 — feedback + Rabbit + MaterialPropertyBlock colour + Windows build + ARCHITECTURE)
Branch/context: **T01–T08 merged.** **T09 implemented & verified in the working tree — pending human commit**
(on branch `feat/t09-feedback-rabbit-build-and-docs`; the `docs: T09 brief` commit already landed — this is the
`feat:` implementation commit).

## Current Objective
Ship Zoo World (M3). **M1 (T01–T05) + M2 (T06–T08) merged; T09 — the final M3 task — done & pending commit.**
The submission is **feature-complete**: spawn → roam → predation → feedback runs, the Rabbit proves the
data-only extension path, the Windows build is produced, and README/ARCHITECTURE ship. After the human commits
T09, the only remaining steps are git-side (push + make the repo public + attach the build).

## Status
**T09 DONE — pending commit.** Per the validated, adversarially-reviewed brief
(`docs/tasks/T09-feedback-rabbit-build-and-docs.md`):
- **Feedback:** prey/predator colour via `MaterialPropertyBlock`; the "Tasty!" label (new `PredatorAte`
  Observer channel → `UI/TastyLabel` + pooled spawner); the death puff (2nd `AnimalDied` subscriber); the spawn
  scale-in (`Animal` CTS + `Simulation.Register` lerp kick); the frog/rabbit jump arc (pure `JumpArc` sampled in
  the tick into a child mesh). All via `Core/FeedbackLerp` (UniTask, cancel-on-despawn).
- **Rabbit (data-only):** `Rabbit.asset` reuses the Frog's `JumpMove`, appended to `AnimalCatalog.asset` — zero
  code. Catalog now Frog/Snake/Rabbit (0.45/0.30/0.25).
- **Prefab/scene:** `Animal.prefab` restructured (mesh → child `Mesh`); new `TastyLabel`/`DeathPuff` prefabs +
  the `DeathPuff.mat` transparent material; `Gameplay.unity` gained an `Effects` root + 3 scope refs.
- **Build/docs:** StandaloneWindows64 build (`Builds/Windows/ZooWorld.exe`, git-ignored); `ARCHITECTURE.md`
  (new) + `README`/`docs/INDEX` updates + the 4 §9 feedback rows.
- **+11 EditMode tests.**

## Checks Run (this session, via MCP)
- **EditMode 133/133 green** (122 prior + 11 new; 3.4 s); **0 Console errors** across every compile batch —
  aside from one **pre-existing benign** compile message (the empty `ZooWorld.Editor.asmdef` has no scripts
  yet → "will not be compiled"; not a T09 change — optionally drop that asmdef for a pristine Console).
- **Play smoke** (`Gameplay.unity`): animals spawn on cadence (active ~11, predator-floor holds); prey
  green/sand vs predator red (`MaterialPropertyBlock`, confirmed live + via direct `OnSpawn`); the jump arc
  lifts the child mesh (≈0.4 of the 0.5 m height); on each eat a **"Tasty!"** label rises+fades at the predator
  + a **death puff** scales+fades at the victim (both caught live); HUD counters tick ("Dead prey: 36" / "Dead
  predators: 12"); Rabbit spawns; Console clean.
- **Windows build:** succeeded (100 MB, 0 errors/warnings); launches + runs; **Player.log clean** (0 exceptions).
- **Code style:** `dotnet format --verify-no-changes` **clean** on `ZooWorld.Runtime` + `ZooWorld.Tests.EditMode`
  (one whitespace fix applied — `SimConfig._jumpArcCurve` collapsed to a single line, matching the sibling
  `_riseEase`); the forbidden-API grep (guardrails §12) is clean (only doc-comment mentions).

## Decisions / deviations (T09)
- **Play-smoke gotcha (not a code bug):** entering Play *immediately* after the heavy asset/script import can
  leave the VContainer scope's container un-built (`Container == null`) → the async spawn loop stalls (0 spawns,
  no error). A **fresh Play entry** (after the import settles) builds cleanly and spawns. The standalone build is
  unaffected. Worth knowing for any future MCP-driven smoke.
- **`Animal.OnSpawn` collapses the child mesh to `localScale = 0`** (beyond the brief's `localPosition`-only
  reset) so the spawn scale-in pops cleanly from 0; the pool-reuse safety holds (cancel-on-despawn CTS + fresh
  collapse on take).

## Blockers / open
- **None — feature-complete.** The build run + smoke are clean; the §9 feedback rows are added; the docs are
  consistent (matrix T09 ✅, the brief's *What was actually done* filled).

## Next Actions
1. **Human (git only):** on `feat/t09-feedback-rabbit-build-and-docs`, commit the implementation —
   `feat: T09 feedback + Rabbit + MaterialPropertyBlock colour + Windows build + ARCHITECTURE` (code + tests +
   prefabs/scene/assets + the docs close-out: this file, the matrix row, the brief, §9 rows, README/ARCHITECTURE).
2. **Submission (git-side):** push, make the repo public (`andreiboikadev/ZooWorld`), attach the `Builds/Windows/`
   build, send the link. (`Builds/` is git-ignored.)
