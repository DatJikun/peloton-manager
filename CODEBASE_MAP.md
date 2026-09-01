# Peloton Manager — CODEBASE MAP

**Status:** ACTIVE — Architecture skeleton plus race prototype v0.

This file is a navigation map, not implementation documentation. Design contracts remain authoritative.

## Solution structure

| Project | Current responsibility |
|---|---|
| `src/Peloton.Domain` | World root, stable IDs, people, `RiderCareer` (+ `PotentialOvr`), `RiderContract`, `CourseProfile`, `RiderStageTime`, ManagerCareer, Employment, Organization, DecisionAuthority, AccessContext, `CalendarEntry` (+ stage/course ids), content/rules identity, DecisionTrace / Spy sinks. |
| `src/Peloton.Rules` | Stable rules-module identity and deterministic aggregate identity. No full legal engine yet. |
| `src/Peloton.Simulation` | Versioned seed derivation, isolated deterministic RNG, whole-world day scheduler, checksum, `PrototypeRaceEngine`, `Course/` generator + compiler. |
| `src/Peloton.Simulation/Race` | Physics, capability, groups/shelter, `RaceSession.Step`, chase decisions, Race Spy export, headless Watch clock and public motion projection. |
| `src/Peloton.Simulation/Course` | Dense profile bricks, classifier, `CourseCatalogGenerator`, `CourseCompiler`, weather from seed. |
| `src/Peloton.Application` | Canonical nine-state machine, Commands, prep/result/debrief projections, `RiderRatingQueries`, `CourseWorldBuilder`, prep checkpoint, save/content/race ports, world creation, RaceLive isolation, skeleton-season orchestration. |
| `src/Peloton.Persistence` | SQLite schema version 8, verified candidate save, envelope identity, snapshot round trip (including course samples, stage times, `PotentialOvr`), integrity checks. |
| `src/Peloton.Content` | JSON pack loaders: skeleton and WT `scenarios` + `roster` + `organizations` + `calendar` + `race-identities`, and `racePrototypeScenarios` (route/tuning templates). |
| `src/Peloton.Infrastructure` | Composition root connecting Application ports to Content, Persistence, and Simulation. |
| `src/Peloton.Client.Godot` | Godot 4.4 .NET career shell + optional Watch Race. Presentation only: Commands + Queries. Main scene `CareerShell.tscn` copies POC v3 chrome. Desk date / Advance Day / Race next / inbox / calendar / default simulate → result table are real. Empty domains (sponsors, scouting, look OVR) come from `CareerLookCatalog`. Watch film is a setting, off by default (D-043 / D-048). Career Hub UI is deleted. |
| `tools/Peloton.SimRunner` | Headless CLI: `run` for skeleton seasons, `race` for the prototype gate, `watch` for rate-controlled supervising-clock output, `day` for Hub, prep, calendar/inbox, result/debrief, and race-due flows. |

Static content lives in `content/peloton.skeleton`, `content/peloton.wt-2026`, and `content/peloton.race-prototype`. `KNOWN_DIFFERENCE_FROM_CODE.md` records remaining prototype limits versus the accepted Race Engine contract.

## Tests

