# Zoo World — Implementation Guardrails

> **Purpose.** The engineering contract for Zoo World: how the code is structured so it stays
> clean, extensible (the graded axis — "1000 animals"), testable, and runs correctly. It does
> **not** replace the design — that's [`game-design.md`](../product/game-design.md). It sits on the
> locked baseline — [`ADR 0001`](adr/0001-tech-baseline.md) — and the animal-architecture decision —
> [`ADR 0002`](adr/0002-animal-architecture.md). Naming/formatting live in
> [`csharp-style.md`](csharp-style.md), machine-enforced by the repo-root `.editorconfig`.
>
> **This revision** folds in the fixes from the 7-lens adversarial hypothesis review (2026-06-25):
> a single `Simulation` tick owner, interface seams that make the rules genuinely headless-testable,
> a deterministic end-of-step collision drain, the physics-correctness contract, and SO-based
> movement strategies (replacing the earlier `[SerializeReference]` choice).
>
> **Right-size this.** Zoo World is a small primitives sim. These are responsibility boundaries,
> not a 50-file mandate. Build the vertical slice first; split a class only when it earns it.

## 1. Non-negotiable goals

Clear ownership (one reason to change); low coupling (spawning, predation, movement, UI don't own
each other); **testable rules** (predation, spawn planning, movement/bounds/jump math, counters run
headless in EditMode — pure C#, no MonoBehaviour, no `Time`/`Random`/`Physics` statics directly);
**no avoidable GC** in the running sim; **correct physics** (visible "fly apart", no frozen animals,
deterministic predation); **extensibility** (a new species is data, not edits).

## 2. Architecture shape

Thin Unity adapters over testable plain-C# rules, with **one system that owns the tick**:

```
Simulation (the ONE FixedUpdate owner) + UI views + composition root  ── adapt Unity
        drive
Pure C# rules: FoodChainResolver, SpawnPlanner, movement/JumpMath, BoundsReturn, DeathCounters
        operate over
Plain value structs + interface seams (AnimalState, MoveContext, FieldBounds, IOccupancyQuery,
ISpawnSequence, IClock, IRandom) and ScriptableObject config (AnimalDefinition, AnimalCatalog, SimConfig)
```

- **`Simulation`** is the single injected service with `Update`/`FixedUpdate`. It iterates the
  active-animal list, builds a `MoveContext`, ticks each animal's strategy, applies the resulting
  velocity, drains the collision queue, and publishes events. **No other gameplay MonoBehaviour has
  a magic `Update`/`FixedUpdate`** (avoids per-object marshalling + makes the tick deterministic).
- **`Animal` is a DUMB adapter:** Rigidbody/collider + runtime state (`dead`, spawn-seq, movement
  state) + it only **enqueues** its `OnCollisionEnter` contacts. It holds **no rules and no service
  dependencies** — so it doesn't need (impossible) constructor injection.
- Gameplay **rules are plain C#** over structs/interfaces; they never touch Unity statics. This is
  what makes the EditMode test claims real.

## 3. Folder layout & namespaces

Per ADR 0001: code under `Assets/_Project/Scripts/{Runtime,Editor}`, data under `ScriptableObjects/`;
tests in `Tests/EditMode`. Assemblies `ZooWorld.Runtime`, `ZooWorld.Editor`, `ZooWorld.Tests.EditMode`.
Root namespace `ZooWorld`, sub-namespaces `.Animals`, `.Spawning`, `.Predation`, `.Core` (shared
structs + seams + the `Simulation` tick owner), `.UI`, `.Config`, `.Composition`. Suffixes: `*View`, `*Service`/`*Resolver`/`*Planner`, `*Presenter`,
`*Definition`/`*Config`, `*Behaviour` (a strategy SO), `*LifetimeScope`/`*EntryPoint`. Avoid
`Manager`, `Helper`, `Utils`, `Data`.

## 4. Core systems

| System | Owns | Must NOT |
|---|---|---|
| **`Simulation`** (the tick owner) | the active-animal list; building `MoveContext`; ticking strategies; applying velocity; draining the collision queue; publishing `AnimalDied` | hold predation/spawn *formulas* (delegates to the pure rules); search the scene |
| `Animal` (dumb adapter) | Rigidbody/collider, `dead`, spawn-seq, `MovementState`, enqueue its collision contacts, pool reset | own rules; have its own `Update`/`FixedUpdate`; resolve services; touch UI |
| `MovementBehaviour` (SO Strategy, **stateless**) | pure motion logic: produce a desired velocity from `(MovementState, MoveContext, MovementTuning)`; variants `WanderMove`, `JumpMove`, `LinearMove` | hold per-animal state (state lives on `Animal`); read Unity statics |
| `FoodChainResolver` (pure) | outcome for a pair from `AnimalState` (role+strength+seq+dead) → `Outcome` | mutate MonoBehaviours; touch Unity physics; double-process |
| `SpawnPlanner` (pure) | `NextInterval(IRandom)`, species weighting, placement via `IOccupancyQuery`, cap + predator-floor over a `PopulationSnapshot` | call `Physics.*` directly; read live scene counts |
| `Spawner`/`AnimalFactory` + pools | the UniTask cadence loop; take/return + reset; create from a definition | `Instantiate`/`Destroy` per spawn; leak UniTask tokens |
| `DeathCounters` (pure) + `HudPresenter` + `HudView` (uGUI) | counts via `AnimalDied`; format & display | recompute from the scene; poll per frame |
| `TastyLabel` + pool | a pooled, **unparented**, **camera-billboarded** world label at the predator's position; own lifetime | be parented to a poolable predator |
| `FieldBounds` service | the XZ play rectangle **computed from the camera frustum on the ground plane** | be a hardcoded constant; be read via `Camera.main` inside a rule |
| `GameLifetimeScope` + `SpawnLoop` (`IAsyncStartable`) | wire dependencies; build `FieldBounds`; start the spawn loop with a scope-tied token | hold gameplay rules; do heavy work in `Configure()` |

## 5. Data & ScriptableObjects

- `AnimalDefinition` (SO): `id`, `displayName`, `role` (Prey/Predator), `strength` (int),
  **`MovementBehaviour movement`** (a reference to a strategy SO), `spawnWeight`, size, color, mass,
  per-species tuning (jump distance/interval, speed). The Definition carries the **tuning**; the
  strategy SO carries the **logic**.
- `AnimalCatalog` (SO): the single list of definitions → drives spawn weighting + pool + DI. A new
  species = **add one SO** (+ a new `MovementBehaviour` SO **only** for genuinely new motion).
- **Movement strategies are `ScriptableObject` and STATELESS** — a shared asset referenced by many
  definitions, so it must hold zero per-animal state: per-animal **mutable** state lives in
  `MovementState`, and per-animal **constants** (speed, jump distance/interval, damping) arrive as an
  **`in MovementTuning`** parameter — never on the SO. (Chosen over `[SerializeReference]`: refactor-safe,
  designer-drags-an-asset, statelessness enforced — see ADR 0002.) An EditMode test asserts strategy SOs
  declare no mutable instance fields.
- `SimConfig` (SO): global tunables (game-design.md §9). **Pool prewarm is derived per definition**
  (`prewarm ≈ round(spawnWeight × cap)`), not a global magic number, so adding a species needs no
  pool re-tuning. Config assets are read-only at runtime.

## 6. Predation contract (deterministic, end-of-step)

`OnCollisionEnter` on an `Animal` does **only** one thing: enqueue the unordered pair `(minSeq, maxSeq)`
into the `Simulation`'s contact buffer (deduped). It does **not** resolve inline — Unity does not
guarantee both halves of a pair fire in the same step, and 3-body piles make in-callback resolution
order-dependent.

Once per FixedUpdate, after physics, `Simulation` **drains** the buffer and resolves deterministically:

1. Sort the queued pairs; process **predator-eats first, then prey×prey bounces**.
2. For each pair, **rebuild each `AnimalState` from the live `Animal` at drain time** (a per-resolve
   snapshot, so the `dead` guard reads current state — not a stale enqueue-time value), then call
   `FoodChainResolver.Resolve(in AnimalState a, in AnimalState b) → Outcome` (pure over role + strength
   + seq + dead). Prey×Prey → bounce, no death; Prey×Predator → prey dies; Predator×Predator → higher
   `strength` survives (tie → lower seq), loser dies.
3. **Idempotency is keyed on the pair** and on each combatant's `dead` flag (early-out if either is
   already dead) — not on "only the lower-seq side acts", so a dropped sibling callback can't drop a
   resolution.
