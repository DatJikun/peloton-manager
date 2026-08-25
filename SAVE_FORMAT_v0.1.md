# Peloton Manager: Save Format

**Title:** Save Format

**Version:** 0.1

**Status:** DRAFT

**Purpose:** Define the durable save contract, SQLite file boundary, schema versioning, migrations, content identity, recovery, and minimum state required for deterministic long careers.

**Authority/Owner:** Project owner (persistence and simulation architecture)

**Supersedes:** none

**Superseded by:** none

**Last reviewed:** 2026-08-25

**Related decisions/ADRs:** D-002, D-004, D-005, D-007, D-008, D-009, D-013, D-014, D-015, D-024, D-025, D-027, D-028, D-029, D-031

---

## 1. Purpose and scope

Peloton Manager uses SQLite as the direction for career saves. A save is a local file managed by the application, not a database server and not an integration surface for UI code.

This document defines what persistence must preserve and how schema, content, rules, migration, integrity, and recovery interact. It does not prescribe production tables for every domain.

### In scope

- one-file SQLite save direction and artifact ownership;
- save metadata and schema version;
- mandatory sequential migrations and migration tests;
- resolved content/rules identity;
- stable identity allocation and no reuse;
- minimum durable state from `DATA_MODEL_v0.1.md`;
- canonical `GameState` checkpoint and scheduler runtime boundary;
- atomic save, autosave, load, recovery, and integrity behavior;
- RaceLive pre-race autosave behavior;
- content mismatch classification;
- determinism, RNG, long-save, and World Spy boundaries.

### Out of scope

- complete SQLite DDL, indexes, and domain table layout;
- full rider, race, physiology, development, contracts, economy, or calendar schema;
- HOT/WARM/COLD thresholds and compaction algorithms;
- cloud sync, cross-device conflict resolution, and online multiplayer storage;
- save-game editor and external database API;
- mid-race snapshots;
- final Card Flow draft persistence policy;
- encryption and anti-tamper policy.

---

## 2. Persistence principles

1. Authoritative World State is saved explicitly. Event sourcing is not the only source of truth.
2. A save identifies the exact content and rules used by the world.
3. A used `WorldEntityId` remains burned for the lifetime of the save.
4. Save/load at the same build and checkpoint preserves gameplay-relevant state.
5. Schema changes are versioned and migrated. The application never silently reinterprets old bytes.
6. A failed save, load, or migration does not damage the last verified artifact.
7. Scheduler work and unresolved domain obligations survive application closure.
8. Runtime pause/progress state is not World State.
9. RaceLive has no mid-race save. Recovery starts from the verified pre-race autosave.
10. Compaction may change representation but not future gameplay (D-015).
11. UI and Godot do not write persistence structures directly.
12. Debug truth does not enter ordinary gameplay state through a save.

---

## 3. Save artifact boundary

The primary career artifact is one SQLite file. Sidecar files may exist for crash-safe replacement, backups, diagnostic exports, or immutable content cache, but the career's authoritative checkpoint is one verified database file.

Conceptual artifact groups:

```text
Save envelope and compatibility metadata
Authoritative current World State
Application checkpoint metadata
Stable identity allocator state
Resolved content and rules identity
People, careers, organizations, employment, authority
Knowledge ownership and durable knowledge records
Scheduler queue and pending domain obligations
Domain records owned by implemented systems
Historical and audit records required by retention policy
Recovery and integrity metadata
```

These groups are contracts, not table names. Implementation may split, combine, index, or archive records as long as ownership, migration, integrity, and query behavior remain valid.

SQLite is internal persistence. The Godot client and ordinary gameplay Queries do not depend on its physical layout.

---

## 4. Save envelope

Every save carries enough metadata to decide whether the application can inspect, migrate, and attach it safely.

```text
SaveEnvelope
    SaveFormatId
    SchemaVersion
    SaveId
    WorldId
    CreatedAtUtc
    LastCommittedAtUtc
    CurrentSimulationTime
    SimulationBuildIdentity
    DeterminismContractVersion
    RNGContractVersion
    ResolvedContentIdentity
    ResolvedRulesetIdentity
    ApplicationCheckpoint
    Integrity/commit metadata
```

Wall-clock timestamps are metadata. They do not decide gameplay ordering.

