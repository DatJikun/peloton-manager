# Peloton Manager: Testing

**Title:** Testing

**Version:** 0.1

**Status:** DRAFT

**Purpose:** Define the testing contract: layers, golden scenarios, probes, invariants, architecture checks, soak tests, playtests, and gates that future code must satisfy. This document does not implement tests.

**Authority/Owner:** Project owner (simulation quality and engineering)

**Supersedes:** none

**Superseded by:** none

**Last reviewed:** 2026-08-25

**Related decisions/ADRs:** D-001, D-002, D-003, D-004, D-005, D-006, D-007, D-008, D-009, D-010, D-013, D-014, D-015, D-016, D-020, D-023, D-024, D-025, D-027, D-028, D-030, D-031

---

## 1. Purpose and scope

Peloton Manager is a deterministic simulation. Tests exist to prove that the world, save, knowledge boundary, and race behave according to locked contracts. They do not prove that the game is fun.

This document is the last pre-code documentation gate named by `DOCS_GOVERNANCE.md` §18 and `ARCHITECTURE.md` §76. Domain-specific acceptance lists stay in their own docs. This file defines the shared testing language, required layers, default recipe, and gates.

### In scope

- test layers and what each layer may claim;
- golden scenarios, probes, invariants, and architecture tests;
- the canonical development/test recipe;
- Milestone 0 / Architecture Skeleton gate;
- save, content, rules, race, AI, and Spy testing boundaries;
- soak and long-save obligations;
- owner playtest role;
- bug/regression workflow;
- build-gate commands after a C# repo exists.

### Out of scope

- executable test projects, fixtures, or `dotnet test` output;
- complete SQLite DDL or production JSON schemas;
- a catalog of every future test method;
- tuning all New Game combinations as separate games;
- claiming that passing tests means the loop is fun;
- Godot UI automation as a substitute for headless simulation tests;
- resolving open questions owned by other DRAFT contracts.

---

## 2. Terms

| Term | Meaning |
|---|---|
| Golden scenario | A fixed recipe, seed, content/rules identity, and command script with a replayable expected result. |
| Probe | A measurement of balance, diversity, performance, or decision density. It may lack a single expected winner. |
| Invariant test | An assertion that must hold for every legal world, not only named goldens. |
| Architecture test | A machine check of assembly/type/dependency rules. |
| Soak | A long headless run used for integrity, growth, and manager analytics. |
| Playtest | An owner-judged session about feel, clarity, and interesting decisions. |
| Fixture | Saved input artifacts: packs, scenario recipe, seed, command script, and optional expected checksum. |
| World checksum | A deterministic digest of gameplay-relevant World State after a defined barrier. |
| Test recipe | The New Game axes and module selection used by a fixture. |
| Canonical test recipe | `historyMode = Dynamic`, `difficulty = Advanced`, `attributeVisibility = Guessed`. |

---

## 3. Principles

1. Automated tests prove contracts, determinism, integrity, and forbidden shortcuts. They do not prove fun.
2. The owner is the judge of feel and decision density (`DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md`, `HANDOFF.md`).
3. Same build, same resolved content/rules, same seed, same ordered commands ⇒ same gameplay-relevant result (D-013).
4. Queries, forecasts, UI open/close, and World Spy must not consume gameplay RNG or change World State (D-014, D-024, D-025).
5. Tests use the same Commands for HumanInput and AIInput. There is no `PlayerTeam` path (D-002, D-004).
6. Truth is not Knowledge. Actor tests must be knowledge-bounded (D-003, D-010).
7. Untrusted content and saves are untrusted. Validation failures are tests, not silent repairs.
8. A bug gets a failing repro/golden when practical, then a root-cause fix (D-030).
9. Headless simulation is the default. Godot is not required to test world, save, or race engine.
10. If a run, benchmark, or platform result was not actually executed, mark it `NOT VERIFIED`. Never claim all tests pass without running them.
11. Domain-specific criteria in `GAME_STATES`, `DATA_MODEL`, `CONTENT_FORMAT`, `RULESETS`, `SAVE_FORMAT`, race, and Spy docs remain authoritative for those domains. This document does not replace them.

