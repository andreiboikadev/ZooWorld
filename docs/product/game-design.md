# Zoo World — Game Design

> **What this is.** The source of truth for *what* we build. Derived from the Sakura Games
> test-task brief (`Zoo world_2026.pdf`, kept locally, **not committed**). It captures our
> interpretation of the brief plus the design decisions made where the brief was silent.
> Values marked **(prov.)** are tunable defaults we chose — not from the brief — and live in a
> ScriptableObject config, never hard-coded. Rules are written to be **unit-testable** (pure
> functions over data, no MonoBehaviour/engine-statics dependency where avoidable).
>
> **The graded axis** (brief): the **animal architecture** — *"clear, easy to extend… assume we
> will add 1000 different animals (birds, spiders, fish, crabs)"*. Everything below serves that.
>
> Last verified against the brief: 2026-06-25.

## 1. Concept

A 3D top-down sandbox: animals spawn over time and roam; a simple predation rule plays out
through physics collisions. **No win/lose** — an observation toy. Visuals are placeholder
primitives (brief: *"No graphics required… Box, spheres… will not affect final results"*) — so
effort goes into architecture, not art. (The "no graphics" clause means art **quality** isn't
graded; required *feedback* — the "Tasty!" label, the counters, prey/predator color
distinction — is still in scope.)

## 2. Brief facts vs. our decisions

| Brief says | Our decision / interpretation |
|---|---|
| *"the **3d** game"*, *"**spheres**"*, *"fly apart **by physics**"* | **3D** scene + **3D physics**. Not 2D. |
| *"Top down view"* | Top-down camera over a horizontal **XZ** plane. |
| *"Every (1-2) seconds one animal appears"* | One spawn per random **1–2 s** (brief), subject to the population safeguard (§5). |
| *"moving randomly"* | Random-wander heading, re-rolled every **0.8–1.5 s (prov.)**. |
| *"move out of screen → change direction to return"* | Field bounds = the **camera's visible XZ footprint** (§3); on exit, steer heading back inward (no physical wall). |
| Prey×Prey *"fly apart by physics"* | Physical (non-trigger) collision; physics resolves the bounce; both survive (§7 grace window makes the bounce visible). |
| Prey×Predator *"become dead and disappear"* | Prey despawned (returned to pool). |
| Predator×anything *"they will eat them"* | Predator consumes prey; predator-vs-predator → §6. |
| Predator×Predator *"one survives… any easiest way"* | Higher **`strength`** (data on `AnimalDefinition`) survives; tie → lower spawn-sequence survives. Deterministic + unit-testable. |
| *"label 'Tasty!'… under predators"* on each eat | Pooled world-space label at the predator's position on **each kill — prey OR rival predator**. Brief-required, not provisional: the brief says predators *"eat"* *"other animals, prey or predators"* and *"each time a predator eats another animal the label 'Tasty!' should appear"*. |
| Frog: *"prey… jumps… every x second… fixed distance"* | Periodic horizontal **leap of nominal fixed distance** (§7); interval & distance **(prov.)**. |
| Snake: *"predator… moving linear (fixed … by second)"* | Constant **linear speed** along its heading **(prov.)**. |
| UI: *"top right… counter of dead preys and predators… uGUI"* | **Two** uGUI counters (dead prey, dead predators), top-right. *(Chosen reading of the singular "counter": two tracked categories — more informative.)* |
| *"don't use ECS"* | Classic OOP + SOLID. No DOTS/ECS. |
| *"DI… better to add"*, *"show architecture patterns"* | **VContainer** DI + named patterns: Strategy, Factory+Object-Pool, Observer. |
| *"publish to git… send a link"* | Public GitHub repo + README/ARCHITECTURE. **Plus (beyond brief):** a standalone Windows build + EditMode tests. |

## 3. World & camera

- 3D scene, single ground reference on **XZ**, **Y** up. Top-down camera (perspective, looking down).
- **Field bounds** = a rectangle on XZ **computed from the camera frustum projected onto the ground plane** (a `FieldBounds` service — ADR 0002), so *"outside bounds" == "off screen"* per the brief. The camera frames the **full** bounds. Bounds drive spawn placement (§5) and the return-to-field steering (§7). **They are not physical colliders.** Size is a function of the camera (**prov. ~20×12 m** at the default framing).

