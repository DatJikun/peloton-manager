# Race Engine Prototype v0 — Implementation Handoff

**Status:** COMPLETE for Tasks 1–8. Owner §49 remains `NOT VERIFIED`.

**Branch:** `feature/race-engine-prototype`

**Approved spec:**
`docs/superpowers/specs/2026-08-25-race-engine-prototype-v0-design.md`

**Execution plan:**
`docs/superpowers/plans/2026-08-25-race-engine-prototype-v0.md`

## Owner locks to preserve

1. A race `DecisionRequest` pauses inside `RaceLive`; no tenth GameState.
2. `double` and a fixed one-second step are prototype choices, not permanent locks.
3. Automated tests do not pass the §49 fun gate. Owner playtest remains `NOT VERIFIED`.
4. Official results cannot come from `StubRaceEngine`; do not change SQLite schema without necessity/migration. Mid-race save remains forbidden.
5. `D-032` (failed designated leader becoming support in a multi-stage race) is deferred. Do not add virtual GC leadership transfer to this one-day prototype.

Also preserve D-002, D-017 through D-025: one human/AI physics model, no direct watt commands, no stamina-zero drop, no hidden-truth decision input, passive RNG-neutral Race Spy.

## Completed

- Tasks 1–3: required-power / capability, groups/shelter/gaps, canonical `RaceSession.Step`, physical proofs.
- Task 3 drafting fix: `MaximumGapDuringPressureM` versus the active ForcePace group; identical weak-rider peaks 880 W.
- Task 4: knowledge-bounded chase tactics, DecisionRequest pause/resolve, Race Spy JSON/Markdown, Spy ON/OFF same checksum.
- Task 5: `content/peloton.race-prototype`, kind `racePrototypeScenarios`, `JsonRacePrototypeCatalog`.
- Task 6: `GameApplication` uses `PrototypeRaceEngine`. Commands: `StartRaceCommand`, `AdvanceRaceCommand`, `RespondToRaceDecisionCommand`. `StubRaceEngine` deleted. SchemaVersion 1 unchanged.
- Task 7: SimRunner `race --scenario --seed` with optional `--trace-json` / `--trace-markdown`. Alias `race.prototype.gate` → `race-scenario.peloton.prototype-v0`. Output: `winner`, `checksum`, `decisionCount`, `spyNeutral`, `crashed`.
- Task 8: `KNOWN_DIFFERENCE_FROM_CODE.md`, `HANDOFF.md`, `CODEBASE_MAP.md`, `README.md`, `AGENTS.md` updated. Fun gate still open.

## Current test state

Run from repository root:

```text
dotnet format PelotonManager.sln --verify-no-changes
dotnet build PelotonManager.sln
dotnet test PelotonManager.sln
dotnet run --project tools/Peloton.SimRunner -- run --scenario scenario.peloton.skeleton --years 10 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- race --scenario race-scenario.peloton.prototype-v0 --seed 91234
```

## Do not claim

- Do not claim the prototype content IDs are a finished career-world participant mapping.
- Do not claim the race is fun or §49 passed.
- Do not claim `D-032` is implemented.

## Task 7–8 handoff

**DONE:** Headless race gate CLI plus documentation that the prototype is official and still limited.

**CHANGED:** SimRunner command dispatch (`run` preserved, `race` added), callable `RacePrototypeCommand`, contract tests, navigation/handoff docs.

**NOT TESTED:** Owner fun review (§49), Godot UI, mid-race persistence (unsupported), 100-year soak, multi-stage GC leadership transfer.

**NEXT:** Owner watches a prototype race. Separate later work: `D-032`.

**FILES TO READ:** this handoff; `KNOWN_DIFFERENCE_FROM_CODE.md`; `tools/Peloton.SimRunner/RacePrototypeCommand.cs`; `tests/Peloton.Application.Tests/SimRunnerContractTests.cs`.
