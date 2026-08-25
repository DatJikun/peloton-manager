# Race Engine Prototype v0 — Implementation Handoff

**Status:** PARTIAL — do not merge or open a ready-for-review PR.

**Branch:** `feature/race-engine-prototype`

**Worktree:** `C:\Users\mwojn\Desktop\Peloton Manager\.worktrees\race-engine-prototype`

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

## Completed and committed

- `6af7095` — approved implementation design.
- `b7c95d7` — eight-task TDD plan.
- `c7079a8` — required-power solver and CP/W'/Pmax/durability capability solver.
- `a4d7f73` — deterministic group ordering, finite shelter slots, dynamic gap grouping.

Task 1 and Task 2 from the plan are complete. Their Simulation suite passed 11/11 before Task 3 began.

## Current uncommitted/WIP checkpoint

Task 3 now contains:

- route and scenario contracts;
- strategic race commands (`HoldPosition`, `ForcePace`, `Attack`, `Conserve`), never watts;
- one `RaceSession.Step()` with a fixed one-second phase loop;
- `PrototypeRaceEngine.RunBatch()` implemented only as a loop over `Step()`;
- required-power/capability/group integration;
- natural distance/gap/shelter updates;
- W' and durability state updates;
- official result metrics and deterministic race checksum;
- test fixtures for parity, repeatability, drafting/position, repeated attacks, durability, natural dropping, and crosswind.

No Application, persistence, content-pack, DecisionRequest, Spy, or SimRunner integration has started.

## Current test state

Last command:

```text
dotnet test tests/Peloton.Simulation.Tests/Peloton.Simulation.Tests.csproj --filter RacePhysicalProofTests --no-restore
```

Before the final drafting-fixture adjustment, 6 of 7 `RacePhysicalProofTests` passed:

- batch versus step parity — PASS;
- same-seed result/checksum — PASS;
- repeated attacks/W' selection — PASS;
- late durability difference — PASS;
- natural gap + lost shelter dropping — PASS;
- crosswind + finite shelter split — PASS;
- drafting/position pace-up survival — FAIL.

The current failing assertion is the sheltered rider's `MaximumGapAheadM < 5`.
Latest diagnostic values:

```text
sheltered rider 12:
finish=163.3684 s
energy=75,615 J
W'=3,326 J
maxGapAhead=106.45 m
lostShelterTransitions=1

exposed rider 14:
finish=163.6988 s
energy=76,352 J
W'=2,701 J
maxGapAhead=3.63 m
lostShelterTransitions=0
```

The lower energy, higher remaining W', and faster finish correctly favor the initially sheltered rider, but `MaximumGapAheadM` currently becomes misleading as ordering/groups change near the finish. Do not simply delete the survival requirement or relax it to make the test green.

## Recommended next action

Use `superpowers:systematic-debugging` before changing code.

1. Re-run the one failing test and inspect when rider 12's 106 m gap is recorded.
2. Determine whether `MaximumGapAheadM` is measuring a gap to the next active rider after leaders finish rather than survival of the original pace-up.
3. Prefer an honest phase/window metric such as `MaximumGapDuringPressureM`, `SeparatedDuringPressure`, or a group-membership outcome captured before finish. The production change must be driven by a new/adjusted failing behavior test.
4. Keep the already observed causal differences (energy, W', finish time), but retain an explicit proof that position can change pace-up survival.
5. Once Task 3 is green, run the full Simulation project and commit it as planned before starting Task 4.

## Important implementation notes

- `RaceSession` clamps desired acceleration to `RaceTuning.MaximumDesiredAccelerationMps2 = 0.5`; this fixed a real prototype failure where a tired rider could become trapped by unbounded catch-up acceleration demand.
- Group resolution is simultaneous and ordered by distance then stable rider ID.
- Shelter modifies CdA only.
- Dropping is speed deficit → gap → shelter loss; there is no stamina/drop flag.
- The current engine is deterministic but has no gameplay RNG use yet. Do not add `new Random()`.
- `RaceResultChecksum` records deterministic `double` values for same-build reference testing. Cross-platform numeric identity remains open/NOT VERIFIED.
- No save schema or content schema has changed.

## Remaining plan

- Finish Task 3 and commit canonical stepped physical scenarios.
- Task 4: knowledge-bounded tactics, DecisionRequest, common World Spy/Race Spy.
- Task 5: validated JSON prototype pack.
- Task 6: make the real engine authoritative in GameApplication without changing SQLite SchemaVersion 1.
- Task 7: SimRunner `race` command and trace export.
- Task 8: docs, full build/test/determinism gate, PR, owner fun gate still open.

## Do not claim

- Do not claim all tests pass: one Task 3 test currently fails.
- Do not claim official results use the prototype: GameApplication still uses `StubRaceEngine` until Task 6.
- Do not claim Race Spy neutrality: Race Spy is not implemented.
- Do not claim the race is fun or §49 passed.
- Do not merge this checkpoint.

