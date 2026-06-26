# T05 — DeathCounters + `AnimalDied` Observer + HUD presenter (+ `IHudView` seam)

| | |
|---|---|
| Milestone | M1 — pure core |
| Depends on | T01 |
| Touches scene/prefabs | **no** — fully headless. The concrete uGUI `HudView`, the `UnityEngine.UI` asmdef ref, the Canvas/labels, DI wiring, and the live smoke are **T08** (scope decision 1) |
| Status | ✅ done |

## Goal

Close M1 with the death-accounting subsystem, headless and fully tested: the pure **`DeathCounters`**
rule (one increment per resolved death, keyed by the victim's `Role`), the typed **`AnimalDied`
Observer** channel it subscribes to (the struct payload shipped in T01; T05 adds the publish/subscribe
seam so T07's `Simulation` can raise it and T05 can count it — the graded GDD §4 Observer, no global
`EventBus`), and the **`HudPresenter`** that formats the two top-right counters against an **`IHudView`**
seam. The concrete uGUI view + its scene live in T08, where the counters first visibly tick. This
unblocks the T08 wiring (the slice's "count" step) and the T09 death-puff (a second `AnimalDied`
subscriber). (GDD §6/§8/§4; guardrails §4/§9/§13/§14; ADR 0002 §4/§7.)

## Scope decisions (resolved — chosen as the best fit for the test-task milestones; flag at review to override)

The matrix lists T05 under **M1 — pure core** yet names a uGUI *view*; M1 is "no scene"
(build-strategy). These resolve that tension and pin the Observer/notification shape, right-sized per
guardrails §18.

1. **The concrete `HudView` (and its uGUI dependency) defers to T08; T05 ships the headless HUD brain +
   the `IHudView` seam.** T05 delivers `DeathCounters`, the `AnimalDied` channel, `HudPresenter`, and the
   `IHudView` interface — all EditMode-tested with a `FakeHudView`. The **`HudView` MonoBehaviour**, the
   **`UnityEngine.UI` asmdef reference**, the **Canvas + two labels**, the **DI registration**, and the
   **Play smoke** are **T08**. *Why (not the matrix's literal "view in T05"):* M1 is headless-by-contract
   (build-strategy: *"pure rules first … no scene"*); more decisively, a `HudView` written in T05 is
   **unverifiable there** — nothing raises `AnimalDied` until T07 and there is no scene/DI until T08, so
   it would be dead, untested code in a "done" task and would pull uGUI into otherwise-pure M1. Behind
   `IHudView` the presenter is complete and fully tested now; T08 adds the ~3-line view exactly where the
   slice lights it up. (Carry-forward — on close, update the README matrix: move "HUD view (uGUI) +
   Canvas + `UnityEngine.UI` ref" onto the **T08** row; trim the **T05** row to "DeathCounters +
   `AnimalDied` Observer + HUD presenter + `IHudView`"; and **add `T05` to the T07 "Depends on" cell** —
   T07's `Simulation` raises via the `AnimalDeathSignal` channel defined here (a hard compile dependency),
   yet the matrix currently lists only T02/T03/T06.)
2. **The `AnimalDied` Observer seam — concrete-raise / interface-subscribe.** T01 shipped the **payload**
   (`readonly struct AnimalDied { Role; Vector3 }`); T05 adds the **channel**:
   - `delegate void AnimalDiedHandler(in AnimalDied e)` — strongly typed, struct **by `in`** (no box, no
     per-raise alloc).
   - `interface IAnimalDeathSignal { event AnimalDiedHandler Died; }` — the **subscribe** side
     (`DeathCounters` depends on this).
   - `sealed class AnimalDeathSignal : IAnimalDeathSignal` with `void Raise(in AnimalDied e)` — the
     **raise** side. T07's `Simulation` injects the **concrete** for `Raise`; subscribers inject
     `IAnimalDeathSignal`. *Rejected:* a separate `IAnimalDeathRaiser` interface (DIP-purist; an extra
     type for a one-line `Raise` — §18). All three in `ZooWorld.Core`, next to `AnimalDied` (the
     `Simulation` raiser is also `.Core` per guardrails §3; `.UI` subscribes — no cross-subsystem cycle).
3. **Counters → presenter notification is a parameterless `event System.Action Changed`; the presenter
   reads the model.** Textbook MVP (the presenter observes its model and projects it to the view) and the
   leanest clean option. *Rejected:* a `DeathTally` struct payload + custom `DeathTallyHandler` — fine and
   §13-consistent, but two new types for an internal UI ping when the presenter can just read
   `DeadPrey`/`DeadPredators` (§18 right-sizing; §13's *struct-payload* rule targets cross-system **domain**
   events like `AnimalDied`, not an intra-UI change notification). Subscription is still a **non-capturing
   instance method**, unsubscribed on dispose (§13).
4. **uGUI widget (legacy `Text` vs `TMP_Text`) is a T08 decision** — a pure view detail behind `IHudView`,
   so it does not touch T05's presenter or tests. *Recommendation for T08:* legacy `UnityEngine.UI.Text`
   (literally "uGUI", zero setup — no TMP Essentials import — enough for two primitive counters).
5. **Namespaces & lifetime.** Channel (decision 2) → `ZooWorld.Core`; `DeathCounters`, `IHudView`,
   `HudPresenter` → `ZooWorld.UI` (new folder/namespace, auto-covered by the `ZooWorld.Runtime` asmdef
   root). `DeathCounters` is a **pure** rule and lives in `.UI` because that is its **subsystem**:
   guardrails §4 groups `DeathCounters` + `HudPresenter` + `HudView` as one HUD cluster, and the project
   puts each pure rule in its own subsystem namespace (`FoodChainResolver`→`.Predation`,
   `SpawnPlanner`→`.Spawning`) — `.Core` is reserved (§3) for shared structs/seams + `Simulation`. Nothing
   in `.UI` touches Unity statics, so the rule stays headless. **(PLAN-02 resolved — keep `.UI`; rejected
   `.Core` and a separate `.Counting` ns, which would split the cohesive HUD cluster.)** **Counts never reset** — single endless session, no
   restart (GDD §10/§12); counters are **not** pooled. `DeathCounters` and `HudPresenter` are
   **`IDisposable`** and **unsubscribe on `Dispose`** (§13); both are VContainer scope-singletons (DI
   binding is T08).
6. **No GDD §9 tunables drive this task.** `DeathCounters`/the presenter read **no** `SimConfig` value —
   counts start at **0** and increment by **1** per death; nothing to tune. The only fixed strings are the
   **exact GDD §8 labels** — `"Dead prey: {n}"` and `"Dead predators: {n}"` — pinned by the presenter
   tests. (Stated so the §9 cross-check isn't mistaken for an omission.)

## Acceptance criteria

### The Observer channel (`ZooWorld.Core`)

- **`AnimalDiedHandler`** — `Core/AnimalDiedHandler.cs`: `public delegate void AnimalDiedHandler(in AnimalDied e);`
- **`IAnimalDeathSignal`** — `Core/IAnimalDeathSignal.cs`: `public interface IAnimalDeathSignal` with
  `event AnimalDiedHandler Died;` (XML doc: the subscribe side; raised once per resolved death by the
  `Simulation`, T07).
- **`AnimalDeathSignal`** — `Core/AnimalDeathSignal.cs`: `public sealed class AnimalDeathSignal :
  IAnimalDeathSignal`:
  - `public event AnimalDiedHandler? Died;`
  - `public void Raise(in AnimalDied e)` → `Died?.Invoke(in e);` — the **only** member; no other state.

### The pure rule (`ZooWorld.UI`)

**`DeathCounters`** — `Assets/_Project/Scripts/Runtime/UI/DeathCounters.cs`:
- `public sealed class DeathCounters : IDisposable` — the HUD model; **no Unity statics** (only the
  `AnimalDied`/`Role` it references; no `Time`/`Random`/`Physics`/`Camera`).
- `public DeathCounters(IAnimalDeathSignal signal)` — stores it and subscribes `signal.Died +=
  OnAnimalDied;` (instance method, **non-capturing** — §13).
- `public int DeadPrey { get; private set; }`, `public int DeadPredators { get; private set; }` — both
  start **0**.
- `public event Action? Changed;` — raised after each increment.
- `private void OnAnimalDied(in AnimalDied e)` — `switch (e.Role)`: `Prey` → `DeadPrey++`; `Predator` →
  `DeadPredators++`; then `Changed?.Invoke();`. **Exactly one** counter moves per call; `e.Position` is
  **ignored** (role-only). Alloc-free (struct payload by `in`, no boxing, no LINQ).
- `public void Dispose()` → `_signal.Died -= OnAnimalDied;`.

### The presenter (`ZooWorld.UI`, headless-testable)

**`IHudView`** — `UI/IHudView.cs`: `public interface IHudView` with `void SetDeadPrey(string text);` and
`void SetDeadPredators(string text);`.

**`HudPresenter`** — `Assets/_Project/Scripts/Runtime/UI/HudPresenter.cs`:
- `public sealed class HudPresenter : IStartable, IDisposable` — formats counts → `IHudView`; **no Unity
  statics**, **not** a MonoBehaviour (testable headless). (`VContainer.Unity.IStartable` is a bare `void
  Start()` interface — zero Unity-static dependency, so the rule stays headless; `VContainer` is already a
  `ZooWorld.Runtime` asmdef reference, so no asmdef change.)
- `public HudPresenter(DeathCounters counters, IHudView view)` — stores both; subscribes
  `counters.Changed += OnCountersChanged;`.
- `public void Start()` — the **`IStartable` entry point**; pushes the **initial** labels (so the HUD
  shows `0`/`0` before any death) by calling the same `OnCountersChanged()`. **`HudPresenter` MUST
  implement `IStartable` for VContainer to call this** — VContainer's `EntryPointDispatcher` invokes only
  registered `IStartable`s, never a plain method of the same signature, so registering a non-`IStartable`
  presenter "as `IStartable`" (T08) would throw / silently no-op and leave the HUD blank until the first
  death. The EditMode test calls `Start()` directly (no VContainer needed).
- `private void OnCountersChanged()` →
  `_view.SetDeadPrey($"Dead prey: {_counters.DeadPrey}");`
  `_view.SetDeadPredators($"Dead predators: {_counters.DeadPredators}");` — **exact** GDD §8 strings.
- `public void Dispose()` → `_counters.Changed -= OnCountersChanged;`.

### Asmdef

- **No change in T05.** (The `UnityEngine.UI` reference rides with the concrete `HudView` in **T08** —
  decision 1. Nothing in T05 uses `UnityEngine.UI`.)

### EditMode tests (`ZooWorld.Tests.EditMode`) — exact assertions

**`FakeHudView`** — `Tests/EditMode/Fakes/FakeHudView.cs`: `sealed class FakeHudView : IHudView` recording
the **last** string per setter: `public string? DeadPreyText`, `public string? DeadPredatorsText`.

**`DeathCountersTests`** — `Tests/EditMode/DeathCountersTests.cs`. Helpers: `Signal()` = `new
AnimalDeathSignal()`; `Died(Role role)` = `new AnimalDied(role, Vector3.zero)`; counters built as `new
DeathCounters(signal)`, driven via `signal.Raise(...)` (exercises the real Observer path).
- **`StartsAtZero`:** fresh → `DeadPrey == 0`, `DeadPredators == 0`.
- **`PreyDeath_IncrementsPreyOnly`:** `signal.Raise(Died(Role.Prey))` → `DeadPrey == 1`,
  `DeadPredators == 0`.
- **`PredatorDeath_IncrementsPredatorOnly`:** `signal.Raise(Died(Role.Predator))` → `DeadPredators == 1`,
  `DeadPrey == 0`.
- **`MixedDeaths_AccumulatePerRole`:** 3×Prey + 2×Predator interleaved → `DeadPrey == 3`,
  `DeadPredators == 2`.
- **`Changed_RaisedOncePerDeath`:** subscribe a counting handler; after 3 deaths → invoked **3** times.
- **`Position_DoesNotAffectCount`:** `new AnimalDied(Role.Prey, new Vector3(5,0,5))` then `(…,
  Vector3.zero)` → `DeadPrey == 2` (role-only; the position payload is ignored).
- **`OnAnimalDied_DoesNotAllocate`:** counters subscribed, **no `Changed` subscriber**; warm once
  (`signal.Raise(Died(Role.Prey))`), then `Assert.That(() => signal.Raise(Died(Role.Prey)),
  UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());` (fully-qualified to avoid NUnit's
  `Is`; the warm-up avoids first-call JIT noise). Satisfies §14's *"DeathCounters … zero-alloc"*.
- **`Dispose_Unsubscribes_NoFurtherCounting`:** after `counters.Dispose()`, `signal.Raise(Died(Role.Prey))`
  leaves `DeadPrey == 0`.

**`HudPresenterTests`** — `Tests/EditMode/HudPresenterTests.cs`. Wire `signal → counters → presenter →
FakeHudView`; call `presenter.Start()` where the initial push is needed.
- **`Start_PushesInitialZeroLabels`:** after `Start()` → `view.DeadPreyText == "Dead prey: 0"`,
  `view.DeadPredatorsText == "Dead predators: 0"` (exact GDD §8 strings).
- **`PreyDeath_UpdatesPreyLabel_ExactFormat`:** `Start()` then `signal.Raise(Died(Role.Prey))` →
  `view.DeadPreyText == "Dead prey: 1"`; `view.DeadPredatorsText == "Dead predators: 0"` (unchanged).
- **`PredatorDeath_UpdatesPredatorLabel`:** after a predator death → `view.DeadPredatorsText == "Dead
  predators: 1"`.
- **`BothLabels_AccumulateIndependently`:** 2×Prey + 3×Predator → `"Dead prey: 2"` / `"Dead predators:
  3"`.
- **`Dispose_StopsUpdatingView`:** after `presenter.Dispose()`, a further `signal.Raise(Died(Role.Prey))`
  does **not** change `view.DeadPreyText` (still the pre-dispose value).

(Matches guardrails §14: *"DeathCounters (one increment per death, correct role, zero-alloc)"*; GDD §6
*"exactly one counter increments per resolved death, keyed by the victim's role … Prey×Prey never
counts"* — a bounce raises **no** `AnimalDied`, so it never reaches the counters, T03/T07.)

## Implementation notes

- **New files:** `Scripts/Runtime/Core/{AnimalDiedHandler,IAnimalDeathSignal,AnimalDeathSignal}.cs`;
  `Scripts/Runtime/UI/{IHudView,DeathCounters,HudPresenter}.cs`; `Tests/EditMode/Fakes/FakeHudView.cs`,
  `Tests/EditMode/{DeathCountersTests,HudPresenterTests}.cs`. **No asmdef change, no new asmdef** (`UI/`
  sits under the `ZooWorld.Runtime` root).
- **`using` outside the namespace** (`System.*` first, then alphabetical), per file as needed: `System`
  (`Action`, `IDisposable`), `UnityEngine` (`Vector3`), `ZooWorld.Config` (`Role`), `ZooWorld.Core`
  (`AnimalDied`, `AnimalDiedHandler`, `IAnimalDeathSignal`), `VContainer.Unity` (`IStartable` —
  `HudPresenter`). Tests add `NUnit.Framework`,
  `UnityEngine.TestTools.Constraints` (the alloc constraint), `ZooWorld.UI`.
- `#nullable enable` per file; `sealed`; **Allman**; block-scoped namespace; XML docs on public types/
  members; C# 9 (no file-scoped ns / `record` / global usings — csharp-style Part 2).
- **Pitfalls:** (a) subscribe with the **instance method** (`OnAnimalDied`/`OnCountersChanged`), never a
  capturing lambda (closure alloc + can't unsubscribe — §13); (b) **unsubscribe in `Dispose`** on both
  classes or a torn-down scope leaks the handler; (c) `OnAnimalDied` moves **exactly one** counter — a
  `switch` on `Role`, not two `if`s; (d) the alloc test must fully-qualify
  `UnityEngine.TestTools.Constraints.Is` (NUnit's `Is` is imported), **warm the path once**, and run with
  **no `Changed` subscriber** (a string-formatting presenter on `Changed` would allocate and fail it —
  it measures `DeathCounters` in isolation); (e) the §8 strings are exact — `"Dead prey: "` / `"Dead
  predators: "` (lower-case noun, one space after the colon); (f) no Unity statics in
  `DeathCounters`/`HudPresenter` (pure — guardrails §10/§12).
- Pure rules ⇒ their tests ship **in the same change** (DoD, guardrails §14).

## Out of scope

- **Raising `AnimalDied`** — the `Simulation` drain calls `AnimalDeathSignal.Raise(in AnimalDied)` once
  per resolved death, sourcing the victim's `Position` from the live body (guardrails §6.2). **T07.**
- **The concrete uGUI view + scene + wiring** (decision 1): the **`HudView : MonoBehaviour, IHudView`**
  adapter (`[SerializeField] private Text …`), the **`UnityEngine.UI` asmdef reference**, the top-right
  **Canvas + two labels** (GDD §8), the DI bindings (`AnimalDeathSignal` as `IAnimalDeathSignal` + the
  concrete raiser, `DeathCounters`, `HudPresenter` as `IStartable`/`IDisposable`, the scene `HudView`),
  and the **Play smoke** (counters tick on screen as predation runs). **T08** — and the widget choice
  (decision 4) is made there.
- **The second `AnimalDied` subscriber** — the **death-puff** at `AnimalDied.Position`. **T09.**
- **"Tasty!" label** — spawned **directly** by the `Simulation` on an eat (`Outcome.RaiseTasty`), **not**
  via `AnimalDied` (GDD §6 — counters and the label are separate channels). **T07/T09.**
- TMP, counter reset/restart, settings-driven HUD, MVVM/data-binding — none (decisions 3/4/5; GDD
  §10/§12; guardrails §18).

## Verification

- **EditMode:** `DeathCountersTests` + `HudPresenterTests` green (zero-start, per-role increment,
  accumulation, `Changed` cadence, zero-alloc, dispose-unsubscribe, exact §8 label strings); **full suite
  green** (64 prior + T05's new); **0 Console errors**. (`Window → Test Runner → EditMode → Run All`, or
  MCP `run_tests` with `Gameplay.unity` active.)
- `dotnet format --verify-no-changes` green on the new files; forbidden-API / Unity-statics grep clean in
  `.UI` + the `.Core` signal (no `Time`/`Random`/`Physics`/`Camera.main`; `Vector3` only).
- **No Play smoke in T05** — fully headless; the live counter tick + on-screen Canvas are smoke-verified
  in **T08** (vertical slice), once T07 raises `AnimalDied` and the HUD is scene-wired (decision 1).

## What was actually done

**Implemented 2026-06-26** — the headless death-accounting subsystem, per the validated brief.

- **Observer channel** (`ZooWorld.Core`): `AnimalDiedHandler` (delegate over `in AnimalDied`),
  `IAnimalDeathSignal` (subscribe), `AnimalDeathSignal : IAnimalDeathSignal` (`Raise(in AnimalDied)` →
  `Died?.Invoke(in e)`).
- **Pure HUD model + presenter** (`ZooWorld.UI`): `DeathCounters : IDisposable` (subscribes in ctor,
  `switch (e.Role)` increments exactly one of `DeadPrey`/`DeadPredators`, raises `event Action Changed`,
  unsubscribes in `Dispose`); the `IHudView` seam; `HudPresenter : IStartable, IDisposable` (formats the
  exact §8 strings → `IHudView`, initial push in `Start()`, unsubscribes in `Dispose`).
- **Tests** (`ZooWorld.Tests.EditMode`): `FakeHudView`; `DeathCountersTests` (7: zero-start, per-role
  increment, accumulation, `Changed` cadence, position-ignored, dispose-unsubscribe); `HudPresenterTests`
  (5: initial 0/0, exact §8 formats, independent accumulation, dispose-stops); `DeathCountersAllocationTests`
  (1: zero-alloc). **13 new tests.**

**Deviation from the brief (1):** the zero-alloc test ships in its own file `DeathCountersAllocationTests.cs`,
not inside `DeathCountersTests`. The brief's pitfall (d) said to *fully-qualify*
`UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory()`, but that does not compile —
`AllocatingGCMemory` is an **extension method** on `ConstraintExpression`, so it needs the namespace `using`
(which also imports `Is`, shadowing NUnit's `Is` used by the other counter tests). Isolating it with
`using Is = UnityEngine.TestTools.Constraints.Is;` is the standard Unity pattern and keeps the assertion 1:1
with the brief's intent (§14 zero-alloc). No design change.

**Verification (this session, via MCP):** **EditMode 77/77 green** (64 prior + T05's 13; 2.47 s); **0 Console
errors** (only an unrelated MCP-bridge WebSocket warning); `dotnet format --verify-no-changes` exit 0 on the
Runtime + Tests new files; forbidden-API / Unity-statics grep clean in `.UI` + the `.Core` signal. **No Play
smoke** (headless by design — the live HUD tick is smoke-verified in T08).

**Commit proposed:** `feat: T05 death counters + AnimalDied Observer + HUD presenter` — _pending human commit_.
