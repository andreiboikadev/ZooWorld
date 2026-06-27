# T09 — Feedback visuals + Rabbit (data-only) + `MaterialPropertyBlock` colour + Windows build + README/ARCHITECTURE → M3 ship

| | |
|---|---|
| Milestone | M3 — ship |
| Depends on | T08 (vertical slice + composition root + scene) — also consumes T02 `JumpMove` SO, T03 `Outcome.RaiseTasty`, T05 `AnimalDied`/`IAnimalDeathSignal` channel, T07 `Simulation` drain |
| Touches scene/prefabs | **yes** — `Assets/_Project/Prefabs/Animal.prefab` (child-mesh restructure); new `Prefabs/TastyLabel.prefab` + `Prefabs/DeathPuff.prefab`; `Scenes/Gameplay.unity` (effects root + new scope refs); `ProjectSettings` (Standalone build target + scenes-in-build); new `ScriptableObjects/Rabbit.asset` |
| Status | ▫ not started |

## Goal

Close the project: turn the running-but-grey vertical slice (T08) into the **shippable M3 deliverable**.
Add the brief-required **feedback** (the "Tasty!" label on each eat; prey/predator **colour** distinction;
a spawn scale-in; a death puff; the frog/rabbit **jump arc**), prove the graded **data-only extension**
path with the **Rabbit** (one new `AnimalDefinition`, zero code), produce the **standalone Windows build**,
and write **`README.md` + `ARCHITECTURE.md`** (with the "how to add a new animal" walkthrough). After T09
the submission is complete (GDD §1/§6/§7/§8/§14; guardrails §7/§8/§9/§11/§13; ADR 0002 §2/§7).

## Decisions (resolved — chosen for the test task; flagged so a reviewer can override)

1. **New provisional GDD §9 numbers (feedback timing).** The arc/pop/puff have no §9 row yet. Proposal —
   add four **(prov.)** rows + serialize them on `SimConfig` (`[Header("Feedback")]`, next to the existing
   `_tastyLifetime`/`_tastyPoolSize`): **jump-arc height 0.5 m**, **jump-arc duration 0.45 s**, **spawn
   scale-in 0.2 s**, **death-puff lifetime 0.4 s**, plus two **`AnimationCurve`** fields — **`_jumpArcCurve`**
   (a **hump**: keys (0,0),(0.5,1),(1,0) — the hop rises then falls) and a shared **`_riseEase`** (a 0→1
   ramp, default `EaseInOut(0,0,1,1)`, used by the pop / Tasty / puff). The arc duration is **cosmetic** —
   it need not equal the physics coast time. **Resolved** at these values; they add §9 rows (same pattern
   as T08's `InnerMargin`).
2. **"Tasty!" is delivered by a new typed Observer channel, not a direct `Simulation`→UI call.** T05's
   out-of-scope said the label is *"spawned directly by the Simulation on an eat (`Outcome.RaiseTasty`)"*;
   realised literally that couples `.Core`→`.UI`. Proposal — a **`PredatorAte` channel mirroring
   `AnimalDied`** (`Core/PredatorAte.cs` struct `{ Vector3 Position }`, `Core/PredatorAteHandler.cs`,
   `Core/IPredatorAteSignal.cs`, `Core/PredatorAteSignal.cs`); the `Simulation` **raises** it (no UI
   dependency, exactly as it raises `AnimalDied`), a `.UI` spawner **subscribes**. This refines T05's
   "directly" wording into the same Observer the rest of the sim uses. *(Alt: extend `AnimalDied` with a
   killer position — rejected: pollutes the clean death payload the counters subscribe to.)* Recommended.
3. **Per-animal feedback ownership — the `Simulation` drives the in-tick / animal-lifetime visuals; the
   `Animal` exposes only `Mesh` + `Token`.** The jump arc (per-tick child-mesh Y) and the spawn scale-in
   (one-shot lerp on take) are driven by the **`Simulation`** (the one tick owner; it holds the clock +
   the feedback tuning), so the `Animal` stays a dumb adapter (≤ ~150 lines, guardrails §16) — it only
   exposes the `_mesh` transform and its `CancellationToken`. The detached/post-mortem visuals ("Tasty!"
   at the predator, death-puff at the victim) are **Observer subscribers** with their own pools (the
   animal is already back in the pool). *(Alt: a per-`Animal` `Update`/visual component — rejected, only
   `Simulation` may tick, guardrails §2/§17. Alt: tick-sample the spawn-pop too and drop the per-animal
   CTS — rejected, guardrails §11 mandates the despawn-cancelled CTS for in-flight feedback.)* Recommended.
4. **"Tasty!" render tech = legacy `TextMesh` (3D world text), fixed-orientation billboard.** GDD §8 wants
   *"world-space text … billboarded to face the camera."* Because T08 locked the camera **straight down −Y**
   (`Dot ≈ 1`), the billboard is a **constant** rotation (lie flat on XZ, readable from above) set **once at
   spawn** — **no `Camera.main` per frame** (guardrails §12). Proposal — a `TextMesh` (one component, no
   per-label world-space `Canvas` overhead; alpha-fade via `textMesh.color`). *(Alt: world-space uGUI
   `Canvas`+`Text` per label — heavier; uGUI is mandated only for the §8 counters, not this label.)*
   Recommended; revisit only if the camera ever tilts.
5. **Namespaces stay within ADR 0001's locked set** (`.Animals`/`.Core`/`.UI`/…; no new `.Feedback`, which
   would edit a locked ADR). Proposal — the lerp helper + the signals → **`ZooWorld.Core`**; `TastyLabel`/
   `DeathPuff` + their pooled spawners → **`ZooWorld.UI`** (presentation); the pure `JumpArc` → **`ZooWorld.Animals`**
   (next to `JumpMath`). **Resolved:** reuse the existing namespaces — no ADR-0001 edit (a `.Feedback`
   namespace would mean superseding a locked ADR for no functional gain).
