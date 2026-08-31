# Peloton Manager: Game States

**Title:** Game States

**Version:** 0.1

**Status:** DRAFT

**Purpose:** Define the canonical game states, legal transitions, runtime barriers, save/load rules, and recovery behavior.

**Authority/Owner:** Project owner (gameplay and application architecture)

**Supersedes:** none

**Superseded by:** none

**Last reviewed:** 2026-08-25

**Related decisions/ADRs:** D-004, D-005, D-006, D-008, D-009, D-013, D-014, D-031, D-034, D-036; OPEN - Hotseat RaceLive resolution

---

## 1. Purpose and scope

This document defines the state machine that controls the active application flow. It answers four questions:

1. Which game state is active?
2. Which commands and navigation paths are legal in that state?
3. Which transition may happen next, and what guards it?
4. Can the game save, load, or recover safely at that point?

The state machine does not model every screen, scheduler phase, employment status, popup, or domain object.

### In scope

- the canonical list of game states;
- legal transitions and transition guards;
- interaction with `AdvanceDay` and persistent `DecisionRequest` objects;
- `RaceLive` isolation, pre-race autosave, and recovery;
- save/load policy by state;
- employed and unemployed manager routing;
- failure semantics and state-machine acceptance tests.

### Out of scope

- screen inventory and visual modality: `UI_SITEMAP_v0.1.md`;
- entity and event fields: `DATA_MODEL_v0.1.md`;
- scheduler ordering keys and RNG isolation: `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`;
- race physics and internal race phases: `RACE_ENGINE_DESIGN_v0.2.md`;
- SQLite tables, migrations, and serialized UI drafts: future `SAVE_FORMAT.md`;
- hotseat RaceLive pause and checkpoint rules.

---

## 2. One canonical state machine

The game has one canonical state machine. Its states are:

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

Terms such as application layer, career session, scheduler runtime, and presentation layer help explain ownership. They do not create parallel state machines.

An active career session is a descriptive container for `Management`, the career Card Flows, and the race states. It is not a tenth state.

### 2.1 Things that are not game states

| Concept | Contract |
|---|---|
| Scheduler idle, processing, or paused at a deterministic barrier | Runtime execution status. It does not replace or nest the active game state. |
| Employed or unemployed | Data on `ManagerCareer.Employment`, not a world or UI state. |
| Modal open, selected tab, scroll position, paused animation | Presentation state owned by the client. |
| Pending `DecisionRequest` | Persistent domain object. The game state remains `Management` or `RaceLive`. |
| Settings | A presentation route hosted by `MainMenu` or `Management`. |
| Employment change, negotiation, sponsor offer, season review | Domain-specific UI/Card Flows hosted by `Management` unless a later owner decision promotes one to a canonical state. |
| Season context rail | Calendar-based orientation, not a state machine. |

This distinction prevents UI details from changing simulation semantics.

---

## 3. State contracts

| State | Authoritative world loaded | Main navigation | World time may advance | Primary responsibility |
|---|---:|---:|---:|---|
| `MainMenu` | No active world | No career shell | No | Start, select, or load a career. |
| `NewGameFlow` | No active world | No career shell | No | Collect a valid world recipe and starting manager setup. |
| `LoadingWorld` | Not yet usable | Blocked | No | Create, load, migrate, validate, and attach a world atomically. |
| `Management` | Yes | Available | Through `AdvanceDay` only | Normal career browsing, commands, decisions, and employment routing. |
| `PreSeasonPlanningFlow` | Yes | Card Flow only | No | Build and confirm a season plan without running the scheduler. |
| `RacePreparationFlow` | Yes | Card Flow only | No | Select squad, roles, objectives, briefing, and Watch/Simulate path. |
| `RaceLive` | Yes, with stage-scoped transient race state | Blocked | Race clock only | Run one stage or race day and handle legal race decisions. |
| `RaceResultsFlow` | Yes | Card Flow only | No | Present the committed official result and immediate consequences. |
| `RaceDebriefFlow` | Yes | Card Flow only | No | Explain plan versus execution and acknowledge follow-up work. |

### 3.1 MainMenu

`MainMenu` has no attached authoritative world. Continue or Load requests enter `LoadingWorld`. New Game enters `NewGameFlow`. App settings do not create another game state.

### 3.2 NewGameFlow

The flow collects scenario, content/rules composition, history mode, difficulty, manager profile, and starting employment. The draft is not an authoritative world.

`CreateWorld` may be issued only from the confirmed summary step. Validation failure leaves the game in `NewGameFlow` and identifies the incompatible or invalid input.

### 3.3 LoadingWorld

`LoadingWorld` owns load/create/migration progress. A world becomes active only after content identity, schema, references, and required invariants pass validation.

