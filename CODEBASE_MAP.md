# Peloton Manager — CODEBASE MAP

**Status:** ACTIVE — Architecture skeleton plus race prototype v0.

This file is a navigation map, not implementation documentation. Design contracts remain authoritative.

## Solution structure

| Project | Current responsibility |
|---|---|
| `src/Peloton.Domain` | World root, stable IDs, people, ManagerCareer, Employment, Organization, DecisionAuthority, AccessContext, `CalendarEntry`, content/rules identity, DecisionTrace / Spy sinks. |
| `src/Peloton.Rules` | Stable rules-module identity and deterministic aggregate identity. No full legal engine yet. |
| `src/Peloton.Simulation` | Versioned seed derivation, isolated deterministic RNG, whole-world day scheduler, checksum, `PrototypeRaceEngine`. |
| `src/Peloton.Simulation/Race` | Physics, capability, groups/shelter, `RaceSession.Step`, chase decisions, Race Spy export. |
| `src/Peloton.Application` | Canonical nine-state machine, Commands, save/content/race ports, world creation, RaceLive isolation, skeleton-season orchestration. |
| `src/Peloton.Persistence` | SQLite schema version 1, verified candidate save, envelope identity, snapshot round trip, integrity checks. |
| `src/Peloton.Content` | JSON pack loaders: skeleton `scenarios` and `racePrototypeScenarios`. |
| `src/Peloton.Infrastructure` | Composition root connecting Application ports to Content, Persistence, and Simulation. |
| `src/Peloton.Client.Godot` | Empty future-client boundary; no Godot SDK or scene code. |
| `tools/Peloton.SimRunner` | Headless CLI: `run` for skeleton seasons, `race` for the prototype gate, `day` for Hub + calendar/inbox / race-due loop. |

Static content lives in `content/peloton.skeleton` and `content/peloton.race-prototype`. `KNOWN_DIFFERENCE_FROM_CODE.md` records remaining prototype limits versus the accepted Race Engine contract.

## Tests

| Project | Coverage |
|---|---|
| `tests/Peloton.Domain.Tests` | Stable ID allocation and no-reuse spine. |
| `tests/Peloton.Simulation.Tests` | Seed derivation plus race physics, groups, decisions, and Spy neutrality. |
| `tests/Peloton.Application.Tests` | GameState guards, content identity, Advance Day, RaceLive isolation, prototype catalog, SimRunner `race` contract, 10-season determinism. |
| `tests/Peloton.Persistence.Tests` | SQLite schema/content/rules metadata, checksum round trip, failed-load atomicity, last-race JSON shape. |
| `tests/Peloton.Architecture.Tests` | Forbidden PlayerTeam-like types, no production `StubRaceEngine`, Godot-free headless assemblies. |

## System ownership

| System | Main project/folder | Design authority | Tests |
|---|---|---|---|
| World time / scheduler | `Peloton.Simulation/DeterministicScheduler.cs` | `ARCHITECTURE.md`, determinism contract | Simulation + Application tests |
| GameState / Commands | `Peloton.Application` | `GAME_STATES_v0.1.md` | Application tests |
| Identity / manager spine | `Peloton.Domain` | `DATA_MODEL_v0.1.md` | Domain + Persistence tests |
| Race engine | `Peloton.Simulation/Race/PrototypeRaceEngine.cs`, `RaceSession.cs` | `RACE_ENGINE_DESIGN_v0.2.md`; limits in `KNOWN_DIFFERENCE_FROM_CODE.md` | `RacePhysicalProofTests`, Application race tests |
| Race Spy | `Peloton.Simulation/Race/RaceSpy.cs`, `Peloton.Domain/DecisionTracing.cs` | `RACE_SPY_DEBUGGING_v0.1.md` | `RaceDecisionAndSpyTests` |
| World Spy | `Peloton.Domain/DecisionTracing.cs` | `WORLD_SPY_AND_DECISION_TRACING_v0.1.md` | Race Spy tests (first specialization) |
| Race content | `Peloton.Content/JsonRacePrototypeCatalog.cs`, `content/peloton.race-prototype` | `CONTENT_FORMAT_v0.1.md` | `RaceContentTests` |
| SimRunner race gate | `tools/Peloton.SimRunner/RacePrototypeCommand.cs` | prototype design §11 | `SimRunnerContractTests` |
| Career Hub query | `Peloton.Application/CareerDay.cs` | `GAME_STATES_v0.1.md` Advance Day; not a UI dashboard | Application tests, `day` SimRunner |
| Career calendar | `Peloton.Domain/CalendarEntry.cs`, `Peloton.Application/CareerCalendarInbox.cs` | stored entries + derived status | Application tests |
| Career inbox query | `Peloton.Application/CareerCalendarInbox.cs`, `ArchiveInboxItemCommand` | rebuilt race-due + race-result items; dismiss lock on race-due | Application tests |
| AI managers | Not implemented | `AI_MANAGER_SYSTEM_v0.2.md` | Not implemented |
| Save / SQLite | `Peloton.Persistence` | `SAVE_FORMAT_v0.1.md` | Persistence + Application tests |
| Career scenarios | `content/peloton.skeleton`, `JsonScenarioCatalog.cs` | `CONTENT_FORMAT_v0.1.md` | Application tests |
| Rules modules | `Peloton.Rules`, scenario JSON | `RULESETS_v0.1.md` | Application + Architecture tests |

Recruitment, contracts, sponsors, knowledge records, full calendar beyond skeleton races, Godot UI, and `D-032` multi-stage leadership transfer are not implemented.

## Dependency direction

```text
Peloton.Domain
├── Peloton.Rules
│   └── Peloton.Simulation
│       └── Peloton.Application
├── Peloton.Application
│   ├── Peloton.Content       implements content port
│   └── Peloton.Persistence   implements save port
└── Peloton.Infrastructure    composition root over Application adapters

Peloton.Client.Godot -> Peloton.Application only
Peloton.SimRunner -> Peloton.Infrastructure + Peloton.Application + Peloton.Content + Peloton.Simulation
```

Domain, Rules, Simulation, and Persistence have no Godot reference. Godot never writes SQLite.

## Where to start debugging

```text
State/command rejection
→ Peloton.Application/GameApplication.cs
→ owning GameState guard

Race physics / finish order
→ Peloton.Simulation/Race/RaceSession.cs
→ RequiredPowerSolver / CapabilitySolver / group + shelter code

Race DecisionRequest pause
→ Peloton.Application pending projection (State stays RaceLive)
→ Peloton.Simulation/Race chase evaluator

Determinism mismatch
→ Peloton.Simulation/WorldChecksum.cs
→ RaceResultChecksum / StableSeedDerivation / command order

Race Spy / knowledge leak
→ Peloton.Domain/DecisionTracing.cs
→ RaceSpy.cs
→ TeamRaceObservation (no W'/durability/truth fields)

Save/load failure
→ Peloton.Persistence/SqliteWorldSaveStore.cs
→ schema/content/rules metadata
→ SQLite quick_check and checksum

Content creation failure
→ Peloton.Content/JsonScenarioCatalog.cs
→ Peloton.Content/JsonRacePrototypeCatalog.cs
→ content/peloton.skeleton
→ content/peloton.race-prototype

SimRunner race gate
→ tools/Peloton.SimRunner/RacePrototypeCommand.cs
→ Program.cs `race` command
```