`SimulationBuildIdentity` identifies the build family needed to interpret current state and rules. Cross-version bit-identical replay is not guaranteed. Compatibility or migration between builds must be explicit.

---

## 5. Schema version contract

`SchemaVersion` is a monotonic save-format version interpreted before domain records attach to runtime objects.

Rules:

- every persistent schema change increments or otherwise advances the canonical schema version;
- each supported migration declares one exact source and target version;
- migrations run in an ordered chain such as `v1 -> v2 -> v3`;
- skipping versions requires an explicit tested migration, not an assumption that intermediate changes are compatible;
- unknown future versions are read-only inspectable at most and cannot attach as a playable world;
- a schema version never changes meaning after release;
- a change to serialization semantics counts as a schema change even if physical column names would stay the same.

Persistent payload kinds may carry their own contract versions. The save schema records how those versions are represented and migrated.

---

## 6. Mandatory migration workflow

Every persistent schema change follows `AI_DEVELOPMENT_RULES_v0.1.md` §24:

```text
schema version change
migration
migration test
save/load regression
recovery consideration
documentation update
```

### 6.1 Migration descriptor

Each migration defines:

```text
MigrationId
SourceSchemaVersion
TargetSchemaVersion
Supported build/content/rules prerequisites
Transformation contract
Stable processing order
Preconditions
Postconditions and integrity checks
Recovery/rollback behavior
Test fixtures
```

### 6.2 Migration execution

Migration uses a recoverable copy or equivalent transactional strategy. It never upgrades the only verified artifact without a rollback source.

Conceptual flow:

```text
Open source in migration mode
-> verify source envelope and SQLite integrity
-> verify exact migration chain and prerequisites
-> create recoverable migration candidate
-> apply migrations in order
-> run structural and domain integrity checks
-> verify target envelope/schema/content identity
-> commit candidate atomically
-> retain source backup according to recovery policy
```

Failure leaves the original save unchanged and reports the failed migration plus stable diagnostic context.

### 6.3 Migration determinism

Migrations:

- use stable ordering for every transformed collection;
- do not use runtime-dependent hashes;
- do not consume gameplay RNG;
- preserve or explicitly version RNG stream state;
- do not allocate an already used ID;
- preserve historical references and pending causal hooks;
- do not infer missing values from the current UI, locale, wall clock, or installed default scenario.

When a migration must create an entity or operational record, it uses the saved allocator state and advances the relevant high-water mark deterministically.

---

## 7. Migration tests and fixtures

Every supported source version has a representative fixture or generator. A migration suite covers:

```text
load source fixture
-> migrate through each supported step
-> validate target schema and domain invariants
-> save target
-> load target again
-> compare gameplay-relevant state
-> continue a deterministic scenario where practical
```

Required cases include:

- smallest valid save;
- normal active career;
- unemployed ManagerCareer;
- pending Decision Requests and scheduler work;
- historical/retired references;
- non-default content/rules composition;
- save near a rules transition;
- corrupted candidate with intact recovery source;
- long-save fixture after compaction when the affected schema touches compactable state.

A migration test failure blocks the schema change. Manual opening of one recent save is not sufficient coverage.

---

## 8. Resolved content identity

A save records the complete `ResolvedContentIdentity` from `CONTENT_FORMAT_v0.1.md`, including:

```text
Resolver contract version
Content schema version(s)
Scenario definition and exact artifact identity
Ordered pack IDs, semantic versions, and cryptographic hashes
Resolved dependency graph/order
Selected content modules by slot
Applied override identities/order
Rules module definitions and contract versions
Aggregate resolved identity/hash
```

The itemized identity is authoritative for diagnostics. The aggregate hash is a quick comparison.

A save does not depend only on a scenario name, pack ID, semantic version, or current installed defaults. Two artifacts with the same PackId/PackVersion and different content hashes are different inputs.

Static content should not be copied wholesale into every save without need. Reproducibility still requires an immutable local artifact cache, a minimal resolved snapshot, or a future accepted combination of both. OQ-CF-005 remains open.

---

## 9. Resolved rules identity

The save records the active `ResolvedRuleset` from `RULESETS_v0.1.md`:

```text
Rules resolution contract version
Selected module identity per slot
RulesContractId and version per module
Effective parameter identities/hashes
Capability set and compatibility result
Aggregate rules identity/hash
```