Failure returns to `MainMenu` without replacing a previously valid save or exposing a partially loaded world.

### 3.4 Management

`Management` is the normal career state for employed and unemployed managers. Persistent navigation and legal Application Commands are available through knowledge-bounded Queries.

The manager's employment changes what the UI may query and which commands are legal. It does not change the state name.

### 3.5 PreSeasonPlanningFlow

This Card Flow does not advance world time. Back and Cancel return to `Management`. Confirm emits the legal planning commands and returns to `Management` after they commit.

### 3.6 RacePreparationFlow

The flow prepares one upcoming race day or stage. It may end in:

- `RaceLive` after a successful pre-race autosave and `StartRace`;
- `RaceResultsFlow` after a headless run through the same canonical race engine;
- `Management` after Cancel, when cancellation remains legal.

Opening the flow does not consume race RNG.

### 3.7 RaceLive

`RaceLive` covers one stage or one race day. It blocks the career shell, management commands, and manual save/load.

Legal actions are limited to:

- pause or resume presentation/simulation at supported boundaries;
- safe presentation settings;
- `RespondToRaceDecision` and other race-scoped commands allowed by the briefing/authority contract;
- exit to `MainMenu`, abandoning the live session.

The renderer never drives physics, command order, or RNG.

### 3.8 RaceResultsFlow

The race result is already committed before this state is entered. The flow displays official outcomes from the canonical race engine. It does not recompute them.

Continue enters `RaceDebriefFlow`.

### 3.9 RaceDebriefFlow

The debrief uses result evidence, staff observations, Decision Records, and knowledge-bounded explanations. It cannot read Race Spy truth through normal Queries.

Finish returns to `Management`. A stage race schedules its next stage as later world work; it does not keep the game inside one multi-stage `RaceLive` state.

---

## 4. Canonical transition map

```text
Application start
    -> MainMenu

MainMenu
    -> NewGameFlow
    -> LoadingWorld

NewGameFlow
    -> MainMenu                 Cancel
    -> LoadingWorld             CreateWorld accepted

LoadingWorld
    -> MainMenu                 Cancel or failure
    -> Management               World attached and validated

Management
    -> LoadingWorld             Load another save
    -> PreSeasonPlanningFlow
    -> RacePreparationFlow
    -> MainMenu                 Close active career

PreSeasonPlanningFlow
    -> Management               Confirm or Cancel

RacePreparationFlow
    -> Management               Cancel
    -> RaceLive                 Watch film setting on; pre-race autosave succeeded
    -> RaceResultsFlow          Simulate (default); canonical race completed

RaceLive
    -> RaceResultsFlow          Stage/race day completed
    -> MainMenu                 Abandon live session

RaceResultsFlow
    -> RaceDebriefFlow

RaceDebriefFlow
    -> Management
```

No transition may be inferred from the currently visible Godot scene. The Application layer owns the state and validates every transition.

---

## 5. Transition processing contract

A transition is processed in this order:

```text
Transition request or committed domain outcome
-> validate active GameState
-> validate DecisionAuthority and AccessContext where applicable
-> validate domain guards
-> finish the current atomic command/barrier
-> commit domain changes
-> set the next GameState
-> emit lifecycle diagnostics/events as required
-> refresh query projections
```

### 5.1 Atomicity

- A rejected command leaves the GameState unchanged.
- A failed transition cannot expose a partially committed world.
- UI navigation cannot skip a guard by loading a scene directly.
- One command or scheduler work item cannot observe two active game states.
- State transition order cannot depend on frame timing, callback order, or unordered collection iteration.

### 5.2 Illegal transitions

The Application layer rejects at least:

- `StartRace` outside `RacePreparationFlow`;
- entry to `RaceLive` before the pre-race autosave commits;
- management navigation or management commands in `RaceLive`;
- `SaveGame` or `LoadGame` in `RaceLive`;
- `AdvanceDay` outside `Management`;
- entry to Results before a canonical race result commits;
- direct Results to Management transition that bypasses required debrief state;
- commands issued by an authority without a current legal assignment;
- Queries that attempt to use a stale former-employer `AccessContext` after an employment change.

Illegal transitions fail with a stable reason code and relevant entity/command IDs. They do not silently redirect to another state.

---

## 6. Advance Day and scheduler runtime

`AdvanceDay` is legal only in `Management`. It starts deterministic scheduler processing but does not change the GameState.

When a race is due, the Hub primary control is **Race next** (D-034): it enters `RacePreparationFlow` instead of issuing `AdvanceDay`. `AdvanceDay` remains rejected until the race is completed. Inbox items do not launch this transition.

