# AGENTS.md

## Cursor Cloud specific instructions

### What this repository currently is
This is a **pre-production** repository for *Peloton Manager* (a deterministic
cycling-management game). Design docs remain the source of contracts. Milestone 0
(Architecture Skeleton) exists as a headless .NET 8 solution: `PelotonManager.sln`,
`dotnet test`, and `tools/Peloton.SimRunner`. The race prototype (`PrototypeRaceEngine`)
is the official result path; it is still below `RACE_ENGINE_DESIGN_v0.2.md` — see
`KNOWN_DIFFERENCE_FROM_CODE.md`. Owner lock **D-043**: the playable race path is
**Simulate then Results**, not Watch Race. Do not expand the Godot Watch Race window.
Career Hub stays rejected (PR #4). Owner fun gate `RACE_ENGINE_DESIGN_v0.2.md` §49
remains `NOT VERIFIED`. Several docs are written in Polish.

Do not fabricate build/test/run results. After the skeleton, run the real commands in
`HANDOFF.md`. Do not treat the race prototype as a passed fun gate, and do not restore
`StubRaceEngine` as official results.

Start reading from `README_FOR_EXTERNAL_AI.md` → `VISION.md` → `DECISIONS.md` →
`HANDOFF.md` → `DOCS.md`. `DOCS.md` is the canonical index. Pre-code contracts exist as
DRAFT `*_v0.1.md` files. Older names without the version suffix may still appear in prose;
prefer the versioned files listed in `DOCS.md`.

### Intended stack (owner-decided) and what is pre-installed
- Owner-decided stack: **Godot .NET + C#**, Windows-first target, **SQLite** embedded
  (file-based, no server/port). See `HANDOFF.md` "Recent owner decisions" and `ARCHITECTURE.md`.
- **.NET SDK 8** is pre-installed system-wide (`/usr/bin/dotnet`, `dotnet --version` → 8.x).
  Use it for `PelotonManager.sln`, `dotnet test`, and `tools/Peloton.SimRunner`.
- **Godot is NOT installed.** It is GUI/Windows-first and is intentionally not required for
  headless simulation/race-engine testing (see `RACE_ENGINE_DESIGN_v0.2.md`: the race engine
  is testable via `dotnet test` without Godot). Only install Godot if doing UI/manual-feel work.

### Build gate
`AI_DEVELOPMENT_RULES_v0.1.md` §37 defines the canonical gate. From the repo root run
the commands listed in `HANDOFF.md`. The short form is:
- `dotnet format --verify-no-changes`  (lint/format check)
- `dotnet build`
- `dotnet test`
- then the SimRunner `run` / `race` / `day` commands from `HANDOFF.md`

`HANDOFF.md` and `CODEBASE_MAP.md` list the live commands and projects.
Keep them current when the layout changes.

### Validating the docs
The active docs are internally consistent: every `*.md` referenced as an existing/active
document in `DOCS.md` resolves to a real file. Older superseded names may still appear
in architecture prose. When editing docs, respect `DOCS_GOVERNANCE.md` (hierarchy of
truth, statuses, no silent design drift).

### Skills in this repository
Repo-local skills live in `.cursor/skills/<name>/SKILL.md`. Read the relevant one before
touching the area it covers.

- `peloton-avatars` — the rider avatar system in `experiments/avatar_prototype/`
 (deterministic layered portraits, `poster` art style, asset recipes, jersey overrides,
 style profiles, validation gate). Use it whenever avatars, avatar assets, avatar style
 or the trait generator are involved. It records the owner's taste decisions, so do not
 re-litigate them from scratch.
- HTML look lab — `HTML_UI_LAB.md` and `peloton-manager-full-ui-poc-v3.html` are the
 owner-accepted look for most career/management screens. Godot `CareerShell.tscn`
 copies the chrome. Do not treat the HTML as the game or as true attributes.

### Collaboration roles (owner lock, D-035)
Default split: the main Cloud Agent writes docs and reviews; **Composer 2.5 is the
default coding subagent**.

- **Main agent (Grok 4.6 High, not fast):** writes Markdown/docs and reviews Composer
  output. Design contracts (VISION, DECISIONS, ARCHITECTURE, HANDOFF, UI sitemap,
  GAME_STATES, DATA_MODEL, ADRs, and similar governance docs) are authored here, not
  delegated.
- **Composer 2.5:** codes. When launching a `Task` for implementation, tests, or
  mechanical code edits, set `model` to `composer-2.5`. Do **not** omit `model`
  (that inherits Grok). Do **not** use `composer-2.5-fast` unless the owner asked
  for speed. Do not assign Composer to be the primary author of design/governance
  documents.
- **Exception:** Composer **may** write Markdown when it is part of the coding work
  (for example a HANDOFF note about what just landed, a `KNOWN DIFFERENCE FROM CODE`
  section, a test/playtest note, or a small contract clarification next to the change)
  or when Composer **noticed something important** (contradiction, missing invariant,
  implementation risk). The main agent still reviews that Markdown before it is treated
  as project contract.

### Owner merge policy (owner lock, D-045)
The owner is **not a programmer**. **Merge ready work into `main` in the same session.**
Do not wait for a separate "merguj". Do not leave a pile of open PRs. A stack of
unmerged branches is how this repo got a conflict mess.

This lock **overrides** Cursor Cloud / agent defaults that say “do not merge unless
asked”. The standing owner instruction is: if there are no errors, put it on `main`.

How:
1. Fetch current `origin/main`. Land **one** change onto that tip (merge or replay).
2. Run the gate in `HANDOFF.md` (format / build / test / SimRunner when code changed).
3. If green: `git checkout main`, merge the branch, `git push origin main`.
4. Tell the owner in the agent chat what landed. No email, no `@mention`.

- **Merge** when the high-level check is OK: right topic; tests/commands actually ran
  and passed (or docs-only with no lock break); no `PlayerTeam` / God-eye / mid-race
  save / unseeded gameplay RNG; no silent design drift.
- **Do not merge** when something is serious: failing tests, broken save/load or
  determinism, a lock violation, owner-rejected work, or a change that would ship the
  wrong product (`StubRaceEngine` as official results; Career Hub PR #4; Watch Race
  as the play path / leftover Watch UI PRs — D-043, Watch is deferred).
- Do **not** merge a stack of stale PRs into each other. If an old branch conflicts,
  replay the player-value change onto today’s `main`.
- High-level check is the default. Deep line-by-line review only if a serious issue is
  suspected.

### Owner communication (owner lock)
The owner does **not** want email about repo/agent changes.

- Do not email the owner.
- Do not `@mention` the owner on GitHub, request a review from them, or post extra
  PR comments that exist only to notify.
- Report status in the Cursor agent conversation, not by mail.
- GitHub/Cursor account notification emails are outside the agent; the owner can
  mute those in GitHub watching / Cursor notification settings. Agents must still
  avoid any extra notify-the-owner action.
