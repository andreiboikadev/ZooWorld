# Zoo World

A small **3D top-down primitives sim**: animals spawn over time, roam, and a simple food chain plays
out through physics collisions. Built as a Senior Unity test task whose graded focus is the
**animal architecture** — clear, SOLID, and easy to extend "as if we'll add 1000 animals".

> **Status:** feature-complete (M3). Spawn → roam → predation → feedback runs; prey/predator colour, the
> "Tasty!" label, a death puff, the frog/rabbit hop, and the dead-prey/dead-predator counters are all live.
> Design + a "how to add a new animal" walkthrough: [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Stack

- **Unity 6.3 LTS** (`6000.3.16f1`) + **URP**.
- **VContainer** (dependency injection) + **UniTask** (async), both via the OpenUPM scoped registry
  and pinned in `Packages/packages-lock.json`. No ECS (per the brief); UI is **uGUI**.

## Run

1. Open the project in **Unity 6000.3.16f1** (Unity restores the packages on first open).
2. Press **Play** — or run the standalone **Windows build** (`Builds/Windows/ZooWorld.exe`, produced via
   File → Build; not committed to git — attached to the submission).

## Test

**Window → General → Test Runner → EditMode → Run All** (assembly `ZooWorld.Tests.EditMode`).
The gameplay *rules* (predation, spawn planning, movement/bounds/jump math, counters) are pure C#
tested headless. Full commands: [`docs/development/build-and-test.md`](docs/development/build-and-test.md).

## Architecture (the graded part)

Composition over inheritance: an `Animal` is a thin adapter holding an `AnimalDefinition`
(ScriptableObject: role, strength, a movement strategy, tuning) + per-animal state. One injected
**`Simulation`** service owns the tick; gameplay **rules are pure C# over plain structs + interface
seams** (so they're unit-testable without the engine); predation is resolved deterministically in an
end-of-step drain; animals + labels are pooled. Patterns shown: **Strategy** (movement),
**Factory + Object Pool**, **Observer** (events → counters/labels), **DI** (VContainer).

Full write-up + the **"how to add a new animal"** walkthrough: **[`ARCHITECTURE.md`](ARCHITECTURE.md)**.

## Documentation

Start at [`docs/INDEX.md`](docs/INDEX.md). Design → [`docs/product/game-design.md`](docs/product/game-design.md);
engineering contract → [`docs/architecture/implementation-guardrails.md`](docs/architecture/implementation-guardrails.md);
decisions → [`docs/architecture/adr/`](docs/architecture/adr/); the plan → [`docs/tasks/README.md`](docs/tasks/README.md).

## Assets

Placeholder **primitive** visuals only — no third-party art/audio assets (see [`docs/assets/asset-ledger.md`](docs/assets/asset-ledger.md)).
