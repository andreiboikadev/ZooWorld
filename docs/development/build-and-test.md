# Build & Test — Zoo World
Last verified: 2026-06-25

The source of truth for how to verify a change. Per-mechanic Definition of Done lives in
[`implementation-guardrails.md`](../architecture/implementation-guardrails.md) §14.

## EditMode unit tests (the primary gate for pure rules)

In the Editor: **Window → General → Test Runner → EditMode → Run All** (assembly
`ZooWorld.Tests.EditMode`). Every pure rule (resolver, spawn planner, movement/bounds/jump math,
counters) ships its tests in the same change; the full suite must be green with **0 Console errors**.

Headless / CI (Unity not open on the project):
```powershell
& "<UnityEditor>\Unity.exe" -batchmode -projectPath "D:\Work\Own\ZooWorld" `
  -runTests -testPlatform EditMode -testResults "TestResults.xml" -quit
```
(Exit code 0 = pass; read `TestResults.xml`. The Editor must not hold the project lock —
close it or use a throwaway clone.)

## Smoke (adapters / integration)

Enter **Play** mode and drive the flow (spawn → move → collide → eat/bounce → counters update),
or use the **MCP for Unity** bridge to enter Play, act, and read the Console. Verify: animals move
(none frozen), prey×prey visibly fly apart, predators eat + "Tasty!" shows, counters increment,
Console clean. Run a **standalone Windows build** (`File → Build` / Build Profiles) before calling
the deliverable done.

## What "done" means

Pure rule: EditMode tests in the same change, full suite green, Console clean. Adapter: a smoke pass
in Play (and a Player-build profile check for the perf-sensitive bits) + no regression in the suite.
Never claim a test passed unless it was run **this session** with cited evidence.
