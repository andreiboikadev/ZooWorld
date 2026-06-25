---
paths:
  - "Assets/**/*.cs"
---
# Unity C# Rules — Zoo World

- Follow [`docs/architecture/implementation-guardrails.md`](../../docs/architecture/implementation-guardrails.md).
  Naming/formatting per [`docs/architecture/csharp-style.md`](../../docs/architecture/csharp-style.md),
  machine-enforced by the repo-root `.editorconfig` (private fields `_camelCase`, **not** bare `camelCase`;
  Allman braces; `using` outside the namespace; block-scoped namespaces — Unity 6.3 = C# 9).
- **One `Simulation` service owns the FixedUpdate tick; `Animal` is a DUMB adapter** (no magic
  `Update`/`FixedUpdate`, no service dependencies — only enqueues its collisions).
- **Gameplay rules are pure C# over plain structs + interface seams** (`IClock`/`IRandom`/
  `ISpawnSequence`/`IOccupancyQuery`/`FieldBounds`/`AnimalState`/…). **No `Time`/`Random`/`Physics`/
  `Camera.main` inside a rule.** Add/Update EditMode tests in the same change for any pure rule.
- **VContainer composition root**; scope-singletons OK, **no static singletons, no service locator,
  no `Resolve` from gameplay**. SO assets bound via `RegisterInstance`.
- **Object pooling** for animals + labels; on despawn cancel the UniTask `CancellationTokenSource`
  (stops in-flight feedback) and reset all state. Feedback = a small UniTask lerp helper (**no DOTween**).
- Predation resolved in the **end-of-step drain** (not inside `OnCollisionEnter`). Typed `readonly
  struct` events, no global string `EventBus`. Animal color via `MaterialPropertyBlock`. **No ECS.**