It also preserves pending effective-dated transitions and the results of applied grandfathering/conversion that can affect future gameplay.

Historical records keep enough effective rule identity to interpret outcomes after later transitions.

Load never replaces saved modules with a newer default preset merely because the calendar year or scenario label matches.

---

## 10. Stable world identity and allocators

`ContentDefinitionId` and `WorldEntityId` remain separate.

The save preserves:

- every live and historically referenced typed identity;
- allocator state or equivalent high-water marks;
- burned identity ranges needed to prove no reuse;
- origin content references where present;
- typed identity category and referential integrity.

Retirement, archival, failed creation after committed allocation, and compaction do not release an ID for reuse.

The exact choice between one global allocator and typed per-entity allocators remains OQ-DM-001. Either choice must use signed 64-bit-compatible world IDs or an accepted equivalent and pass lifetime uniqueness tests.

Loading an older checkpoint restores its recorded allocator state. It never scans for the first visible gap and reuses it.

---

## 11. Minimum authoritative World State

The save must preserve the gameplay-relevant current state defined by implemented domain contracts. At the day-one boundary this includes or references:

```text
World identity and current simulation time
Master seed and versioned RNG state/counters
Resolved content and rules identity
Stable allocator state
Person and career-role identities/lifecycles
ManagerCareer state and employment history
Current Employment records
Organization identities and effective-dated history
DecisionAuthority and AuthorityAssignment records
Knowledge-store ownership and durable KnowledgeRecords
Personal Knowledge and RelationshipMemory
RecruitmentCase state and linked domain obligations
Scheduler work
Pending DecisionRequests
DecisionRecords and HistoricalRecords required by retention policy
Implemented domain state that can affect future gameplay
```

Detailed rider, race, physiology, contract, calendar, economy, sponsor, and equipment state belongs to later domain models. Their save contracts must join this envelope rather than create independent save systems.

### 11.1 AccessContext

`AccessContext` is request-scoped and is not persisted as an authority grant. Load derives a fresh context from current ManagerCareer, Employment, DecisionAuthority, AuthorityAssignment, and permission facts.

A cached context from a former employer cannot restore confidential access after load.

---

## 12. Employment, authority, and knowledge

Save/load preserves:

- active and historical Employment identity, effective dates, status, and organization/manager references;
- ManagerCareer and Person identity across employer changes;
- DecisionAuthority identity and effective AuthorityAssignments;
- OrganizationKnowledge ownership by OrganizationId;
- PersonalKnowledge and RelationshipMemory ownership by PersonId;
- knowledge provenance, confidentiality, portability, confidence, and causal references required by the active model;
- pending organization and personal obligations.

Load validation rejects or quarantines impossible states such as two active primary employments for one ManagerCareer when current rules forbid them, an authority assignment to a missing manager, or organization knowledge owned by the wrong organization.

The save has no `PlayerTeam`, `IsHumanTeam`, or organization subtype that changes domain rules. Human and AI control remains authority data.

---

## 13. Scheduler and ordered commands

The save preserves every scheduler item and ordering field that may affect future simulation:

```text
StableWorkId
SimulationTimestamp
ProcessingPhase
AuthorityAssignedSequence
work kind and payload contract version
owner/target references
lifecycle and idempotency state
causation/correlation references where required
```

Pending or accepted Commands that survive a checkpoint preserve their `CommandId`, versioned payload, authority context references, order/idempotency data, and lifecycle. Processed Commands need only follow their audit/retention contract.

Save occurs at an atomic boundary. It never captures half-applied work or a partially committed Command.

On load, the scheduler rebuilds runtime queues in canonical order:

```text
SimulationTimestamp
-> ProcessingPhase
-> AuthorityAssignedSequence
-> StableWorkId / CommandId tie-break
```

Collection iteration, SQLite row order without an explicit key, and physical insertion order are not business ordering.

---

## 14. DecisionRequest and domain obligations

Every unresolved `DecisionRequest` persists as a domain object, including:

- stable request identity;
- owner or authority scope;
- acting organization where applicable;
- creation time and deadline;
- request kind and related domain references;
- lifecycle status;
- delegated/default resolution policy;
- deterministic ordering fields;
- resolution reference if already resolved at the checkpoint.

