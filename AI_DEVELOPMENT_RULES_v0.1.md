# Peloton Manager — AI Development Rules

**Version:** 0.1  
**Status:** REVIEW  
**Authority:** mandatory implementation workflow under `DECISIONS.md` (D-028, D-035), `ARCHITECTURE.md`, `DOCS_GOVERNANCE.md`  
**Purpose:** allow a non-programmer owner to safely develop the project with multiple AI coding sessions without silent architecture drift, undocumented behavior, giant risky changes or impossible-to-debug regressions.

---

## 1. North star

> **AI should leave the repository easier to understand than it found it, without turning the codebase into a wall of comments and documents.**

The goal is not maximum documentation. The goal is traceability:

```text
Why does this system exist?
Which decision authorized it?
Which files own it?
What assumptions does it rely on?
How is it tested?
What changed?
Where should we look if it breaks?
```

A future AI must be able to answer those questions quickly.

## 2. Golden rules

### G-001 — No silent design changes
AI must not silently change an accepted game or architecture decision because another implementation is easier. If implementation conflicts with `DECISIONS.md`, `VISION.md`, `ARCHITECTURE.md`, or an ACCEPTED/LOCKED system design, identify the conflict and do not override it silently.

### G-002 — Small, reviewable tasks
One coding task should have one primary goal. A task may touch several files when required by the same behavior, but unrelated cleanup belongs elsewhere.

### G-003 — No opportunistic refactors
Do not refactor unrelated code “while here” unless required to complete the task safely. If unavoidable, separate and explain it.

### G-004 — Code owns behavior; docs own contracts
Documentation explains architecture, public contracts, invariants, ownership, lifecycle, reasons, trade-offs and extension points. It does **not** narrate code line-by-line.

### G-005 — Tests are part of implementation
A feature is not complete merely because it compiles.

### G-006 — Git history is project memory
Commits and PRs must explain meaningful changes. Avoid messages like `fix`, `stuff`, `update files`.

### G-007 — Composer 2.5 codes; the main agent writes contracts
When this repo is developed with a main Cloud Agent plus subagents (D-035): coding `Task` launches use Composer 2.5 (`composer-2.5`). Do not inherit Grok. Do not use Composer 2.5 Fast unless the owner asked for speed. Design/governance Markdown stays with the main agent (Grok 4.6 High). See `AGENTS.md`.

## 3. Required read order before coding

```text
VISION.md
DECISIONS.md
HANDOFF.md
DOCS.md
ARCHITECTURE.md
DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md
AI_DEVELOPMENT_RULES_v0.1.md
```

Then read only system docs relevant to the task. On Cursor Cloud also follow `AGENTS.md`. Coding subagents are Composer 2.5 (G-007, D-035). Do not feed every design document to every coding session when unnecessary.

## 4. Task card before implementation

Every non-trivial task starts with:

```text
FEATURE:
GOAL:
PLAYER VALUE:

IN SCOPE:
OUT OF SCOPE:

AUTHORITATIVE DOCS:
AFFECTED MODULES:

COMMANDS / QUERIES:
DATA / SAVE IMPACT:
RNG DOMAIN:
DOMAIN EVENTS:
WORLD SPY TRACE:

ACCEPTANCE TESTS:
BALANCE PROBES:
MANUAL PLAYTEST:

DOCS TO UPDATE:
```

`PLAYER VALUE` is mandatory when meaningful. This prevents architecture work from becoming detached from gameplay value.

## 5. Repository boundaries

Expected dependency direction:

```text
Domain
↑
Simulation / Rules
↑
Application
↑
Persistence / Content / Infrastructure
↑
Godot Client
```

Hard rules:
- Domain does not depend on Godot.
- Simulation does not depend on Godot UI.
- UI does not mutate Domain state directly.
- Persistence does not decide gameplay.
- World Spy observes; it never controls gameplay.
- data-only content does not execute arbitrary gameplay code in MVP.