6. **Task size — ship as one T09, or split T09a/T09b?** T09 bundles six deliverables (feedback, colour,
   Rabbit, build, two docs). It is the single M3 row in the matrix. Proposal — keep **one** brief but
   stage the work (feedback+colour+Rabbit+tests **first**, smoke-green, **then** build+docs), one `feat:`
   commit. *(Alt: split into `T09a` (code+tests) and `T09b` (build+`README`/`ARCHITECTURE`) — offered if
   the reviewer prefers two smaller commits.)* Recommended: one.
7. **Build artifact location & git.** Proposal — build **StandaloneWindows64** to `Builds/Windows/ZooWorld.exe`,
   **git-ignored** (not committed — too large), attached to the submission per GDD §14; only
   `ProjectSettings` (scenes-in-build + player settings) is committed. **Resolved:** that path, git-ignored.

## Acceptance criteria

Exact names; numbers cite game-design.md §9 (those marked **new** are decision 1). New runtime code under
`Assets/_Project/Scripts/Runtime/`. Pure rules ship EditMode tests in the **same** change (guardrails §14).
`#nullable enable`, `sealed`, Allman, block-scoped namespace, XML docs, C# 9 — csharp-style.

### A. Prey/predator colour via `MaterialPropertyBlock` — `AnimalSpec`, `CatalogProjection`, `Animal`, prefab