4. The `Simulation` applies each `Outcome`: despawn the dead animal, raise a single
   `readonly struct AnimalDied { Role Role; Vector3 Position; }`, and (for an eat) spawn the
   "Tasty!" label. **Counters subscribe to `AnimalDied` only** — one increment per death, by victim
   role. Prey×Prey never counts. Predator×Predator = exactly one dead-predator + one "Tasty!".

## 7. Physics setup (correctness contract)

- **Profile (all animals):** dynamic Rigidbody, **freeze posY + rotX/Z**, gravity OFF,
  **`sleepThreshold = 0`** (a gravity-off body otherwise sleeps during low-velocity moments and
  silently ignores velocity sets → animals freeze — a real bug), **Collision Detection = Discrete**
  (leaps are far too slow to tunnel: ~1.5 m over ~0.3 s ≈ 0.1 m/step ≪ the ~1 m body; Continuous is
  pure cost here), **low `linearDamping ≈ 4` (prov.)** (without it a `VelocityChange` burst on a
  gravity-off body never decelerates — the leap has no finite distance and `JumpMath` no closed
  form; the damping is the model the live body and `JumpMath` share). Dedicated **`Animal` layer**,
  collision matrix = **Animal×Animal + Animal×Floor**.
- A **thin static floor collider** at y = plane (invisible) — removes the degenerate
  min-penetration-axis case where a contact resolves along the frozen Y and the solver fights the
  constraint into XZ jitter. ("No ground collider" was a self-imposed constraint with no upside.)