## 4. Animals — the architecture (graded core)

An animal is **composition, not deep inheritance**: a thin `Animal` MonoBehaviour adapter that
holds a data definition + a plug-in movement strategy; all predation outcomes live in one
resolver (§6). 

- **`AnimalDefinition`** (ScriptableObject): `id`, `displayName`, **`role`** (Prey/Predator),
  **`strength`** (int — predator-vs-predator winner + the seam toward future tiered diets),
  **a movement-strategy reference** (Strategy; SO-based — mechanism in [ADR 0002](../architecture/adr/0002-animal-architecture.md)),
  `spawnWeight`, `size`, `color`, `mass`, per-species tuning. Read-only at runtime.
- **Movement strategies** (Strategy): `Wander`, `Jump` (frog), `Linear` (snake). Future
  `Fly`/`Swim`/`Crawl` slot in as new strategy assets — no edits elsewhere.
- **`AnimalCatalog`** (ScriptableObject): the single list of all `AnimalDefinition`s. The spawner
  reads weights from it, the pool is keyed by definition, and DI binds it.

**Extension cost (honest, not "zero edits"):** adding a species that **reuses** an existing
movement behaviour = **pure data** — author one `AnimalDefinition` and add it to the
`AnimalCatalog` (one list). Adding **new** motion = one new movement-strategy asset. **No
edits** to existing animals, the resolver, the spawner, the pool, or physics code. (The `1000`
in the brief refers to **authored species variety**, not concurrent instances — live population
is capped, §5.)

**Predation model — scope (deliberate right-sizing):** roles are **binary** Prey/Predator and
"a predator eats any non-predator; predators fight by `strength`". This covers the brief exactly
(it only specifies prey-vs-prey / prey-vs-predator / predator-vs-predator). Multi-tier / omnivore
/ size-based diets (*"a hawk eats a snake"*) are **out of scope**; the `strength` field + the
single `FoodChainResolver` are the seam if that's ever needed. We do **not** build a diet matrix
now (over-engineering for a 3-species ship).

**Shipped species:** Frog (Prey, Jump) + Snake (Predator, Linear) **+ Rabbit (Prey, *reuses*
Jump)** — the Rabbit exists purely to *prove* the data-only extension path.

**Patterns:** Strategy (movement behaviours), **`AnimalFactory` = the pool's spawn+configure
API** (one layer, not a factory wrapping a pool), Observer (typed death/eat events → counters &
labels), DI (VContainer composition root). *(No "state machine" — Boot→Running is just the
composition-root bootstrap; not dressed up as a pattern.)*

## 5. Spawning & population

- The **spawner** emits one animal every random **1–2 s** (brief), choosing a species by
  **weighted random** over the `AnimalCatalog` (**prov.** weights §9).
- **Placement:** a valid in-bounds position with a clearance check (≥ **animal size ~1 m (prov.)**
  center distance) against active animals via `Physics.CheckSphere` on the Animal layer; up to
  **N=10 (prov.)** attempts, else **skip this tick** (no force-place).
- **Object pooling is mandatory** — animals are taken from / returned to a pool keyed by
  definition; never `Instantiate`/`Destroy` at runtime.
