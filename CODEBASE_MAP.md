# Peloton Manager — CODEBASE MAP

**Status:** ACTIVE — Milestone 0 architecture skeleton.

This file is a navigation map, not implementation documentation. Design contracts remain authoritative.

## Solution structure

| Project | Current responsibility |
|---|---|
| `src/Peloton.Domain` | World root, stable IDs, people, ManagerCareer, Employment, Organization, DecisionAuthority, AccessContext, content/rules identity. |
| `src/Peloton.Rules` | Stable rules-module identity and deterministic aggregate identity. No full legal engine yet. |
| `src/Peloton.Simulation` | Versioned seed derivation, isolated deterministic RNG, whole-world day scheduler, checksum, explicit stub race. |
| `src/Peloton.Application` | Canonical nine-state machine, Commands, save/content ports, world creation, race isolation, skeleton-season orchestration. |
| `src/Peloton.Persistence` | SQLite schema version 1, verified candidate save, envelope identity, snapshot round trip, integrity checks. |
| `src/Peloton.Content` | Headless JSON pack/scenario loader and validation for the skeleton pack. |
| `src/Peloton.Infrastructure` | Composition root connecting Application ports to Content, Persistence, and Simulation. |
| `src/Peloton.Client.Godot` | Empty future-client boundary; no Godot SDK or scene code in Milestone 0. |
| `tools/Peloton.SimRunner` | Headless CLI for repeatable multi-season runs. |

Static skeleton content lives in `content/peloton.skeleton`. `KNOWN_DIFFERENCE_FROM_CODE.md` records where the race/checksum/allocator skeleton is intentionally below the accepted future contracts.

## Tests

| Project | Coverage |
|---|---|
| `tests/Peloton.Domain.Tests` | Stable ID allocation and no-reuse spine. |
| `tests/Peloton.Simulation.Tests` | Seeded stub-race golden and repeatability. |
| `tests/Peloton.Application.Tests` | GameState list/guards, content recipe identity, whole-world Advance Day, RaceLive isolation/recovery, 10-season headless determinism. |
| `tests/Peloton.Persistence.Tests` | SQLite schema/content/rules metadata, checksum round trip, failed-load atomicity. |
| `tests/Peloton.Architecture.Tests` | Forbidden PlayerTeam-like types and Godot-free headless assemblies. |

## System ownership

| System | Main project/folder | Design authority | Tests |
|---|---|---|---|
| World time / scheduler | `Peloton.Simulation/DeterministicScheduler.cs` | `ARCHITECTURE.md`, determinism contract | Simulation + Application tests |
| GameState / Commands | `Peloton.Application` | `GAME_STATES_v0.1.md` | Application tests |
| Identity / manager spine | `Peloton.Domain` | `DATA_MODEL_v0.1.md` | Domain + Persistence tests |
| Race stub | `Peloton.Simulation/StubRaceEngine.cs` | `KNOWN_DIFFERENCE_FROM_CODE.md`; future authority is `RACE_ENGINE_DESIGN_v0.2.md` | Simulation + Application tests |
| Race Spy | Not implemented | `RACE_SPY_DEBUGGING_v0.1.md` | Not implemented |
| World Spy | Not implemented | `WORLD_SPY_AND_DECISION_TRACING_v0.1.md` | Not implemented |
| AI managers | Not implemented | `AI_MANAGER_SYSTEM_v0.2.md` | Not implemented |
| Save / SQLite | `Peloton.Persistence` | `SAVE_FORMAT_v0.1.md` | Persistence + Application tests |
| Content packs | `content/`, `Peloton.Content` | `CONTENT_FORMAT_v0.1.md` | Application tests |
| Rules modules | `Peloton.Rules`, scenario JSON | `RULESETS_v0.1.md` | Application + Architecture tests |

Recruitment, contracts, sponsors, knowledge records, full calendar, full Race Engine, Race Spy, World Spy, and Godot UI are not implemented in Milestone 0.

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
Peloton.SimRunner -> Peloton.Infrastructure + Peloton.Application
```

Domain, Rules, Simulation, and Persistence have no Godot reference. Godot never writes SQLite.

## Where to start debugging

```text
State/command rejection
→ Peloton.Application/GameApplication.cs
→ owning GameState guard

Determinism mismatch
→ Peloton.Simulation/WorldChecksum.cs
→ StableSeedDerivation / StubRaceEngine / command order

Save/load failure
→ Peloton.Persistence/SqliteWorldSaveStore.cs
→ schema/content/rules metadata
→ SQLite quick_check and checksum

Content creation failure
→ Peloton.Content/JsonScenarioCatalog.cs
→ content/peloton.skeleton
```