- A low-restitution, low-friction **PhysicMaterial** on animals to keep crowded contacts from
  grinding.
- **Prey×Prey "fly apart" needs an explicit impulse** — velocity-driven movers can meet with ~zero
  relative velocity, so passive restitution is invisible. On the authoritative prey×prey resolution
  the `Simulation` applies `rb.AddForce(contactNormal × kick, ForceMode.VelocityChange)` (kick ≈
  3–5 m/s, tunable) to both, then opens the grace window. Under `linearDamping ≈ 4` the kick coasts
  ~0.9 m across the 0.6 s window — a clean, readable separation.
- **Grace window** = a plain `float graceUntil` on the `Animal` (set to `clock.Now + ~0.6 s` on a
  bounce, idempotent extend). While `clock.Now < graceUntil` the mover **does not apply the strategy
  (wander/leap) velocity** (the bounce coasts and reads) — **except `BoundsReturn`, which still
  overrides when the body is out of bounds** (staying on-screen outranks the bounce visual); and
  **`JumpMove` defers a new leap** until expiry (so no burst is silently swallowed). At expiry it
  **blends** the wander velocity back in (lerp, not hard set) and the next wander heading is **biased
  away from the partner** so they don't re-converge. **Grace is
  NOT a UniTask await** (per-collision awaits churn + have an overlapping-await re-entrancy bug + are
  untestable; the float is zero-alloc and `IClock`-testable).

## 8. Movement & the tick

- `Simulation.FixedTick`: for each active animal, build the ambient `MoveContext { float Dt; IRandom Rng;
  in FieldBounds Bounds; IClock Clock; }`, then call `desired = animal.Movement.Tick(ref
  animal.MovementState, in ctx, in animal.Tuning)` — `animal.Tuning` is the per-animal `MovementTuning`
  (Speed/JumpDistance/JumpInterval/LinearDamping) cached at spawn from the `AnimalDefinition` +
  `SimConfig.linearDamping`, so the shared stateless SO reads its species' constants without holding them.
  The strategy advances `MovementState` by ref and returns a desired XZ velocity; apply bounds-return; set
  `rb.linearVelocity = desired` (skipped during grace — except a `BoundsReturn` override when the body is
  out of bounds).
- **Bounds-return** is a pure `(position, in FieldBounds, heading) → heading` function with an inner
  margin (hysteresis), steering toward the field centre. `FieldBounds` is a struct computed once from
  the camera — never `Camera.main` inside the rule.
- **Jump (Frog/Rabbit):** `JumpMove` issues a horizontal velocity burst sized by a **pure `JumpMath`**
  closed-form from `(tuning.JumpDistance, tuning.LinearDamping, ctx.Dt)` — inverting the per-step damping
  recurrence `v *= 1/(1 + linearDamping·dt)` so the burst `v0 ≈ jumpDistance × linearDamping`
  (≈ 6 m/s for 1.5 m at damping 4) lands the nominal distance; a mid-leap collision legitimately
  diverts it. `JumpMove` does **not** start a leap while `clock.Now < graceUntil` (it defers).
  (`JumpMath` is the one physics-coupled pure rule — the EditMode test pins the closed-form; the Play
  smoke confirms the live leap matches within tolerance.) The visual hop is a **child-mesh Y offset
  sampled from an `AnimationCurve`** in the tick — sampling a curve directly is the cheapest, most
  physics-consistent option (no tween/coroutine).
- **Linear (Snake):** constant speed along its wander/return heading.

## 9. DI & libraries (VContainer / UniTask)

