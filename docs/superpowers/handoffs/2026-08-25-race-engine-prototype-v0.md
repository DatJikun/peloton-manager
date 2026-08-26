# Race Engine Prototype v0 — Implementation Handoff

**Status:** PARTIAL — Tasks 1–6 green. Do not merge; Tasks 7–8 remain.

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
- Task 4 — knowledge-bounded chase tactics, five DecisionRequest gates, a
  RaceLive-compatible pause/resolution lifecycle, common World Spy traces, and
  Race Spy JSON/Markdown projections.
- Task 5 — data-only `racePrototypeScenarios` JSON pack, strict untrusted-input
  validation, deterministic definition-to-runtime mapping, and a headless
  `IRaceScenarioCatalog` that leaves the existing `scenarios` loader intact.
- Task 6 — `GameApplication` now creates and advances a `PrototypeRaceEngine`
  session from the validated prototype catalog. A pending decision remains in
  `RaceLive`, is exposed through an immutable query projection, and accepts only
  the matching request, authority, and legal strategic option. Completion writes
  the existing `LastRace` JSON shape and follows the unchanged results/debrief flow.
- The verified pre-race autosave remains the only recovery point. Save and load
  commands are still rejected during `RaceLive`; SQLite `SchemaVersion` remains 1.
- `SkeletonCareerRunner` advances the real session and resolves requests through
  their delegated/default policy. The production `StubRaceEngine` path and its
  obsolete test were removed; the physical and decision determinism proofs remain.
- Task 3 drafting/position survival: `MaximumGapDuringPressureM` measures gap to the active ForcePace group while those riders are still racing. `MaximumGapAheadM` is neighbor gap and can invert survival (dropped rider 12 sitting just ahead of 14).

Task 4 keeps tactical evaluation behind `TeamRaceObservation`. The DTO contains
published gaps, visible split/position signals, organization interpretations,
objective, and confidence; it has no rider truth, W' balance, or durability
field. `RaceDecisionRequest` has deterministic identity, authority, a race-time
barrier, defensible options, delegated/default resolution, and explicit
pending/resolved lifecycle.

`IWorldSpySink` is write-only. The no-op and collecting sinks receive the same
already-computed `DecisionTrace`; gameplay never queries them and they own no
RNG. Trace structure keeps actor-known inputs and interpretations separate from
the developer-only `TruthDebugRef`.

Task 5 validates every definition in a pack before constructing a
`RaceScenario`. Stable issue codes cover invalid ranges, duplicate IDs, missing
or wrong-owner references, unsupported fields, oversized/missing resources, and
paths outside the pack. JSON property order, filesystem enumeration order, and
definition-array order do not choose runtime identity. Content IDs are
namespaced strings; only after validation are they mapped in ordinal order to
the transient IDs required by the current prototype contracts. The loader has
no RNG input and allocates no World State.

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

59 passed, 0 failed (including `RaceContentTests` 18/18,
`RacePhysicalProofTests` 7/7, `RaceDecisionAndSpyTests` 6/6, Application 28/28,
Persistence 4/4, Architecture 3/3, and Simulation 23/23).

## Remaining plan

- Task 7: SimRunner `race` command and trace export.
- Task 8: docs, full gate, owner fun §49 still `NOT VERIFIED`.

## Do not claim

- Do not claim the prototype content IDs are a finished career-world participant
  mapping. Task 5 still maps validated content definitions to transient prototype
  IDs; production roster integration remains later work.
- Do not claim the race is fun or §49 passed.
- Do not merge this branch yet.

## Task 6 handoff

**DONE:** Prototype race sessions are authoritative in the career application spine.

**CHANGED:** Application race commands/query, dependency wiring, skeleton runner,
neutral `RaceSummary` naming with unchanged JSON fields, skeleton race-rules identity,
and regression/architecture/persistence coverage.

**TESTED:** `dotnet test PelotonManager.sln --no-restore` — 59 passed, 0 failed.

**NOT TESTED:** Owner fun review (§49), Godot UI, mid-race persistence (explicitly
unsupported), 100-year soak, and Task 7 CLI/trace export.

**RISKS:** The fixture's numeric participant IDs are still the Task 5 transient
prototype mapping rather than career roster identities. This is acceptable only as
the current headless prototype boundary and must not be mistaken for final roster
integration.

**NEXT:** Task 7 — add the SimRunner `race` command and trace export without changing
the official application engine path.

**FILES TO READ:** Task 7 in the execution plan; §11 in the approved design;
`src/Peloton.SimRunner/Program.cs`; the Task 4 Race Spy projections; this handoff.