---

## 4. Test layers

Use the layers in `AI_DEVELOPMENT_RULES_v0.1.md` §21. Each layer has a job.

| Layer | Proves | Does not prove | When required |
|---|---|---|---|
| Unit | Pure calculations, ordering, ID formatting, numeric helpers | World behavior or fun | Any isolated function with a contract |
| Domain / Application | Commands, invariants, AccessContext, DecisionRequest, GameState guards | Feel, balance, long-run growth | Any public command or state transition |
| Simulation | World/race systems over time | Owner fun | After a system can Advance Day or run a race |
| Determinism | Same inputs ⇒ same checksum/events; RNG isolation | That the result is interesting | Any gameplay RNG domain or barrier |
| Save / load | Round trip, migration, recovery, content/rules identity | That the UI looks right | Any persistence or schema change |
| Long-run headless | Integrity, ID uniqueness, growth, manager analytics | Day-one polish | Architecture must allow it; 100-year soak is a required engineering case, not a day-one skeleton task |
| Architecture | Forbidden types and dependency direction | Runtime correctness | From first C# solution layout |
| Playtest | Fun, clarity, decision density, UI obstruction | Machine invariants | Before expanding a loop that failed a feel test |

A feature that changes a contract must update the relevant layer and the owning doc.

---

## 5. Canonical test recipe

New Game has three independent axes (`ARCHITECTURE.md` §22–24):

```text
historyMode:           Historical | Dynamic | Chaos
difficulty:            Beginner | Advanced | Expert
attributeVisibility:   All | Guessed | None
```

They are scenario/save recipe fields, not three separate games and not 27 codepaths.

### 5.1 Default development path

The canonical development, CI, and golden path is:

```text
Dynamic + Advanced + Guessed
```

This matches the architecture example recipe. Historical is not a ruleset era. Difficulty is guidance and stated pressure, not a hidden AI buff. `None` is a first-class play mode.

### 5.2 What must exist immediately in the test harness

- All three axes are explicit fixture fields.
- The same simulation accepts any legal combination without a human-only branch.
- Knowledge-layer tests exist for `Guessed`, plus at least one command/query pair each for `All` and `None`.
- Beginner/Expert may start as presentation/support intensity checks. They do not need separate world goldens.

### 5.3 What must not be done for MVP

- Do not author 27 fully tuned golden careers.
- Do not treat Historical or Chaos as unique content packs unless the recipe actually selects different modules.
- Do not implement only `All` and add `None` later. That would rewrite UI and AI against the Truth/Knowledge boundary.

---

## 6. Golden scenarios, probes, and invariants

### 6.1 Golden scenario contract

A golden fixture records at least:

```text
ScenarioDefinitionId
Resolved content identity
Resolved rules identity
historyMode / difficulty / attributeVisibility
Seed and RNG domain declaration
Ordered command script
Legal GameState barriers where assertions run
Expected world checksum and/or typed expected events
```

Goldens fail if the checksum, forbidden event, or invariant diverges. They must not read filesystem order, UI timing, or locale to compute the expected result.

Minimum golden families, once the matching code exists:

| Family | Must show |
|---|---|
| New world | CreateWorld from the canonical recipe is deterministic |
| Advance Day | One day advances the whole world, not only the player's employer |
| Save round trip | Save/load at a legal checkpoint preserves gameplay-relevant state |
| Employment change | ManagerCareer identity survives; former employer confidential access is gone |
| Race isolation | Pre-race autosave, no mid-race save, crash restores pre-race state |
| Knowledge bound | Two organizations querying the same subject can legally differ |
| Symmetry | The same Command is legal for HumanInput and AIInput |
| Content identity | Same PackId/PackVersion with a different hash is a mismatch, not a quiet substitution |

Exact command scripts are implementation work. This table is the contract family list.

### 6.2 Probes

Probes measure, they do not replace goldens.

Examples:

- decision density in a live race (owner + Race Spy);
- manager trait success by era after a long run;
- save size and Advance Day latency by decade;
- whether Beginner shows more warnings without hiding legal controls.

A probe may be noisy. It cannot silently rewrite World State to look better.

### 6.3 Invariants

Invariants are true for every legal save, including custom mixed-era scenarios.

Core invariant families and their owning docs:

| Family | Owning contract |
|---|---|
| Stable IDs never reused | `DATA_MODEL_v0.1.md`, `LONG_SAVE_AND_PERFORMANCE_v0.2.md`, D-007 |
| Nine GameStates only | `GAME_STATES_v0.1.md`, D-031 |
| ManagerCareer, not PlayerTeam | `DATA_MODEL_v0.1.md`, D-004 |
| Knowledge ownership and provenance | `DATA_MODEL_v0.1.md`, D-003, D-009, D-010 |
| Human/AI command symmetry | `AI_MANAGER_SYSTEM_v0.2.md`, D-002, D-005 |
| Save/load, migration, recovery | `SAVE_FORMAT_v0.1.md` §36 |
| Content resolution identity | `CONTENT_FORMAT_v0.1.md` §23 |
| Rules composition and transitions | `RULESETS_v0.1.md` §24 |
| Determinism, RNG isolation, barriers | `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md` |
| Causal-safe compaction | `LONG_SAVE_AND_PERFORMANCE_v0.2.md`, D-015 |
| Race decisions cannot consume hidden truth | `RACE_ENGINE_DESIGN_v0.2.md`, D-020 |
| Spy is observational | `WORLD_SPY_AND_DECISION_TRACING_v0.1.md`, D-024, D-025, D-027 |

When a domain doc and this file disagree, the domain doc wins for that domain until an owner decision updates both.

---

## 7. Milestone 0 — Architecture Skeleton gate

`ARCHITECTURE.md` §50 is the first implementation gate. It is not the whole game.

The skeleton is accepted only when:

1. `dotnet test` passes.
2. A headless runner simulates 10 seasons without crash.
3. The same seed produces the same result.
4. Godot is not required to test the race engine.
5. A JSON scenario names the active modules.
6. Changing one rules module does not require changing race UI.
7. Save writes schema version and content/rules identity.
8. `RaceLive` blocks `SaveGame`.

Until a C# solution exists, this section is a future gate, not a current CI result. Do not fabricate pass/fail.

---

## 8. Save, content, and rules tests

### 8.1 Save

Follow `SAVE_FORMAT_v0.1.md` §36. Any persistent schema change also follows `AI_DEVELOPMENT_RULES_v0.1.md` §24:

```text
schema version change
migration
migration test
save/load regression
recovery consideration
documentation update
```

Required properties:

- no attached save with `GameState = LoadingWorld` or a mid-race `RaceLive` checkpoint;
- failed save/load/migration leaves the last verified artifact intact;
- scheduler pause/progress is reconstructed, not stored as World State;
- Card Flow draft persistence stays behind OQ-GS-002.

### 8.2 Content

Follow `CONTENT_FORMAT_v0.1.md` §23.

Required properties:

- resolution is pure and repeatable;
- untrusted packs cannot escape the pack boundary or execute code;
- validation does not mutate World State or consume gameplay RNG;
- scenario difficulty/history/visibility fields cannot smuggle extra content for human players.

### 8.3 Rules

Follow `RULESETS_v0.1.md` §24.

Required properties:

- mixed-era custom scenarios resolve without being normalized to one era;
- difficulty cannot grant AI hidden truth or hide legal controls;
- historical outcomes keep the rules identity that was effective when they occurred.

---

## 9. Race, AI, and Spy tests

### 9.1 Race engine

The first race prototype must prove the nine points in `RACE_ENGINE_DESIGN_v0.2.md` §4. Deeper physiology waits until those hold.

Race tests are headless by default. Watch Race, accelerated live race, and headless simulation use the same rules and state transitions.

Crash or exit during `RaceLive` restores the verified pre-race autosave. Renderer attached versus headless must not change the result.

### 9.2 AI managers