| Project | Coverage |
|---|---|
| `tests/Peloton.Domain.Tests` | Stable ID allocation and no-reuse spine. |
| `tests/Peloton.Simulation.Tests` | Seed derivation plus race physics, groups, decisions, Watch clock invariants, and Spy neutrality. |
| `tests/Peloton.Application.Tests` | GameState guards, prep actions, result/debrief projections, Watch/Simulate parity, content identity, Advance Day, RaceLive isolation, CLI contracts, 10-season determinism. |
| `tests/Peloton.Persistence.Tests` | SQLite schema/content/rules metadata, checksum round trip, prep checkpoint recovery through Results/Debrief, failed-load atomicity, last-race JSON shape. |
| `tests/Peloton.Architecture.Tests` | Forbidden PlayerTeam-like types, no production `StubRaceEngine`, Godot-free headless assemblies. |
| `tests/Peloton.Client.Godot.Tests` | Career shell host (Advance Day / Race next / save-load / default simulate → results table + team filter), look catalog, optional Watch film, interpolator, film duration, map profile sampling. Compiles Godot-free host + catalog sources; does not need the Godot editor. |

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
| Godot Watch Race | `src/Peloton.Client.Godot` (`WatchRaceHost`, `WatchRaceScreen`) | `D-033` renderer; optional film, off by default; `UI_SITEMAP` RaceLive; §49 still owner-only | `WatchRaceHostTests`, `WatchMotionInterpolatorTests`, `WatchFilmDurationTests`, `WatchRouteProfileTests` |
| Godot career shell | `src/Peloton.Client.Godot` (`CareerShellHost`, `CareerShellScreen`, `CareerLookCatalog`, `LookChrome`) | POC v3 chrome; desk/calendar/inbox; default simulate → result table (D-043); look catalog for empty domains; Career Hub deleted (D-048) | `CareerShellHostTests`, `CareerLookCatalogTests` |
| HTML look lab | `HTML_UI_LAB.md`, `peloton-manager-full-ui-poc-v3.html`, `08e-constructivist-desk.html`, `14-race.html` | owner-accepted chrome for management shell; not a client | open in a browser |
| Career day query | `Peloton.Application/CareerDay.cs`, `ClubRosterProjection`, `ClubFinanceProjection` | `GAME_STATES_v0.1.md` Advance Day; desk primary action (`advance-day` / `race-next`); employer roster wages via `ClubRosterProjection`; employer cash via `ClubFinanceProjection`; Management only; not a UI dashboard | Application tests, `day` SimRunner |
| Race preparation | `Peloton.Application/RacePreparation.cs`, `PreSeasonPlanning.cs`, `WorldRaceScenarioAssembler.cs`, `GameApplication.cs` | world roster + route template + entry filter + player strategy (`D-036` phase 1–3); readiness scales CP/Pmax at assemble | Application + Persistence tests |
| RiderCareer / world–race bind | `Peloton.Domain/RiderCareer.cs`, `RiderContract.cs`, `OrganizationRaceEntry`, `WorldRaceScenarioAssembler.cs`, `content/peloton.skeleton/skeleton-roster.json`, `content/peloton.wt-2026/` | `CAREER_WORLDTOUR_SLICE_v0.1.md` phase 1–7; `RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md` (D-049): event-shaped WT fields (TDU 140 / monuments 175 / GT 176 / other WT 154), captain-first squad order, skeleton stays 12; contract expiry on Advance Day; finance tick after expiry; active-contract lookup for wages | `CareerWorldTourBindTests`, `CareerWorldTourPhase2Tests`, `CareerWorldTourPhase3Tests`, `CareerWorldTourPhase4Tests`, `CareerWorldTourPhase5Tests`, `CareerWorldTourPhase6Tests`, `CareerWorldTourPhase7Tests`, `WorldTourFeelProbeTests` |
| Pre-season planning | `Peloton.Application/PreSeasonPlanning.cs`, `GameApplication.cs` | `PreSeasonPlanningFlow` draft entry by `RaceContentId`; confirm commits `OrganizationRaceEntry` | `CareerWorldTourPhase3Tests` |
| Race result / debrief | `Peloton.Application/RaceResultDebrief.cs`, `GameApplication.cs` | committed `LastRace` uses world `RiderCareer.Id`; org on `RaceResultPlacement`; `RaceResultForOrganization` filter (D-043); career history append on `RecordRace` | Application + Persistence tests |
| Contract negotiation | `Peloton.Application/ContractNegotiation.cs`, `GameApplication.cs` | D-044 thin offer/accept in `Management`; no transfer fee | `CareerWorldTourPhase7Tests` |
| Rider ratings (D-046) | `Peloton.Application/RiderRatings.cs`, `RiderRatingProjection.cs`, `ClubRosterProjection` | derived 1–99 from physiology; `PotentialOvr` stored | `RiderRatingTests` |
| Course engine (D-047) | `Peloton.Simulation/Course/*`, `Peloton.Domain/CourseProfile.cs`, `content/peloton.wt-2026/race-identities.json`, `CourseWorldBuilder.cs` | 25 m samples, identity generator, calendar per stage, assembler compile | `CourseEngineTests`, `CourseEngineIntegrationTests` |
| Career calendar | `Peloton.Domain/CalendarEntry.cs`, `Peloton.Application/CareerCalendarInbox.cs` | stored entries + derived status | Application tests |
| Career inbox query | `Peloton.Application/CareerCalendarInbox.cs`, `ArchiveInboxItemCommand` | rebuilt race-due + race-result items; dismiss lock on race-due | Application tests |
| AI managers | Not implemented | `AI_MANAGER_SYSTEM_v0.2.md` | Not implemented |
| Save / SQLite | `Peloton.Persistence` | `SAVE_FORMAT_v0.1.md` | Persistence + Application tests |
| Career scenarios | `content/peloton.skeleton`, `JsonScenarioCatalog.cs` | `CONTENT_FORMAT_v0.1.md` | Application tests |
| Rules modules | `Peloton.Rules`, scenario JSON | `RULESETS_v0.1.md` | Application + Architecture tests |

Transfer market, sponsors, scouting, knowledge records, and `D-032` multi-stage leadership transfer are not implemented. Career Hub UI is deleted (D-048). Godot career shell shows empty domains from `CareerLookCatalog` (look only) and presents D-043 simulate → results. Watch film stays optional and off by default. Look-catalog OVR/cash is not World State. Rider ratings (D-046) and the dense course engine (D-047) are landed.

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

Godot career shell
→ src/Peloton.Client.Godot/CareerShellHost.cs
→ CareerShellScreen.cs
→ CareerLookCatalog.cs (look only)

Godot Watch Race
→ src/Peloton.Client.Godot/WatchRaceHost.cs
→ WatchRaceScreen.cs
→ GameApplication RaceWatch / PendingRaceDecision / RaceResult

SimRunner race gate
→ tools/Peloton.SimRunner/RacePrototypeCommand.cs
→ Program.cs `race` command
```
