# Peloton Manager: Minimal Data Model

**Title:** Minimal Data Model

**Version:** 0.1

**Status:** DRAFT

**Purpose:** Define the day-one domain identities, ownership boundaries, knowledge model, recruitment case, and event references needed before the architecture skeleton.

**Authority/Owner:** Project owner (domain and simulation architecture)

**Supersedes:** none

**Superseded by:** none

**Last reviewed:** 2026-08-25

**Related decisions/ADRs:** D-002, D-003, D-004, D-005, D-007, D-009, D-010, D-013, D-014, D-015, D-024, D-025, D-027, D-031

---

## 1. Purpose and scope

This document defines the smallest durable domain model needed to start the architecture skeleton without guessing identity, authority, or information ownership.

It is a contract model, not a database schema. Field groups describe meaning and ownership. Future implementation may split or combine storage records while preserving these contracts.

### In scope

- `Person` and career-role identity;
- `ManagerCareer` and employment;
- `Organization` without Human/AI subtypes;
- `DecisionAuthority` and its assignment;
- request-scoped `AccessContext`;
- Public, Organization, and Personal Knowledge;
- `RelationshipMemory`;
- `RecruitmentCase` and dossier projection;
- `ContentDefinitionId` versus `WorldEntityId`;
- identities and minimum semantics for Commands, scheduler work, events, observations, decisions, history, and notifications;
- boundaries needed by deterministic save/load and World Spy.

### Out of scope

- SQLite tables, indexes, migrations, and serialization layout;
- full rider physiology, race state, training, health, or development fields;
- detailed staff, contract, sponsor, finance, equipment, and calendar schemas;
- content JSON schema and rules-module manifests;
- long-save compaction algorithms and retention thresholds;
- Godot nodes, UI view models, and scene ownership;
- online multiplayer authority implementation.

---

## 2. Core invariants

1. The player is a `ManagerCareer`, never a team.
2. `ManagerCareer`, employment, and `DecisionAuthority` are separate concepts.
3. Organizations have one domain type regardless of human or AI input.
4. Simulation Truth is not actor Knowledge.
5. Normal Queries are filtered through `AccessContext`.
6. AI and human authorities issue the same Application Commands.
7. Stable world identities are never reused in a save.
8. `ContentDefinitionId` identifies content; `WorldEntityId` identifies an instance in one save.
9. Queries and forecasts do not mutate World State or consume gameplay RNG.
10. Each event-family contract has its own identity and lifecycle. There is no universal Event DTO.
11. World Spy may inspect debug truth but cannot feed normal UI, AI, or knowledge stores.
12. Compaction may change representation, not future causality.

---

## 3. Minimal relationship map

```text
WorldState
├── Person
│   ├── RiderCareer        [thin identity only in v0.1]
│   ├── StaffCareer        [thin identity only in v0.1]
│   └── ManagerCareer
│       ├── Employment?
│       ├── PersonalKnowledgeStore
│       └── RelationshipMemory
├── Organization
│   ├── OrganizationKnowledgeStore
│   ├── ManagerEmployment?
│   └── RecruitmentCase*
├── DecisionAuthority
│   └── AuthorityAssignment*
├── Scheduler records
├── Command and event-family records
└── ResolvedContentIdentity

Request boundary
└── AccessContext
    ├── ViewerPersonId?
    ├── CurrentOrganizationId?
    ├── DecisionAuthorityId?
    └── PermissionScope
```

The arrows describe references and ownership. They do not prescribe object nesting or SQL foreign-key layout.

---

## 4. Identity taxonomy

### 4.1 Content and world identity

| Identity | Meaning | Stability |
|---|---|---|
| `ContentDefinitionId` | Namespaced string for a definition, for example `race.tour_de_france` | Stable across compatible content versions according to content policy |
| `WorldEntityId` | Signed 64-bit, save-local identity for a created world entity | Never reused for the lifetime of the save |
| `PersonId` | Typed world identity for one person | Survives role and employer changes |
| `RiderCareerId` | Identity of a rider-career record | Separate from PersonId |
| `StaffCareerId` | Identity of a staff-career record | Separate from PersonId |
| `ManagerCareerId` | Identity of a manager-career record | Separate from PersonId |
| `OrganizationId` | Identity of one organization | Survives renames, sponsor changes, and authority changes |

An entity created from content may keep an `OriginDefinitionId`. The content ID does not replace the world identity.

Example:

```text
ContentDefinitionId = organization.vistula_racing
OrganizationId = 1042 in this save
```

### 4.2 Operational identities

The following are typed, stable, and never reused within their own contract:

```text
EmploymentId
DecisionAuthorityId
AuthorityAssignmentId
OrganizationKnowledgeStoreId
PersonalKnowledgeStoreId
KnowledgeRecordId
RelationshipMemoryId
RecruitmentCaseId
StableWorkId
CommandId
DomainEventId
ObservationSignalId
DecisionRequestId
DecisionRecordId
HistoricalRecordId
NotificationProjectionId
DecisionTraceId          // diagnostic identity, not domain truth
```

The exact allocator layout, including global versus per-type sequences, belongs to `SAVE_FORMAT.md`. It must preserve typed identity, stable ordering where required, and the no-reuse invariant.

### 4.3 Identity rules

- Names and localized text are not identities.
- Runtime hashes are not persistent IDs or seed material.
- Deleting or compacting active data does not release an ID.
- References to retired people and renamed organizations remain valid.
- Typed IDs prevent a Person, Organization, Command, and Event with the same numeric value from being confused.
- Canonical business ordering uses the explicit scheduler/command key, not an incidental EntityId sort.

---

## 5. WorldState root

`WorldState` is the authoritative current state. It is saved explicitly; the project does not use event sourcing as the only source of truth.

The root must provide or reference:

```text
World identity
Current simulation time
MasterSeed and deterministic RNG contract/version
ResolvedContentIdentity
Simulation/content schema versions
Stable identity allocators or allocator state
Active people, careers, organizations, employments, and authorities
Scheduler queue and persistent DecisionRequests
Domain state owned by later system models
```

The active `GameState` belongs to the Application/session boundary defined in `GAME_STATES_v0.1.md`. Scheduler processing status is runtime and is not another field that changes world semantics.

---

## 6. Person and career roles

### 6.1 Person

`Person` is the stable historical identity of a human in the world.

Minimum contract:

```text
Person
    PersonId
    OriginDefinitionId?
    identity/profile references
    birth/death or lifecycle dates where applicable
    lifecycle status
```

Detailed demographic, physiological, personality, and localization fields belong to later system/content documents.

### 6.2 Career-role records

A person may have role records over time:

```text
Person
├── RiderCareer
├── StaffCareer
└── ManagerCareer
```

Career IDs do not have to equal `PersonId`. A retired rider who later becomes staff keeps the same PersonId and receives the appropriate career record.

Whether roles may overlap is a Rules decision. The identity model does not assume that a person can have only one role during their lifetime.

### 6.3 ManagerCareer

Minimum contract:

```text
ManagerCareer
    ManagerCareerId
    PersonId
    lifecycle status
    traits and skills references/state
    reputation state
    active EmploymentId?          // derived/indexed from Employment lifecycle
    employment history references
    PersonalKnowledgeStoreId
    RelationshipMemory references
```

`ManagerCareer` does not contain a `PlayerTeam` flag and does not become a different type when controlled by a human.

---

## 7. Employment

`Employment` records the effective relationship between a ManagerCareer and an Organization.

Minimum contract:

```text
Employment
    EmploymentId
    ManagerCareerId
    OrganizationId
    role/position
    effective start
    effective end?
    status
    contract reference?
    termination reason?
```

Invariants:

- A ManagerCareer has zero or one active primary manager employment.
- No active employment means unemployed, not a different manager/world type.
- An Organization may have a vacancy or an explicit acting/interim role.
- Ending employment does not delete its historical record.
- Accepting new employment ends or supersedes the previous active employment according to Rules.
- Staff and riders do not follow a manager automatically. Their movement uses normal market and contract systems.
- Employment does not grant permanent ownership of the organization's confidential knowledge.

The future contract system may generalize employment terms. This document locks the identity and lifecycle boundary only.

---

## 8. Organization

`Organization` represents a team or other organization participating in the world.

Minimum contract:

```text
Organization
    OrganizationId
    OriginDefinitionId?
    lifecycle status
    effective-dated identity/branding references
    organization identity and strategy references
    active manager EmploymentId?  // derived/indexed from Employment lifecycle
    OrganizationKnowledgeStoreId
```

`Employment` is the system of record for the relationship. If active-employment references are stored on ManagerCareer or Organization for efficient lookup, validation must keep them consistent with that lifecycle.