NotificationProjection is not enough. Archiving or deleting an Inbox projection cannot remove its source request, offer, case, deadline, registration, or scheduled work.

Load revalidates authority and domain references before exposing legal responses. It does not transfer ownership to the current human merely because the save was opened by a player.

Modal versus Inbox routing remains OQ-UI-001/OQ-GS-001. This save contract does not resolve it.

---

## 15. RNG state

D-013 requires:

```text
same simulation build
+ same resolved content/rules
+ same initial state
+ same ordered commands
= same gameplay result
```

The save therefore preserves or deterministically reconstructs:

- MasterSeed;
- RNG contract/derivation version;
- independent gameplay stream identities;
- stream state, counters, or deterministic derivation position;
- domain scope keys needed for future draws;
- any pending work state that determines when draws occur.

Cosmetic and diagnostic RNG are not gameplay streams. Opening a Query, forecast, Inbox item, settings screen, or World Spy viewer cannot alter saved gameplay RNG.

No persistent seed derives from runtime hash codes. A save/load round trip cannot shift an unrelated RNG domain.

---

## 16. GameState checkpoint

The serialized `GameState` type contains exactly the D-031 values:

```text
MainMenu
NewGameFlow
LoadingWorld
Management
PreSeasonPlanningFlow
RacePreparationFlow
RaceLive
RaceResultsFlow
RaceDebriefFlow
```

It belongs to the application checkpoint envelope, not to Simulation Truth.

### 16.1 Legal saved checkpoints

Not every enum value is a legal world-save checkpoint:

| GameState | Save checkpoint contract |
|---|---|
| `MainMenu` | No attached authoritative world to save. |
| `NewGameFlow` | New-game recipe draft is not a world save. |
| `LoadingWorld` | Never persisted as an attached playable checkpoint. |
| `Management` | Normal save at an atomic boundary. |
| `PreSeasonPlanningFlow` | Conditional stable checkpoint; local draft policy remains open. |
| `RacePreparationFlow` | Conditional before `StartRace`; source of pre-race autosave. |
| `RaceLive` | Never written as a world checkpoint. The verified pre-race autosave remains authoritative. |
| `RaceResultsFlow` | Legal only after official result commit and at a stable checkpoint. |
| `RaceDebriefFlow` | Legal at a stable checkpoint; pending obligations remain domain objects. |

A load validates the saved enum and its domain guards. It does not infer GameState from a Godot scene or visible screen.

### 16.2 Scheduler runtime is not saved state

Values such as:

```text
idle
advancing
paused at deterministic barrier
loading progress
render paused
```

are runtime or presentation status. They are not extra GameState values and are not World State.

The durable causes of a pause do persist: scheduler queue, pending DecisionRequest, current simulation time, and completed atomic work. Load reconstructs runtime status from those facts.

---

## 17. Card Flow draft persistence

OQ-GS-002 remains open: local Card Flow drafts may be serialized or discarded with explicit confirmation.

This document locks only the boundary:

- a local draft is not authoritative domain state;
- a saved checkpoint cannot present an uncommitted draft as committed world data;
- Confirm issues legal Application Commands at an atomic boundary;
- pending domain obligations persist independently of the open card;
- draft policy must be consistent for human and future hotseat authorities;
- any serialized draft needs its own version and migration policy.

No choice between "persist draft" and "discard with confirmation" is made here.

---

## 18. RaceLive and pre-race autosave

`RaceLive` covers one stage or race day and has no mid-race save (D-008).

Required entry sequence:

```text
GameState = RacePreparationFlow
-> reach stable pre-race boundary
-> create pre-race autosave candidate
-> verify SQLite, envelope, content identity, and domain integrity
-> commit/rotate verified pre-race autosave
-> accept StartRace
-> enter RaceLive and create transient race state
```

If save creation or verification fails, `StartRace` fails, gameplay race RNG is not started, and GameState remains `RacePreparationFlow`.

During RaceLive:

- manual Save and Load are rejected;
- no live simulation snapshot replaces the pre-race artifact;
- pausing does not create a persisted world state;
- exit or crash discards transient live state;
- Continue/Load restores the verified pre-race autosave through `LoadingWorld`.

The watched and headless paths use the same canonical race model. Presentation attachment does not change the save or simulation contract.

