# Current Status
Last updated: 2026-06-26
Updated by: AI session (T03 — food-chain resolver)
Branch/context: `feat/t03-food-chain-resolver` (off `dev`)

## Current Objective
Implement Zoo World per the task plan (`docs/tasks/README.md`). **M1 (pure core) progressing** — T01–T03
done; T04–T05 next (spawn planner, counters), all headless-testable.

## Status
**T03 (FoodChainResolver) COMPLETE.** Shipped the predation rule:
- Pure rule (`ZooWorld.Predation`): `FoodChainResolver.Resolve(in AnimalState, in AnimalState) → Outcome`
  — dead-guard first, then a tuple `switch` over `(Role, Role)`: prey×prey bounce; predator eats prey;
  predator duel won by higher `Strength` (tie → lower `Seq` survives). Stateless instance (DI singleton).
- Returns **logic only** — `Position`/`BounceNormal` left `Vector3.zero`; the Simulation (T07) sources the
  spatial data from the live bodies at drain.
- 9 new EditMode tests (`FoodChainResolverTests`): every matrix cell, strength + strength-outranks-seq +
  tie, order-independence, dead-guard idempotency (duel **and** prey branches), exactly-one-victim.
- `Outcome.cs` `<remarks>` updated to record the resolved position-sourcing answer.

(T01 `c7cc632`/PR #1, T02 `51c9948`/PR #2 — both merged to `dev`.)

## Checks Run (this session, via MCP)
- **EditMode: 56/56 green** (T01's 27 + T02's 20 + T03's 9; `ZooWorld.Tests.EditMode`).
- 0 Console errors (clean compile after a forced refresh/import).
- `dotnet format --verify-no-changes` exit 0 on both new files; forbidden-API + Unity-statics grep clean
  in `.Predation`.
- No Play smoke in T03 (the live drain/impulse/labels/position-sourcing are smoke-verified in T07).

## Decisions Made (T03)
- **Resolver returns logic only; Simulation sources spatial data.** `AnimalState` unchanged (no position).
  Resolves T01's `Outcome.Position` carry-forward toward *"Simulation sources from the live body."*
- **`FoodChainResolver` is an instance `sealed class`** (Lifetime.Singleton DI service, guardrails §9), not
  a `static` rule; the DI binding is T08.
- **Every `Death` raises "Tasty!"** (`raiseTasty: true` always) — no non-predation death in this sim;
  `RaiseTasty` kept as a forward seam.

## Blockers
- None.

## Open / carry-forward
- **→ T07 (predation drain):** the Simulation must **source** the spatial data from the live bodies at
  drain — the **victim** position (→ `AnimalDied`/death-puff, by `DeadSeq`) and the **predator** position
  (→ "Tasty!", a *different* point; GDD §8), plus the prey×prey **contact normal** (→ separation impulse).
  The resolver leaves `Outcome.Position`/`BounceNormal` `Vector3.zero`. **Read `DeadSeq`/`VictimRole` only
  when `Kind == Death`** (None/Bounce hard-code `0L`/`Prey` placeholders; `MonotonicSpawnSequence` starts at
  1, so `0L` is never a live id — but gate on `Kind`, not the value). Prefer a mechanical non-zero assert
  over visual-only smoke (origin sits inside the field). Recorded in `Outcome.cs` `<remarks>` + the README
  T07 row.
- **→ T06/T07 (movement):** pool-reset must seed a random `Heading` (LinearMove has no self-heal) plus
  `NextLeapTime`/`NextHeadingReroll`; the `JumpMove` leap-coast applies as an impulse (not
  velocity-set-then-zero); `JumpMath`'s live leap distance is calibrated in the T07 Play smoke.

## Next Actions
1. **Human (git only):** commit the working tree as `feat: T03 food-chain resolver` (resolver + tests +
   doc-close: brief Status, matrix row, this doc, `Outcome.cs` `<remarks>`, README T07 row) — then push +
   PR → `dev`. This doc-close is final; a fresh session starts clean on T04.
2. Start **T04** (SpawnPlanner — interval, weighting, `IOccupancyQuery` placement, cap + predator-floor +
   EditMode tests).