The organization has no `IsPlayerTeam`, `HumanTeam`, or `AITeam` subtype. Human/AI control comes from authority assignment.

Organization identity survives:

- sponsor and name changes;
- manager dismissal or resignation;
- periods without a permanent manager;
- the human manager leaving;
- AI or human authority reassignment.

Effective-dated branding/history must allow historical results to display the correct period identity. The detailed structure is deferred to later Organization and Save documents.

---

## 9. DecisionAuthority and assignment

### 9.1 DecisionAuthority

`DecisionAuthority` identifies the source that supplies decisions. It does not identify the manager or employer.

```text
DecisionAuthority
    DecisionAuthorityId
    kind = HumanInput | AIInput | RemoteHuman [future]
    lifecycle status
```

All authority kinds submit the same Application Commands and pay the same domain costs.

### 9.2 AuthorityAssignment

An effective-dated assignment binds authority to a decision scope.

```text
AuthorityAssignment
    AuthorityAssignmentId
    DecisionAuthorityId
    ManagerCareerId?
    ActingOrganizationId?
    PermissionScope
    effective start
    effective end?
    status
```

The optional manager supports vacancies and explicit interim/organizational automation. It does not permit player-only shortcuts.

Command validation checks the current assignment, requested acting organization, manager employment where required, and domain-specific permission.

Changing employment and changing authority are separate operations even when one workflow performs both atomically.

---

## 10. AccessContext

`AccessContext` is a request-scoped value used by Queries, forecasts, and command validation.

```text
AccessContext
    ViewerPersonId?
    CurrentOrganizationId?
    DecisionAuthorityId?
    PermissionScope
```

It is derived from current world/session facts and validated authority assignments. It is not an alternate store of truth and must not preserve stale employer access.

### 10.1 Query rules

- Public Queries work without an organization.
- An employed manager may receive current-employer Organization Knowledge allowed by scope.
- An unemployed manager receives public and Personal Knowledge only.
- After a job change, a new context points to the new organization. The old confidential store is no longer queryable.
- Spectator/debug scope is explicit and cannot be passed to ordinary player UI or AI decision code.
- Forecasts use the same context and remain read-only and RNG-neutral.

### 10.2 Command rules

`CommandEnvelope` records issuer, acting organization, and authority context. The Application layer revalidates these values against World State. A client cannot grant itself permission by constructing a broader context.

---

## 11. Knowledge ownership and provenance

The canonical information path is:

```text
Simulation Truth / DomainEvent
-> publication and observation rules
-> ObservationSignal
-> Public, Organization, or Personal Knowledge
-> interpretation / forecast
-> human or AI decision
```

### 11.1 Knowledge scopes

| Scope | Owner | Typical content | Portability |
|---|---|---|---|
| Public Knowledge/Evidence | Public world | official results, published contracts, rankings, visible events | Available according to publication/time rules |
| `OrganizationKnowledgeStore` | One OrganizationId | scouting, medical, recruitment, internal reports, rival assessments | Stays with the organization |
| `PersonalKnowledgeStore` | One PersonId | personal observations and memories acquired legally | Follows the person according to explicit portability |
| `RelationshipMemory` | One PersonId | effective-dated relationship experiences and summaries | Personal unless a separate signal makes it organizational/public |

### 11.2 KnowledgeRecord

Minimum semantics:

```text
KnowledgeRecord
    KnowledgeRecordId
    owner scope and owner ID
    subject reference
    kind = Fact | Observation | Interpretation
    source reference/type
    known-by reference(s)
    observed/effective time?
    acquired time
    confidence
    confidentiality
    portability
    staleness/expiry semantics
    evidence/derived-from references
    superseded-by reference?
```

The record stores what the actor knows or believes, not an unrestricted pointer that allows decision code to read hidden subject state.

### 11.3 Portability rules

- Organization Knowledge is not copied when a manager or staff member leaves.
- Portable Personal Knowledge must already exist in the person's store or be created by an explicit legal conversion/summarization rule.
- `Confidential` does not become `Personal` merely because a person viewed it at work.
- A personal memory may preserve a relationship or experience without cloning the former employer's database.
- Knowledge provenance survives long enough to explain important decisions and confidence.

### 11.4 Lazy creation

Knowledge subjects and records are created after a real source exists, such as scouting, public evidence, direct interaction, staff input, or agent contact. The model does not create every Organization by every Person combination in advance.

Detailed retention and compaction belong to `LONG_SAVE_AND_PERFORMANCE_v0.2.md`.

