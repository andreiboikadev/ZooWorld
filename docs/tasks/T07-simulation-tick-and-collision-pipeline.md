# T07 — `Simulation` tick owner + collision pipeline (drive → grace → bounds → enqueue → drain → resolve → events)

| | |
|---|---|
| Milestone | M2 — adapters |
| Depends on | T02, T03, T05, T06 |
| Touches scene/prefabs | **no** — runtime code + EditMode tests only. Edits 4 merged runtime types (`Animal`, `AnimalFactory`, `MovementBehaviour`, `JumpMove` — all **additive**). The Play smoke spawns transient objects into `Gameplay.unity` but commits **no** scene/prefab/asset change (floor collider + scene placement + DI wiring are T08). |
| Status | ✅ done — pending commit |

## Goal

Bring the animals to life: the **`Simulation`** — the ONE service that owns the FixedUpdate tick (guardrails §2,
ADR 0002 §3 KEYSTONE) — iterates the active animals, ticks each strategy, applies the resulting velocity
(driving continuous movers, letting the jumper's burst coast, suppressing strategy velocity during the
post-bounce grace, and steering back at the bounds), then **drains the end-of-step collision queue**,
resolves each pair through the T03 `FoodChainResolver`, despawns the dead, raises `AnimalDied`, and applies
the prey×prey separation impulse + grace. After T07 the world *runs* (move → collide → eat/bounce → count
event) — T08 only adds the spawn cadence + DI + scene so it runs **on its own**. (GDD §6/§7/§8/§11;
guardrails §2/§4/§6/§7/§8/§10/§13/§14; ADR 0002 §3/§5/§6.)

## Scope decisions (resolved — chosen as the best fit; **please validate before commit**, cheap to flip now)

T07 is the integration hub: it wires five merged subsystems and is the first real caller of the collision
pipeline. These pin the boundaries the GDD/guardrails leave open; flag any reading you want changed.

1. **`Simulation` is a plain `sealed class` implementing `VContainer.Unity.IFixedTickable` + `IContactSink` +
   `IDisposable` — NOT a MonoBehaviour.** Constructor-injected; `FixedTick()` is a public method the DI
   PlayerLoop calls in production and a test calls directly. *Why:* it makes the entire tick + drain
   **headless-testable** (`new Simulation(...)`, `Register`, `Enqueue`, `FixedTick()` — assert state, no
   Play), and VContainer's `IFixedTickable` supplies the FixedUpdate without a magic MonoBehaviour — exactly
   how T05's `HudPresenter : IStartable` works (so `VContainer` is already a `ZooWorld.Runtime` asmdef ref;
   no asmdef change). Collision *detection* must still be a Unity message, so it stays on the `Animal`
   MonoBehaviour (decision 5); *resolution* lives in the plain `Simulation`. *Rejected:* a MonoBehaviour
   `Simulation` (can't `new` it in tests; needs Play to tick — the project's whole point is headless rules).
2. **Velocity application is a pure decision + a thin apply.** A pure `MovementDrive.Decide(...) →
   DriveCommand { DriveMode Mode; Vector3 Velocity }` (Mode ∈ `None`/`SetVelocity`/`Impulse`) encodes the
   whole grace×bounds×coast×impulse interaction; the `Simulation` just executes the command
   (`SetVelocity` → `Body.linearVelocity = v`; `Impulse` → `Body.AddForce(v, ForceMode.VelocityChange)`;
   `None` → leave the body coasting). Continuous movers (`WanderMove`/`LinearMove`) are `SetVelocity`; the
   jumper's burst is `Impulse` (honouring the **T02 §5 PINNED** "`AddForce(VelocityChange)`, never
   `rb.linearVelocity = Vector3.zero`"); a jumper's idle/grace ticks and an in-bounds grace tick are `None`
   (the bounce/coast reads); out-of-bounds **overrides grace** and rides the magnitude of `desired` (so a
   continuous mover returns at `Speed` and a jumper's *leap* is redirected toward centre — see decision 3
   for the Speed-0 frog). To pick `Impulse` vs `SetVelocity` **without the `Simulation` type-switching on the
   concrete strategy** (open/closed), add `public virtual bool IsImpulseDriven => false;` to
   `MovementBehaviour` and override `=> true;` in `JumpMove` — **the only edits to merged T01/T02 types, both
   additive/backward-compatible** (a property, not a field → the T02 stateless-SO reflection test still
   passes). *Rejected alt (zero merged-edits):* drop the flag, apply every non-zero `desired` as
   `SetVelocity` (the burst too) and only `None` on zero — simpler and makes the leap velocity directly
   EditMode-assertable + gives an exact fixed distance, **but** silently refines the T02 §5/guardrails §7
   "`AddForce`" wording. Recommended only if you'd rather not touch merged code; flag your preference.
