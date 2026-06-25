# Documentation Index — Zoo World
Last verified: 2026-06-25

| Document | Purpose | Update when |
|---|---|---|
| `product/game-design.md` | What we build — rules, entities, numbers, deliverables (source of truth) | Gameplay/design changes |
| `architecture/implementation-guardrails.md` | How it's engineered — the contract (Simulation, seams, physics, DI, pooling, tests) | Architecture/code-quality rules change |
| `architecture/csharp-style.md` | C# style (naming/format) — enforced by repo-root `.editorconfig` | The style or its enforcement changes |
| `architecture/adr/0001-tech-baseline.md` | Locked baseline: Unity 6.3/URP/VContainer/3D-physics/namespaces/asmdefs | Baseline changes (supersede, don't edit) |
| `architecture/adr/0002-animal-architecture.md` | Animal architecture decision (SO strategies, Simulation tick, seams, libs) | The animal architecture changes |
| `tasks/README.md` | The implementation plan (matrix + per-task briefs) | A task is authored/closed |
| `development/setup.md` | As-built dev environment (versions, deps, MCP) | Setup/versions change |
| `development/build-and-test.md` | Exact build/test commands | Commands/verification change |
| `development/agent-verification.md` | Verify state before claiming it (self-review discipline) | The discipline changes |
| `assets/asset-ledger.md` | Third-party assets + licenses (none — primitives) | An asset is added |
| `handoff/current-status.md` | Current active state | End of every session |

## Start here
Front door: [`../README.md`](../README.md).
1. `../CLAUDE.md`
2. `handoff/current-status.md`
3. `product/game-design.md`
4. `architecture/implementation-guardrails.md`
5. `architecture/csharp-style.md` (the style contract — before writing C#)
6. `tasks/README.md` (+ the active brief)

Rule: no new doc unless it's linked here. Reference background (`reference/`) is added if/when needed.
