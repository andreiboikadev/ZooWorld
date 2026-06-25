# ADR 0001: Technology baseline & project structure

Status: Accepted
Date: 2026-06-25
Decision owner: Andrei Boika

## Context

Zoo World is a Senior Unity test task graded on **animal architecture** (extensible toward
~1000 species), SOLID, DI, and named patterns; **ECS is forbidden**, UI must be **uGUI**, and the
deliverable is a public git repo + a standalone Windows build. We lock the engine/stack, the DI
approach, the runtime/physics model, and the code/assembly structure **before** deriving the
engineering guardrails and the task plan, so later work doesn't re-decide paths or churn.
Design source of truth: [`docs/product/game-design.md`](../../product/game-design.md).

## Decision

**Engine & rendering.** Unity **6.3 LTS (6000.3.16f1)** + **URP 17.3.0** (project already
created). Deliverable runs in the Editor **and** as a standalone **Windows** player.

**Runtime / physics.** **3D physics** (brief: *"3d"* + *"spheres"* + *"fly apart by physics"*),
top-down camera over the **XZ** plane. Animals are **dynamic Rigidbodies constrained to the
plane** (freeze pos-Y + rot-X/Z), **gravity off**, on a dedicated **Animal** physics layer with an
**Animal×Animal** collision matrix; field bounds = the camera footprint (steering, not colliders).
Details: game-design.md §3/§7.

**Dependency injection.** **VContainer 1.18.0**, installed via the **OpenUPM scoped registry**
(`package.openupm.com`, scope `jp.hadashikick.vcontainer`) so `packages-lock.json` pins it. A
manual **composition root** (`LifetimeScope`) wires everything; **no service locator, no STATIC
singletons** (container scope-singletons are expected — see [ADR 0002](0002-animal-architecture.md)). Justified by the brief explicitly inviting a DI framework. (VContainer was chosen
over the maintained Extenject fork for being modern, GC-free, source-gen, and actively released —
weighed in the session notes.)

**Architecture** (summary; full contract in `implementation-guardrails.md`, next). Composition
over inheritance: `Animal` = `AnimalDefinition` (ScriptableObject) + a `MovementBehaviour`
ScriptableObject (Strategy) + binary **Prey/Predator** role + `strength`. (Animal-architecture
specifics — SO strategies, the single `Simulation` tick owner, the interface seams, the end-of-step
collision drain — are refined in [ADR 0002](0002-animal-architecture.md).) One
`FoodChainResolver` owns predation (pair-authoritative, `dead`-guarded). `AnimalFactory` is the
**pool's** spawn+configure API. Typed **Observer** events drive the uGUI counters and the "Tasty!"
labels. **Object pooling** for animals + labels. **No ECS/DOTS.** Patterns shown: Strategy,
Factory+Object-Pool, Observer, DI.

**Code & assemblies.**
- Root namespace **`ZooWorld`** — sub-namespaces `.Animals`, `.Spawning`, `.Predation`, `.Core`,
  `.UI`, `.Config`, `.Composition`.
- Folder layout: `Assets/_Project/{Scripts/{Runtime,Editor}, ScriptableObjects, Prefabs, Scenes,
  Materials}`; tests in `Assets/_Project/Tests/EditMode`.
- Assembly definitions: **`ZooWorld.Runtime`**, **`ZooWorld.Editor`**, **`ZooWorld.Tests.EditMode`**
  (references `ZooWorld.Runtime` + the Test Framework).
- C# style **enforced by the repo-root `.editorconfig`** (IDE1006), narrative in
  `docs/architecture/csharp-style.md`. C# 9 ceiling (Unity 6.3): block-scoped namespaces, no
  global usings, no `record`/`init` in serialized types.

## Consequences

- **Easier:** a new species is data (one `AnimalDefinition` + catalog entry); pure rules are
  plain C#, unit-testable in EditMode without the engine; DI keeps wiring in one obvious place.
- **Constrained:** no ECS (by brief); predation is **binary** (multi-tier is a documented seam via
  `strength`, deliberately not built); live population is **capped (~120)** for stability.
- **Risks:** VContainer needs the OpenUPM registry resolved (network on first import);
  3D-physics-on-a-plane depends on correct Rigidbody constraints + a strict pool-reset contract
  (specified in the guardrails).
- Once accepted, **supersede** this ADR with a new one rather than editing it.
