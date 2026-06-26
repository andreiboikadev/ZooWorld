# T03 — FoodChainResolver (pure 2×2 + strength + dead-guard → Outcome)

| | |
|---|---|
| Milestone | M1 — pure core |
| Depends on | T01 |
| Touches scene/prefabs | no (pure rule + EditMode tests only; no assets/scene) |
| Status | ▫ not started |

## Goal

Implement the single pure **`FoodChainResolver`** that decides the predation outcome for one collision
pair from `(role, strength, seq, dead)` → `Outcome`, with EditMode tests covering every matrix cell, the
strength/seq tiebreak, and the dead-guard. Headless C# over T01's `AnimalState`/`Outcome` (no Unity, no
physics). This is the predation brain the `Simulation` drain (T07) calls per pair — it unblocks T07.
(GDD §6/§2; guardrails §6/§10/§14; ADR 0002 §4/§5.)

## Scope decisions (please validate before commit)

The GDD/guardrails leave the **resolver↔Simulation boundary** — chiefly T01's `Outcome.Position`
carry-forward — to settle here. These resolve it so the criteria are concrete. Flag if any reading is
wrong; cheap to flip now.

1. **The resolver returns LOGIC only; spatial fields are Simulation-supplied** (resolves T01's
   carry-forward toward *"Simulation sources position from the live body"*). The resolver fills
   `Outcome.Kind` / `DeadSeq` / `VictimRole` / `RaiseTasty` and passes **`Vector3.zero`** for the
   `position` (in `Outcome.Death`) and the bounce `normal` (in `Outcome.Bounce`). **`AnimalState` is
   unchanged** — no `Position` field added.
   - *Why:* the resolver never uses position to decide anything, so putting it on `AnimalState` would be
     dead pass-through input (an ISP smell on the graded "clean architecture" axis). The spatial points
     the sim needs are inherently the Simulation's: the **victim's** death position (→ `AnimalDied` /
     death-puff), the **predator's** position (→ "Tasty!", GDD §8 — a *different* point from the
     victim's), and the prey×prey contact **normal** (a physics `ContactPoint`, not derivable from two
     `AnimalState`s). The Simulation holds the live bodies at drain (guardrails §6.2 — *"rebuild each
     `AnimalState` from the live `Animal`"*), so it maps `DeadSeq` / the pair → transforms with zero
     extra plumbing.
   - *Rejected alt (A):* add `Vector3 Position` to `AnimalState` and derive the bounce normal from
     `normalize(a.pos − b.pos)`. Makes `Outcome` spatially complete/testable, but pollutes the resolver
     input with unused data and the position-difference normal is only an approximation of the true
     contact normal.
   - *Rejected alt (B) — slim `Outcome` to logic-only (drop `Position`/`BounceNormal`):* the natural
     endpoint of this principle (no spatial field ⇒ no zero to forget). **Not chosen for T03** because
     T01 already shipped `Outcome` with both fields and `OutcomeTests` pins them
     (`Death_CarriesVictimSeqRolePosition`, `Bounce_…CarriesNormal`) — removal re-opens merged T01, out of
     scope for this pure-core task. The fields stay as **reserved spatial slots the resolver leaves
     `Vector3.zero`**; per guardrails §10 (`Outcome` is *"filled from the live `Animal`"*) the Simulation
     sources them. Dropping them is the preferred cleanup if a later task revisits `Outcome`'s shape.
   - **→ T07 carry-forward (named so it isn't lost):** an eat needs **two distinct world points** the
     resolver does not carry — the **victim** body (→ `AnimalDied` + death-puff) and the **predator** body
     (→ "Tasty!", GDD §8) — and `Outcome` has a **single** `Position` slot, so T07 must **not** try to put
     both there. Holding the live bodies at drain (guardrails §6.2), T07 **sources each point directly** —
     the victim point by `DeadSeq` → its transform, the predator point from the surviving body — and the
     prey×prey `BounceNormal` from the contact / body delta. The resolver leaves `Position`/`BounceNormal`
     `Vector3.zero`; **T07 reads `DeadSeq`/`VictimRole` only when `Kind == Death`** (`None`/`Bounce`
     hard-code `DeadSeq 0L` / `VictimRole Prey` — placeholders, not values to key despawn/counters on; the
     `Kind` gate is the contract, `0L` is not a sentinel — backstop only: `MonotonicSpawnSequence` starts
     at 1, so `0L` is never a live id). A leftover zero driving any event/label ⇒ world-origin artifact;
     origin sits **inside** the ~20×12 m field (GDD §3), so visual smoke can miss it — T07 should **assert
     the sourced position is non-zero**, not rely on the Play-smoke alone.
   - **On close, propagate this resolved decision** (don't leave it in brief prose only, mirroring T02's
     pinned-carry-forward): record the *answer* in `Outcome.cs` `<remarks>` — replace its open *"settled in
     T03 … whether `Position` is sourced here or by the `Simulation`"* with the resolved *"resolver leaves
     `Position`/`BounceNormal` `Vector3.zero`; the `Simulation` sources spatial data from the live bodies
     at drain (T07)"* — and widen the README T07 row scope to name the position/normal sourcing.
     (`current-status.md` is rewritten at each task-close, so it carries the obligation forward as part of
     the T03 close, not a separate durable anchor.)
2. **`FoodChainResolver` is an instance `sealed class`, not a `static` rule.** Per guardrails §9 it's a
   `Lifetime.Singleton` DI service constructor-injected into `Simulation`. It is stateless (no fields,
   parameterless), so tests just `new FoodChainResolver()`. (Contrast `JumpMath`/`BoundsReturn`, which are
   `static` because strategies call them directly, not via DI.) The DI **binding** is T08; T03 only ships
   the class.
3. **Every `Death` raises "Tasty!".** This sim has no non-predation death — prey is eaten, or a predator
   loses a duel and is eaten — so the resolver always passes `raiseTasty: true` for a `Death` (GDD §6: a
   predator eats *"prey or predators"*; "Tasty!" on *each* eat). The `Outcome.Death(…, raiseTasty: false)`
   overload stays unused by the resolver. (So `RaiseTasty` is currently equivalent to `Kind == Death` —
   kept as a forward seam in T01's `Outcome` (a future non-eating death, or flipping the GDD §15
   predator-vs-predator "Tasty!"), not accidental duplication, and not removable in T03.)

## Acceptance criteria

### The rule (`ZooWorld.Predation`)

**`FoodChainResolver`** — `Assets/_Project/Scripts/Runtime/Predation/FoodChainResolver.cs`:
- `public sealed class FoodChainResolver` — stateless, parameterless, **no fields** (DI singleton, §9).
- `public Outcome Resolve(in AnimalState a, in AnimalState b)` — pure; the **only** public member. No
  Unity statics (`Time`/`Random`/`Physics`/`Camera`); `UnityEngine` used only for `Vector3.zero`.
  Decision precedence, top to bottom:
  1. **Dead-guard (first):** `a.Dead || b.Dead` → `Outcome.None`. (Idempotency — a re-drained pair with
     an already-dead member produces nothing; *"never both-die"*.)
  2. **Prey × Prey** (`a.Role == Prey && b.Role == Prey`) → `Outcome.Bounce(Vector3.zero)` (both live;
     normal sourced by T07 from the live contact).
  3. **Prey × Predator** (one of each, **either arg order**) → the **prey** dies:
     `Outcome.Death(preySeq, Role.Prey, Vector3.zero, raiseTasty: true)`.
  4. **Predator × Predator** → duel: survivor = **higher `Strength`**; **tie → lower `Seq`** survives;
     the loser dies: `Outcome.Death(loserSeq, Role.Predator, Vector3.zero, raiseTasty: true)`.
- **Order-independent:** `Resolve(a, b)` and `Resolve(b, a)` return the same
  `Kind`/`DeadSeq`/`VictimRole`/`RaiseTasty` (unordered-pair contract, guardrails §6).
- Implement the 2×2 with a tuple `switch` on `(a.Role, b.Role)` after the dead-guard (csharp-style §3 —
  switch/pattern over an if-ladder).

### EditMode tests (`ZooWorld.Tests.EditMode`) — exact assertions

**`FoodChainResolverTests`** — `Assets/_Project/Tests/EditMode/FoodChainResolverTests.cs`. Fixtures are
hand-built `AnimalState`s (the resolver reads **no** `SimConfig`/§9 tunables — predation is role/strength/
seq logic; the §9 `bounceKick`/`graceSeconds` are T07's). Local helpers: `Prey(long seq, bool dead = false)`
= `new AnimalState(Role.Prey, 0, seq, dead)`; `Pred(int strength, long seq, bool dead = false)` =
`new AnimalState(Role.Predator, strength, seq, dead)`. Assert **only** the logical fields
(`Position`/`BounceNormal` are zero placeholders, validated in the T07 smoke):

- **`PreyVsPrey_Bounces_NoDeath_NoTasty`:** `Resolve(Prey(1), Prey(2))` → `Kind == Bounce`;
  `RaiseTasty == false`.
- **`PreyVsPredator_PreyDies_Tasty`:** `Resolve(Prey(1), Pred(5, 2))` → `Kind == Death`; `DeadSeq == 1`;
  `VictimRole == Prey`; `RaiseTasty == true`.
- **`PredatorVsPrey_PreyDies_OrderIndependent`:** `Resolve(Pred(5, 1), Prey(2))` → `Kind == Death`;
  `DeadSeq == 2`; `VictimRole == Prey`. (With the previous test: prey dies regardless of arg position.)
- **`Duel_HigherStrengthSurvives_LoserDies_Tasty`:** `Resolve(Pred(5, 1), Pred(3, 2))` → `Kind == Death`;
  `DeadSeq == 2`; `VictimRole == Predator`; `RaiseTasty == true`.
- **`Duel_StrengthOutranksSeq`:** `Resolve(Pred(3, 1), Pred(5, 2))` → `DeadSeq == 1` (the weaker dies even
  though it holds the lower seq — `Strength` is checked **before** the seq tiebreak).
- **`Duel_StrengthTie_LowerSeqSurvives`:** `Resolve(Pred(5, 1), Pred(5, 2))` → `DeadSeq == 2` (higher seq
  dies; lower seq survives); `VictimRole == Predator`.
- **`Duel_OrderIndependent`:** for both the distinct-strength and the tie case,
  `Resolve(a, b).DeadSeq == Resolve(b, a).DeadSeq`.
- **`EitherAlreadyDead_ReturnsNone`:** the dead-guard precedes **every** role branch, not just the duel
  arm. Assert a **duel** cell — `Resolve(Pred(5, 1, dead: true), Pred(3, 2))` → `Kind == None`,
  `RaiseTasty == false` — **and** a **prey** cell — `Resolve(Prey(1, dead: true), Pred(5, 2))` → `None`
  (would leak `Death(prey)` if the guard sat inside the Pred×Pred `switch` arm), plus the symmetric
  `Resolve(Pred(5, 2), Prey(1, dead: true))` → `None`. (Dead-guard wins over both the duel and the prey
  branch — idempotency. This is the exact failure mode pitfall (a) names.)
- **`Death_ExactlyOneVictim_NeverBothDie`:** every `Death` result's `DeadSeq` equals **exactly one** of
  the two input seqs (the loser) — assert across a prey×predator and a duel case.

(Matches guardrails §14: *"FoodChainResolver — each matrix cell; strength + tie; dead-guard idempotency —
one death/one count, never both-die"*.)

## Implementation notes
- **New files:** `Scripts/Runtime/Predation/FoodChainResolver.cs` (ns `ZooWorld.Predation`) +
  `Tests/EditMode/FoodChainResolverTests.cs` (ns `ZooWorld.Tests.EditMode`). **No new asmdef** —
  `Predation/` sits under the existing `ZooWorld.Runtime` asmdef root (asmdefs include subfolders); the
  test is already in `ZooWorld.Tests.EditMode`.
- **`using` outside the namespace** (`System.*` first, then alphabetical): `UnityEngine` (`Vector3`),
  `ZooWorld.Config` (`Role`), `ZooWorld.Core` (`AnimalState`, `Outcome` — the resolver sets `Kind` via the
  factories, so it never names `OutcomeKind`; the test file does). `#nullable
  enable`; `sealed`; Allman; XML docs on the public type + `Resolve`; C# 9 (block-scoped ns).
- Construct outcomes **only** via the T01 factories — `Outcome.None`, `Outcome.Bounce(Vector3.zero)`,
  `Outcome.Death(seq, role, Vector3.zero, raiseTasty: true)` (the `Outcome` ctor is private by design).
- **Pitfalls:** (a) the dead-guard must be **first** — before any role branch — or an already-dead pair
  leaks a death/bounce; (b) Prey×Predator must handle **both** arg orders (don't assume `a` is the prey);
  (c) the duel must compare `Strength` **before** the `Seq` tiebreak; (d) **no positions/normals** — leave
  them `Vector3.zero` (T07 sources spatial data from the live bodies), and touch **no** Unity statics
  (pure rule, guardrails §10/§12).
- Pure rule ⇒ tests ship **in the same change** (DoD, guardrails §14); no scene/Play work in T03.

## Out of scope
- The `Simulation` drain — `OnCollisionEnter` enqueue/dedupe of `(minSeq, maxSeq)`, per-pair + `dead`-flag
  idempotency keying, eats-before-bounces ordering, despawn, raising `AnimalDied`, spawning "Tasty!", the
  prey×prey **separation impulse**, and **sourcing the death/eat positions + bounce normal** from the live
  bodies (the resolver leaves `Outcome.Position`/`BounceNormal` `Vector3.zero`) — all **T07**.
- `DeathCounters` + the `AnimalDied` subscription — **T05**.
- DI registration (`GameLifetimeScope` binding of `FoodChainResolver`) — **T08**.
- Adding `Position` to `AnimalState` — rejected (scope decision 1).
- Multi-tier / omnivore / size-based diets — GDD non-goal; `strength` + this one resolver are the seam.

## Verification
- **EditMode:** `FoodChainResolverTests` green (every cell + strength + strength-outranks-seq + tie +
  order-independence + dead-guard); **full suite green** (T01's 27 + T02's 20 + T03's new); **0 Console
  errors**. (`Window → Test Runner → EditMode → Run All`, or MCP `run_tests` with `Gameplay.unity` active.)
- `dotnet format --verify-no-changes` green on Runtime; forbidden-API / Unity-statics grep clean in
  `.Predation` (no `Time`/`Random`/`Physics`/`Camera.main`; `Vector3` only).
- **No Play smoke in T03** — the live drain / impulse / labels / position-sourcing are smoke-verified in
  **T07**.

## What was actually done
—