Hotseat RaceLive pause/checkpoint ownership remains open. This document does not add a multiplayer save protocol.

---

## 19. Save operation

A normal save starts only at a legal GameState and atomic domain boundary.

Conceptual flow:

```text
Request save
-> validate GameState and atomic boundary
-> establish one coherent authoritative checkpoint
-> write candidate through a transaction
-> persist envelope, World State, identities, ordering, and obligations
-> run SQLite and domain integrity checks
-> finalize candidate and durability boundary
-> replace/rotate target through a recoverable operation
-> report committed SaveId/checkpoint identity
```

The application does not report success before the committed artifact passes required checks.

Player-visible metadata such as save name, last played time, screenshot, or summary may be stored separately or inside metadata. Its failure cannot corrupt the authoritative checkpoint.

### 19.1 Concurrent input

The save captures a stable checkpoint. New Commands are either:

- completed before the checkpoint;
- ordered after it;
- rejected/deferred while the checkpoint is established.

No Command appears half before and half after the saved boundary.

---

## 20. Load operation

Load occurs through `LoadingWorld` and attaches the world atomically.

```text
Select artifact
-> open with safe SQLite configuration
-> read envelope without attaching world
-> validate file/integrity and supported schema
-> resolve exact content/rules identity
-> choose exact migration chain if needed
-> migrate a recoverable candidate if needed
-> validate references, allocators, scheduler, authority, and domain invariants
-> reconstruct gameplay RNG and runtime indexes
-> derive fresh AccessContext
-> validate legal saved GameState
-> attach complete world atomically
```

Failure returns to `MainMenu` and keeps the source save unchanged. No partial world becomes queryable.

Runtime caches and read models may rebuild after validation. Rebuild order and output cannot change authoritative state or consume gameplay RNG.

---

## 21. Atomicity, backups, and recovery

Persistence uses SQLite transactions plus a file replacement/rotation strategy appropriate to the target filesystem. The implementation must test actual Windows behavior before claiming crash safety.

Recovery artifacts may include:

- last verified manual save;
- rotating autosaves;
- pre-race autosave;
- seasonal archive autosave;
- migration source backup;
- incomplete candidate marked as uncommitted.

Recovery rules:

- a candidate never outranks a verified committed artifact solely because its timestamp is newer;
- each candidate carries commit/integrity metadata;
- failed migration retains its source and diagnostic report;
- automatic recovery selection is deterministic and explained to the user;
- recovery does not substitute different content/rules;
- loading a backup preserves its own allocator and world state exactly.

Exact rotation counts and disk-space policy remain open until implementation and soak measurements.

---

## 22. Integrity checks

Integrity validation has layers.

### 22.1 File and envelope

- expected save-format marker;
- readable SQLite file and supported SQLite features;
- successful SQLite integrity checks appropriate to the operation;
- supported `SchemaVersion`;
- complete commit marker/checkpoint identity;
- recognized simulation/determinism/RNG contracts.

### 22.2 Content and rules

- exact pack IDs, versions, and cryptographic hashes available;
- dependency and override order matches;
- supported content/rules contract versions;
- active and pending rule modules resolve;
- no silent default substitution.

### 22.3 Identity and references

- typed IDs are valid and unique in their contract;
- allocator high-water state cannot issue an already used ID;
- all required current references resolve;
- historical references remain renderable through durable identity/effective history;
- origin definitions have the declared identity where required.

### 22.4 Domain invariants

- employment and authority assignments are internally valid;
- knowledge records keep legal owner scope and provenance;
- pending Decision Requests have owners, lifecycle, and domain references;
- scheduler ordering keys are complete and unambiguous;
- idempotency state prevents duplicate future effects;
- saved GameState is legal for the checkpoint;
- RNG streams/counters match the recorded contract.

### 22.5 Causal integrity

Unresolved contracts, delayed investigations, promises, sponsor consequences, rule transitions, scheduler work, and other future hooks cannot disappear through save, migration, or compaction.

Integrity checks report stable issue codes and affected IDs. A repair tool may be designed later, but load cannot invent gameplay state to hide corruption.

---

## 23. Content identity mismatch

Mismatch classification occurs before world attachment.