If a feature requires breaking a dependency rule, create an architecture decision rather than sneaking around it.

## 6. File and class ownership

Avoid dumping grounds such as `GameManager`, `GlobalManager`, `Helpers`, `Utils`, `Misc`, `CommonStuff` when they gather unrelated responsibilities.

Prefer domain names such as:

```text
RecruitmentCaseService
ContractOfferValidator
RaceScheduler
OrganizationKnowledgeStore
WPrimeModel
SponsorMarketEvaluator
```

A file/class should have a recognizable reason to change.

## 7. Comment policy

### Comment WHY, not WHAT

Good:

```csharp
// W' recovery is resolved after group movement so a rider cannot
// recover using shelter gained later in the same simulation phase.
```

Bad:

```csharp
// Subtract W' used
wPrime -= used;
```

Comments are appropriate for:
- non-obvious invariants,
- deterministic ordering assumptions,
- numeric approximations,
- performance shortcuts,
- compatibility workarounds,
- rule interpretation,
- known limitations,
- reasons strange-looking code must remain.

Do not comment obvious assignments, loops, trivial getters or every property.

### TODO policy
Never use `TODO fix later`. Prefer:

```text
TODO(RACE-014): Replace prototype shelter approximation after P3 crosswind calibration.
```

Important TODOs reference an issue/task/decision.

## 8. Public API documentation

Public domain/application contracts get concise docs where the type/name alone is insufficient. Document preconditions, ownership, side effects, determinism expectations and result/error semantics. Do not write paragraphs for obvious DTO fields.

## 9. Naming and units

Use English for code/domain identifiers. Names should express domain meaning. Units must be explicit in formula-heavy systems:

```text
PowerW
EnergyJ
MassKg
DistanceM
DurationSeconds
MoneyMinorUnits
```

Avoid ambiguous `value`, `amount`, `time`, `power` where the unit is unclear.

## 10. No magic gameplay numbers

Gameplay constants must not be scattered through code. Prototype values may exist, but must be named and attributable to configuration/calibration.

Bad:

```csharp
if (gap > 3.7 && wind > 8.2)
```

Better:

```csharp
if (gap > tuning.ShelterLossDistanceMeters &&
    wind > tuning.CrosswindPressureThreshold)
```

Do not over-configure mathematical constants or intrinsic definitions.

## 11. Determinism rules

Never in gameplay code:
- `new Random()` ad hoc,
- runtime-dependent hash for seed derivation,
- unordered collection iteration affecting outcomes,
- UI/cosmetic RNG affecting simulation,
- wall-clock time affecting gameplay,
- debug mode changing execution semantics.

If parallelism is introduced, merge results in canonical deterministic order. State cannot depend on thread completion order.

## 12. Commands, Queries, Forecasts

Commands request state changes. Queries read state.

Queries and forecasts must:
- not mutate World State,
- not consume gameplay RNG,
- not secretly advance simulation,
- not read hidden truth unavailable to the actor.

## 13. Domain Events and knowledge

Domain Events describe authoritative things that happened. They are not UI notifications, scheduler jobs, or automatically public knowledge.

Never bypass:

```text
Truth → publication/observation → knowledge → interpretation → decision
```

## 14. Error model

Expected invalid actions return structured domain/application results rather than crashing.

Impossible internal states should fail loudly in development with stable IDs/context and diagnostic traces. Do not silently swallow invalid states.

## 15. World Spy requirement

Every important automated decision should emit a compatible `DecisionTrace`.

Feature implementation should answer:

```text
What trace proves this automated decision can be explained?
```

Examples: contract offer, sponsor selection, staff hire, race chase, manager dismissal, calendar change.

World Spy remains observational only.

## 16. Structured logging

Prefer structured logs:

```text
Event=ContractOfferSubmitted
OrganizationId=42
RiderId=913
OfferId=1802
```

Use ordinary logs for lifecycle/errors, World Spy for high-volume reasoning diagnostics. Never spam every race tick into normal logs.

## 17. Git branches

