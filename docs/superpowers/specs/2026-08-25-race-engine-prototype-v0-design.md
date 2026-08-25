# Race Engine Prototype v0 — Implementation Design

**Status:** APPROVED IMPLEMENTATION NOTE

**Authority:** Subordinate to `DECISIONS.md`, `ARCHITECTURE.md`,
`RACE_ENGINE_DESIGN_v0.2.md`, `RACE_SPY_DEBUGGING_v0.1.md`, and
`WORLD_SPY_AND_DECISION_TRACING_v0.1.md`. This note does not close any
`O-RACE-*` question or create a production numeric lock.

**Goal:** Replace the official seed-ranking race stub with the smallest
headless canonical race model that can test the nine Race Engine prototype
proofs and expose a non-trivial strategic decision.

**Player value:** A watched race should create a coherent physical and tactical
story with at least one decision where spending resources now competes with
preserving another objective. Automated tests prove mechanics and invariants;
only the owner playtest can judge the engagement gate in
`RACE_ENGINE_DESIGN_v0.2.md` §49.

## 1. Owner-approved hard hooks

1. A `DecisionRequest` pauses race execution while the canonical GameState
   remains `RaceLive`. No tenth GameState is introduced.
2. `double` arithmetic and a fixed one-second step are prototype reference
   choices, not permanent production decisions.
3. Green tests do not pass the §49 fun gate. Every engagement claim remains
   `NOT VERIFIED` until the owner watches the prototype.
4. Official race results cannot come from `StubRaceEngine`. The SQLite schema
   is not changed without necessity, and RaceLive continues to forbid saves.

## 2. Scope boundary

The prototype implements route physics, CP/W'/Pmax capability, two-part
durability, deterministic groups and position slots, drafting and shelter,
dynamic gaps, conditional briefing, knowledge-bounded chase decisions, a live
strategic DecisionRequest, and passive Race Spy traces.

It does not implement Godot, direct rider power commands, glycogen, heat,
hydration, cobble physiology, crashes, radio failures, a TT optimizer, sprint
trains, career systems, or adaptive/event-compressed race simulation.

## 3. Canonical engine and execution modes

`PrototypeRaceEngine` creates a `RaceSession` from a validated
`RaceDefinition`, entrant profiles, team policies, ordered commands, a race
seed, and an optional World Spy sink.

`RaceSession.Step()` advances exactly one simulated second through the
canonical phase order. It returns one of:

- `Advanced`;
- `DecisionRequired` with the pending domain request;
- `Completed` with the official result.

Batch execution is a loop over the same `Step()` method. It may resolve pending
requests through the request's delegated/default policy and then continue. It
does not call a separate physics model or result generator. Live stepping and
batch execution therefore share state transitions, ordering, arithmetic, and
RNG behavior.

## 4. Race state and focused components

The session owns transient state only for one race day:

- immutable race definition and route segments;
- environment and current route segment;
- ordered groups and longitudinal/lateral rider slots;
- rider truth: distance, speed, power, W' balance, durability loads, work,
  shelter, gap, group, and intent;
- team intent and conditional briefing policy;
- actor observations and interpretations;
- pending/resolved DecisionRequests;
- official result and race metrics.

Focused components keep one reason to change:

- `RequiredPowerSolver` calculates aero, rolling, gravity, and acceleration
  demand. Shelter changes the aerodynamic component only.
- `CapabilitySolver` derives effective CP, short-duration capacity, realizable
  power, W' use/recovery, and durability effects.
- `PositionAndGroupResolver` resolves deterministic slot movement, shelter
  capacity, internal gaps, splits, and merges from one shared snapshot.
- `RaceIntentResolver` translates briefing, team orders, observations, and
  rider autonomy into tactical intent. It does not accept direct human watts.
- `ChaseDecisionEvaluator` compares sporting value, resource cost,
  opportunity cost, and expected rival contribution using actor-legal
  observations only.
- `RaceDecisionGate` evaluates materiality, choice, delegation, information,
  and novelty before creating a request.

Gameplay constants live in named prototype tuning records rather than being
scattered through the phase code.

## 5. One-second phase order

Each step follows the documented barrier:

1. immutable logical snapshot;
2. permitted observation and interpretation;
3. rider/team intent;
4. desired motion and effort;
5. required-power and realizable-capability solve;
6. simultaneous position/group/gap resolution;
7. W' and durability update;
8. permitted prototype rule effects;
9. information publication;
10. DecisionRequest detection;
11. passive diagnostic emission.

Stable rider, group, team, and trace ordering is explicit. Unordered collection
iteration and wall-clock timing never decide gameplay.

## 6. Natural dropping and attacks

An attack is an intent to accelerate and create a physical speed difference.
It is not a success roll. Repeated attacks consume supra-CP work and can change
which rider can answer later.

A rider falls behind when the required power for desired motion exceeds
realizable power. The lower realized speed grows a gap; the gap can remove
shelter and make returning more expensive. No generic stamina value and no
scripted `DropRider` or crosswind split command exists.

## 7. Information and decisions