AI is tested as a user of Application Commands, not as a second simulation.

Minimum AI tests, once managers exist:

- rejected illegal command leaves World State unchanged;
- AI cannot read rival hidden condition or true potential;
- unemployed and employed AI careers both live in `Management`;
- diversity comes from traits, skills, knowledge, staff, organization identity, and rules context — not from a random label.

### 9.3 World Spy and Race Spy

Spy is mandatory diagnostic infrastructure (D-023, D-025). It is not a gameplay overlay.

- Spy OFF and Spy DECISIONS produce identical gameplay checksums and knowledge state.
- Traces answer what happened, what the actor knew, which options were considered, why one was chosen, and what followed.
- Player-facing Why and developer Spy remain separate (D-027).
- Spy never becomes an input to rules, Commands, or normal UI.

---

## 10. Soak and long-save tests

A 100-year career is a required engineering test case (`LONG_SAVE_AND_PERFORMANCE_v0.2.md`). It is not required to pass on the first Architecture Skeleton day. The skeleton and save contract must not make it architecturally impossible.

The soak reports the metrics listed in that document, including:

- save size, load/save time, Advance Day latency;
- active vs archived entity counts;
- invalid references and reused IDs;
- deterministic world checksum;
- manager population and trait/era analytics.

Compaction regression:

```text
same world at year X
branch A: compact
branch B: no compact
same future commands
=> same gameplay-relevant future
```

ID uniqueness regression from the same document remains an invariant, including the large generate/archive/generate pattern.

---

## 11. Architecture tests

From first solution layout, machine-check where practical (`AI_DEVELOPMENT_RULES_v0.1.md` §38–39):

```text
Domain has no Godot dependency
Simulation has no Godot UI dependency
forbidden dependency directions
no special PlayerTeam domain type
no IsHumanTeam gameplay branches
no GlobalRandom in gameplay assemblies
no UI writing SQLite directly
no forecast consuming gameplay RNG
no AI reading truePotential or rival hidden condition
```

A failing architecture test blocks merge even if a golden still passes.

---

## 12. Playtests

Automated tests cannot close a fun gate.

Owner playtests record, at least:

- what was interesting;
- what was boring;
- what was obvious too early;
- what was unreadable;
- where UI blocked a decision.

A boring core loop is not defended by realism. If the race prototype fails interesting-decision density, deeper physiology does not proceed.

Playtest the canonical recipe first. Beginner, Expert, Historical, Chaos, All, and None are later feel passes, except for the early `All`/`None` knowledge-layer command checks in §5.2.

---

## 13. Bug and regression workflow

When practical (`AI_DEVELOPMENT_RULES_v0.1.md` §22–23):

```text
1. Reproduce.
2. Add failing regression test/repro scenario.
3. Find root cause.
4. Fix root cause.
5. Confirm test passes.
6. Check adjacent invariants.
7. Update docs only if a contract changed.
```

Do not accumulate one-off conditions such as `if playerTeam`, `if year == ...`, or famous-race exceptions unless content/rules genuinely define them.

Classify failures as bad data, rule, heuristic, state transition, information, calibration, or implementation. World Spy should help distinguish them.

---

## 14. Build gate

After a C# repo exists, the canonical commands are:

```text
dotnet format --verify-no-changes
dotnet build
dotnet test
dotnet run --project tools/Peloton.SimRunner -- <scenario>
```

`HANDOFF.md` must then contain the real commands and project names. Until bootstrap, those commands are not runnable and must not be reported as passing.

`CODEBASE_MAP.md` must later say where each system's tests live.

---

## 15. Locked decisions