Recommended:

```text
main
feature/<issue>-short-name
fix/<issue>-short-name
refactor/<issue>-short-name
docs/<issue>-short-name
```

`main` should remain runnable/testable.

## 18. Commit discipline

Prefer small meaningful commits:

```text
feat(recruitment): add ContactAgent command validation
test(recruitment): cover unavailable agent and expired case
docs(recruitment): document agent-contact state transition
```

Important commits include:

```text
Why:
What:
Tests:
Decision refs:
```

Example:

```text
fix(race): resolve group splits after simultaneous movement

Why:
Sequential rider updates let lower EntityId riders retain shelter unfairly.

What:
Movement now resolves from a shared phase snapshot.

Tests:
P3CrosswindSplitDeterminism
P4CloseableGap

Decision refs:
D-019, D-021
```

## 19. Pull Request template

Every meaningful PR should include:

```text
## Goal
## Player value
## What changed
## What did NOT change
## Architecture / decision references
## Tests run
## World Spy / diagnostics
## Save/data impact
## RNG/determinism impact
## Manual test steps
## Risks / known limitations
## Docs updated
```

AI fills it in; do not leave empty boilerplate.

## 20. Diff-size warning

Large diffs are allowed when genuinely required, but roughly `>15 files` or `>800 changed lines` should trigger an explicit check whether the work should be split. Generated migrations/data are excluded from naive line counting.

Do not reformat unrelated files in behavioral PRs. Clean diffs are a debugging tool.

## 21. Test layers

Use:
- unit tests for pure calculations,
- Domain/Application tests for commands and invariants,
- Simulation tests for world/race systems,
- determinism tests,
- save/load tests,
- long-run headless tests,
- manual owner playtests for fun/clarity.

Automated tests do not prove the game is fun.

## 22. Bug workflow

When practical:

```text
1. Reproduce.
2. Add failing regression test/repro scenario.
3. Find root cause.
4. Fix root cause.
5. Confirm test passes.
6. Check adjacent invariants.
7. Update docs only if contract changed.
```

Avoid “probably fixed”.

## 23. Root-cause rule

Avoid accumulating one-off conditions such as:

```text
if riderId == ...
if year == ...
if playerTeam ...
if TourDeFrance && stage == ...
```

unless genuinely defined by content/rules. Classify failures as bad data, rule, heuristic, state transition, information, calibration or implementation. World Spy should help distinguish them.

## 24. Save/schema changes

Any persistent schema change requires:

```text
schema version change
migration
migration test
save/load regression
recovery consideration
documentation update
```

Never modify SQLite schema silently.

## 25. Content schema changes

Require:
- schema version,
- validator update,
- migration/compatibility policy,
- sample content update,
- tests.

No silent reinterpretation of existing mod fields.

## 26. Dependencies

Adding a library/package requires documenting:

```text
why needed
license compatibility
maintenance/status
determinism implications
runtime/platform implications
brief alternatives considered
```

Do not add packages for trivial standard-library functionality.

## 27. Performance work

Workflow:

```text
measure → identify bottleneck → change → measure again → verify determinism → verify behavior
```

Race Engine optimizations require parity tests against the reference model.

## 28. Content/save safety

Treat mod/content/save inputs as untrusted. Validate schema, ranges, references, versions and required capabilities. Data-only mods do not execute arbitrary code in MVP.

## 29. Documentation update rules

Update docs when:
- public contract changes,
- architecture changes,
- save/content schema changes,
- accepted gameplay behavior changes,
- invariant changes,
- new module/system is introduced.

Do not update high-level docs merely because a private helper changed.

## 30. CODEBASE_MAP

Maintain `CODEBASE_MAP.md` after repository bootstrap. It answers where each major system lives, which project owns it, where its tests live and where debugging starts. It is a map, not a tutorial.

## 31. Handoff after meaningful tasks

Leave a short handoff:

```text
DONE:
CHANGED:
TESTED:
NOT TESTED:
RISKS:
NEXT:
FILES TO READ:
```

