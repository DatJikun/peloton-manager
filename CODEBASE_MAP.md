# Peloton Manager — CODEBASE MAP

**Status:** ACTIVE — Architecture skeleton plus race prototype v0.

This file is a navigation map, not implementation documentation. Design contracts remain authoritative.

## Solution structure

| Project | Current responsibility |
|---|---|
| `src/Peloton.Domain` | World root, stable IDs, people, `RiderCareer`, `RiderContract`, ManagerCareer, Employment, Organization, DecisionAuthority, AccessContext, `CalendarEntry`, content/rules identity, DecisionTrace / Spy sinks. |
| `src/Peloton.Rules` | Stable rules-module identity and deterministic aggregate identity. No full legal engine yet. |
| `src/Peloton.Simulation` | Versioned seed derivation, isolated deterministic RNG, whole-world day scheduler, checksum, `PrototypeRaceEngine`. |
| `src/Peloton.Simulation/Race` | Physics, capability, groups/shelter, `RaceSession.Step`, chase decisions, Race Spy export, headless Watch clock and public motion projection. |
| `src/Peloton.Application` | Canonical nine-state machine, Commands, prep/result/debrief projections, prep checkpoint, save/content/race ports, world creation, RaceLive isolation, skeleton-season orchestration. |
| `src/Peloton.Persistence` | SQLite schema version 4, verified candidate save, envelope identity, snapshot round trip (including `RiderCareer`, `RiderContract`, results, `OrganizationRaceEntry`), integrity checks. |
| `src/Peloton.Content` | JSON pack loaders: skeleton `scenarios` + `roster`, and `racePrototypeScenarios` (route/tuning templates). |
| `src/Peloton.Infrastructure` | Composition root connecting Application ports to Content, Persistence, and Simulation. |
| `src/Peloton.Client.Godot` | Godot 4.4 .NET Watch Race window. Presentation only: Commands + Queries, interpolated `RaceWatch` icons, decision overlay, Results from `LastRace`. Uses Infrastructure as composition root; does not hold World State or open SQLite. |
| `tools/Peloton.SimRunner` | Headless CLI: `run` for skeleton seasons, `race` for the prototype gate, `watch` for rate-controlled supervising-clock output, `day` for Hub, prep, calendar/inbox, result/debrief, and race-due flows. |

Static content lives in `content/peloton.skeleton` and `content/peloton.race-prototype`. `KNOWN_DIFFERENCE_FROM_CODE.md` records remaining prototype limits versus the accepted Race Engine contract.

## Tests