- **`Core/AnimalSpec.cs` (extend):** add `Color Color { get; }` (ctor param `Color color` inserted **between
  `mass` and `spawnWeight`** — that exact slot; the call sites pass positional args).
  *(Ripples to all **5** `new AnimalSpec(` sites — `CatalogProjection.cs:33` plus the test spec-builders
  `AnimalTests.Spec`, `AnimalFactoryTests.Spec`, `SimulationTests.PreySpec`/`PredatorSpec` — each needs the
  new `Color` arg (tests can pass `Color.white`) or the suite won't compile.)*
- **`Composition/CatalogProjection.cs` (extend):** `specs[i] = new AnimalSpec(def.Role, def.Strength,
  def.Size, def.Mass, def.Color, def.SpawnWeight, def.Movement, in tuning)` — pass `def.Color`.
- **`Animals/Animal.cs` (extend, stays a dumb adapter):**
  - `[SerializeField] private MeshRenderer _renderer = null!;` and `[SerializeField] private Transform _mesh = null!;`
    (the child mesh — also the jump-arc / scale-in target); `public Transform Mesh => _mesh;`.
  - A reused block: `private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");`
    (**URP/Lit** — not `_Color`) + `private static MaterialPropertyBlock? s_block;`.
  - In `OnSpawn`, **null-guarded** (headless `AnimalTests` build an `Animal` with no visual child): if
    `_renderer != null`, `s_block ??= new MaterialPropertyBlock(); _renderer.GetPropertyBlock(s_block);
    s_block.SetColor(s_baseColorId, spec.Color); _renderer.SetPropertyBlock(s_block);`. **Never**
    `_renderer.material` (guardrails §12/§17). Reset `_mesh.localPosition = Vector3.zero` if `_mesh != null`.
  - **Feedback CTS (guardrails §11 — the linchpin of pool-reuse safety, make it an explicit edit not buried prose):**
    `private CancellationTokenSource? _cts; public CancellationToken Token => _cts!.Token;`. In `OnSpawn` (first
    reset line): `_cts?.Cancel(); _cts?.Dispose(); _cts = new CancellationTokenSource();`. In `OnDespawn` (beside
    the existing velocity-zero + `_contacts = null`): `_cts?.Cancel(); _cts?.Dispose(); _cts = null;` — cancel
    stops any in-flight feedback lerp; a cancelled token can't be reused, so take replaces it.
- **`Animal.prefab` restructure:** move the `MeshFilter` + `MeshRenderer` from the **root** onto a child
  **`Mesh`** GameObject (localPos/rot 0, scale 1); the **root keeps** `Rigidbody` + `SphereCollider`
  (radius 0.5) + `Animal` on the **Animal** layer (8). Assign `_renderer`/`_mesh` to the child. *(The child
  has no collider — physics is unchanged; the visual mesh now offsets in Y without touching the frozen-Y
  body.)*

### B. Jump arc — pure `JumpArc` + `MovementState.LeapStartTime` + `Simulation` write

- **`Animals/MovementState.cs` (extend):** add `public float LeapStartTime;` (clock time the current leap
  began; sentinel = grounded).
- **`Animals/SpawnSeed.cs` (extend):** `state.LeapStartTime = float.NegativeInfinity;` (so a just-spawned
  jumper shows no phantom hop before its first leap — `now < leapStart` ⇒ grounded).
- **`Animals/JumpMove.cs` (extend):** when the burst fires, `state.LeapStartTime = ctx.Clock.Now;` (the one
  added line, alongside the existing `Heading`/`NextLeapTime` writes).
- **`Animals/JumpArc.cs` (new, pure):**
  `public static float Height(float now, float leapStart, float duration, float height, AnimationCurve curve)`
  — `if (duration <= 0f || now < leapStart) return 0f;` `float t = (now - leapStart) / duration;`
  `if (t >= 1f) return 0f;` `return curve.Evaluate(t) * height;`. (`AnimationCurve.Evaluate` is
  headless-safe — EditMode-tested, like `JumpMath`.)
- **`Config/SimConfig.cs` (+ `SimConfig.asset`) (extend):** add under `[Header("Feedback")]` (beside the existing
  `_tastyLifetime`/`_tastyPoolSize`): `_jumpArcHeight = 0.5f`, `_jumpArcDuration = 0.45f`, `_spawnPopDuration = 0.2f`,
  `_deathPuffLifetime = 0.4f`, `AnimationCurve _jumpArcCurve` (**hump**), `AnimationCurve _riseEase`
  (`= AnimationCurve.EaseInOut(0,0,1,1)`), each with a getter. The two curves **must have C# field initializers** so a
  `ScriptableObject.CreateInstance<SimConfig>()` default (the hermetic tests) yields non-null curves.
- **`Core/FeedbackTuning.cs` (new struct):** `readonly struct FeedbackTuning` carrying `JumpArcHeight`
  (0.5 m, **new**), `JumpArcDuration` (0.45 s, **new**), `AnimationCurve JumpArc`, `SpawnPopDuration`
  (0.2 s, **new**), `AnimationCurve PopEase`. The production `JumpArc` is the **hump** `config.JumpArcCurve`;
  `PopEase` ← `config.RiseEase`. Built by `CatalogProjection.BuildFeedbackTuning(config)`.
- **`Core/Simulation.cs` (extend):** ctor takes an additional `in FeedbackTuning feedback`. In `FixedTick`,
  for an `IsImpulseDriven` mover, write the hop **null-guarded** (headless test Animals have no `Mesh` child —
  mirror the §A/§D guards, else every JumpMove `SimulationTests` case NREs): `Transform mesh = animal.Mesh;
  if (mesh != null) { mesh.localPosition = new Vector3(0f, JumpArc.Height(_clock.Now,
  animal.MovementState.LeapStartTime, _feedback.JumpArcDuration, _feedback.JumpArcHeight, _feedback.JumpArc),
  0f); }` (only jumpers; non-jumpers keep the mesh at zero). No change to the movement/collision logic.

### C. "Tasty!" label — `PredatorAte` channel + pooled billboard + UniTask rise/fade

- **`Core/PredatorAte.cs` (new):** `readonly struct PredatorAte { Vector3 Position }` (the predator's
  world position on an eat). **`Core/PredatorAteHandler.cs`:** `delegate void PredatorAteHandler(in PredatorAte e);`.
  **`Core/IPredatorAteSignal.cs`:** `event PredatorAteHandler Ate;`. **`Core/PredatorAteSignal.cs`:**
  `sealed class PredatorAteSignal : IPredatorAteSignal` with `Raise(in PredatorAte e)` → `Ate?.Invoke(in e);`
  — a 1:1 mirror of the `AnimalDied` channel (T05).
- **`Core/Simulation.cs` (extend):** ctor takes `PredatorAteSignal ateSignal`. In `ApplyDeath`, **gated on
  the existing `outcome.RaiseTasty`** (true for prey×predator **and** predator×predator), raise it at the
  **survivor** position: `Animal survivor = ReferenceEquals(victim, a) ? b : a;
  if (outcome.RaiseTasty) { _ateSignal.Raise(new PredatorAte(survivor.transform.position)); }` — read
  before the survivor moves; the victim's `AnimalDied` is unchanged. (GDD §6/§8 — label at the **predator**,
  exactly one per kill, prey **or** rival predator.)