Do not write an essay.

## 32. GitHub Issues

Meaningful features/bugs should have an issue/task ID. Suggested labels:

```text
type:feature
type:bug
type:refactor
type:docs
area:race
area:ai
area:recruitment
area:save
area:ui
priority:critical
priority:high
status:blocked
needs-owner-decision
```

Issues should reference relevant decision IDs.

## 33. ADR / DDR

Create an ADR/DDR for choices that are hard to reverse, affect many modules, alter save/content compatibility, architecture direction or a locked gameplay principle. Do not create ADRs for trivial implementation details.

## 34. Owner decision boundary

The owner should not need to judge syntax or implementation minutiae. When owner input is required, present:

```text
Decision needed:
Option A:
Pros:
Cons:
Option B:
Pros:
Cons:
Recommendation:
Hard-to-change-later consequence:
```

## 35. AI may disagree

AI should challenge an implementation that would violate invariants, create serious technical debt, risk save corruption, break determinism or undermine gameplay. Explain concrete consequences; never override the owner silently.

## 36. No fake confidence

If build/test/migration/benchmark/platform behavior was not actually verified, mark it `NOT VERIFIED`. Never claim all tests pass without running them.

## 37. Build gate

Canonical commands after bootstrap (live list is in `HANDOFF.md`):

```text
dotnet format --verify-no-changes
dotnet build
dotnet test
dotnet run --project tools/Peloton.SimRunner -- <scenario>
```

`HANDOFF.md` is the source of the real SimRunner flags. Do not invent a shorter gate.

## 38. Architecture tests

Machine-check where practical:
- Domain has no Godot dependency,
- Simulation has no Godot UI dependency,
- forbidden dependency directions,
- no special `PlayerTeam` domain type,
- no forbidden RNG construction in gameplay assemblies.

## 39. Forbidden shortcuts

Do not introduce:

```text
PlayerTeam
IsHumanTeam gameplay branches
GlobalRandom
SimulationSingleton owning everything
Godot node as authoritative domain state
UI writes SQLite directly
AI reads truePotential
AI reads rival hidden condition
forecast consumes gameplay RNG
schema change without migration
```

## 40. Prototype code

Prototype implementations may be simpler but must be named/marked and have known limitations plus replacement/calibration issue references. Prototype approximations must not silently become permanent architecture.

## 41. AI self-review checklist

Before finalizing:

```text
[ ] Stayed in scope?
[ ] Violated a locked decision?
[ ] Added a player-only shortcut?
[ ] Leaked Simulation Truth?
[ ] Used gameplay RNG incorrectly?
[ ] Changed save/content format?
[ ] Units explicit?
[ ] Deterministic ordering explicit?
[ ] Relevant tests added/run?
[ ] World Spy explains important automation?
[ ] Docs updated only where contract changed?
[ ] Git diff reasonably clean?
```

## 42. Documentation examples

Bad documentation narrates implementation:

```text
The constructor assigns riderId, then Validate checks...
```

Good documentation preserves contract:

```text
SubmitContractOffer is an Application Command.
It requires an active RecruitmentCase and legal contact.
Human and AI organizations use the same command.
It may emit ContractOfferSubmitted.
```

## 43. Definition of Done

A non-trivial coding task is DONE only when applicable items are satisfied:

```text
behavior implemented
build passes
relevant tests pass
determinism implications checked
save/data impact handled
World Spy trace exists if automation is important
manual test instructions exist where relevant
contract docs updated if needed
Git diff reviewed
handoff written
```

## 44. Owner-facing completion report

AI should finish with:

```text
Done:
What changed:
Verified:
Risk:
Try it:
Git branch/commit/PR:
Next logical task:
```

Plain language first. Technical detail on request.

## 45. Ultimate rule

> **Never optimize for making the current AI session look productive at the expense of making the repository understandable next month.**

Clean Git history, tests, structured diagnostics and concise contract documentation are part of the product.