| Decision | Testing consequence |
|---|---|
| D-001 | Goldens assert simulated outcomes, not scripted historical results. |
| D-002 / D-005 | Human and AI tests share Commands; authority is data. |
| D-003 / D-010 | Actor tests are knowledge-bounded; God-eye is a failing test. |
| D-004 | No `PlayerTeam` type, fixture, or assertion helper. |
| D-006 | Advance Day tests move the whole world. |
| D-007 | ID uniqueness is an invariant, including after archive/compaction. |
| D-008 | RaceLive tests forbid mid-race save and require pre-race autosave. |
| D-009 | Job-change tests prove confidential knowledge does not follow the manager. |
| D-013 | Pause, render, and UI timing cannot be golden inputs. |
| D-014 | Forecast/query tests must leave checksum and RNG unchanged. |
| D-015 | Compact vs uncompacted branches share gameplay-relevant future. |
| D-016 | Core loop tests come before a full balance lab. |
| D-020 | Race decision tests cannot feed hidden truth to DS/AI. |
| D-023 / D-025 | Spy infrastructure is required in race/world tests, and must be non-causal. |
| D-030 | Bugs get repro fixtures before patch stacking. |
| D-031 | Serialized GameState tests allow exactly nine values. |

---

## 16. Open questions

| ID | Question | Decision deadline |
|---|---|---|
| OQ-TS-001 | Exact world-checksum algorithm and which fields are gameplay-relevant | Before first golden checksums are treated as merge-blocking |
| OQ-TS-002 | Which goldens are PR-blocking vs nightly vs release | Before CI is wired |
| OQ-TS-003 | 100-year soak cadence (nightly, weekly, release) and minimum machine spec | Before soak is automated |
| OQ-TS-004 | How many `All` / `None` knowledge goldens are required before first playable MVP | Before attribute-visibility UI implementation |
| OQ-TS-005 | Numeric cross-platform policy for golden floats/fixed-point | Follows the open numeric policy in `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md` |

These remain open and are not resolved here:

- OQ-GS-002 Card Flow draft persistence;
- OQ-GS-003 hotseat RaceLive;
- OQ-DM-001 / OQ-SF-001 allocator layout;
- OQ-CF-005 / OQ-SF-002 content snapshot vs cache;
- OQ-SF-005 cross-build save compatibility window.

---

## 17. Deferred

- full UI screenshot/automation suite;
- Workshop/mod distribution tests;
- online/hotseat multiplayer tests;
- Database Editor tests;
- complete 27-combination balance lab;
- performance lab beyond the soak metrics already named;
- naming of production test projects (`Peloton.Domain.Tests` and similar) until Architecture Skeleton exists.

---

## 18. Non-goals

- proving fun with unit tests;
- a second human-only test harness;
- using Godot as the source of expected world results;
- mid-race save fixtures;
- silent content substitution to keep a golden green;
- treating Historical/Chaos/Beginner/Expert as separate engines;
- implementing tests in this documentation session;
- closing other documents' open questions by inventing test defaults.

---

## 19. Implementation notes

- Keep goldens and probes in headless projects. Godot may later host playtest helpers, not expected-result authority.
- Name RNG domains in fixtures. Cosmetic RNG must be declared and isolated.
- Prefer typed expected events over huge opaque blobs when diagnosing a failure.
- Record resolved content/rules identity in every world-level fixture.
- When adding a Command, add the rejection cases: stale authority, wrong GameState, knowledge-bounded query, and RNG-neutral forecast where applicable.
- Prototype race code may be simpler, but must be marked and covered by the §4 prototype proofs. Approximations must not silently become architecture.
- After bootstrap, put the real test commands in `HANDOFF.md` and the test locations in `CODEBASE_MAP.md`.

---

## 20. Migration impact

This DRAFT creates no test project and migrates no fixtures.

The first implementation establishes schema version 1, content schema version 1, and the first golden folder in a separate reviewed task. Later contract changes must say which goldens, probes, and soaks are invalidated and how they are rebuilt.

---

## 21. Test criteria for this contract

This document is doing its job when later implementation can answer:

- Which layer does this change belong to?
- Which golden family must move?
- Is this a probe or an invariant?
- Does it use the canonical recipe or an explicit other recipe?
- Can it run headless?
- Does Spy/forecast/query stay RNG-neutral?
- Is `PlayerTeam` absent?
- What remains `NOT VERIFIED`?

If a new system cannot name its layer, owning doc, and golden family, the testing contract is incomplete for that system and should be extended in the system's own doc first.