```text
GameState = Management
player issues AdvanceDay
runtime processes ScheduledWork in canonical order
runtime reaches end-of-day barrier or a blocking DecisionRequest
GameState is still Management
```

While the command is executing, the client may show progress or temporarily reject duplicate input. That client feedback is not a new state.

### 6.1 Deterministic pause

A pause occurs only at a barrier defined by the scheduler contract. The pause cannot change:

- which work at the same timestamp/phase already completed;
- canonical processing order;
- gameplay RNG consumption;
- the knowledge available at decision time;
- the result produced after the same response command.

After a management `DecisionRequest` resolves, the scheduler may continue the same simulation day. The calendar date advances only when the end-of-day barrier completes.

### 6.2 Race runtime

Race simulation runtime operates while `GameState = RaceLive`. Pausing the race leaves the state unchanged. A race Decision Request stops at a race barrier and is resolved through a normal race command.

---

## 7. DecisionRequest contract at the state boundary

`DecisionRequest` is a persistent domain object with, at minimum:

- stable identity;
- owner/DecisionAuthority scope;
- creation time and deadline;
- trigger and related domain references;
- delegated/default resolution policy;
- pending/resolved/expired lifecycle;
- reference to its resolution when resolved.

It is not `PopupOpen = true` and does not own navigation.

The request policy determines whether it blocks scheduler continuation. Presentation remains open:

- time-critical race requests may use a blocking overlay;
- management requests may route through Inbox/Decision Queue;
- the exact modal versus Inbox rule remains OQ-UI-001.

The implementation must not assume there can be only one human-owned request in the future. Single-player may present one at a time, but ownership and ordering stay explicit.

---

## 8. Employment and organization context

The same `Management` state supports:

```text
ManagerCareer with active Employment -> employed career shell
ManagerCareer without active Employment -> unemployed career shell
```

Accepting, ending, or losing employment changes domain data atomically. The next Query derives a new `AccessContext`.

After a job change:

- the ManagerCareer and Person identities remain stable;
- the former organization continues in the same world;
- confidential Organization Knowledge stays with the former employer;
- Personal Knowledge and Relationship Memory follow their explicit portability rules;
- no `PlayerTeam` is created or transferred;
- human/AI authority assignment changes without changing the Organization type.

Employment negotiation and dismissal presentation run inside `Management`. They do not create additional canonical game states.

---

## 9. Save, load, and recovery matrix

| State | Manual save | Load another save | Autosave/recovery contract |
|---|---|---|---|
| `MainMenu` | No active world | Yes, via `LoadingWorld` | None |
| `NewGameFlow` | No world save | Return to MainMenu first | New-game draft persistence is not part of world save |
| `LoadingWorld` | No | No | Failure never replaces the source save |
| `Management` | Yes at an atomic boundary | Yes, via `LoadingWorld` after confirmation | Normal autosave policy |
| `PreSeasonPlanningFlow` | Conditional at a stable checkpoint | Cancel/confirm flow first | Unconfirmed local draft cannot become hidden authoritative state |
| `RacePreparationFlow` | Conditional before `StartRace` | Cancel flow first | `StartRace` requires committed pre-race autosave |
| `RaceLive` | Forbidden | Forbidden | Exit/load restores pre-race autosave; no mid-race snapshot |
| `RaceResultsFlow` | Conditional after race result commit | Finish/exit through legal flow | Result is authoritative; presentation cannot recompute it |
| `RaceDebriefFlow` | Conditional at a stable checkpoint | Finish/exit through legal flow | Pending domain obligations must remain represented outside UI-only draft |

"Conditional" means the authoritative world is between atomic commands and no scheduler/race step is partially applied. Future `SAVE_FORMAT.md` must choose whether local Card Flow drafts are serialized or explicitly discarded with confirmation. A UI draft may never masquerade as committed world state.

### 9.1 RaceLive recovery

```text
RacePreparationFlow
-> create and verify pre-race autosave
-> StartRace commits
-> RaceLive

RaceLive exit/crash
-> discard transient live session
-> MainMenu
-> Continue/Load
-> LoadingWorld reads pre-race autosave
```

If the pre-race autosave fails, entry to `RaceLive` fails and the game remains in `RacePreparationFlow`.

---

## 10. Presentation-state boundary

Godot may own temporary presentation values such as:

- selected tab and filters;
- expanded panels;
- scroll positions;
- animation or camera settings;
- currently visible Decision Request route;
- Card Flow form draft before confirmation.

These values cannot:

- mutate World State directly;
- consume gameplay RNG;
- authorize a command;
- expose hidden truth;
- decide canonical command/event order;
- change race outcomes when rendering is attached or absent.

---

## 11. Failure behavior