3. **Bounds-return for the Speed-0 jumper: a `margin ≥ leap-distance` invariant (primary) + a force-leap
   safety net (decided).** `Frog`/`Rabbit` carry cruise `Speed = 0` (T01 — a jumper idles between leaps), so
   `steer × Speed` is zero and can't return a drifting frog; and a leap coasts
   `BurstSpeed / LinearDamping = JumpDistance ≈ 1.5 m` (`JumpMath`), which **exceeds the default 1 m inner
   margin** — so a frog leaping outward near the edge could land past the *true* edge (an earlier draft's
   "0.9 m bounce-coast < 1 m margin" reasoning omitted this larger *leap* coast; the GDD requires "off screen →
   return"). **Primary guarantee:** size `InnerMargin ≥ JumpDistance` (≈ 1.5–2 m; T08 builds `FieldBounds`
   from the camera, GDD §3) — then an in-bounds jumper, which only ever leaps from ≥ `InnerMargin` inside the
   edge, cannot clear the true edge, and the bounce kick's ~0.9 m coast stays in too, so a jumper never leaves
   frame. **Safety net** (so a mis-tuned margin can't silently leak a frog off-screen): when the `Simulation`
   tick sees an **idle** (`desired == 0`) **out-of-bounds** jumper it sets `NextLeapTime = clock.Now`, firing
   a **recovery leap inward next tick** (redirected by the OOB-launch branch) instead of waiting out the
   interval. `MovementDrive.Decide` stays pure (idle-OOB → `None` for *this* tick's velocity; the recovery is
   a `NextLeapTime` nudge in the tick loop, which already owns spawn-seeding of that field). *(The nudge
   **defers while the jumper is in its grace window** — `JumpMove` checks grace before the leap condition — so
   a frog bounced out at the very edge can't recovery-leap for ≤ `GraceSeconds` 0.6 s; this is harmless because
   the primary margin invariant already keeps it in frame and the bounce kick's inward coast still applies — the
   net only bites if the invariant is mis-sized.)* *Rejected:* a continuous glide (`steer × BurstSpeed` — reads
   as a slide, not hops) or a new `SimConfig` bounds-return speed (a §9 number for one edge case — §18).
4. **Spatial sourcing from the live bodies; T07 raises `AnimalDied` only — the "Tasty!" channel is T09.**
   Per T03's resolved decision the resolver leaves `Outcome.Position`/`BounceNormal` at `Vector3.zero`; the
   `Simulation` sources them at drain from the live bodies — the **victim's** position (→ `AnimalDied.Position`)
   and the prey×prey **bounce normal** (the XZ body delta). The matrix scopes the "Tasty!" *label* to T09
   (feedback); since the predator-eat position's only consumer is that label, T07 **does not** source it or
   raise a Tasty signal yet (building a raise-seam with no subscriber = the dead-code T05 rejected for
   `HudView`). The drain holds both bodies, so T09's add is a ~2-line raise. T07's smoke asserts the sourced
   `AnimalDied.Position` is **non-zero** (T03's anti-world-origin guard). *Rejected:* introduce a
   `TastySignal` in T07 (no subscriber until T09).
5. **`Animal` gains `OnCollisionEnter` that only enqueues into the `Simulation` via an `IContactSink` seam.**
   Per guardrails §2/§6 the dumb adapter "only enqueues its `OnCollisionEnter` contacts into the Simulation's
   contact buffer" — a back-reference set imperatively at `Register` (NOT DI-injected, NOT a rule), so the
   "no service dependencies" rule holds. `OnCollisionEnter` does one `GetComponent<Animal>` on the other
   body (a contact event, not the per-frame tick — allowed by §12) and calls `_contacts.Enqueue(this,
   other)`. *Rejected:* a per-animal C# `event` the `Simulation` subscribes/unsubscribes on every
   take/return (delegate churn on the hot pool path) — the single sink reference set on take is cheaper.
6. **Drain = two ordered passes (deaths then bounces), unordered-pair dedup, per-resolve dead-guard.** Pass A
   resolves every queued pair and applies only `Death`s (so an eaten animal is `IsDead` before any bounce is
   considered); pass B re-resolves and applies `Bounce`s (a pair with a just-eaten member now dead-guards to
   `None`). This realises guardrails §6.1 "eats first, then bounces" + the 3-body correctness without
   depending on Unity's callback order. The grace **away-heading bias + blend-back lerp** (guardrails §7) are
   **deferred as tuning polish** (the shipped prey are jumpers that re-roll heading on the next leap, so the
   bias is near-dead for them; the kick + grace-suppression already make the bounce read) — flag to pull in
   if you want a future wander-prey to separate cleanly. The T06 **double-despawn guard** lands here (T07 is
   `Despawn`'s first caller); the dead-guard + dedup are the primary protection, the guard is belt-and-braces.

## §9 numbers used (game-design.md §9 / `SimConfig`)

`GraceSeconds 0.6` · `BounceKick 4` m/s · `LinearDamping 4` · `JumpInterval 1.5` s · `JumpDistance 1.5` m →
`BurstSpeed = JumpDistance × LinearDamping = 6` m/s · Snake `Speed 2.5` m/s · `WanderReroll [0.8, 1.5]` s ·
`FieldBounds` `HalfExtents (10, 6)`, `InnerMargin ≥ JumpDistance` (≈ **1.5–2 m** for the production field — so
a leap from the margin can't clear the true edge, decision 3; the GDD's provisional `~1 m` is raised to meet
this. The EditMode fixtures deliberately use `1` to *reach* the out-of-bounds safety-net paths the invariant
would otherwise make unreachable) · `MaxPopulation 120` + `PredatorFloor 1` → cap `121`.

## Acceptance criteria

### Value seams (`ZooWorld.Core`)

**`IContactSink`** — `Core/IContactSink.cs`: `public interface IContactSink` with
`void Enqueue(Animal a, Animal b);` (the `Simulation`'s inbound collision seam; `Animal` depends on this, the
`Simulation` implements it — same `ZooWorld.Runtime` assembly, no asmdef cycle). It references `Animal`
(`.Animals`) — the first `.Core`→`.Animals` namespace edge, an accepted consequence of guardrails §3 placing
the `Simulation` (which already references `Animal`/`AnimalFactory`) in `.Core`; move it to `.Animals` if you
prefer `.Core` to hold no adapter references (either compiles — the edge exists via `Simulation` regardless).

**`SimulationTuning`** — `Core/SimulationTuning.cs`: `public readonly struct SimulationTuning`; ctor
`(float bounceKick, float graceSeconds)`; read-only props `float BounceKick`, `float GraceSeconds`. The two
§9 combat constants the `Simulation` needs, passed as plain values (not the `SimConfig` SO — mirrors T04's
`SpawnTuning`, so the `Simulation` is testable without authoring an SO). Built at composition (T08) from
`SimConfig.BounceKick`/`GraceSeconds`.

### Pure helper — spawn seeding (`ZooWorld.Animals`)

**`SpawnSeed`** — `Animals/SpawnSeed.cs`: `public static class SpawnSeed` with
`public static void Apply(ref MovementState state, IClock clock, IRandom rng, in MovementTuning tuning)`:
- `state.Heading = MovementHeading.RandomXz(rng);` — a non-zero unit XZ heading (required: `LinearMove` has
  no self-heal, GDD §11 — an unseeded snake freezes).
- `state.NextLeapTime = clock.Now + tuning.JumpInterval;` — **no frame-1 leap** (T06's clockless reset left
  it `0`; `JumpMove` leaps when `Now >= NextLeapTime`).
- `state.NextHeadingReroll = clock.Now + rng.Range(tuning.WanderRerollMin, tuning.WanderRerollMax);` — no
  frame-1 re-roll.
- `state.GraceUntil` left `0` (no grace at spawn).
- Draw order (pins the fakes): `RandomXz` angle first, then the wander-reroll interval (two `Value01` draws).
- No Unity statics beyond `Mathf`/`Vector3` (via `MovementHeading`); `#nullable enable`, Allman, XML doc, C# 9.

### Pure helper — velocity decision (`ZooWorld.Animals`)

**`DriveMode`** — `Animals/DriveMode.cs`: `public enum DriveMode { None, SetVelocity, Impulse }`.

**`DriveCommand`** — `Animals/DriveCommand.cs`: `public readonly struct DriveCommand`; ctor
`(DriveMode mode, Vector3 velocity)`; read-only `DriveMode Mode`, `Vector3 Velocity`. Static
`public static DriveCommand None { get; }` = `new(DriveMode.None, Vector3.zero)`.

**`MovementDrive`** — `Animals/MovementDrive.cs`: `public static class MovementDrive` with
`public static DriveCommand Decide(Vector3 desired, bool isImpulseDriven, float now, float graceUntil, bool isWithinInner, Vector3 steerToCenter)`.
Logic (top to bottom):
1. **Out of bounds** (`!isWithinInner`): if `desired == Vector3.zero` → `DriveCommand.None` (an idle jumper —
   no velocity to drive *this* tick; the tick loop's recovery-leap nudge returns it next tick — decision 3);
   else `v = steerToCenter * desired.magnitude`, return `isImpulseDriven ? Impulse(v) : SetVelocity(v)`.
   (Bounds-return **overrides grace** — staying on-screen outranks the bounce, guardrails §7/§8. The tick loop
   separately writes `steerToCenter` back into `state.Heading` so continuous movers don't oscillate — see the
   `Simulation` tick.)
2. **In grace** (`now < graceUntil`): `DriveCommand.None` (suppress strategy velocity; the bounce coasts).
3. **Idle** (`desired == Vector3.zero`): `DriveCommand.None` (jumper between leaps — coast under damping;
   never set zero, the T02 §5 freeze bug).
4. Otherwise: `isImpulseDriven ? Impulse(desired) : SetVelocity(desired)`.

No Unity statics beyond `Vector3` (`.magnitude`/`==`); `#nullable enable`, Allman, XML doc, C# 9.

### The tick owner (`ZooWorld.Core`)

**`Simulation`** — `Scripts/Runtime/Core/Simulation.cs`:
- `public sealed class Simulation : IFixedTickable, IContactSink, IDisposable` (`VContainer.Unity.IFixedTickable`).
- ctor `public Simulation(IClock clock, IRandom random, in FieldBounds bounds, in SimulationTuning tuning,
  FoodChainResolver resolver, AnimalDeathSignal deathSignal, AnimalFactory factory)` — stores all; owns
  `private readonly List<Animal> _active`, `private readonly List<(Animal a, Animal b)> _pendingPairs`,
  `private readonly HashSet<(long, long)> _pendingKeys`. (Injects the **concrete** `AnimalDeathSignal` for
  `Raise` — T05 decision 2.)
- `public void Register(Animal animal)` — `SpawnSeed.Apply(ref animal.MovementState, _clock, _random, in
  animal.Tuning);` (warms `animal.Body` so the tick never calls `GetComponent`); `animal.SetContactSink(this);`
  `_active.Add(animal);`. (Called by T08's spawner on each take; the T07 tests/smoke call it directly.)
- `void IContactSink.Enqueue(Animal a, Animal b)` — guard `ReferenceEquals(a, b)` / equal `Seq`; key
  `(long lo, long hi) = a.Seq < b.Seq ? (a.Seq, b.Seq) : (b.Seq, a.Seq)`; if `_pendingKeys.Add((lo, hi))` →
  `_pendingPairs.Add((a, b))` (unordered-pair dedup — both halves of a contact + repeats collapse to one).
- `public void FixedTick()` — **tick then drain** (guardrails §2):
  - **Tick:** `var ctx = new MoveContext(_clock.Dt, _random, in _bounds, _clock);` then for each active
    animal with `Movement != null`: `Vector3 pos = a.transform.position;` `Vector3 desired =
    a.Movement.Tick(ref a.MovementState, in ctx, in a.Tuning);` `bool within = BoundsReturn.IsWithinInner(in
    _bounds, pos);` `Vector3 steer = BoundsReturn.Steer(in _bounds, pos, a.MovementState.Heading);`
    **Out-of-bounds recovery (`!within`)** — *two* writes the carry-forwards require (both invisible to the
    pure `MovementDriveTests`): (1) **write the steered (toward-centre, unit) heading back** —
    `a.MovementState.Heading = steer;` — so a continuous mover doesn't re-read its stale *outward* heading and
    oscillate at the edge (T02 PINNED: *"the `Simulation` writes the steered heading back"*; `LinearMove` has
    no self-heal); (2) if it is an **idle jumper** (`a.Movement.IsImpulseDriven && desired == Vector3.zero`),
    `a.MovementState.NextLeapTime = _clock.Now;` so it fires a **recovery leap inward next tick** instead of
    coasting off-screen (decision 3). Then `DriveCommand cmd = MovementDrive.Decide(desired,
    a.Movement.IsImpulseDriven, _clock.Now, a.MovementState.GraceUntil, within, steer);` apply: `SetVelocity` →
    `a.Body.linearVelocity = cmd.Velocity`; `Impulse` → `a.Body.AddForce(cmd.Velocity,
    ForceMode.VelocityChange)`; `None` → nothing. (Iterate by index; no LINQ, no per-frame allocation —
    guardrails §12/§15.) **Read positions via `a.transform.position`, not `Body.position`** — the transform is
    physics-synced each step in Play and is the value `OnSpawn` actually writes, so the sourced positions are
    reliable in the headless EditMode tests (`Rigidbody.position` can read stale with no physics step).
  - **Drain** (decision 6): **Pass A** over `_pendingPairs` — `Outcome o = _resolver.Resolve(Snapshot(a),
    Snapshot(b));` `if (o.Kind == OutcomeKind.Death) ApplyDeath(in o, a, b);`. **Pass B** over
    `_pendingPairs` — re-resolve; `if (o.Kind == OutcomeKind.Bounce) ApplyBounce(a, b);`. Then
    `_pendingPairs.Clear(); _pendingKeys.Clear();`.
  - `private static AnimalState Snapshot(Animal a)` → `new AnimalState(a.Role, a.Strength, a.Seq, a.IsDead)`
    (rebuilt per resolve so the dead-guard reads **current** state — guardrails §6.2).
  - `private void ApplyDeath(in Outcome o, Animal a, Animal b)` — `Animal victim = a.Seq == o.DeadSeq ? a :
    b;` `Vector3 deathPos = victim.transform.position;` `victim.MarkDead();` `_deathSignal.Raise(new
    AnimalDied(o.VictimRole, deathPos));` `_active.Remove(victim);` `_factory.Despawn(victim);`. (Source the
    position **before** despawn, from `transform.position` — the live victim point, never `Vector3.zero` — T03.)
  - `private void ApplyBounce(Animal a, Animal b)` — `Vector3 d = a.transform.position - b.transform.position;
    d.y = 0f;` `Vector3 n = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.right;` (deterministic fallback for a
    degenerate co-located pair); `a.Body.AddForce(n * _tuning.BounceKick, ForceMode.VelocityChange);`
    `b.Body.AddForce(-n * _tuning.BounceKick, ForceMode.VelocityChange);` then open grace idempotently:
    `float until = _clock.Now + _tuning.GraceSeconds; a.MovementState.GraceUntil =
    Mathf.Max(a.MovementState.GraceUntil, until); b.MovementState.GraceUntil =
    Mathf.Max(b.MovementState.GraceUntil, until);`. (No `AnimalDied` — a bounce never counts, GDD §6.)
  - `public void Dispose()` — null each still-active animal's sink (`foreach (a in _active) a.SetContactSink(null);`
    — symmetry with the `OnDespawn` clear, so an animal that's still active at scope teardown carries no
    dangling back-reference regardless of `Simulation`-vs-`AnimalFactory` dispose order), then `_active.Clear();
    _pendingPairs.Clear(); _pendingKeys.Clear();` (the factory is DI-owned/disposed separately).
- No Unity statics beyond `Vector3`/`Mathf`/`ForceMode`/`Rigidbody` access; **no** `GameObject.Find`/
  `Camera.main`/`Time`/`UnityEngine.Random`/LINQ in the tick (guardrails §12); `#nullable enable`, `sealed`,
  Allman, XML docs, C# 9.

### Edits to merged runtime types (all additive)

- **`Animal`** (`Animals/Animal.cs`) — add: `private IContactSink? _contacts;`
  `public void SetContactSink(IContactSink? sink) => _contacts = sink;` and
  `private void OnCollisionEnter(Collision collision)` → `if (_contacts == null || collision.rigidbody ==
  null) return; Animal? other = collision.rigidbody.GetComponent<Animal>(); if (other != null)
  _contacts.Enqueue(this, other);`. Add `internal bool Pooled { get; set; }` for the despawn guard. **Clear
  the sink on return:** add `_contacts = null;` to `OnDespawn()` (§11 reset symmetry — a pooled body carries
  no back-reference; `Register` re-sets it on the next take). (Still no `Update`/`FixedUpdate`; still no
  rules/`Resolve`; ≤ ~150 lines — guardrails §16.)
- **`AnimalFactory`** (`Animals/AnimalFactory.cs`) — **double-despawn guard:** set `animal.Pooled = true` in
  `CreatePooled` and at the end of `Despawn`; set `animal.Pooled = false` in `Spawn` (after the take); at the
  top of `Despawn` `if (animal.Pooled) return;` (a repeated `Despawn(a)` is a no-op — no double-push/alias).
- **`MovementBehaviour`** (`Config/MovementBehaviour.cs`) — add `public virtual bool IsImpulseDriven => false;`
  (continuous by default; the burst-vs-set seam for the `Simulation`).
- **`JumpMove`** (`Animals/JumpMove.cs`) — add `public override bool IsImpulseDriven => true;`.

### EditMode tests (`ZooWorld.Tests.EditMode`)

**`MovementDriveTests`** — `Tests/EditMode/MovementDriveTests.cs` (pure; no objects):
- **`Continuous_InBounds_NoGrace_SetsDesired`:** `Decide((2.5,0,0), false, now: 1f, graceUntil: 0f, within:
  true, steer: (2.5,0,0))` → `Mode == SetVelocity`, `Velocity == (2.5,0,0)`.
- **`Continuous_InGrace_Coasts`:** `Decide((2.5,0,0), false, now: 1f, graceUntil: 5f, within: true, steer:
  (2.5,0,0))` → `Mode == None`.
- **`OutOfBounds_OverridesGrace_ReturnsAtDesiredMagnitude`:** `Decide((2.5,0,0), false, now: 1f, graceUntil:
  5f, within: false, steer: (-1,0,0))` → `Mode == SetVelocity`, `Velocity == (-2.5,0,0)` (toward centre,
  magnitude preserved, even though in grace).
- **`Jumper_LaunchInBounds_Impulse`:** `Decide((6,0,0), true, now: 2f, graceUntil: 0f, within: true, steer:
  (6,0,0))` → `Mode == Impulse`, `Velocity == (6,0,0)`.
- **`Jumper_Idle_Coasts`:** `Decide(Vector3.zero, true, now: 2f, graceUntil: 0f, within: true, steer:
  Vector3.zero)` → `Mode == None`.
- **`Jumper_IdleOutOfBounds_Coasts`:** `Decide(Vector3.zero, true, now: 2f, graceUntil: 0f, within: false,
  steer: (-1,0,0))` → `Mode == None` (the margin + next leap return it — decision 3).
- **`Jumper_LaunchOutOfBounds_RedirectsBurstToCentre`:** `Decide((0,0,6), true, now: 2f, graceUntil: 0f,
  within: false, steer: (-1,0,0))` → `Mode == Impulse`, `Velocity == (-6,0,0)` (burst magnitude 6 steered
  toward centre).

**`SpawnSeedTests`** — `Tests/EditMode/SpawnSeedTests.cs` (pure; `FakeClock`/`FakeRandom`; `tuning =
new MovementTuning(2.5f, 1.5f, 1.5f, 4f, 0.8f, 1.5f)`):
- **`Apply_SeedsNonZeroHeading`:** `Apply(ref s, new FakeClock(now: 5f), new FakeRandom(0f, 0.5f), tuning)`
  → `s.Heading == new Vector3(1f, 0f, 0f)` (angle `0 → (cos 0, 0, sin 0) = (1,0,0)`, a **clean** exact heading
  — draw `0f` for the angle so the test dodges the `cos(π/2) ≈ -4.4e-8` trig noise that defeats an exact
  `Vector3` assert), `s.Heading.magnitude, Is.EqualTo(1f).Within(1e-5f)`, `s.Heading != Vector3.zero` (the
  snake-freeze guard). *(NUnit `Is.EqualTo(Vector3)` is component-exact and `.Within` does **not** apply to a
  whole `Vector3` — assert scalar components, or seed a noise-free heading, never `Is.EqualTo(vec).Within`.)*
- **`Apply_SeedsNextLeapTime_NoFrameOneLeap`:** same call → `s.NextLeapTime == 6.5f` (`5 + JumpInterval 1.5`)
  and `s.NextLeapTime > clock.Now` (GDD §11).
- **`Apply_SeedsNextHeadingReroll`:** → `s.NextHeadingReroll == 6.15f` (`5 + (0.8 + 0.5×0.7)`).
- **`Apply_LeavesGraceZero`:** → `s.GraceUntil == 0f`.

**`SimulationTests`** — `Tests/EditMode/SimulationTests.cs` (headless integration — drives the real
`FixedTick`/drain with no Play; physics is *not* simulated, so assert **state** the tick changes, not motion).
Fixture mirrors `AnimalFactoryTests`: a stub `Animal` prefab (`new GameObject("Animal", typeof(Rigidbody),
typeof(SphereCollider), typeof(Animal))`), strategy SOs via `ScriptableObject.CreateInstance<LinearMove>()` /
`<JumpMove>()`, an `AnimalFactory` over hand-built specs, `FakeClock`/`FakeRandom`, a literal `FieldBounds(new
Vector3(0,0,0), new Vector2(10,6), 1f)`, `SimulationTuning(4f, 0.6f)`, a real `FoodChainResolver`, a real
`AnimalDeathSignal` (subscribe a local handler to capture payloads). `[TearDown]` disposes the factory +
`DestroyImmediate`s the prefab GO + the strategy SOs. Helper to `Spawn`+`Register` an animal of a given index.
**`FakeRandom` draw budget:** each `Register`→`SpawnSeed.Apply` consumes **2** `Value01` (angle, then
wander-reroll) per animal, and a `JumpMove` leap consumes **1** more — queue enough per test (or rely on
`FakeRandom`'s exhausted-queue→`0f` fallback, which gives a clean `(1,0,0)` heading; the `FieldBounds`
`InnerMargin 1f` here is deliberately below `JumpDistance` to reach the OOB safety-net paths — decision 3 / §9).
- **`Register_SeedsState`:** register a jumper → `animal.MovementState.Heading != Vector3.zero` and
  `animal.MovementState.NextLeapTime == clock.Now + 1.5f` (integration of the `SpawnSeed` contract).
- **`FixedTick_DrivesLinearMover`:** a snake (`LinearMove`, `Speed 2.5`), `FakeRandom(0f, …)` so the seeded
  `Heading == (1,0,0)` (clean — no trig noise), spawned **in bounds** (e.g. origin); `FixedTick()` →
  `snake.Body.linearVelocity == new Vector3(2.5f, 0f, 0f)` (continuous `SetVelocity`; proves it does **not**
  freeze — the snake bug). *(Reading `linearVelocity` after a direct set works headless — `AnimalTests`
  already does it; seed angle 0 so every component is exact, per the `SpawnSeedTests` NUnit-`Vector3` note.)*
- **`FixedTick_OutOfBounds_WritesSteeredHeadingBack`** (the boundary-jitter guard, T02 carry-forward): a snake
  (`LinearMove`) at `transform.position` past the inner margin (e.g. `x = 9.5`, `HalfExtents.x 10`,
  `InnerMargin 1` → inner edge `9`) with `MovementState.Heading` forced **outward** to `(1,0,0)`; `FixedTick()`
  → `snake.MovementState.Heading.x < 0` (the toward-centre steer was written back, so next tick `LinearMove`
  re-reads an *inward* heading, not the stale outward one) **and** `snake.Body.linearVelocity.x < 0` (driven
  inward this tick). Without the write-back the snake oscillates at the edge forever — invisible to the pure
  `MovementDriveTests`; the full no-oscillation read is Play-smoke.
- **`FixedTick_JumperAdvancesLeapState`:** a frog seeded `NextLeapTime = 1.5`; `clock.Now = 1.5`;
  `FixedTick()` → `frog.MovementState.NextLeapTime == 3.0f` and `Heading != Vector3.zero` (the leap fired and
  re-armed; the burst's *velocity* is `Impulse` → confirmed in the Play smoke, not here).
- **`FixedTick_OutOfBoundsIdleJumper_ForcesRecoveryLeap`** (decision 3 — the off-screen-drift guard): a frog
  (`JumpMove`) registered (so `NextLeapTime = clock.Now + 1.5`, i.e. idle this tick, not in grace) with
  `transform.position` past the inner margin; `FixedTick()` → `frog.MovementState.NextLeapTime == clock.Now`
  (the recovery-leap nudge fired, so the next tick leaps — redirected *toward centre* by `MovementDrive`'s OOB
  branch, which overrides `JumpMove`'s fresh random heading with `steerToCenter × BurstSpeed`, not random).
- **`FixedTick_InBoundsIdleJumper_DoesNotNudge`** (the nudge's negative control): an **in-bounds** idle frog;
  `FixedTick()` → `NextLeapTime == clock.Now + 1.5f` (unchanged) — the recovery nudge is gated on
  out-of-bounds and must NOT fire in-bounds.
- **`Drain_PredatorEatsPrey_RaisesDeath_Despawns`:** predator (`Strength 5`) + prey at a known **non-origin**
  `posA` (e.g. `(3,0,4)`, so the **non-zero** assert is meaningful); `Enqueue(predator, prey)`; `FixedTick()`
  → `prey.IsDead == true`, captured `AnimalDied.Role == Prey`,
  `AnimalDied.Position == posA` (sourced from `transform.position`, **non-zero**), `predator.IsDead == false`,
  `prey.gameObject.activeSelf == false` (despawned), **exactly one** death event.
- **`Drain_PreyVsPrey_BothLive_OpensGrace_NoDeath`:** two prey; `Enqueue(a, b)`; `FixedTick()` → both
  `IsDead == false`, both `MovementState.GraceUntil == clock.Now + 0.6f`, **zero** death events. (The kick's
  velocity is Play-smoke; the grace state is asserted here.)
- **`Drain_DuplicateContacts_ResolveOnce`:** `Enqueue(predator, prey)`, `Enqueue(prey, predator)`,
  `Enqueue(predator, prey)`; `FixedTick()` → exactly **one** `AnimalDied` — the **behavioural** contract
  (duplicate/reversed contacts cause no double-death). *(Both the unordered dedup AND the dead-guard guarantee
  this outcome, so the test asserts the observable contract, not dedup in isolation; the dedup's distinct
  effect — a double prey×prey kick — is a Play-mode concern, since headless `AddForce` is a no-op. The dedup
  mechanism is logic-reviewed.)*
- **`Drain_EatsBeforeBounces_DeadPreyDoesNotBounce`:** predator `P`, `preyA`, `preyB`; enqueue the **bounce
  pair first** — `Enqueue(preyA, preyB)` **then** `Enqueue(P, preyA)`; `FixedTick()` → `preyA.IsDead`, one
  `AnimalDied(Prey)`, `preyB.IsDead == false`, `preyB.MovementState.GraceUntil == 0f`. *(The bounce-first order
  is deliberate and load-bearing: a one-pass drain iterating in enqueue order would open preyB's grace on
  `(preyA, preyB)` BEFORE preyA is eaten, so this order genuinely discriminates the two-pass
  deaths-before-bounces ordering; guardrails §6.1 + 3-body correctness.)*
- **`Drain_PredatorDuel_HigherStrengthSurvives`:** predators `Strength 5` vs `Strength 3`; `Enqueue`;
  `FixedTick()` → the `Strength 3` body `IsDead`, `AnimalDied.Role == Predator`, the `Strength 5` body alive,
  and **exactly one** `AnimalDied` (the duel-winner logic + that pass B re-resolves the same pair to `None`
  after the death — no double-kill; the two-pass *ordering* itself is proved by `Drain_EatsBeforeBounces`).
- **`Despawn_Twice_NoDoublePush`** (the T06 guard, may live in `AnimalFactoryTests` instead): `a = Spawn(0,…);
  Despawn(a); int free = FreeCount(0); Despawn(a);` → `FreeCount(0) == free` (idempotent).

(Guardrails §14: the resolver/bounds/jump math already ship pure tests; T07 adds the velocity-decision +
spawn-seed pure tests and the drain/grace/dedup/ordering integration tests. The *physical* leap distance,
fly-apart separation, and no-freeze-over-time are the Play smoke.)

## Implementation notes

- **New files:** `Core/{IContactSink, SimulationTuning, Simulation}.cs`; `Animals/{SpawnSeed, DriveMode,
  DriveCommand, MovementDrive}.cs`; `Tests/EditMode/{MovementDriveTests, SpawnSeedTests, SimulationTests}.cs`.
  **Edited (additive):** `Animals/Animal.cs`, `Animals/AnimalFactory.cs`, `Config/MovementBehaviour.cs`,
  `Animals/JumpMove.cs`. **No asmdef change** (all under `ZooWorld.Runtime`; `VContainer.Unity` already
  referenced — T05).
- **`using` outside the namespace** (`System.*` first): `Simulation` needs `System` (`IDisposable`),
  `System.Collections.Generic` (`List`/`HashSet`), `UnityEngine`, `VContainer.Unity` (`IFixedTickable`),
  `ZooWorld.Animals` (`Animal`/`AnimalFactory`/`BoundsReturn`/`MovementDrive`/`SpawnSeed`/`DriveCommand`),
  `ZooWorld.Config` (`Role`), `ZooWorld.Predation` (`FoodChainResolver`). `IContactSink` needs
  `ZooWorld.Animals` (`Animal`). Tests add `NUnit.Framework`, `ZooWorld.Tests.EditMode.Fakes`.
- **FixedUpdate ordering (why tick-then-drain works):** VContainer dispatches `IFixedTickable.FixedTick()`
  **before** `Physics.Simulate` in the FixedUpdate loop (it inserts its runner before
  `ScriptRunBehaviourFixedUpdate`), so the velocities **and** the in-drain `AddForce` kick set in `FixedTick`
  take effect in the **same** physics step. Unity fires `OnCollisionEnter` *after* `Physics.Simulate`, so
  contacts from step *N*'s physics are buffered and drained at step *N+1*'s `FixedTick`. **Consequence to
  expect (not a bug):** a fresh overlap is detected at the end of step *N* and its kick lands at step *N+1*,
  so the visible "fly apart" trails contact by one FixedStep (~20 ms) — the end-of-step drain is deterministic
  and callback-order-independent (guardrails §6, ADR 0002 §5), not a swallowed impulse.
- **Pitfalls:** (a) **never** `rb.linearVelocity = Vector3.zero` for a jumper's idle/coast tick (the T02 §5
  freeze bug) — that path is `DriveMode.None`; (b) bounds-return **overrides** grace when out of bounds
  (staying on-screen outranks the bounce, §7/§8) — the `MovementDrive` OOB branch is first; (c) rebuild
  `AnimalState` **per resolve** (`Snapshot`), never cache it across the drain, or the dead-guard goes stale;
  (d) source `AnimalDied.Position` from the live victim body **before** `Despawn`; (e) the prey×prey kick is
  `AddForce(VelocityChange)` to **both** bodies (passive restitution is invisible for velocity-driven movers,
  §7); (f) `OnCollisionEnter`'s `GetComponent` is a contact event, not the tick — the per-frame loop reads
  the **cached** `Body` (warmed at `Register`), never `GetComponent` (§12); (g) idempotent grace extend
  (`Mathf.Max`), so a second contact in the window doesn't shrink it; (h) the two-pass drain re-resolves in
  pass B (cheap pure call) — do **not** cache pass-A outcomes (they read pre-death state); (i) the
  `None`/coast path depends on `sleepThreshold = 0` (set in `OnSpawn`, guardrails §7) keeping an idle body
  awake so the next `SetVelocity`/`Impulse` isn't silently ignored — never raise it, and let the Play smoke
  idle a frog > 2 leap intervals to confirm its next leap still fires (catches a sleep regression).
- **DI shape (for T08, not built here):** register `Simulation` as a scope-singleton bound to itself +
  `IFixedTickable` + `IContactSink`; inject the concrete `AnimalDeathSignal`; **register the `Simulation`'s
  other ctor deps as scope-singletons — `FoodChainResolver` (class shipped T03), `IClock`/`IRandom`
  (`UnityClock`/`SeededRandom`, T01), and the `AnimalFactory` (T06)** — so the ctor resolves; build
  `SimulationTuning` + `FieldBounds` from `SimConfig`/the camera. T07 ships the class with a plain ctor so T08
  only wires it.

## Out of scope

- **Spawn cadence + DI + scene** — the UniTask `IAsyncStartable` spawn loop, `GameLifetimeScope` wiring,
  `AnimalSpec`/`SpeciesWeight` build, production `IOccupancyQuery`/`FieldBounds`-from-camera, the floor
  collider + Floor layer + Animal×Floor, scene placement, HUD `HudView`, and the **vertical-slice + crowd +
  HUD Play smoke** — **T08**.
- **"Tasty!" channel + label, death-puff, `MaterialPropertyBlock` colour, child-mesh jump arc, the CTS in the
  reset, the Rabbit** — **T09** (decision 4; T06 decision 6).
- **Grace away-heading bias + blend-back lerp** (guardrails §7) — deferred tuning polish (decision 6).
- Multi-tier diets, spatial partitioning, eviction — GDD non-goals / guardrails §15/§18.

## Verification

- **EditMode:** `MovementDriveTests` (7) + `SpawnSeedTests` (4) + `SimulationTests` (≈10, incl. the
  bounds-return heading-writeback + jumper recovery-leap guards) green; **full suite green** (89 prior + T07's
  new); **0 Console errors**. (`Window → Test Runner → EditMode → Run All`, or MCP
  `run_tests` with `Gameplay.unity` active; new files need a `scope: all` refresh — the T03/T04 import gotcha.)
- `dotnet format --verify-no-changes` green on the new/edited files; forbidden-API / Unity-statics grep clean
  in `.Core`/`.Animals` (no `GameObject.Find`/`Camera.main`/`Time`/`Random`/`.material`/LINQ-in-loops;
  `GetComponent` only in `OnCollisionEnter`, never in the tick).
- **Play smoke (this task's physical proof — MCP, transient, not committed):** hand-wire a `Simulation` +
  `AnimalFactory` + 1 Snake + 2 Frogs in Play (a throwaway driver calling `FixedTick()` per `FixedUpdate`),
  run ~2–3 s and confirm: the **snake** travels in a straight line at ~2.5 m/s and **turns cleanly** at the
  bounds — no edge-jitter/oscillation (the heading-writeback) — and never freezes; a **frog** leaps ~**1.5 m**
  then coasts to rest (Y stays 0, gravity off, no freeze) — calibrates `JumpMath` in-vivo (±tolerance; T02) —
  and **stays within the framed field** (a frog leaping outward near the edge hops back within a step, not
  off-screen — decision 3); two prey forced to overlap visibly **fly apart** and both survive
  (no `AnimalDied`); a prey + predator forced to overlap → the prey **despawns**, `AnimalDied(Prey)` fires
  (a subscribed counter ticks), the predator survives; `OnCollisionEnter` actually enqueues; **Console
  clean**. (The full cadence/cap/HUD-on-screen/crowd smoke is **T08**.)
- Never claim a test passed unless it ran **this session** with cited evidence (agent-verification §2).

## What was actually done

**Implemented 2026-06-27** — the `Simulation` tick owner + the end-of-step collision pipeline, per the
validated brief.

- **`ZooWorld.Core`:** `IContactSink` (the Animal→Simulation enqueue seam); `SimulationTuning` (readonly
  struct — BounceKick/GraceSeconds); **`Simulation`** — a plain `sealed class : IFixedTickable, IContactSink,
  IDisposable` (ctor-injected; `Register` seeds + wires the sink + warms `Body` + adds to the active list;
  `FixedTick` ticks movement → applies the `MovementDrive` command → drains; the two-pass drain (deaths then
  bounces, per-resolve `dead`-guard, unordered dedup) raises `AnimalDied` and applies the prey×prey impulse +
  grace; `Dispose` clears sinks + buffers).
- **`ZooWorld.Animals`:** `SpawnSeed` (heading + leap/reroll seeding); `DriveMode`/`DriveCommand`/
  `MovementDrive` (the pure grace×bounds×coast×impulse velocity decision). The bounds-return
  **heading-writeback** + the idle-OOB-jumper **recovery-leap nudge** live in the `Simulation` tick.
- **Edits to merged types (additive):** `Animal` (+`OnCollisionEnter`→enqueue, `SetContactSink`, `Pooled`,
  `_contacts = null` on despawn); `AnimalFactory` (double-despawn guard via `Pooled`); `MovementBehaviour`
  (+`virtual IsImpulseDriven => false`); `JumpMove` (+`override => true`).
- **Velocity model:** the **`IsImpulseDriven` flag** (the brief's recommended option) — not the zero-edit alt.
- **Tests** (`ZooWorld.Tests.EditMode`): `MovementDriveTests` (7), `SpawnSeedTests` (4), `SimulationTests`
  (12: register-seed, linear-drive, bounds heading-writeback, jumper leap-advance, idle-OOB recovery-leap,
  in-bounds-no-nudge, predator-eats, prey×prey grace, dedup, eats-before-bounces (bounce-first → discriminates
  two-pass), predator-duel, double-despawn). **23 new.**

**Deviations from the brief (2, both mechanical — intent unchanged):**
1. **Call-site `in` dropped on `animal.Tuning`** (`SpawnSeed.Apply` / `Movement.Tick` calls): the explicit
   `in` keyword requires an lvalue, but `Tuning` is a property (rvalue) → **CS8156**. Dropping the keyword
   passes it as `in` via a compiler temp (identical semantics); the existing strategy tests pass `in` from
   *locals*, which is why it first surfaced here.
2. **Test asmdef gained a `VContainer` reference** — the brief's *"no asmdef change"* was inaccurate. The
   `SimulationTests` cast `(IContactSink)sim` forces the compiler to resolve `Simulation`'s interfaces, one of
   which (`IFixedTickable`) is in VContainer → **CS0012**. `autoReferenced` adds VContainer only to the
   predefined assemblies, not to the explicit (`overrideReferences`) test asmdef. Production API unchanged.

**Verification (this session, via MCP):** **EditMode 112/112 green** (89 prior + 23 new; re-verified this
session after the test-rigor hardening; 3.06 s); **0 Console
errors** (only the unrelated MCP-bridge WebSocket warning); `dotnet format --verify-no-changes` exit 0 on all
new/edited files; forbidden-API / Unity-statics grep clean (the only `GetComponent` is the lazy `Body` getter
+ the `OnCollisionEnter` contact event — never the tick). **Play smoke** (manual `Physics.Simulate` stepping):
the **snake** travels linearly ~2.3 m/s and **turns cleanly at the inner margin** (maxX 8.04, never the 10
edge; heading flips inward — no jitter), Y stays 0; the **frog** leap is finite, Y stays 0, never sleeps,
gravity off; **predator eats prey** → prey despawns + `AnimalDied(Prey)` → `DeathCounters.DeadPrey == 1`,
predator survives; **prey×prey** kick physically separates (1.40→3.09 m, placed non-overlapping → isolated
from PhysX depenetration) + grace opens, both live, no count.

**Smoke findings:**
- **Live leap ≈ 1.37 m vs the JumpMath nominal 1.5 m** (~8% undershoot). Root cause confirmed by an isolated
  single-Rigidbody drag probe: Unity integrates linear damping as `v ← v·(1 − damping·dt)` **damp-then-move**
  (per-step factor 0.92 = `1 − 4·0.02`), so the `distance × damping` closed form (which assumes the
  `1/(1+damping·dt)` model) lands ~8% short — coast 1.378 m for `v0 = 6`. **Within the provisional GDD §7
  feel — no fix needed** (the leap contract holds: finite / on-plane / no-freeze). An optional ~+9% `JumpMath`
  tweak could centre it on 1.5 m; better decided at the T08 tuning pass with the full field visible.
  *(Re-verification correction: an earlier reading of "1.87 m / 24% overshoot" was a **smoke-harness
  artifact** — the test frog was spawned on top of the still-active prefab source body, so PhysX
  depenetration shoved it ~0.5 m before the leap, inflating the origin-relative measurement. With the prefab
  source deactivated the idle frog stays at 0.000 and the clean single leap is 1.37 m. **Product code is
  correct — an idle animal does not drift**; the prior reading was the measurement's fault, not the code's.)*
- **`OnCollisionEnter` does not dispatch under `Physics.Simulate` in Edit mode** (the solver runs, MonoBehaviour
  collision callbacks don't), so the drain was driven via manual `Enqueue` (exactly what `OnCollisionEnter`
  does). The `OnCollisionEnter`→enqueue wiring is code-verified and the enqueue→drain→physics path is now
  confirmed live; the **live-callback Play smoke belongs to the T08 vertical slice** (the brief already scopes
  the full Play smoke there).

**Commit proposed:** `feat: T07 Simulation tick owner + end-of-step collision pipeline` — _pending human commit_.
