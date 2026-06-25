# C# Style Guide — Zoo World

> Teaches the conventions; the repo-root **`.editorconfig`** enforces the machine-checkable ones
> (naming, formatting) in Visual Studio, VS Code (C# Dev Kit), Rider, and `dotnet format`/CI. Where
> this doc and the `.editorconfig` differ, **the `.editorconfig` wins** — fix whichever is wrong.
> **Generate code that already conforms** — don't emit a draft the `.editorconfig` then flags.

## Part 0 — Why

Code that reads consistently for humans and LLMs, diffs cleanly across machines, is cheap to
maintain. Consistency beats personal preference. The *narrative + rationale* live here; the
*machine-checked rules* live in `.editorconfig` — one source of truth per fact.

## Part 1 — Engine-agnostic C# spine

### 1. Naming

| Element | Casing | Example |
|---|---|---|
| Namespace, class, struct, enum, delegate, record | `PascalCase` | `SpawnPlanner`, `Role` |
| Interface | `I` + `PascalCase` | `IClock`, `IRandom` |
| Method, property, event (any accessibility) | `PascalCase` | `Resolve()`, `Now` |
| Private/protected/internal **instance** field | `_camelCase` | `_maxPopulation` |
| Private/protected/internal **static** field (incl. `static readonly`) | `s_camelCase` | `s_sharedBuffer` |
| Constant (`const`) — any accessibility | `PascalCase` | `DefaultGrace` (not `DEFAULT_GRACE`) |
| Public `static readonly` value | `PascalCase` | `Zero` |
| Local variable, parameter | `camelCase` | `deltaTime` |
| Type parameter | `T` / `T`+`PascalCase` | `T`, `TKey` |
| Enum members | `PascalCase` | `Prey`, `Predator` |
| File name | = the public type it contains | `SpawnPlanner.cs` |

- **The `_`/`s_` prefixes are for FIELDS only.** Methods/properties stay `PascalCase` at every
  accessibility. A non-public field is `_maxRetries`, never `maxRetries`.
- **The underscore disambiguates a field from a parameter/local without `this.`** — write
  `_maxRetries = maxRetries;`, not `this.maxRetries = maxRetries;`.
- No `m_`/Hungarian/type-encoding prefixes. Acronyms 3+ letters cased as words (`HttpClient`);
  2-letter stay upper (`IO`, `UI`).
- Booleans read as assertions: `IsDead`, `HasPredator`. Constants `PascalCase`, not `ALL_CAPS`.
- `async` methods returning `Task`/`UniTask` end in `Async` (skip only for entry points/handlers).
- Prefer `const` (compile-time constant) / `static readonly`; prefer `readonly` to a mutable field.

### 2. Layout & formatting

- **Allman braces** (open/close each on their own line). **4 spaces, no tabs.** One statement/decl
  per line. **Always braces**, even single-line bodies. Blank line between members; none right after `{`.
- **`using` directives OUTSIDE the namespace**, `System.*` first then alphabetical; remove unused.
- **One public type per file**, named after it. Small related `private`/nested types may share.
- **Member order:** nested types/delegates → static fields (const, static readonly, other) →
  instance fields → properties → constructors (then finalizer) → methods; static before instance;
  `public`→`internal`→`protected`→`private`.
- **`var` only when the type is obvious from the RHS** (`var planner = new SpawnPlanner();` yes;
  `int count = 0;` not `var`). Target-typed `new()` when the type is on the left (`SpawnPlanner p = new();`).
- Interpolation for short strings; `StringBuilder` in loops; no `+` in hot loops.
- Parentheses to make precedence obvious in non-trivial expressions.

### 3. Types & language usage

- **Access modifiers always explicit.** `sealed` by default for classes not designed for inheritance;
  `readonly` every field not reassigned after construction.
- **Prefer properties over public fields.** Expose `{ get; }` / `{ get; private set; }`.
- **Enable nullable reference types** and honor it (annotate, guard clauses).
- **Language keywords, not framework types:** `string`/`int`/`float`, not `String`/`Int32`/`Single`.
- **Pattern matching & switch expressions** over long `if/else` ladders and casts.
- **Exceptions:** specific types + clear message; never bare `catch (Exception)`; not for control flow.
- **Immutable carriers:** small DTO/event payloads as `readonly struct` (e.g. `AnimalDied`, `Outcome`).
- `static` members called via the type name.

### 4. Documentation & comments

- **XML doc (`///`) on public types/members** — summary/`<param>`/`<returns>` where it adds info.
- `//` for in-body notes (capital, period, one space, own line above the code). **Comment the *why*,
  not the *what*.**

### 5. Enforcement

The repo-root `.editorconfig` encodes Part 1's naming + machine-checkable formatting (member
ordering & XML-doc coverage stay doc-only — catch in review). Naming violations surface as
**`IDE1006`** — most at **warning** level; type-parameter and local/parameter casing are
**suggestion**-level (advisory, not build-noise). Run `dotnet format` / the IDE cleanup; wire
`dotnet format --verify-no-changes` into CI.