| Failure | Required result |
|---|---|
| Command rejected | Stay in the current state; return stable reason and context. |
| New-world validation fails | Stay in `NewGameFlow`; preserve the editable recipe. |
| Load/migration/integrity check fails | Return to `MainMenu`; keep the source save unchanged. |
| Pre-race autosave fails | Stay in `RacePreparationFlow`; do not start race RNG or transient state. |
| Race command rejected | Stay in `RaceLive`; retain the same decision boundary. |
| Result commit fails | Do not enter `RaceResultsFlow`; report a recoverable failure with reproduction IDs. |
| Stale AccessContext after employment change | Reject query/command and require context refresh. |
| Duplicate transition request | Resolve idempotently or reject without duplicate domain effects. |

Impossible state combinations should fail loudly in development and emit structured diagnostics. They should not be repaired by silently choosing a convenient screen.

---

## 12. Locked decisions

| Decision | Game-state consequence |
|---|---|
| D-004 | `Management` follows ManagerCareer, not a permanent team. |
| D-005 | DecisionAuthority is validated separately from manager identity and employment. |
| D-006 | `AdvanceDay` is the UX action; scheduler execution remains runtime. |
| D-008 | `RaceLive` covers one stage/day, blocks management navigation and mid-race save, and requires pre-race autosave. |
| D-009 | Employer change re-derives access without copying confidential organization knowledge. |
| D-013 | Pause, UI timing, and render order cannot change outcomes. |
| D-014 | State projections and forecasts remain read-only and RNG-neutral. |
| D-031 | The nine-state list in section 2 is canonical; scheduler and presentation status do not add states. |

---

## 13. Open questions

| ID | Question | Decision deadline |
|---|---|---|
| OQ-GS-001 | Modal versus Inbox-first routing for management `DecisionRequest` objects | Before production Decision Queue routing |
| OQ-GS-002 | Serialize local Card Flow drafts or discard them with confirmation on save/load | Before `SAVE_FORMAT_v0.1.md` is accepted |
| OQ-GS-003 | Exact pause/checkpoint behavior for multiple human authorities in `RaceLive` | Before hotseat RaceLive work |
| OQ-GS-004 | Whether Season Review later needs promotion from a Management-hosted flow to a canonical state | Before deep post-season implementation |

---

## 14. Deferred

- hotseat authority switching and privacy handoff UI;
- online authority/replication states;
- exact Card Flow draft serialization;
- platform suspend/resume behavior;
- recovery from versioned race simulation builds;
- post-season flow depth.

---

## 15. Non-goals

- a second state machine for the scheduler, presentation, employment, or career session;
- a special `PlayerTeam` state or human-only world branch;
- direct Godot scene mutation as the source of state truth;
- mid-race save;
- using Inbox messages as Decision Request persistence;
- changing game state when the player opens a Query or forecast;
- using World Spy truth to drive a transition or normal UI route.

---

## 16. Implementation notes

- Application owns the canonical `GameState`; Godot renders it.
- Transition guards should be testable without Godot.
- Scheduler runtime status belongs to runtime diagnostics, not the save's game-state enum.
- Domain obligations must survive UI closure through their own objects, especially `DecisionRequest`, offers, registrations, and deadlines.
- `DATA_MODEL_v0.1.md` defines the identities referenced here. `SAVE_FORMAT.md` will define serialization and migration.
- State transition diagnostics may link to World Spy, but World Spy stays observational and RNG-neutral.

---

## 17. Test and playtest criteria

### State and transition tests

- The canonical enum contains exactly the nine states in section 2.
- Direct illegal transitions are rejected and leave the source state unchanged.
- New Game cannot skip required cards before `CreateWorld`.
- A failed load never attaches a partial world.
- `AdvanceDay` is rejected outside `Management`.
- Scheduler pause and resume leave `GameState = Management` and produce the same result for the same response command.
- Employed and unemployed careers both use `Management`.
- Employer change invalidates the old organization AccessContext without changing ManagerCareer identity.

### Race boundary tests

- `RaceLive` entry is impossible before a successful pre-race autosave.
- Management navigation, management commands, Save, and Load are rejected in `RaceLive`.
- Renderer attached versus headless does not alter the state transition sequence or race result.
- Exiting RaceLive and loading restores the pre-race state.
- A stage finish follows `RaceLive -> RaceResultsFlow -> RaceDebriefFlow -> Management`.
- The next stage of a stage race starts from a later `RacePreparationFlow`, not the previous RaceLive session.

### Information and diagnostics tests

- Opening/closing a modal or settings route does not change World State or consume gameplay RNG.
- A pending Decision Request survives UI navigation and is not stored only as a notification.
- Player-facing debrief Queries cannot access Race Spy truth.
- Spy OFF and Spy DECISIONS produce identical gameplay state and GameState transitions.
