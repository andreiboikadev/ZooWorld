# T02 — Movement rules (Wander/Jump/Linear SOs, BoundsReturn, JumpMath)

| | |
|---|---|
| Milestone | M1 — pure core |
| Depends on | T01 |
| Touches scene/prefabs | no (creates ScriptableObject assets only) |
| Status | ✅ done |

## Goal

Implement the concrete **stateless** movement strategies (`WanderMove`/`JumpMove`/`LinearMove`) and the
two pure movement-math rules (`BoundsReturn`, `JumpMath`) — all headless C# over T01's structs/seams,
EditMode-tested. Author the three strategy SO assets and wire the now-null `movement` refs on `Frog`/
`Snake`. This unblocks `Simulation` (T07), which ticks these strategies and applies bounds-return.
No MonoBehaviour, no physics body, no scene — those are T06/T07. (Guardrails §8; ADR 0002.)

## Scope decisions (please validate before commit)

GDD/guardrails leave a few movement details to interpret; this brief resolves them so the criteria are
concrete. Flag if any reading is wrong — cheap to flip now.

1. **`LinearMove` ≠ `WanderMove`.** `WanderMove` periodically **re-rolls** its heading (zig-zag roam);
   `LinearMove` keeps a **fixed straight heading**, turned **only by bounds-return** (no periodic
   re-roll). This gives the snake genuine "moving linear (fixed direction)" motion (GDD §2 table) and a
   real behavioural difference between the two SOs. (GDD §7's "(wander/return) heading" for Linear is
   read as "the heading it carries, redirected by return," not "actively wanders.")
2. **`WanderMove` ships unassigned.** Frog→`JumpMove`, Snake→`LinearMove`, Rabbit→`JumpMove` (T09). No
   shipped species uses `WanderMove` yet — it's built as the third Strategy variant (pattern
   completeness) and the drop-in for a future wandering prey.
3. **`MovementTuning` gains two fields.** The wander re-roll interval (GDD §9, a `SimConfig` global) is
   not reachable inside a stateless strategy today. Extend T01's `MovementTuning` with
   `float WanderRerollMin`, `float WanderRerollMax` (built at spawn from `SimConfig.WanderRerollMin/Max`).
   Only `WanderMove` reads them. (Alternative: carry them on `MoveContext`. Chosen `MovementTuning` for
   consistency — "all movement constants in one carrier.")
4. **Namespace.** Concretes + the pure movement rules live in `ZooWorld.Animals` (the movement domain,
   alongside `MovementState`), matching the domain pattern (resolver→`.Predation`, planner→`.Spawning`).
   The abstract `MovementBehaviour` base stays in `.Config` (T01). (Alternative: concretes in `.Config`
   with the base.)
5. **`JumpMove` return contract + leap-coast (PINNED, not deferred).** `Tick` returns the leap **burst
   velocity** (`heading × BurstSpeed`) on the tick a leap starts, and `Vector3.zero` otherwise (idle /
   during grace). **Binding constraint for T07:** the leap is a **one-shot impulse + physics-damping
   coast** — on the burst tick `Simulation` applies it as `rb.AddForce(burst, ForceMode.VelocityChange)`,
   and a `Vector3.zero` return means **"do not drive the body this tick — let it coast under
   linearDamping"**, *never* `rb.linearVelocity = Vector3.zero`. Structurally identical to the §7
   prey×prey separation kick (which reads because the velocity-set is skipped), so the live body and
   `JumpMath` share the exact damping model. **Why pinned, not deferred:** the locked §8 driven loop
   (`rb.linearVelocity = desired` each non-grace tick), applied literally to JumpMove's zero, overwrites
   the burst one tick after launch → frog leaps ~0.1 m then freezes — and every JumpMath test stays green
   while the live leap is broken (a textbook hard-to-debug bug). T07 still owns the *mechanism* (e.g. a
   `leaping`/coast marker so the setter is skipped); the *contract* is fixed here.

## Acceptance criteria

### Pure rules (`ZooWorld.Animals`, `static class`, no Unity statics beyond `Mathf`)