---

## 12. RelationshipMemory

`RelationshipMemory` is personal, effective-dated memory that may influence later behavior.

Minimum contract:

```text
RelationshipMemory
    RelationshipMemoryId
    owner PersonId
    subject PersonId or OrganizationId
    memory kind
    created/effective time
    strength/confidence where applicable
    source event/interaction references
    confidentiality and portability
    active summary or causal hook
```

It is not a full transcript of every interaction. Later systems decide which experiences have causal or historical value.

---

## 13. RecruitmentCase and dossier

`RecruitmentCase` is the organization's durable case for investigating or pursuing one subject. A dossier is a Query projection built from the case and knowledge visible to the AccessContext.

Minimum contract:

```text
RecruitmentCase
    RecruitmentCaseId
    owner OrganizationId
    subject PersonId
    opened at
    status
    priority
    responsible PersonId/StaffCareerId?
    next review/deadline?
    linked KnowledgeRecordIds
    linked agent-contact references
    linked negotiation references
    resolution/outcome?
```

Invariants:

- Dossier completeness is not an XP bar or mandatory 100 percent gate.
- The case never exposes rival private market state unless a legal signal created that knowledge.
- Agent statements become sourced information, not Simulation Truth.
- If Rules allow direct agent contact without a case, `ContactAgent` may create a minimal case automatically.
- Closing a case does not delete signed contracts, decisions, or historically meaningful outcomes.
- Inbox items reference the case; they are not the system of record for its deadline or status.

---

## 14. Scheduler, command, and event-family contracts

These records share correlation and stable-reference conventions, but they are separate contracts. Implementations must not collapse them into one generic Event DTO.

### 14.1 ScheduledWork

```text
ScheduledWork
    StableWorkId
    SimulationTimestamp
    ProcessingPhase
    AuthorityAssignedSequence
    work kind and versioned payload/reference
    owner/target references
    lifecycle status
    idempotency state/key
```

Canonical ordering is:

```text
SimulationTimestamp
-> ProcessingPhase
-> AuthorityAssignedSequence
-> StableWorkId / CommandId tie-break
```

### 14.2 CommandEnvelope

```text
CommandEnvelope
    CommandId
    RequestedAtSimulationTime
    IssuerPersonId?
    ActingOrganizationId?
    DecisionAuthorityId
    Permission/authority context reference
    command kind and versioned payload
    idempotency key?
    correlation/causation references
```

Commands express intent. They do not prove permission and do not mutate domain entities directly from UI or AI code.

### 14.3 DomainEvent

```text
DomainEvent
    DomainEventId
    OccurredAtSimulationTime
    domain and event kind
    versioned payload
    related entity references
    causation/correlation references
```

A Domain Event states that authoritative World State changed. It is not automatically actor knowledge or a notification.

### 14.4 ObservationSignal

```text
ObservationSignal
    ObservationSignalId
    CreatedAtSimulationTime
    subject/evidence reference
    source DomainEvent or observation source
    publication/recipient scope
    signal kind and versioned payload
    causation/correlation references
```

Publication/observation rules decide whether a signal creates or updates Knowledge for a recipient.

### 14.5 DecisionRequest

```text
DecisionRequest
    DecisionRequestId
    owner DecisionAuthorityId or explicit authority scope
    acting OrganizationId?
    created time
    deadline?
    request kind and related domain references
    lifecycle status
    delegated/default resolution policy
    deterministic ordering fields
    resolved DecisionRecordId?
```

The request may pause processing only at a deterministic barrier. Presentation as modal or Inbox remains separate.

### 14.6 DecisionRecord

```text
DecisionRecord
    DecisionRecordId
    DecisionRequestId?
    decision time
    ActorPersonId?
    ActingOrganizationId?
    DecisionAuthorityId
    chosen action / emitted CommandIds
    actor-known input or knowledge snapshot references
    outcome/reason codes appropriate to the domain
    related DecisionTraceId?
```

`DecisionRecord` is domain/audit history. It preserves who decided and on what legal information basis. The richer developer `DecisionTrace` remains diagnostic.

### 14.7 HistoricalRecord

```text
HistoricalRecord
    HistoricalRecordId
    effective/occurred time
    historical kind
    involved entity references
    structured outcome
    effective-dated identity/branding references where needed
    source event/decision references
```

Historical prose is projected from structured records unless a later policy preserves a specific text snapshot.

### 14.8 NotificationProjection