| Outcome | Meaning | Load behavior |
|---|---|---|
| Exact match | Every required artifact identity and contract matches | Continue normal validation. |
| Compatible schema migration | Exact source is available and a tested migration produces supported target identity/state | Migrate a candidate, verify, then attach. |
| Artifact recoverable | Recorded immutable pack exists in local cache/backup but is not active | Offer/use the exact artifact under recovery policy. |
| Missing content | Required artifact is unavailable | Do not attach; name missing pack/version/hash and recovery options. |
| Hash mismatch | ID/version exists but bytes differ | Treat as different content; never accept silently. |
| Unsupported contract | Build cannot interpret recorded content/rules contract | Reject with required version/build information. |
| Incompatible replacement | Proposed pack cannot preserve definitions/references/semantics | Reject; do not remap by names. |

A user may later choose an explicit conversion workflow, but it must produce a new migrated artifact with diagnostics and backup. A confirmation dialog alone is not a migration.

---

## 24. Long-save and compaction boundary

This document does not duplicate HOT/WARM/COLD categories, retention targets, archive selection, knowledge compaction, or size thresholds. Those remain canonical in `LONG_SAVE_AND_PERFORMANCE_v0.2.md`.

Save-format requirements derived from D-007 and D-015 are:

- compacted records retain stable historical identity and valid references;
- no compacted ID becomes allocatable again;
- future causal hooks survive in full or equivalent summarized state;
- compacted and uncompacted branches continue to the same gameplay-relevant future under identical build/content/commands;
- schema migrations work for both active and compacted representations;
- integrity checks cover archive/current boundaries;
- 100-year soak tests measure file size, load/save time, database integrity, growth, and deterministic checksum.

Compaction runs as an explicit pipeline at deterministic safe boundaries. It is not a collection of incidental deletes during UI Queries.

---

## 25. Event, history, and notification retention

The save does not keep every transient event forever. Each event-family contract has its own persistence policy.

Minimum boundary:

- Authoritative current state persists directly.
- Pending `ScheduledWork`, Commands that survive the checkpoint, and unresolved Decision Requests persist.
- `DomainEvent` retention follows audit, reaction, information, and history requirements; it is not the sole world source.
- `ObservationSignal` or resulting Knowledge needed for current/future behavior persists according to knowledge policy.
- `DecisionRecord` retains actor, authority, action, and legal decision-time basis required by audit/causality.
- `HistoricalRecord` retains structured outcomes and effective-dated identity.
- `NotificationProjection` may be rebuilt or compacted; its source obligation cannot be lost.

OQ-DM-005 remains open for the permanent versus compactable detail inside Decision Records.

---

## 26. World Spy and diagnostics

World Spy and Race Spy are observational diagnostics.

They:

- never become authoritative input on load;
- never grant Knowledge or AccessContext permissions;
- never alter RNG state, scheduler order, or checksums of gameplay state;
- may be stored or exported separately under bounded retention;
- may link to durable DecisionRecord, Command, DomainEvent, and HistoricalRecord IDs;
- may be absent without making a normal save unplayable.

Developer truth fields cannot be placed in ordinary query projections or knowledge stores for convenience. Player-facing Why remains knowledge-bounded.

Save/load tests compare Spy OFF and Spy DECISIONS for identical gameplay state.

---

## 27. SQLite safety boundary

A save file is untrusted input until validated. The persistence layer:

- opens only the selected save/candidate paths managed by the application;
- does not load SQLite extensions from a save;
- does not execute arbitrary SQL, triggers, scripts, or external commands supplied as content;
- applies resource limits and cancellation to inspection/migration where practical;
- does not expose raw database handles to Godot UI or mods;
- validates strings, blobs, counts, enum values, dates, and payload versions before domain attachment;
- keeps backup and candidate paths inside validated save locations;
- logs failures without exposing private filesystem or credential data in normal UI.

Mods change worlds through validated content and rules, not direct save writes.

---

## 28. Save compatibility policy

Compatibility decisions use four independent identities:

```text
Save schema version
Simulation build/determinism contract
Resolved content identity
Resolved rules identity
```

Matching only one is insufficient.

Supported paths:

- same build/contracts and exact content/rules: normal load;
- supported newer build with explicit save/content/rules migration: migrate candidate and verify;
- unsupported newer/future schema: reject without modification;
- changed content under same visible version: mismatch;
- missing exact content without accepted snapshot/cache: recoverable blocker, not automatic substitution.