| Project | Coverage |
|---|---|
| `tests/Peloton.Domain.Tests` | Stable ID allocation and no-reuse spine. |
| `tests/Peloton.Simulation.Tests` | Seed derivation plus race physics, groups, decisions, Watch clock invariants, and Spy neutrality. |
| `tests/Peloton.Application.Tests` | GameState guards, prep actions, result/debrief projections, Watch/Simulate parity, content identity, Advance Day, RaceLive isolation, CLI contracts, 10-season determinism. |
| `tests/Peloton.Persistence.Tests` | SQLite schema/content/rules metadata, checksum round trip, prep checkpoint recovery through Results/Debrief, failed-load atomicity, last-race JSON shape. |
| `tests/Peloton.Architecture.Tests` | Forbidden PlayerTeam-like types, no production `StubRaceEngine`, Godot-free headless assemblies. |
| `tests/Peloton.Client.Godot.Tests` | Watch interpolator and host command path (StartRace clock, decision pause, LastRace result, abandon rollback). Compiles Godot-free host sources; does not need the Godot editor. |

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
| Headless Watch clock | `Peloton.Simulation/Race/RaceWatch.cs`, `tools/Peloton.SimRunner/RaceWatchCommand.cs` | `D-033` clock contract | `RaceWatchTests`, `SimRunnerContractTests` |
| Godot Watch Race | `src/Peloton.Client.Godot` (`WatchRaceHost`, `WatchRaceScreen`, `project.godot`) | `D-033` renderer; `UI_SITEMAP` RaceLive; §49 still owner-only | `WatchRaceHostTests`, `WatchMotionInterpolatorTests` |
| Career Hub query | `Peloton.Application/CareerDay.cs`, `ClubRosterProjection` | `GAME_STATES_v0.1.md` Advance Day; Hub primary action (`advance-day` / `race-next`); employer roster wages via `ClubRosterProjection`; Management only; not a UI dashboard | Application tests, `day` SimRunner |
| Race preparation | `Peloton.Application/RacePreparation.cs`, `PreSeasonPlanning.cs`, `WorldRaceScenarioAssembler.cs`, `GameApplication.cs` | world roster + route template + entry filter + player strategy (`D-036` phase 1–3); readiness scales CP/Pmax at assemble | Application + Persistence tests |
| RiderCareer / world–race bind | `Peloton.Domain/RiderCareer.cs`, `RiderContract.cs`, `OrganizationRaceEntry`, `WorldRaceScenarioAssembler.cs`, `content/peloton.skeleton/skeleton-roster.json` | `CAREER_WORLDTOUR_SLICE_v0.1.md` phase 1–4; start lists from entered org rosters (skip null `OrganizationId`); contract expiry on Advance Day | `CareerWorldTourBindTests`, `CareerWorldTourPhase2Tests`, `CareerWorldTourPhase3Tests`, `CareerWorldTourPhase4Tests` |
| Pre-season planning | `Peloton.Application/PreSeasonPlanning.cs`, `GameApplication.cs` | `PreSeasonPlanningFlow` draft entry by `RaceContentId`; confirm commits `OrganizationRaceEntry` | `CareerWorldTourPhase3Tests` |
| Race result / debrief | `Peloton.Application/RaceResultDebrief.cs`, `GameApplication.cs` | committed `LastRace` uses world `RiderCareer.Id`; career history append on `RecordRace` | Application + Persistence tests |
| Career calendar | `Peloton.Domain/CalendarEntry.cs`, `Peloton.Application/CareerCalendarInbox.cs` | stored entries + derived status | Application tests |
| Career inbox query | `Peloton.Application/CareerCalendarInbox.cs`, `ArchiveInboxItemCommand` | rebuilt race-due + race-result items; dismiss lock on race-due | Application tests |
| AI managers | Not implemented | `AI_MANAGER_SYSTEM_v0.2.md` | Not implemented |
| Save / SQLite | `Peloton.Persistence` | `SAVE_FORMAT_v0.1.md` | Persistence + Application tests |
| Career scenarios | `content/peloton.skeleton`, `JsonScenarioCatalog.cs` | `CONTENT_FORMAT_v0.1.md` | Application tests |
| Rules modules | `Peloton.Rules`, scenario JSON | `RULESETS_v0.1.md` | Application + Architecture tests |

Recruitment, transfer market, sponsors, knowledge records, full calendar beyond skeleton races, Career Hub UI, and `D-032` multi-stage leadership transfer are not implemented. Godot Watch is the first playable race window, not the career shell.

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

Peloton.Client.Godot -> Peloton.Application + Peloton.Infrastructure (composition root only)
Peloton.SimRunner -> Peloton.Infrastructure + Peloton.Application + Peloton.Content + Peloton.Simulation
```

Domain, Rules, Simulation, and Persistence have no Godot reference. `Peloton.Client.Godot` may reference the Godot SDK. Godot never writes SQLite; save/load stay Application commands.

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

Godot Watch Race
→ src/Peloton.Client.Godot/WatchRaceHost.cs
→ WatchRaceScreen.cs
→ GameApplication RaceWatch / PendingRaceDecision / RaceResult

SimRunner race gate
→ tools/Peloton.SimRunner/RacePrototypeCommand.cs
→ Program.cs `race` command
```