```text
NotificationProjection
    NotificationProjectionId
    viewer/recipient scope
    source case/request/event references
    created time
    presentation category
    read/archive presentation state
```

Notifications may be rebuilt or compacted according to later policy. Reading or archiving one cannot cancel its source deadline, offer, case, or Decision Request.

---

## 15. DecisionTrace and World Spy boundary

`DecisionTrace` is the cross-system developer diagnostic defined by `WORLD_SPY_AND_DECISION_TRACING_v0.1.md`.

It may link:

```text
DecisionRecord
-> CommandEnvelope
-> DomainEvent
-> ObservationSignal
-> later DecisionRecord / HistoricalRecord
```

Hard boundary:

- Spy never mutates World State.
- Spy never consumes gameplay RNG or changes ordering.
- Debug truth may be stored/read by developer tooling only.
- Normal Queries and AI decision code cannot read `SimulationTruthContext` from traces.
- Player-facing Why uses actor-legal Knowledge from the relevant AccessContext.
- Trace retention does not turn debug output into domain causality.

---

## 16. Persistence and long-save boundary

This model establishes durable identities and causal references. It does not define storage tables.

The future save design must preserve:

- D-007: a used stable entity ID is never assigned again;
- D-015: compaction cannot change future gameplay;
- resolved content identity and schema versions;
- current authoritative World State;
- scheduler work and unresolved domain obligations;
- enough actor-known context to audit important decisions;
- referential integrity after retirement, renaming, and archival.

HOT/WARM/COLD policy, detailed retention, archive snapshots, size targets, and the compaction pipeline remain owned by `LONG_SAVE_AND_PERFORMANCE_v0.2.md`. This document does not duplicate them.

Any future persistent schema change requires a schema version, migration, migration test, save/load regression, and recovery consideration.

---

## 17. Commands, Queries, and forecasts

### Commands

UI, AI, and future remote input submit `CommandEnvelope` objects to Application. Rules validate identity, authority, state, costs, deadlines, and domain invariants before mutation.

### Queries

Queries read projections through `AccessContext`. They cannot expose an unrestricted domain entity when that would bypass knowledge filtering.

### Forecasts

Forecasts:

- use only Knowledge available to the AccessContext;
- return ranges/confidence where appropriate;
- do not mutate World State;
- do not consume gameplay RNG;
- do not run the hidden true future as a preview;
- return the same underlying result for the same input state/context; presentation formatting may differ without changing the forecast.

---

## 18. Cross-model lifecycle examples

### 18.1 Manager changes employer

```text
AcceptManagerContract Command
-> old Employment ends
-> new Employment becomes active
-> authority assignment updates as required
-> Domain Events are emitted
-> new AccessContext is derived
-> former OrganizationKnowledgeStore remains with former employer
-> PersonalKnowledge/RelationshipMemory remains only under explicit portability
```

### 18.2 Agent contact creates information

```text
ContactAgent Command
-> validate authority/rules/workload
-> create or link RecruitmentCase if needed
-> agent actor responds from market context
-> Domain Event
-> ObservationSignal for contacting organization
-> KnowledgeRecord with source = AgentStatement
-> dossier Query projects the new information
```

### 18.3 Automated decision is traced

```text
ScheduledWork triggers AI review
-> AccessContext and OrganizationKnowledge determine legal inputs
-> DecisionRecord preserves actor/authority/action
-> Application Command commits through normal rules
-> DecisionTrace records developer reasoning separately
```

---

## 19. Locked decisions

| Decision | Data-model consequence |
|---|---|
| D-002 | Human and AI use the same command/event/entity model. |
| D-003 | DomainEvent and Simulation Truth do not become Knowledge automatically. |
| D-004 | Player identity is ManagerCareer; employment is replaceable. |
| D-005 | DecisionAuthority is separate from person, career, organization, and employment. |
| D-007 | Stable world identities are never reused. |
| D-009 | Confidential Organization Knowledge does not transfer automatically with staff/manager movement. |
| D-010 | AI cannot query true ability/potential or rival private state. |
| D-013 | Stable identity and explicit ordering replace runtime-dependent order. |
| D-014 | Query/forecast data paths are pure and knowledge-bounded. |
| D-015 | Compaction cannot remove a future causal hook. |
| D-024/D-027 | Debug truth and player-facing explanation use separate access paths. |
| D-025 | Important automation links to the common Decision Trace framework. |
| D-031 | Game-state identity is not duplicated in scheduler or presentation records. |

