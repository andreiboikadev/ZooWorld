# Setup (as-built) — Zoo World
Last verified: 2026-06-25

How the dev environment is configured, so a new machine/session can reproduce it. The locked tech
baseline is [`ADR 0001`](../architecture/adr/0001-tech-baseline.md); current state is
[`current-status.md`](../handoff/current-status.md).

## Engine & packages
- **Unity 6.3 LTS — `6000.3.16f1`** (pinned in `ProjectSettings/ProjectVersion.txt`); **URP 17.3.0**.
- Dependencies (`Packages/manifest.json`, pinned in `packages-lock.json`), both via the **OpenUPM
  scoped registry** (`package.openupm.com`):
  - **VContainer `1.18.0`** (`jp.hadashikick.vcontainer`) — DI.
  - **UniTask `2.5.11`** (`com.cysharp.unitask`) — async.
  - *(DOTween was evaluated and **dropped** — not on OpenUPM + weakest-justified; feedback is a small
    UniTask lerp helper.)*
- Repo-root `.editorconfig` enforces C# style (IDE1006) in VS / VS Code / Rider / `dotnet format`.

## Reproduce on a new machine
1. Install **Unity Hub + Unity 6000.3.16f1** (with Windows build support).
2. Clone the repo and open it in that exact Unity version — packages restore automatically.
3. Open Test Runner (EditMode) to confirm the suite is green; press Play to run the sim.

## MCP for Unity (assistant automation — optional, committed for reproducibility)
- Unity-side bridge: **`com.coplaydev.unity-mcp`** (in `manifest.json`); it runs a local HTTP MCP
  server at `http://127.0.0.1:8080/mcp`.
- Client config: a committed project **`.mcp.json`** (`UnityMCP`, `type: http`) — any machine that
  opens the project in Claude Code gets the bridge.
- Per-machine prereqs (for the Unity-side server): **Python ≥ 3.11 + uv**. After cloning, restart
  Claude Code so it picks up `.mcp.json`, approve the `UnityMCP` server, keep Unity open (server runs).

## Git / assistant policy
- The human owns all commits; the assistant's git is read-only (enforced by `.claude/settings.json`
  deny rules for Bash + PowerShell). Add deps by editing `manifest.json`.
