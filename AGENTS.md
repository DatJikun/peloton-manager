# AGENTS.md

## Cursor Cloud specific instructions

### What this repository currently is
This is a **pre-production** repository for *Peloton Manager* (a deterministic
cycling-management game). Design docs remain the source of contracts. Milestone 0
(Architecture Skeleton) now exists as a headless .NET 8 solution: `PelotonManager.sln`,
`dotnet test`, and `tools/Peloton.SimRunner`. There is still **no playable game UI**.
Godot is a compile stub only. `StubRaceEngine` is not the real race engine — see
`KNOWN_DIFFERENCE_FROM_CODE.md`. Several docs are written in Polish.

Do not fabricate build/test/run results. After the skeleton, run the real commands in
`HANDOFF.md`. Do not expand the race stub into `RACE_ENGINE_DESIGN_v0.2.md` without a
separately scoped task.

Start reading from `README_FOR_EXTERNAL_AI.md` → `VISION.md` → `DECISIONS.md` →
`HANDOFF.md` → `DOCS.md`. `DOCS.md` is the canonical index. Pre-code contracts exist as
DRAFT `*_v0.1.md` files. Older names without the version suffix may still appear in prose;
prefer the versioned files listed in `DOCS.md`.

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

`HANDOFF.md` and `CODEBASE_MAP.md` already list the real skeleton commands and projects.
Keep them current when the layout changes.

### Validating the docs (the repo's current deliverable)
The active docs are internally consistent: every `*.md` referenced as an existing/active
document in `DOCS.md` resolves to a real file. Older superseded names may still appear
in architecture prose. When editing docs, respect `DOCS_GOVERNANCE.md` (hierarchy of
truth, statuses, no silent design drift).

### Collaboration roles (owner lock)
Default split when this repo is developed with a main Cloud Agent plus Composer 2.5
subagents:

- **Main agent (Grok 4.6 High, not fast):** writes Markdown/docs and reviews Composer
  output. Design contracts (VISION, DECISIONS, ARCHITECTURE, HANDOFF, UI sitemap,
  GAME_STATES, DATA_MODEL, ADRs, and similar governance docs) are authored here, not
  delegated.
- **Composer 2.5 / Codex:** codes. Do not assign Composer to be the primary author of those
  design/governance documents.
- **Exception:** Composer **may** write Markdown when it is part of the coding work
  (for example a HANDOFF note about what just landed, a `KNOWN DIFFERENCE FROM CODE`
  section, a test/playtest note, or a small contract clarification next to the change)
  or when Composer **noticed something important** (contradiction, missing invariant,
  implementation risk). The main agent still reviews that Markdown before it is treated
  as project contract.

### Owner merge policy (owner lock)
The owner is **not a programmer**. Do not wait for a separate "merguj" after a check that
already found the work ready.

- **Merge** when the high-level check is OK: right topic, tests/commands actually run and
  pass (or docs-only with no lock break), no `PlayerTeam` / God-eye / mid-race save /
  unseeded gameplay RNG, no silent design drift.
- **Do not merge** when something is serious: failing tests, broken save/load or
  determinism, a lock violation, owner-rejected work, or a change that would ship the
  wrong product (for example treating `StubRaceEngine` as the real race engine, or
  PR #4 Career Hub after the owner rejected that UI).
- High-level check is the default. Deep line-by-line review only if a serious issue is
  suspected.
- Tell the owner in plain language what landed and what was held.
