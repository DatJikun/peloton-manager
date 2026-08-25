# Race Engine Prototype v0 — Implementation Handoff

**Status:** PARTIAL — Task 3 green. Do not merge; official results still come from `StubRaceEngine`.

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

Also preserve D-002, D-017 through D-025: one human/AI physics model, no direct watt commands, no stamina-zero drop, no hidden-truth decision input, passive RNG-neutral Race Spy.

## Completed

- `6af7095` — approved implementation design.
- `b7c95d7` — eight-task TDD plan.
- `c7079a8` — required-power solver and CP/W'/Pmax/durability capability solver.
- `a4d7f73` — deterministic group ordering, finite shelter slots, dynamic gap grouping.
- `c54f7a1` — canonical `RaceSession.Step()`, batch as a loop, physical fixtures.
- Task 3 drafting/position survival: `MaximumGapDuringPressureM` measures gap to the active ForcePace group while those riders are still racing. `MaximumGapAheadM` is neighbor gap and can invert survival (dropped rider 12 sitting just ahead of 14).

## Root cause of the Task 3 red test

`DraftingPositionChangesEnergyCostAndPaceUpSurvival` failed because `MaximumGapAheadM` was the wrong quantity:

- Rider 12 really split from the ForcePace group during the pace-up (peak neighbor gap ~106 m at t=95, all four still racing). That was a real drop, not a finish-regroup artifact.
- Rider 14's small `MaximumGapAheadM` (~3.6 m) was the gap to already-dropped rider 12, so neighbor-max inverted the survival proof.
- Weak-rider `PeakPowerW` 740 W could not cover first-second ForcePace accel + 90°/7 mps aero even in shelter. Peaks are now identical 880 W so **position/shelter** decides who holds the pace.

The survival assertion was not relaxed. It now uses `MaximumGapDuringPressureM` (`< 5` sheltered, `> 5` exposed) plus energy, remaining W', and finish time.

## Current test state

```text
dotnet test PelotonManager.sln
```

31 passed, 0 failed (including `RacePhysicalProofTests` 7/7 and Simulation 18/18).

## Remaining plan

- Task 4: knowledge-bounded tactics, DecisionRequest (pause stays `RaceLive`), World/Race Spy, Spy OFF/ON checksum identity.
- Task 5: validated JSON prototype pack.
- Task 6: make the real engine authoritative in GameApplication without changing SQLite SchemaVersion 1. Then `StubRaceEngine` stops producing official results.
- Task 7: SimRunner `race` command and trace export.
- Task 8: docs, full gate, owner fun §49 still `NOT VERIFIED`.

## Do not claim

- Do not claim official results use the prototype: GameApplication still uses `StubRaceEngine` until Task 6.
- Do not claim Race Spy neutrality: Race Spy is not implemented.
- Do not claim the race is fun or §49 passed.
- Do not merge this branch yet.