## Part 2 — Engine overlay: Unity 6.3 + URP (flat 3D, desktop)

**C# language-version ceiling — Unity 6.3 = C# 9.0 (Roslyn).** Therefore:

- **No file-scoped namespaces** (C# 10) — use block-scoped `namespace Foo { … }` (the `.editorconfig`
  pins `block_scoped`). **No global usings** (C# 10) — `using` per file, outside the namespace.
- **Avoid `record`/`init`-only setters** (need an `IsExternalInit` shim) and **never in a serialized
  type**. Prefer `{ get; private set; }`.
- **Allowed C# 9:** target-typed `new()`, switch expressions, pattern matching, static lambdas,
  `nint`/`nuint`. (Raw string literals are C# 11 — unavailable.)

**Serialization / inspector idiom:**

- Inspector-editable data as **`[SerializeField] private T _field;`** — keep it private. The Inspector
  strips the `_` and Title-cases it (`_moveSpeed` → "Move Speed"), so `_camelCase` stays intact.
- For an auto-property that must serialize: **`[field: SerializeField] public T Value { get; private set; }`**.
- **`ScriptableObject` for designer config/data** (`AnimalDefinition`, `AnimalCatalog`, `SimConfig`,
  the movement-strategy SOs); treat config assets as **read-only at runtime**.

**Hot-path / performance (overrides Part 1's "LINQ is fine"):**

- **No LINQ, no per-frame allocations, no boxing in `FixedUpdate`/`Update`** or the `Simulation` tick.
  Cache references; reuse buffers; pool short-lived objects. Struct event payloads, non-capturing
  callbacks (no closures in hot async/animation paths). Set animal color via `MaterialPropertyBlock`,
  never `renderer.material`.
- No `GameObject.Find`/`FindObjectOfType`/`Camera.main` per frame (cache; use the injected seams).

**Lifecycle idioms:**

- Order MonoBehaviour messages conventionally: `Awake` → `OnEnable` → `Start` → tick → `OnDisable`
  → `OnDestroy`. Subscribe in `OnEnable`/composition, unsubscribe in `OnDisable`/dispose.
- **Keep testable rules out of MonoBehaviours** — pure C# (no `UnityEngine` statics, no `Time`/
  `Random`/`Physics` directly; inject `IClock`/`IRandom`/seams) so they run headless in EditMode.
  MonoBehaviours are thin adapters; **only `Simulation` has a magic `FixedUpdate`** (guardrails §2).
- One assembly per layer via `.asmdef` (`ZooWorld.Runtime`/`.Editor`/`.Tests.EditMode`); first-party
  code under `Assets/_Project/`.

## Sources (verified June 2026)

- Microsoft .NET/C# coding conventions — https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Code-style naming rules in `.editorconfig` (IDE1006) — https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/naming-rules
- Google C# Style Guide — https://google.github.io/styleguide/csharp-style.html
- Unity C# compiler / language version (6.3 = C# 9) — https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html