- **Population safeguard:** a configurable **max count (prov. ~120)**. At the cap the spawner
  **pauses**, resuming when predation drops the count. *(This intentionally deviates from the
  brief's literal "spawn forever" to keep the sim stable — not a brief requirement.)*
- **Anti-deadlock:** if the field reaches the cap with **no predators present**, the count can
  never fall. The spawner therefore keeps a **predator floor**: the predator **spawn-weight is
  boosted as the predator count → 0**, so a predator reappears **without evicting any live animal**
  (honouring "removed only by predation"). **(prov.: guarantee ≥1 predator on screen.)**

## 6. Predation (one resolver owns it)

A single pure **`FoodChainResolver`** decides the outcome for a colliding pair from their roles +
`strength`. **Resolution is deterministic and idempotent:** collisions are **enqueued** and resolved
once per physics step (**not inside the callback** — Unity's callback order isn't guaranteed), keyed
per unordered pair, early-outing if either is already `dead`. Mechanism: guardrails §6.

| A \ B | Prey | Predator |
|---|---|---|
| **Prey** | bounce (physics only; both live) | A eaten → despawn; B shows "Tasty!" |
| **Predator** | B eaten → despawn; A shows "Tasty!" | higher `strength` lives (tie → lower seq lives); loser despawns; winner shows "Tasty!" |

- **Counters are driven by the resolver's single `AnimalDied(role)` event**, never by the
  "Tasty!" display event. **Exactly one** counter increments per resolved death, keyed by the
  **victim's role**, **once** (the `dead` flag prevents re-processing). Prey×Prey never counts.
- A predator-vs-predator encounter yields **exactly one** dead-predator increment and **one**
  "Tasty!".

## 7. Movement & physics control

- **Physics profile (all animals):** dynamic Rigidbody, **freeze position-Y + rotation X/Z**
  (stay on the plane, upright), **gravity OFF**, **`sleepThreshold = 0`** (else gravity-off bodies
  sleep & freeze), **Discrete** collision, **low `linearDamping` (≈ 4, prov.)** (a leap burst then
  travels a finite nominal distance and the prey×prey kick decays into a visible coast — a drag-free
  gravity-off body would slide forever), a **thin static floor collider** (avoids degenerate
  Y-penetration jitter). A dedicated **Animal physics layer**; matrix = **Animal×Animal + Animal×Floor**.
- **Control vs. physics:** a mover sets a **desired velocity** each `FixedUpdate`. On the **prey×prey**
  bounce the resolver applies an **explicit separation impulse** (passive restitution alone is too
  weak to read), then the animal **yields control for a grace window (prov. ~0.6 s, blend-back)**
  with the next wander heading **biased away from the partner**. Grace suppresses only the **strategy
  (wander/leap) velocity** (the bounce coasts and reads) — **bounds-return still steers if the body
  leaves the field** (staying on-screen outranks the bounce), and a **new leap is deferred** to after
  the window. Engineering detail: guardrails §7.
- **RandomWander:** move at species speed along a heading re-rolled every **0.8–1.5 s (prov.)**.
- **Bounds-return** overrides wander as a **pure function** `(position, bounds, heading) →
  heading`: when the position is **outside the rectangle** (with an **inner margin** for
  hysteresis so a single crossing can't toggle every frame), steer the heading **toward the
  bounds centre**. Directly unit-testable.
- **Jump (Frog/Rabbit):** every **~1.5 s (prov.)** apply a **horizontal velocity burst** sized
  (from `linearDamping` via the pure `JumpMath` closed-form, `v0 ≈ distance × damping` ≈ 6 m/s) to
  travel a **nominal ~1.5 m (prov.)** along the heading;
  idle between leaps. "Fixed distance" = nominal under no collision — a mid-leap collision
  legitimately alters the landing (that *is* "fly apart by physics"). Any visual hop is a
  **child-mesh Y offset (visual only)**, never Rigidbody Y (which stays frozen).
- **Linear (Snake):** constant **~2.5 m/s (prov.)** along its (wander/return) heading.

## 8. UI (uGUI)

- **Two counters** top-right: "Dead prey: N", "Dead predators: N". Updated via the `AnimalDied`
  event (Observer), never polled.
- **"Tasty!"** label: a **pooled world-space text spawned at the predator's world position,
  NOT parented to the predator** (lives under a static labels root), **billboarded to face the
  camera** (top-down world text is otherwise edge-on/unreadable). It runs out its **~1 s (prov.)**
  independently, so a predator that despawns mid-display doesn't drag or kill it. The label pool
  resets its own timer/position on take and detaches on return.

## 9. Numbers (all in a ScriptableObject config — (prov.) unless marked brief)

| Value | Default | Source |
|---|---|---|
| Spawn interval | random 1–2 s | brief |
| Spawn weights (Frog / Snake / Rabbit) | 0.45 / 0.30 / 0.25 (normalized) | prov. |
| Predator floor (min on screen) | 1 | prov. |
| Frog & Rabbit jump interval / distance | 1.5 s / 1.5 m | prov. |
| Snake speed | 2.5 m/s | prov. |
| Wander heading re-roll | every 0.8–1.5 s | prov. |
| Post-collision control grace (blend-back) | 0.6 s | prov. |
| Prey×prey separation kick | ~4 m/s | prov. |
| Linear damping (all animals) | 4 — jump burst derived (≈ distance×damping) | prov. |
| Field bounds (≈ camera footprint) | ~20×12 m | prov. |
| Field bounds inner margin (bounds-return) | ≥ jump distance (≈ 1.5 m) | prov. |
| Max population (safeguard) | ~120 | prov. |
| Spawn clearance / max attempts | ~1 m / 10 | prov. |
| Animal pool prewarm / max | derived per definition: round(spawnWeight × maxPopulation) / max ≥ maxPopulation | prov. |
| "Tasty!" label lifetime / pool | 1.0 s / 16 | prov. |
| Jump-arc height / duration (visual hop) | 0.5 m / 0.45 s | prov. (T09) |
| Spawn scale-in duration | 0.2 s | prov. (T09) |
| Death-puff lifetime | 0.4 s | prov. (T09) |
| Animal size | ~1 m | prov. |

## 10. Flow

Minimal: **Boot** (composition root wires dependencies, builds the scene) → **Running** (endless:
spawn + simulate). No menu, no results, no pause. A composition-root bootstrap, not a state machine.

## 11. Pooling reset contract

On both **return-to-pool** and **take-from-pool**, an `Animal` resets (via `OnDespawn()` /
`OnSpawn()` the pool calls): Rigidbody `linearVelocity`/`angularVelocity` = 0 and wake/sleep handled;
transform position/rotation set; `dead` = false; all behaviour timers/accumulators reset
(randomised where applicable); any in-flight leap/grace state cleared. The "Tasty!" label pool
has its own reset (timer/position/parent). A reused animal must never carry stale velocity, a
stuck `dead` flag, or a frame-1 jump.

## 12. Non-goals

No ECS/DOTS; no win/lose/score; no networking; no save; no real art/audio; no AI seeking/
pathfinding (predators don't hunt — predation is contact-only); no multi-tier/omnivore diets; no
actual 1000 species authored (we ship the *architecture* + 3 species); no menus/pause.

## 13. Performance

Target **60 FPS desktop**. Object pooling for animals + labels; no per-frame allocations or LINQ
in `Update`/`FixedUpdate`; no `GameObject.Find`/`Camera.main` per frame; population cap; simple
primitive colliders. Detailed forbidden-API list + the pool-reset checklist live in
`docs/architecture/implementation-guardrails.md` (next).

## 14. Deliverables

- Public GitHub repo (`andreiboikadev/ZooWorld`) with a clean history.
- `README.md` (run + overview) + an `ARCHITECTURE.md` including a **"how to add a new animal"** walkthrough (authored in T09).
- A **standalone Windows build** attached.
- EditMode unit tests for the pure rules: predation resolver (each matrix cell, strength tie,
  dead-guard idempotency), spawn weighting/placement, wander + bounds-return math, jump distance,
  counters (one increment per death, correct category).

## 15. Open / provisional

- All **(prov.)** numbers are tuning defaults, confirmed by feel during smoke.
- Bonus species fixed as **Rabbit (Prey, reuses Jump)**; its visuals/exact tuning stay provisional.
- The predator-vs-predator "Tasty!" and the two-counter reading are **chosen interpretations** of
  silent/ambiguous brief points (flagged inline) — cheap to flip if a reviewer reads them otherwise.
- **Leap drag & grace scope (resolved, prov.):** animals carry **`linearDamping ≈ 4`**; the jump
  burst is derived from it (`JumpMath`), and the grace window suppresses only the strategy velocity
  (bounds-return stays active, a new leap defers). Both are tuning defaults — adjustable in T02/T07.