The project may later define a supported-version window. Removing a migration path requires an owner decision, release policy, and clear archival/export consequence.

---

## 29. Failure behavior

| Failure | Required result |
|---|---|
| Save requested outside a legal boundary | Reject without changing artifact or world. |
| Candidate write/commit fails | Preserve last verified save and report failure. |
| SQLite integrity fails | Do not attach; offer verified recovery artifacts if available. |
| Schema unknown or migration chain missing | Do not modify source; state required version/path. |
| Migration step fails | Discard/quarantine candidate, retain source backup and diagnostics. |
| Content/rules identity mismatch | Do not substitute defaults; report exact differences. |
| Reference or allocator integrity fails | Do not attach a partially repaired world. |
| Pre-race autosave fails | Stay in RacePreparationFlow and do not start race RNG. |
| Runtime crash during RaceLive | Restore from verified pre-race autosave on next load. |
| Derived cache rebuild fails | Keep authoritative source intact; fail load or rebuild according to cache criticality. |

No failure handler invents a manager employment, authority, knowledge record, race result, or scheduler decision to make the save appear valid.

---

## 30. Locked decisions

| Decision | Save consequence |
|---|---|
| D-002 | Human and AI organizations share one schema and world rules. |
| D-004 | Save follows ManagerCareer across employers; there is no permanent player team. |
| D-005 | ManagerCareer and DecisionAuthority persist as separate identities. |
| D-007 | Stable WorldEntityIds and allocator state never permit reuse. |
| D-008 | RaceLive has no mid-race snapshot; entry requires a verified pre-race autosave. |
| D-009 | Organization Knowledge remains owned by the organization after job changes and load. |
| D-013 | Build, resolved content/rules, full state including RNG/order, and ordered Commands define reproducibility. |
| D-014 | Saving/loading or rebuilding Queries/forecasts cannot consume gameplay RNG or reveal truth. |
| D-015 | Migration and compaction preserve future gameplay causality. |
| D-024/D-027 | Developer truth and normal knowledge/UI remain separate in persistence. |
| D-025 | Durable decision IDs may link to diagnostics; Spy remains non-causal. |
| D-031 | Saved GameState uses exactly nine values; scheduler pause/status is runtime. |

---

## 31. Open questions

| ID | Question | Decision deadline |
|---|---|---|
| OQ-SF-001 / OQ-DM-001 | One global WorldEntityId allocator or typed per-entity allocators with equivalent no-reuse guarantees | Before persistence implementation |
| OQ-SF-002 / OQ-CF-005 | Immutable shared content cache, minimal snapshot per save, or a defined hybrid | Before SAVE_FORMAT acceptance |
| OQ-SF-003 / OQ-GS-002 | Serialize local Card Flow drafts or discard them with explicit confirmation | Before Card Flow save implementation |
| OQ-SF-004 / OQ-DM-005 | Permanent DecisionRecord core versus compactable diagnostic detail | Before DecisionRecord persistence schema |
| OQ-SF-005 | Exact SimulationBuildIdentity and supported cross-build load window | Before first public save version |
| OQ-SF-006 | Autosave/backup rotation counts, disk budget, and seasonal archive policy | Before save UI implementation |
| OQ-SF-007 | Which query/read-model caches are persisted versus rebuilt | Before performance schema design |
| OQ-SF-008 | Save encryption, compression, and anti-tamper policy | Before distribution requirements demand them |

OQ-UI-001 and hotseat RaceLive remain open and are not changed by this document.

---

## 32. Deferred

- production tables, indexes, constraints, and query plans;
- domain storage for full rider/race/physiology/training/contracts/economy systems;
- exact archive and compaction representation;
- cloud saves and cross-device conflicts;
- hotseat and online multiplayer checkpoint protocols;
- save editor, export API, and mod repair tools;
- encryption, signing, compression, and anti-tamper;
- platform suspend/resume behavior;
- exact content cache/snapshot implementation;
- user-facing migration and recovery screen layouts.

---

## 33. Non-goals