- **`UI/TastyLabel.cs` (new, `MonoBehaviour`):** a `TextMesh` reading `"Tasty!"`; `public void ApplyProgress(
  float t01)` rises (`localPosition.y` lerp) and fades (`color.a = 1 - t01`) — driven by the lerp helper
  (decision 4: fixed billboard rotation set on take, **no per-frame `Camera.main`**). **No `Update`.**
- **`UI/TastyLabelSpawner.cs` (new, plain class):** holds a `Stack<TastyLabel>` pool (prewarm
  **`config.TastyPoolSize` = 16**, GDD §9) parented under the effects root; subscribes
  `IPredatorAteSignal.Ate` with a **non-capturing instance method** (guardrails §13); on raise, takes a
  label, positions it at `e.Position`, and runs `FeedbackLerp.RunAsync(config.TastyLifetime /*1.0 s, GDD
  §9*/, config.RiseEase, label.ApplyProgress, ct)` then returns it to the pool. **On take, reset the label**
  (`localPosition.y = 0`, `color.a = 1`) — the §11 label-pool reset; if the 16-slot pool is empty under a burst,
  **grow-by-one or skip** (never `Pop` an empty stack — mirror `AnimalFactory`). `IDisposable` — unsubscribes,
  cancels in-flight lerps, **and destroys every pooled+active instance** via `Application.isPlaying ? Destroy :
  DestroyImmediate` (mirroring `AnimalFactory.Dispose` — edit-mode-safe, no leak).

### D. Spawn scale-in (Simulation-driven) + death puff (`AnimalDied` subscriber)