- **One composition root** (`GameLifetimeScope`). Pure rules (`FoodChainResolver`, `SpawnPlanner`,
  `DeathCounters`, `IClock`, `IRandom`, `ISpawnSequence`) are `Lifetime.Singleton` **scope** services
  (constructor-injected). SO assets (`AnimalCatalog`, `SimConfig`) are serialized on the scope and
  bound via **`RegisterInstance`**. `Simulation`, the factory, and the pools are scope singletons.
  **"No singletons" means no STATIC singletons and no service locator** — container-scoped singletons
  owned by the scope and disposed on teardown are correct and expected. **No `Resolve` from gameplay.**
- Because `Animal` is a dumb adapter (no service deps) and strategies are pure SOs, **nothing
  un-injectable needs injection** — the classic VContainer pooled-MonoBehaviour seam is dissolved.
- **UniTask** — the spawn-cadence loop and the "Tasty!" label lifetime only. Run the loop from an
  **`IAsyncStartable` entry point** (not `Configure()`), awaiting a scope-tied `CancellationToken`;
  wrap in `SuppressCancellationThrow()`/catch `OperationCanceledException` for clean teardown; one
  fresh `CancellationTokenSource` per pooled object on take (the previous one cancelled on despawn — a
  cancelled token can't be reused). **Rule timing stays on
  `IClock`**, not UniTask. `SpawnPlanner.NextInterval(IRandom)` is the pure interval; UniTask just
  awaits it.
- **Feedback animations = a small UniTask lerp helper** (no DOTween — dropped: not on OpenUPM + the
  review's weakest-justified lib). Three spots: "Tasty!" rise+fade, spawn scale-in, and a **death puff** — a *separate* pooled effect at
  the death position (the dying animal returns to the pool at once, so the death visual can't live on
  it; the jump arc is an `AnimationCurve`, §8). The helper lerps a value over a duration
  with an `AnimationCurve` ease, driven by UniTask and **tied to the object's `CancellationToken`**;
  on despawn the token is cancelled, which stops the animation cleanly — no per-tween bookkeeping,
  no stale-animation-on-reuse footgun. Pass state by value (no capturing closures in the hot path).

## 10. Interface seams (what makes the rules headless)

Every rule depends on an injected seam, never a Unity static:

| Seam | Production impl | Test impl |
|---|---|---|
| `IClock` | `Time.time`/`Time.fixedDeltaTime` wrapper | a settable fake clock |
| `IRandom` | seeded `System.Random` wrapper | a seeded/stubbed sequence |
| `ISpawnSequence` | a never-reused monotonic `long`, assigned at take-from-pool | a hand-set counter |
| `IOccupancyQuery` | `Physics.CheckSphere`/`OverlapSphereNonAlloc` on the Animal layer | a list of circles / always-clear/blocked stub |
| `FieldBounds` (struct) | computed from the camera frustum at composition | a literal rectangle |
| `AnimalState` / `Outcome` (structs) | filled from the live `Animal` | hand-built |
| `PopulationSnapshot` (struct) | `{ total, predatorCount, ... }` from the active list | hand-built |
| `MovementTuning` (struct) | built at spawn from `AnimalDefinition` + `SimConfig.linearDamping` | hand-built |

## 11. Pooling reset contract

Animals + labels are pooled; never `Instantiate`/`Destroy` in the running sim. On **return** and
**take** an `Animal` resets: `linearVelocity`/`angularVelocity` = 0; transform; `dead` = false; a
fresh `ISpawnSequence` id; `MovementState` timers/leap/grace; and its **`CancellationTokenSource`** —
**cancelled on despawn** (which stops any in-flight feedback lerp) and replaced with a **fresh CTS on
take** (a cancelled token can't be reused). The label pool resets its own timer/position/animation the
same way. A reused object must never carry stale velocity, a stuck
`dead` flag, a frame-1 jump, or a live animation.

## 12. Forbidden / restricted APIs

Forbidden in runtime/hot paths: `GameObject.Find*`/`FindObjectsOfType`/`FindAnyObjectByType`;
`GetComponent` in the tick; `Camera.main` per frame (cache it / use `FieldBounds`); **LINQ in
per-frame loops**; `SendMessage`; `Resources.Load`; repeated `Instantiate`/`Destroy`;
`new WaitForSeconds` (use UniTask); **`renderer.material` for color** (use `MaterialPropertyBlock` —
`renderer.material` instantiates + breaks SRP batching). Before handoff:
```powershell
rg -n "GameObject\.Find|FindObjectsOfType|FindAnyObjectByType|SendMessage|Resources\.Load|Camera\.main|\.material\b|DOTween|DG\.Tweening" Assets/_Project
```
Any runtime match is removed or justified.

## 13. Events & GC

Events are **typed `readonly struct`** payloads via a strongly-typed delegate; subscribe once at
composition with **instance-method handlers (no capturing lambdas)**; unsubscribe on dispose. No
global `EventBus`. `AnimalDied` carries `(Role, Vector3)`. Near-zero steady-state GC means: struct
events, non-capturing async callbacks, `MaterialPropertyBlock` colors, the alloc-free UniTask feedback
helper, pooled everything — "near-zero" = no per-frame steady-state alloc (label/eat spikes excepted).

## 14. Testing (Definition of Done — per mechanic)

Not done until: (1) **EditMode unit tests in the same change** for the pure rule; (2) a **smoke pass**
(Play / MCP — enter Play, drive the flow, Console clean); (3) **full suite re-run, no regression**.
Headless tests (all over structs/seams, §10): `FoodChainResolver` (each matrix cell; strength + tie;
dead-guard idempotency — one death/one count, never both-die); `SpawnPlanner` (`NextInterval` range;
weighting; placement via a fake `IOccupancyQuery` + skip-after-N; cap pause; predator-floor
(`predatorCount==0` at/below cap → the next spawn is a predator, no eviction) over a `PopulationSnapshot`); `BoundsReturn` (the pure reflection/hysteresis fn); `JumpMath` (closed-form
distance round-trip from `linearDamping`); movement-during-grace (`JumpMove` defers a leap while `Now < graceUntil`; `BoundsReturn` still overrides out-of-bounds); `DeathCounters` (one increment per death, correct role, zero-alloc). Strategy
SOs: assert no mutable instance fields. Play-mode smoke confirms the real prey-prey "fly apart" reads,
the leap travels ~nominal, and no leftover animation fires on a reused pooled object.

## 15. Performance

Target **60 FPS desktop** (the wall with these fixes is ~150–200 animals; the cap ~100–120 is safe
with headroom). Limiter = contacts/sec + GC cadence, not the movement ticks. **Discrete** collision;
`sleepThreshold` tuned; one `Simulation` tick loop (not 120 per-object `FixedUpdate`); pooled
animals+labels; `MaterialPropertyBlock` color (SRP-batched, ~1 draw cluster); the alloc-free UniTask
feedback helper; struct events. **Do NOT add spatial partitioning** (PhysX broadphase covers 120 in a small
box); the spawn overlap-scan is O(n) and negligible at n≈120. Slightly enlarging the board cuts
density/jitter. Profile a Player build, not the Editor.

## 16. Code-review checklist

Single responsibility? rules readable without UI/physics? **`Animal` stayed a dumb adapter (no
`Update`, no rules, ≤ ~150 lines)?** one `Simulation` tick owner? dependencies explicit, one
composition root, no static singletons / no `Resolve` in gameplay? rules over structs/seams (no Unity
statics)? collision resolved in the end-of-step drain (not inline)? grace is an `IClock` float (not a
UniTask await)? feedback animations cancelled on despawn (UniTask token)? events
struct + non-capturing? color via `MaterialPropertyBlock`? rules covered by EditMode tests in the
same change, full suite green?

## 17. Red flags (refactor on sight)

A class > ~300 lines (Animal > ~150); a gameplay MonoBehaviour with its own `Update`/`FixedUpdate`
other than `Simulation`; collision resolved inside `OnCollisionEnter`; a rule calling `Physics.*` /
`Camera.main` / `Time` / `Random` directly; a per-collision UniTask await; a strategy SO with a
mutable instance field; a feedback animation (UniTask lerp) not cancelled on despawn (fires on the reused object); `Resolve`
from gameplay or a static singleton; `renderer.material` color set; testable logic trapped in a
MonoBehaviour; a library used in one trivial place "for the CV".

## 18. Acceptable simplicity (anti-over-engineering)

Good architecture here is **not maximal**. Acceptable: binary Prey/Predator + a 2×2 resolver
(`strength` is the seam, multi-tier diets are out); one authored scene + a composition root; SO
strategies + structs; primitive visuals; **two** justified UPM libraries (VContainer + UniTask),
each genuinely exercised; feedback is a small UniTask lerp helper (DOTween was dropped — not on
OpenUPM + the review's weakest-justified lib). Overkill: ECS/DOTS (banned), spatial partitioning, networking,
Addressables, a save system, perception/seeking AI, a diet matrix, MVVM for two counters. The ideal
result is boring in the best way — explicit, deterministic, easy to debug.