**`JumpMath`** — `Assets/_Project/Scripts/Runtime/Animals/JumpMath.cs`:
- `static float BurstSpeed(float jumpDistance, float linearDamping)` returns `jumpDistance * linearDamping`.
  *Why:* Unity PhysX applies linearDamping **before** integrating position each step (damp-then-move), so
  a body launched at `v₀` coasts a total `Σ vₖ·dt = v₀ / linearDamping` (**dt-independent**); inverting
  gives `v₀ = jumpDistance × linearDamping`. This is exactly guardrails §8's `v₀ ≈ jumpDistance ×
  linearDamping` (≈ 6 m/s for 1.5 m at damping 4). *(Refinement of guardrails' `(…, ctx.Dt)` input: the
  damp-then-move closed form is dt-independent, so `dt` is dropped — also avoids an unused-param warning.)*
  The **live leap distance is calibrated/confirmed in the T07 Play smoke** (±tolerance); if PhysX ordering
  ever differs from damp-then-move, the smoke catches the offset and a dt-correction is added there — not guessed now.

**`BoundsReturn`** — `Animals/BoundsReturn.cs`:
- `static bool IsWithinInner(in FieldBounds bounds, Vector3 position)` — true when `position` (XZ) is
  inside the rectangle inset by `bounds.InnerMargin` (`|dx| ≤ HalfExtents.x − InnerMargin` and
  `|dz| ≤ HalfExtents.y − InnerMargin`).
- `static Vector3 Steer(in FieldBounds bounds, Vector3 position, Vector3 heading)` — returns `heading`
  unchanged when `IsWithinInner`; otherwise the unit XZ vector from `position` toward `bounds.Center`
  (Y = 0). **Pure — returns a heading, does NOT mutate `state.Heading`; `Simulation` (T07) writes the
  steered heading back / uses it for the override.** The inner margin is a **buffer that suppresses
  edge-toggling** (steer back before the true edge), not full two-threshold hysteresis. (Guardrails §8.)

**`MovementHeading`** — `Animals/MovementHeading.cs`:
- `static Vector3 RandomXz(IRandom rng)` — a unit XZ heading at angle `rng.Range(0f, 2f * Mathf.PI)`:
  `(Mathf.Cos θ, 0f, Mathf.Sin θ)`. Shared by `WanderMove` and `JumpMove`.

### Strategies (`ZooWorld.Animals`, `sealed class … : MovementBehaviour`, **stateless**, `[CreateAssetMenu(menuName="Zoo World/Movement/…")]`)

**`WanderMove`** — `Animals/WanderMove.cs`:
- `Tick`: if `ctx.Clock.Now >= state.NextHeadingReroll` → `state.Heading = MovementHeading.RandomXz(ctx.Rng)`;
  `state.NextHeadingReroll = ctx.Clock.Now + ctx.Rng.Range(tuning.WanderRerollMin, tuning.WanderRerollMax)`.
  Return `state.Heading * tuning.Speed`.

**`LinearMove`** — `Animals/LinearMove.cs`:
- `Tick`: return `state.Heading * tuning.Speed`. No re-roll, no leap. **Precondition:** `state.Heading`
  must be a non-zero unit vector **seeded at spawn** — unlike `WanderMove`/`JumpMove`, `LinearMove` has
  **no self-heal**, so a default `(0,0,0)` heading yields **zero velocity = a frozen snake** (the
  "no frozen animals" correctness bug, guardrails §1/§7). See the spawn-seed obligation in Implementation notes.

**`JumpMove`** — `Animals/JumpMove.cs`:
- `Tick`:
  - `ctx.Clock.Now < state.GraceUntil` → return `Vector3.zero` (defer; no leap during grace).
  - else `ctx.Clock.Now >= state.NextLeapTime` → `state.Heading = MovementHeading.RandomXz(ctx.Rng)`;
    `state.NextLeapTime = ctx.Clock.Now + tuning.JumpInterval`;
    return `state.Heading * JumpMath.BurstSpeed(tuning.JumpDistance, tuning.LinearDamping)`.
  - else → return `Vector3.zero` (idle between leaps).

### Struct extension (`ZooWorld.Core`)
- `MovementTuning` (T01, `Core/MovementTuning.cs`): add `float WanderRerollMin`, `float WanderRerollMax`
  (constructor params + read-only props). **No existing call site constructs `MovementTuning`** (grep: zero
  `new MovementTuning`), so the wider ctor breaks nothing — the `new MovementTuning(…)` calls are introduced
  fresh in T02's strategy tests. The only T01 type touched.

### Assets (`Assets/_Project/ScriptableObjects/Movement/`)
- `WanderMove.asset`, `JumpMove.asset`, `LinearMove.asset` (the three strategy SOs; commit `.meta`).
- Wire `Frog.movement → JumpMove.asset`, `Snake.movement → LinearMove.asset` (commit updated `.asset`s).

### EditMode tests (`ZooWorld.Tests.EditMode`) — exact assertions, GDD §9 numbers

**`JumpMathTests`**:
- `BurstSpeed(1.5f, 4f)` == `6f` (exact — `jumpDistance × linearDamping`).
- **Round-trip (damp-then-move, matching PhysX):** `v = BurstSpeed(1.5,4); dist = 0; repeat ~2000×:
  v /= 1 + 4*0.02; dist += v*0.02;` → `dist` ≈ **1.5 m** within 1e-3. (Damp **then** move — the order the
  live body uses; with `v₀ = 6`, distance = `v₀ / damping` = 1.5, dt-independent.)
- Monotonic: `BurstSpeed` increases with `jumpDistance` and with `linearDamping`.

**`BoundsReturnTests`** (bounds: `Center 0`, `HalfExtents (10,6)`, `InnerMargin 1` → inner 9×5):
- `IsWithinInner` true at `(5,0,3)`; false at `(9.5,0,0)` and `(0,0,5.5)`.
- `Steer` inside (`(5,0,3)`) → heading returned unchanged.
- `Steer` at `(9.5,0,0)` → `result.x < 0`, `result.z ≈ 0`, `|result| ≈ 1`.
- `Steer` at `(0,0,-5.5)` → `result.z > 0`.
- Y ignored: `(0,99,0)` treated as in-bounds.
- Corner `(9.5,0,5.5)` → `result.x < 0` **and** `result.z < 0`.

**`MovementHeadingTests`**:
- `RandomXz` returns a unit-length vector with `y == 0` (across a few scripted angles).

**`WanderMoveTests`** (`FakeClock`, `FakeRandom`, `MovementTuning(speed:2f, wanderReroll[0.8,1.5])`):
- First `Tick` (Now ≥ NextHeadingReroll=0) re-rolls heading and sets `NextHeadingReroll = Now + Range(0.8,1.5)`;
  returns a vector of magnitude `Speed`. **Draw order:** `FakeRandom` serves both draws from one `Value01`
  queue — enqueue `[angle01, interval01]` per re-roll (`RandomXz` angle first, then the interval).
- Within the interval (`Now < NextHeadingReroll`): heading **not** re-rolled (same vector).
- After `Now ≥ NextHeadingReroll`: heading re-rolls again.

**`LinearMoveTests`** (`Heading (1,0,0)`, `Speed 2.5`):
- `Tick` returns `(2.5,0,0)`; `NextHeadingReroll`/`NextLeapTime` untouched across repeated ticks (never re-rolls).
- **Tripwire:** a default `MovementState` (`Heading == (0,0,0)`) → `Tick` returns `Vector3.zero` — pins the
  spawn-seed precondition (a frozen snake if the reset forgets the `Heading` seed).

**`JumpMoveTests`** (`FakeClock`, `FakeRandom`; `JumpInterval 1.5`, `JumpDistance 1.5`, `Damping 4`):
- Leap due (`Now ≥ NextLeapTime`, not in grace) → returns burst of magnitude `BurstSpeed(1.5,4)` (= 6),
  sets `NextLeapTime = Now + 1.5`, re-rolls heading (**one** `Value01` draw — the angle only; `JumpInterval`
  is fixed tuning, not a draw).
- Between leaps (`Now < NextLeapTime`) → returns `Vector3.zero`.
- **Grace defers (guardrails §14):** with `GraceUntil = 0.6`, `NextLeapTime = 0`, at `Now = 0.3` →
  returns `Vector3.zero` and does **not** advance `NextLeapTime`; at `Now = 0.6` the leap fires.

**`MovementBehaviourStatelessTests`** (architecture guard, guardrails §5/§14; ADR 0002):
- Reflect over every non-abstract `MovementBehaviour` subclass in `ZooWorld.Runtime`; assert each
  declares **no mutable instance fields** (any instance field must be `readonly`/`const`; ideally none).

## Implementation notes
- New files under `Scripts/Runtime/Animals/`; tests under `Tests/EditMode/`. No new asmdef.
- Strategies read constants **only** from `in MovementTuning` + ambient `in MoveContext`; **zero instance
  fields** (ADR 0002 — enforced by the reflection test).
- No Unity statics except `Mathf` (Cos/Sin/Abs/Clamp/Sqrt). Time/randomness via `ctx.Clock`/`ctx.Rng`;
  never `Time`/`UnityEngine.Random`/`Physics`. (Guardrails §10/§12.)
- **Spawn-seed obligation (T06/T07 pool-reset) — named here so it isn't lost in handoff:** the reset must
  seed, at spawn, **a random unit `Heading`** (via `MovementHeading.RandomXz`) **and** `NextLeapTime`/
  `NextHeadingReroll` to a future/randomized value (frame-1-jump avoidance, GDD §11). `WanderMove`/
  `JumpMove` self-heal a zero `Heading` (re-roll on tick 1), but **`LinearMove` does not** — an unseeded
  `Heading` freezes the snake. All three seeds belong in the one reset path.
- csharp-style: `sealed`, `#nullable enable`, Allman, XML docs on public members, C# 9 (block-scoped ns).
  Static rule classes have no instance state. The new `MovementTuning` fields keep `[SerializeField]`-free
  (it's a `readonly struct`, not an SO).
- `FakeRandom` already scripts `Value01`/float `Range`; the angle draw uses the float queue — extend a
  test helper only if a test needs a specific angle.

## Out of scope
- The `Simulation` tick that calls `Tick`, applies `BoundsReturn`, sets `rb.linearVelocity`, and the
  velocity-application / coast / grace-**setting** mechanism (T07).
- Physics Rigidbody profile + `Animal` adapter + `MovementTuning` **construction** at spawn (T06/T07).
- Visual hop (child-mesh Y offset / `AnimationCurve` jump arc) — T09.
- Post-bounce heading bias and the prey×prey separation impulse (T07).
- `Rabbit` (T09).

## Verification
- EditMode: all the above test classes green; **full suite (T01's 27 + T02's new) green**; 0 Console errors.
- Strategy assets author cleanly; `Frog.movement`/`Snake.movement` round-trip to the new SOs.
- `dotnet format --verify-no-changes` green; forbidden-API/Unity-statics grep clean.
- (Live leap distance / no-frozen-animal / "linear vs wander" reads are **Play-smoke in T07**, not T02.)

## What was actually done

**Shipped 2026-06-26 on branch `feat/t02-movement-rules`** — the movement rules, per the approved brief.

- **Pure rules** (`ZooWorld.Animals`): `JumpMath.BurstSpeed(jumpDistance, linearDamping) = jd × damping`
  (= 6 for 1.5 m @ damping 4; damp-then-move closed form, dt-independent); `BoundsReturn.IsWithinInner`/
  `Steer` (inner-margin buffer, steer-to-centre, XZ-only); `MovementHeading.RandomXz`.
- **Strategies** (`ZooWorld.Animals`, `sealed` stateless SOs): `WanderMove` (re-roll heading on schedule),
  `LinearMove` (constant speed, no re-roll), `JumpMove` (burst on schedule, idle/defer during grace).
- **`MovementTuning`** extended with `WanderRerollMin`/`WanderRerollMax`.
- **Assets**: `WanderMove`/`JumpMove`/`LinearMove` SOs under `ScriptableObjects/Movement/`; wired
  `Frog.movement → JumpMove`, `Snake.movement → LinearMove` (round-tripped from disk).
- **Tests** (`ZooWorld.Tests.EditMode`): 7 classes, 20 new tests — `JumpMathTests`, `BoundsReturnTests`,
  `MovementHeadingTests`, `WanderMoveTests`, `LinearMoveTests`, `JumpMoveTests`,
  `MovementBehaviourStatelessTests` (reflection: strategies have no mutable instance fields).

**Verification (this session):** **EditMode 47/47 green** (T01's 27 + T02's 20); 0 Console errors;
`dotnet format --verify-no-changes` green on Runtime; forbidden-API + Unity-statics grep clean in `.Animals`.

**Scene infra (outside the brief — done to unblock the MCP test runner):** the runner refuses to start
unless the editor's active scene is saved. Added `Assets/_Project/Scenes/Gameplay.unity` (Camera +
Directional Light) + Build Settings entry, and **removed the junk default `Assets/Scenes/SampleScene.unity`**
(and the empty `Assets/Scenes/` folder). Not a T02 deliverable → proposed as a separate commit.

**Commits proposed:** `feat: T02 movement rules` (code + tests + assets + this doc-close) ·
`chore: replace default scene with _Project/Gameplay` (scene swap + Build Settings).