- **Spawn scale-in:** `Animal` exposes `public CancellationToken Token` (a `CancellationTokenSource`
  **replaced fresh in `OnSpawn`**, **cancelled in `OnDespawn`** — guardrails §11) and
  `public void SetSpawnScale(float t01) { if (_mesh != null) _mesh.localScale = Vector3.one * t01; }`. The
  **`Simulation.Register`** kicks one lerp: `FeedbackLerp.RunAsync(_feedback.SpawnPopDuration /*0.2 s*/,
  _feedback.PopEase, animal.SetSpawnScale, animal.Token).Forget();` (scale 0→1 on the child; the root keeps
  the `size` scale).
- **Death puff** — the **second `AnimalDied` subscriber** (T05): **`UI/DeathPuffSpawner.cs` (new, plain
  class)**, a pooled translucent sphere (its own `DeathPuff.prefab` + `Stack<DeathPuff>`), subscribes
  `IAnimalDeathSignal.Died`; on raise spawns at **`e.Position`** (the victim's world position) and runs a
  scale-up+fade over **`config.DeathPuffLifetime` = 0.4 s** (**new**) via the lerp helper, then pools it. The
  fade sets **alpha via a `MaterialPropertyBlock`** on a shared transparent URP material (never `renderer.material`
  — §12 grep / SRP). Reset-on-take + `IDisposable` destroy-instances **exactly as §C** (per-event spikes, off the
  steady-state hot path — GDD §13). (Guardrails §9 — a **separate** pooled effect, since the dying animal is already pooled.)

### E. The UniTask feedback lerp helper — `Core/FeedbackLerp.cs`

- `public static async UniTask RunAsync(float duration, AnimationCurve ease, Action<float> apply,
  CancellationToken ct)`: accumulate real frame time (`UnityEngine.Time.deltaTime`) across
  `await UniTask.Yield(PlayerLoopTiming.Update, ct)` until `elapsed >= duration`, calling
  `apply(ease.Evaluate(Mathf.Clamp01(elapsed / duration)))` each frame, then a final `apply(1f)`. Wrap so a
  **cancel is clean** (`SuppressCancellationThrow()` / catch `OperationCanceledException`) — a despawn mid-lerp
  stops it (guardrails §9/§11). `apply` is passed as a **method group** (`label.ApplyProgress`,
  `animal.SetSpawnScale`) — no per-call closure (the spike path, guardrails §13).

### F. Rabbit — **data-only** extension proof (zero code)

- **`ScriptableObjects/Rabbit.asset` (new `AnimalDefinition`):** `_id rabbit`, `_displayName Rabbit`,
  `_role 0` (Prey), `_strength 0`, **`_movement` = the existing `JumpMove.asset`** (guid
  `4a602f22bc4c33d4bb74a51a250619d3` — the **same** asset the Frog references: reuse, not a new strategy),
  `_spawnWeight 0.25` (GDD §9), `_color` a distinct prey tone (**prov.** ≈ `(0.85, 0.78, 0.55)` sand),
  `_size 1`, `_mass 1`, `_speed 0`, `_jumpDistance 1.5`, `_jumpInterval 1.5` (GDD §9 — reuses the frog's jump).
- **`ScriptableObjects/AnimalCatalog.asset` (extend):** append `Rabbit.asset` as the **third** `_definitions`
  entry (after Frog, Snake). Weights then read **0.45 / 0.30 / 0.25** (GDD §9; normalized by the planner). **No
  code, no new strategy SO, no prefab** — the proof of GDD §4 / ADR 0002 §1. The honest extension cost: a **3rd
  pool** auto-prewarms `round(0.25 × 120) = 30` instances at zero code. **Drag the *existing* `JumpMove.asset`**
  (guid above) — authoring a fresh/duplicate strategy would break the reference-equality test below. Commit the `.meta`.

### G. Composition wiring (additive) — `Composition/GameLifetimeScope.cs`, `CatalogProjection`

- New serialized refs: the `TastyLabel` prefab, the `DeathPuff` prefab, and a single **effects-root**
  `Transform` (the static parent for both pools).
- `Configure` builds `FeedbackTuning feedback = CatalogProjection.BuildFeedbackTuning(_config);` and binds:
  - **`PredatorAteSignal`** dual-exposed in **one** registration (mirrors `AnimalDeathSignal`):
    `Register<PredatorAteSignal>(Singleton).AsSelf().As<IPredatorAteSignal>()` (the `Simulation` raises the
    concrete; the `TastyLabelSpawner` subscribes the interface — **one** shared instance).
  - The `Simulation` factory lambda gains `in feedback` and `c.Resolve<PredatorAteSignal>()` args.
  - **`RegisterEntryPoint<TastyLabelSpawner>(c => new TastyLabelSpawner(c.Resolve<IPredatorAteSignal>(),
    _tastyPrefab, _config, _effectsRoot), Singleton)`** and **`RegisterEntryPoint<DeathPuffSpawner>(c =>
    new DeathPuffSpawner(c.Resolve<IAnimalDeathSignal>(), _puffPrefab, _config, _effectsRoot), Singleton)`**
    (each subscribes in its ctor / `IStartable`; both `IDisposable` → auto-disposed on teardown). No static
    singletons, no `Resolve` from gameplay (guardrails §9).

### H. Scene + prefabs + Windows build — `Gameplay.unity`, `Animal.prefab`, new prefabs, `ProjectSettings`

- `Gameplay.unity`: add a static **`Effects`** root (empty) + wire the three new scope refs (`TastyLabel`
  prefab, `DeathPuff` prefab, `Effects` root). Camera/HUD/floor/scope from T08 unchanged.
- `Animal.prefab`: the §A child-mesh restructure. New `Prefabs/TastyLabel.prefab` (`TextMesh` + `TastyLabel`)
  and `Prefabs/DeathPuff.prefab` (a translucent primitive sphere + `DeathPuff`). Commit all `.meta`.
- **Windows build (GDD §14):** `Gameplay.unity` as **scene 0** in **Scenes In Build**; target
  **StandaloneWindows64**; build to `Builds/Windows/ZooWorld.exe` (decision 7 — git-ignored). Player settings:
  **Company Name** (a real value, not the `DefaultCompany` default — it drives the AppData path + shows in the
  submission), Product Name "Zoo World", windowed default. The build **runs** the sim (smoke-verified, Player log
  clean — the T08 no-`EventSystem` / Input-System state carries into the build).

### I. `README.md` + `ARCHITECTURE.md` (GDD §14 deliverables)

- **`ARCHITECTURE.md` (new, repo root):** the overview (one `Simulation` tick owner; pure rules over
  structs + seams; the end-of-step drain; pooling; the named patterns — Strategy/Factory+Pool/Observer/DI)
  **and the "how to add a new animal" walkthrough**, using **Rabbit as the worked example** (author one
  `AnimalDefinition`, add it to the catalog — done). Link from `README.md` + `docs/INDEX.md`.
- **`README.md` (update):** replace the stale *"implementation is starting from task T01"* (§Status) with
  the shipped state; link `ARCHITECTURE.md`; note the Windows build under **Run**.

## Pure-rule EditMode test assertions (ship in the same change)

**`JumpArcTests` (new)** — the pure hop curve (a known `AnimationCurve`, e.g. `AnimationCurve.Linear(0,0,1,1)`):
- **Grounded before the leap:** `Height(now: 0, leapStart: float.NegativeInfinity, …) == 0` and `now < leapStart` → 0.
- **At lift-off (`now == leapStart`):** `== curve.Evaluate(0) * height` (0 for the linear/ease curve).
- **Mid-leap (`now == leapStart + duration/2`, linear curve):** `≈ 0.5 * height` (tol 1e-4).
- **Landed (`now >= leapStart + duration`):** `== 0`.
- **Guard:** `duration <= 0` → 0.
- **Height scales linearly:** doubling `height` doubles the result at a fixed `t`.

**`CatalogProjectionTests` (extend):**
- **Colour round-trips:** for each `i`, `specs[i].Color == def[i].Color` (hermetic + real-asset cases) — extend
  the hermetic `Definition()` helper with a `Color` param (`SetField "_color"`) + author **distinct** colours, so
  the hermetic case asserts a non-trivial value, not `white == white`.
- **Rabbit data-only (real-asset):** `AnimalCatalog.asset` now has `Definitions.Count == 3`;
  `Definitions[rabbit].Movement` `Is.SameAs` `Definitions[frog].Movement` (both AssetDatabase-loaded — the
  shared `JumpMove.asset` SO instance); the three `SpawnWeight`s are `0.45 / 0.30 / 0.25`; the projection
  preserves catalog order (Frog, Snake, Rabbit).
- `BuildFeedbackTuning`: `JumpArcHeight 0.5`, `JumpArcDuration 0.45`, `SpawnPopDuration 0.2` from a default
  `SimConfig` (and the curve refs are non-null).

**`SimulationTests` (extend)** — the Tasty channel (headless, with a recording `PredatorAteSignal` subscriber):
- **Prey×Predator eat:** register a predator + a prey, enqueue the contact, `FixedTick` → drain → the
  `PredatorAte` signal fires **exactly once** at the **predator's** `transform.position`; `AnimalDied` fires
  once with `Role.Prey`.
- **Predator×Predator duel:** two predators (strengths 5 vs 1) → one `PredatorAte` at the **winner's**
  position; one `AnimalDied` with `Role.Predator`.
- **Prey×Prey bounce:** **no** `PredatorAte` raised (`RaiseTasty` false).

*(The lerp helper, `TastyLabel`/`TastyLabelSpawner`, `DeathPuff`/`DeathPuffSpawner`, the `Animal` MPB
colour, the prefab restructure, the `Simulation` mesh-Y write, and the build are adapters/visuals →
**smoke-verified**, not unit-tested — guardrails §10/§14.)*

## Implementation notes

**New files** — `Core/{PredatorAte,PredatorAteHandler,IPredatorAteSignal,PredatorAteSignal,FeedbackTuning,
FeedbackLerp}.cs`, `Animals/JumpArc.cs`, `UI/{TastyLabel,TastyLabelSpawner,DeathPuff,DeathPuffSpawner}.cs`;
tests `JumpArcTests.cs` (+ extend `CatalogProjectionTests.cs`, `SimulationTests.cs`); assets
`ScriptableObjects/Rabbit.asset`, `Prefabs/{TastyLabel,DeathPuff}.prefab`; docs `ARCHITECTURE.md`.
**Existing-to-extend** — `Core/{AnimalSpec,Simulation}.cs`, `Animals/{Animal,MovementState,SpawnSeed,
JumpMove}.cs`, `Config/SimConfig.cs` (+ `SimConfig.asset`), `Composition/{CatalogProjection,GameLifetimeScope}.cs`,
`Prefabs/Animal.prefab`, `ScriptableObjects/AnimalCatalog.asset`, `Gameplay.unity`, `ProjectSettings`,
`README.md`, `docs/INDEX.md`. Commit `.meta` with every new asset.

**Guardrail refs** — `MaterialPropertyBlock` colour, never `.material` (§12/§17); jump arc = child-mesh Y from
an `AnimationCurve` **in the tick** (§8); feedback = a small UniTask lerp helper tied to the object's CTS,
cancelled on despawn (§9/§11); typed `readonly struct` events, non-capturing instance-method handlers (§13);
`Animal` stays a dumb adapter, only `Simulation` ticks (§2/§16/§17); one composition root, no static
singletons / no `Resolve` from gameplay (§9).

**Pitfalls** —
- **URP colour property is `_BaseColor`** (not the built-in `_Color`); set it on a **reused**
  `MaterialPropertyBlock`. A `renderer.material`/`.color` write instantiates a material + breaks SRP
  batching + trips the §12 grep.
- **Headless `AnimalTests`/`SimulationTests`** build an `Animal` with no `Mesh`/`MeshRenderer` child →
  **null-guard** the mesh/colour writes in `OnSpawn`, `SetSpawnScale`, **and the §B `Simulation.FixedTick`
  arc write** (mirrors the lazy-`Body` test-safety in `Animal`). The per-tick write is the easy one to forget —
  every JumpMove `SimulationTests` case (`FixedTick_*`, `Drain_PreyVsPrey`) ticks it and would NRE if unguarded.
- **`AnimalSpec` ctor gains `Color`** → update **every** construction (`CatalogProjection` + any test that
  builds a spec) or it won't compile.
- **Despawn must cancel the CTS** (`OnDespawn`) and **take must replace it** (`OnSpawn`) — a reused token is
  already cancelled; a leaked lerp would animate the **reused** pooled object (guardrails §11/§17).
- **Tasty position = the survivor, not the victim** — `AnimalDied.Position` is the victim; the label goes at
  the predator. Read `survivor.transform.position` in `ApplyDeath` **before** despawn moves anything.
- **`RaiseTasty` already gates both eat cases** (prey×predator + predator×predator) — do **not** re-derive it
  from roles; read `outcome.RaiseTasty`. Prey×Prey is a `Bounce` (no Tasty), so the channel is never raised there.
- **No `Camera.main` for the billboard** — the camera is locked straight-down (T08), so set the label's
  rotation **once on take**; never read the camera per frame (§12).
- **`Simulation` ctor grows two args** (`in FeedbackTuning`, `PredatorAteSignal`) → update the
  `GameLifetimeScope` factory lambda **and** `SimulationTests` construction.
- **Build:** `Gameplay.unity` must be in **Scenes In Build** or the player loads an empty scene; verify the
  **Player log** (not just the Editor Console) is clean.

## Out of scope

No new gameplay **rules** (T09 is feedback + data + packaging); no multi-tier diets; no perception/seeking AI;
no audio; no menus/restart; no TMP (legacy `TextMesh`/`Text` only); no spatial partitioning; no second
authored scene; no >3 species (Rabbit is the data-only proof, not a content push). The build is
**StandaloneWindows64** only (no Mac/Linux/WebGL). DOTween stays dropped (UniTask lerp helper).

## Verification

- **EditMode:** the new/extended tests above pass; **full suite green** (no regression on the **122** prior
  tests — T08 left the suite at 122/122 green, per `current-status.md`; the new T09 cases lift the total to
  roughly **135**), **0 Console errors**. Run via MCP `run_tests` (active scene saved) or Test Runner (build-and-test.md).
- **Smoke (Play / MCP) on `Gameplay.unity`:** prey are one colour, predators another (MPB, SRP-batched);
  on each eat a **"Tasty!"** label rises+fades at the **predator**; a **death puff** fires at the victim; a
  spawned animal **scales in**; frogs **and rabbits** visibly **hop** (child-mesh Y arc, body stays on the
  plane); the **Rabbit** spawns and behaves (data-only); counters still tick; Console clean.
  `dotnet format --verify-no-changes` + the forbidden-API grep (guardrails §12) clean on the new/edited files.
- **Pool-reuse (guardrails §14/§11/§17 — the most-likely-silent T09 regression):** an animal despawned **mid
  spawn-pop** and immediately re-spawned shows **no stale partial scale / no leftover lerp** — the child
  `_mesh.localScale` settles at `Vector3.one` (full pop) and the root keeps its `size` scale; confirms the
  OnDespawn-CTS-cancel + OnSpawn-fresh-CTS contract.
- **Build:** the **StandaloneWindows64** player launches and runs the sim; **Player log clean**.
- **Done** = full suite green + Console clean + the smoke flow + the build run observed **this session** with
  cited evidence (build-and-test.md; agent-verification.md §2). On close (same pass, guardrails §14 /
  agent-verification §5): fill **What was actually done**, flip the matrix **T09 Status `▫`→`✅`** (the Brief flag is already `✓`), add
  the §9 feedback rows to `game-design.md`, ship `README`/`ARCHITECTURE`, and update `current-status.md`.

## What was actually done

— *(filled on close: what shipped, deviations, the commit, the date.)*