---

## 20. Open questions

| ID | Question | Decision deadline |
|---|---|---|
| OQ-DM-001 | One global WorldEntityId allocator or typed per-entity allocators with equivalent no-reuse guarantees | Before `SAVE_FORMAT_v0.1.md` is accepted |
| OQ-DM-002 | Exact boundary between a manager Employment record and the future generic contract model | Before Contracts/Data Model expansion |
| OQ-DM-003 | Which personal observations remain portable raw records versus summarized Relationship/Knowledge memory | Before persistence schema for Personal Knowledge |
| OQ-DM-004 | Exact interim/vacancy authority assignment rules when an Organization has no ManagerCareer | Before AI organization skeleton implementation |
| OQ-DM-005 | Which DecisionRecord fields are permanent domain history versus compactable diagnostic detail | Before World Spy persistence and SAVE_FORMAT are accepted |

---

## 21. Deferred

- rider physiology, performance, development, training, health, and race truth fields;
- detailed staff role/capacity model;
- contracts, negotiations, market competition, sponsor, finance, and equipment fields;
- calendar, race edition, stage, result, and classification schemas;
- organization strategy and effective branding structures;
- content pack and rules-transition schemas;
- SQLite layout, indexes, migrations, and archive tables;
- exact knowledge merge, staleness, and compaction algorithms;
- hotseat/remote-human privacy handoff implementation.

---

## 22. Non-goals

- a `PlayerTeam` type or `IsHuman` organization flag;
- a universal Event class used for every contract;
- event sourcing as the only World State;
- an omniscient dossier or AI query path;
- storing deadlines only in Inbox/news;
- Godot nodes as domain entities or sources of truth;
- direct UI writes to SQLite/domain state;
- runtime hashes as persistent identity or RNG seeds;
- a full save schema hidden inside this document;
- World Spy fields exposed through ordinary player Queries.

---

## 23. Implementation notes

- Use typed IDs at domain boundaries even if SQLite later stores signed 64-bit values.
- Keep content definitions immutable/resolved for a loaded save; instances reference their origin without sharing identity.
- Validate references and authority in Application/Rules, not in Godot scenes.
- Persist unresolved Decision Requests and scheduler obligations; notifications alone are insufficient.
- Store exact units for money, dates/ticks, counters, and IDs. Race numeric policy remains open pending the race spike.
- Add models system by system. This v0.1 should not become a speculative catalog of every future field.
- `GAME_STATES_v0.1.md` owns legal state transitions. This document supplies the referenced identities and domain lifecycles.

---

## 24. Test criteria

### Identity and lifecycle

- Generate, retire/archive, and generate more entities without reusing any stable ID.
- One Person may retain identity across Rider, Staff, and Manager career records.
- Organization identity survives rename, manager change, and authority change.
- A ManagerCareer can move Organization while retaining Person/ManagerCareer identity.
- No domain type or field named `PlayerTeam`, `HumanTeam`, or equivalent controls rules.

### Authority and access

- HumanInput and AIInput authorities issue the same command types.
- A command with a stale/invalid AuthorityAssignment is rejected without mutation.
- Unemployed manager Queries return public/personal data and no former employer confidential data.
- Job change immediately changes organization-scoped Query results.
- Debug/spectator scope cannot be constructed by normal UI authority.

### Knowledge

- DomainEvent alone does not create actor Knowledge without publication/observation rules.
- The same subject Query returns different legal projections for two organizations with different Knowledge.
- ContactAgent creates a sourced Knowledge Record, not hidden truth.
- Organization Knowledge does not transfer automatically when a manager leaves.
- Forecast refresh leaves world checksum and gameplay RNG state unchanged.

### Event and decision contracts

- Each event-family record has a typed identity and lifecycle.
- Duplicate Command/ScheduledWork delivery cannot create duplicate domain effects.
- Notification read/archive does not change its source Decision Request, deadline, offer, or case.
- A DecisionRecord preserves actor/authority and decision-time knowledge references.
- Spy OFF and Spy DECISIONS produce identical gameplay outcomes and knowledge state.

### Persistence boundary

- Save/load preserves identities, current employment, authority assignments, Knowledge ownership, pending Decision Requests, and scheduler ordering data.
- Compacted and uncompacted branches produce the same gameplay-relevant future under the same build/content/commands.
- No invalid historical reference appears after retirement, organization rename, or archival.
