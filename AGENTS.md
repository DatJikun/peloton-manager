# AGENTS.md

## Cursor Cloud specific instructions

### What this repository currently is
This is a **documentation-only, pre-production** repository for *Peloton Manager* (a
deterministic cycling-management game). Every tracked file is Markdown — there is **no
source code, no build system, no tests, and no runnable application yet**. `HANDOFF.md`
states this directly ("gameplay repo jeszcze nie istnieje") and `README_FOR_EXTERNAL_AI.md`
says "Do not begin broad gameplay implementation yet". Note: several docs are written in Polish.

Do not fabricate build/test/run results for a game that does not exist. The current
"development" work is authoring and reviewing the design/governance Markdown docs.

Start reading from `README_FOR_EXTERNAL_AI.md` → `VISION.md` → `DECISIONS.md` →
`HANDOFF.md` → `DOCS.md`. `DOCS.md` is the canonical index of which docs are active vs.
`NOT STARTED`; several docs cross-reference future files (e.g. `UI_SITEMAP.md`,
`GAME_STATES.md`, `DATA_MODEL.md`, `SAVE_FORMAT.md`) that intentionally do not exist yet.

### Intended stack (owner-decided) and what is pre-installed
- Owner-decided stack: **Godot .NET + C#**, Windows-first target, **SQLite** embedded
  (file-based, no server/port). See `HANDOFF.md` "Recent owner decisions" and `ARCHITECTURE.md`.
- **.NET SDK 8** is pre-installed system-wide (`/usr/bin/dotnet`, `dotnet --version` → 8.x).
  This is the toolchain needed to bootstrap the planned C# solution and run the future
  headless `Peloton.SimRunner` / race spikes.
- **Godot is NOT installed.** It is GUI/Windows-first and is intentionally not required for
  headless simulation/race-engine testing (see `RACE_ENGINE_DESIGN_v0.2.md`: the race engine
  is testable via `dotnet test` without Godot). Only install Godot if doing UI/manual-feel work.

### Build gate (only meaningful once C# code is bootstrapped)
`AI_DEVELOPMENT_RULES_v0.1.md` §37 defines the canonical gate. Once a `.sln`/`.csproj`
exists, run from the repo root:
- `dotnet format --verify-no-changes`  (lint/format check)
- `dotnet build`
- `dotnet test`
- `dotnet run --project tools/Peloton.SimRunner -- <scenario>`  (headless sim check)

After a real repo bootstrap, `HANDOFF.md` and `CODEBASE_MAP.md` must be populated with the
actual commands and project layout (they are currently placeholders/templates).

### Validating the docs (the repo's current deliverable)
The active docs are internally consistent: every `*.md` referenced as an existing/active
document in `DOCS.md` resolves to a real file; unresolved references are the intentional
`NOT STARTED` future docs and older superseded versions. When editing docs, respect
`DOCS_GOVERNANCE.md` (hierarchy of truth, statuses, no silent design drift).