- a complete production SQLite schema or ready-to-run table definitions;
- event sourcing as the only source of World State;
- direct SQLite access from Godot UI, AI logic, or mods;
- `PlayerTeam`, `IsHumanTeam`, or a separate human save branch;
- `GlobalRandom`, runtime hash seed derivation, or UI-driven RNG state;
- Godot nodes as persistence/domain truth;
- mid-race save or RaceLive snapshot;
- scheduler `idle`, `advancing`, or pause as World State;
- Inbox or modal state as DecisionRequest persistence;
- World Spy truth in normal save Queries or Knowledge;
- silent schema migration, content substitution, or repair;
- duplication of the full HOT/WARM/COLD and compaction design.

---

## 34. Implementation notes

- Keep persistence behind Application/domain contracts. Godot uses Commands and Queries.
- Use exact numeric representations for IDs, money, counters, dates, and ticks.
- Define explicit ordering for every persistence query that feeds gameplay.
- Keep schema migration code isolated from gameplay Commands and gameplay RNG.
- Include stable issue codes, schema/content/rules identities, and relevant entity IDs in recovery diagnostics.
- Build indexes from measured query patterns and 100-year profiling, not from speculative table design in this document.
- Keep source backups until a migrated candidate has passed target load and integrity checks.
- Document each future domain's durable state, derived caches, history, and causal hooks before changing the save schema.

---

## 35. Migration impact

This DRAFT creates no SQLite file and changes no implemented save. The first implementation establishes `SchemaVersion = 1` through a separate reviewed task.

Every later change states:

- source and target schema versions;
- exact transformed contracts and identities;
- content/rules compatibility impact;
- treatment of active, historical, pending, and compacted state;
- RNG and deterministic ordering impact;
- backup/recovery behavior;
- migration fixtures and save/load regression results.

---

## 36. Test criteria

### Round trip and identity

- Save and load at the same build/content/rules produce equivalent gameplay-relevant World State.
- Every typed ID, reference, allocator high-water mark, Employment, AuthorityAssignment, and knowledge owner survives round trip.
- Retired/archived identities remain referenced and cannot be reallocated.
- A job change followed by save/load preserves ManagerCareer identity and blocks former-employer confidential access.
- Fresh AccessContext derivation after load matches current employment/authority.

### Scheduler, decisions, and RNG

- Pending ScheduledWork retains canonical ordering and executes once after load.
- Pending Decision Requests retain owner, deadline, default/delegation policy, and domain references.
- Notification read/archive state cannot remove a source obligation.
- Gameplay RNG stream states/counters survive or reconstruct without shifting unrelated domains.
- Saving, loading, querying, forecasting, and Spy mode do not consume gameplay RNG.
- Spy OFF and Spy DECISIONS produce identical gameplay state/checksums.

### GameState and race recovery

- The serialized GameState enum contains exactly the nine D-031 values.
- No save artifact can attach with `LoadingWorld` or a mid-race `RaceLive` checkpoint.
- RaceLive entry is rejected until a verified pre-race autosave commits.
- Exit/crash during RaceLive restores the pre-race state, not transient race progress.
- Scheduler runtime pause/progress is reconstructed from durable causes and is not stored as World State.
- Card Flow draft behavior stays behind OQ-GS-002 until accepted.

### Migration

- Every supported schema edge has a fixture, migration, integrity check, and target round trip.
- A failed migration leaves the source byte-for-byte available and does not attach a partial world.
- Migration order is deterministic and does not consume gameplay RNG.
- Migration cannot reuse IDs or lose historical references/pending causal hooks.
- Compact and uncompacted fixtures both migrate and continue correctly.

### Content and recovery

- Exact content/rules artifacts load normally.
- Same PackId/PackVersion with a different hash fails as a mismatch.
- Missing pack, unsupported contract, and incompatible replacement produce distinct diagnostics.
- The loader never chooses current default content/rules for an existing save.
- A corrupt newest candidate does not outrank an older verified artifact.
- Pre-race, manual, autosave, seasonal, and migration backups remain distinguishable by purpose and checkpoint.

### Long save

- A 100-year soak measures file size, growth, save/load latency, integrity, ID uniqueness, scheduler size, and historical query latency according to `LONG_SAVE_AND_PERFORMANCE_v0.2.md`.
- Compact and uncompacted branches produce the same gameplay-relevant future for the same build/content/commands.
- No historical reference breaks after organization rename, retirement, archival, migration, or compaction.
