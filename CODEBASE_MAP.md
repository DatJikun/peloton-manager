# Peloton Manager — CODEBASE MAP

**Status:** TEMPLATE — populate during repository bootstrap.

This file is a navigation map, not implementation documentation.

## Solution structure

```text
src/
  Peloton.Domain/
  Peloton.Simulation/
  Peloton.Rules/
  Peloton.Application/
  Peloton.Persistence/
  Peloton.Content/
  Peloton.Infrastructure/
  Peloton.Client.Godot/

tools/
  Peloton.SimRunner/
  Peloton.ContentValidator/
  Peloton.DatabaseEditor/      [later]

tests/
  Peloton.Domain.Tests/
  Peloton.Simulation.Tests/
  Peloton.Application.Tests/
  Peloton.Persistence.Tests/
  Peloton.Architecture.Tests/
```

## System ownership

| System | Main project/folder | Design authority | Tests |
|---|---|---|---|
| World time / scheduler | TBD | ARCHITECTURE + determinism contracts | TBD |
| Race Engine | TBD | RACE_ENGINE_DESIGN | TBD |
| Race Spy | TBD | RACE_SPY_DEBUGGING | TBD |
| World Spy | TBD | WORLD_SPY_AND_DECISION_TRACING | TBD |
| Recruitment | TBD | design/data model | TBD |
| Contracts | TBD | design/data model | TBD |
| Organization Knowledge | TBD | data model | TBD |
| AI managers | TBD | AI_MANAGER_SYSTEM | TBD |
| Sponsors | TBD | design/rulesets | TBD |
| Save/SQLite | TBD | SAVE_FORMAT | TBD |
| Content packs | TBD | CONTENT_FORMAT | TBD |

## Dependency direction

Populate with real project references after bootstrap.

## Where to start debugging

```text
Unexpected automated decision
→ World Spy trace
→ decision ID
→ command
→ owning Application/Domain system

Unexpected race result
→ Race Spy
→ reproduction bundle
→ physics/group/intent trace

Save corruption
→ migration/version logs
→ persistence tests
→ DB integrity report

Determinism mismatch
→ checksum
→ RNG/event ordering trace
```