Decision code receives a race observation DTO containing published gaps,
visible position/split signals, team resource interpretations, threat
estimates, objectives, and confidence. It cannot receive rival W' balance,
exact durability, or other truth-state objects.

The prototype supports a strategic request such as a material chase decision
with legal options:

- commit support now;
- wait for rivals;
- protect the second leader;
- trust the DS/default policy.

The request has stable identity, owner authority, trigger time, deadline or
race barrier, legal options, and delegated/default resolution. Responding uses
the same Application command contract for human and AI authorities.

## 8. World Spy and Race Spy

The common diagnostic boundary consists of structured `DecisionTrace` data and
an observational `IWorldSpySink`. Race-specific traces extend the common fields
rather than creating a second logging architecture.

Important traces preserve separately:

- Simulation Truth debug reference/context;
- actor-known inputs;
- actor interpretations and confidence;
- considered options and meaningful cost dimensions;
- selected option and reason;
- emitted strategic command;
- linked later outcome.

Race Spy also records decisive group/shelter/gap transitions needed to explain
drops, attacks, and crosswind splits. A no-op sink and a collecting sink receive
the same already-computed trace payloads. Neither sink is queried by gameplay or
owns RNG. Spy OFF and Spy ON must produce identical result and gameplay
checksum.

Structured JSON export is the machine artifact. A concise Markdown projection
is generated from the captured structure. Verbose tick history is not written
to the normal career save.

## 9. Content fixture

A separate data-only JSON prototype pack defines:

- a small peloton with enough riders and teams for shelter and chase behavior;
- CP, W', Pmax, W' recovery, low/high-intensity durability, mass, system mass,
  CdA, Crr, positioning, handling, and tactical awareness;
- deterministic team objectives and conditional briefings;
- synthetic route segments covering flat road, a sustained climb, and an
  exposed crosswind sector;
- named prototype tuning identity.

The loader treats the pack as untrusted input, validates ranges, uniqueness,
references, ordering, and path containment, and does not execute mod code.
Existing skeleton content continues to load.

## 10. Application and persistence integration

`GameApplication` depends on the canonical race interface, not the concrete
stub. Race preparation stores a strategic briefing. `StartRace` first verifies
the existing pre-race autosave and then creates the transient session.

Race advancement can stop on a pending request without changing the GameState
from `RaceLive`. A response command validates authority, request identity, and
legal option before the same session continues. Completion commits one neutral
`RaceSummary`, then preserves the existing flow:

```text
RaceLive -> RaceResultsFlow -> RaceDebriefFlow -> Management
```

The persisted last-race shape remains route ID, winner ID, and ordered finisher
IDs. Renaming stub-specific code to neutral race terminology does not change
the serialized JSON property shape or SQLite tables, so SchemaVersion remains
1. If implementation proves that any persisted field must change, work stops
for the required migration decision instead of silently changing the schema.

## 11. SimRunner

The existing multi-season skeleton command remains supported. A race prototype
command runs the fixture through the canonical engine and prints:

- winner;
- official finish-order checksum;
- DecisionRequest/decision count;
- Spy OFF versus Spy ON neutrality result.

The runner may export structured Race Spy output to an explicitly supplied
development-artifact path. It does not require Godot.

## 12. Automated proof map

Each prototype claim is protected by a named headless behavior test:

1. drafting/position lowers aero and energy cost and can change pace-up
   survival;
2. repeated attacks change later W' availability and selection;
3. durability creates a visible fresh-versus-late capability difference;
4. realized speed, gap growth, and shelter loss can drop a rider without a
   scripted DNF/drop flag;
5. crosswind and finite sheltered slots can split a group;
6. two teams can rationally choose different chase responses from different
   objectives/resources in the same observed situation;
7. protect and chase briefings materially change team behavior while physics
   rules remain identical;
8. at least one DecisionRequest exposes two or more legal, non-dominated
   strategic options;
9. Spy OFF and Spy ON produce identical finish order and checksum.

Additional contract tests cover same-seed repeatability, batch-versus-step
parity, knowledge-bounded decision inputs, existing RaceLive save rejection,
pre-race recovery, architecture boundaries, and absence of a stub-generated
official result.

The tests do not claim that a decision feels interesting. The owner questions
in §49 remain a manual gate and are reported as `NOT VERIFIED`.

## 13. Delivery slices

One branch and PR contain logically reviewable commits:

1. race definitions, physics, capability, and their red/green tests;
2. groups, slots, shelter, gaps, and scenario proofs;
3. briefing, observations, chase decisions, DecisionRequest, and World/Race
   Spy traces;
4. GameApplication, content pack, persistence-safe result rename, and
   SimRunner integration;
5. documentation, full regression verification, and PR handoff.

`KNOWN_DIFFERENCE_FROM_CODE.md`, `HANDOFF.md`, and `CODEBASE_MAP.md` are updated
to describe what landed and which prototype limitations remain. Canonical
architecture documents are not rewritten.

## 14. Explicitly open after this prototype

- final numeric representation;
- final/adaptive timestep;
- calibrated production CP/W' and durability functions;
- production shelter and position resolution;
- complete information delay/radio environment;
- renderer and race-watching UX;
- every owner engagement question from §49.

