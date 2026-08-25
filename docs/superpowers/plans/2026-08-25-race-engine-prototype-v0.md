# Race Engine Prototype v0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace seed-ranked official race results with one deterministic,
phase-based, one-second-step race engine that proves the nine prototype
mechanics and exposes knowledge-bounded decisions through Race Spy.

**Architecture:** `PrototypeRaceEngine` creates one transient `RaceSession`.
Live stepping calls `Step()` once; batch execution repeatedly calls the same
method and resolves requests through their declared default. Small solvers own
physics, capability, groups, tactics, knowledge, and diagnostics while
`GameApplication` retains the nine-state and pre-race-save boundaries.

**Tech Stack:** .NET 8, C# with nullable/warnings-as-errors, xUnit, JSON content,
existing SQLite save store, existing deterministic seed derivation.

**Spec:** `docs/superpowers/specs/2026-08-25-race-engine-prototype-v0-design.md`

## Global Constraints

- `RaceLive` remains active when a race DecisionRequest pauses execution.
- `double` and `dt = 1 second` are prototype choices, not permanent locks.
- Automated tests never claim the §49 engagement gate is passed.
- Official results do not come from `StubRaceEngine`.
- Do not change SQLite SchemaVersion 1 or the persisted `lastRace` JSON shape.
- No direct human watt, effort-percent, or attack-duration command.
- Decision logic consumes observations, never rival truth state.
- Race Spy is a passive `IWorldSpySink`; Spy OFF/ON must be gameplay-neutral.
- No Godot dependency, `PlayerTeam`, ad-hoc `Random`, runtime hash seed, generic
  stamina-zero drop, or scripted crosswind split.
- Every production behavior starts with a failing test and an observed RED run.

---

### Task 1: Race contracts, required power, and capability

**Files:**
- Create: `src/Peloton.Simulation/Race/RaceDefinition.cs`
- Create: `src/Peloton.Simulation/Race/RaceRiderProfile.cs`
- Create: `src/Peloton.Simulation/Race/RaceTuning.cs`
- Create: `src/Peloton.Simulation/Race/RequiredPowerSolver.cs`
- Create: `src/Peloton.Simulation/Race/CapabilitySolver.cs`
- Create: `tests/Peloton.Simulation.Tests/Race/RequiredPowerSolverTests.cs`
- Create: `tests/Peloton.Simulation.Tests/Race/CapabilitySolverTests.cs`

**Interfaces:**
- `RequiredPowerSolver.Calculate(RequiredPowerInput) -> RequiredPowerBreakdown`
- `CapabilitySolver.Evaluate(RaceRiderProfile, RiderPhysiologyState,
  desiredPowerW, durationSeconds) -> CapabilityResult`
- Units are explicit in every public member.

- [ ] **Step 1: Write the failing required-power tests**

```csharp
[Fact]
public void ShelterReducesOnlyAerodynamicDemand()
{
    RequiredPowerBreakdown exposed = RequiredPowerSolver.Calculate(Input(1.0));
    RequiredPowerBreakdown sheltered = RequiredPowerSolver.Calculate(Input(0.62));

    Assert.True(sheltered.AerodynamicPowerW < exposed.AerodynamicPowerW);
    Assert.Equal(exposed.RollingPowerW, sheltered.RollingPowerW, 8);
    Assert.Equal(exposed.GravityPowerW, sheltered.GravityPowerW, 8);
}

[Fact]
public void PositiveGradientRaisesRequiredPower()
{
    Assert.True(RequiredPowerSolver.Calculate(Input(1.0, gradient: 0.07)).TotalPowerW
        > RequiredPowerSolver.Calculate(Input(1.0, gradient: 0.0)).TotalPowerW);
}
```

- [ ] **Step 2: Run the two tests and confirm RED because the race contracts do
  not exist**

Run: `dotnet test tests/Peloton.Simulation.Tests --filter RequiredPowerSolverTests`

- [ ] **Step 3: Implement the smallest formula decomposition**

Use:

```text
P_aero = 0.5 * rho * (CdA * shelter) * relativeAirSpeed^3
P_roll = Crr * totalMass * g * cos(atan(gradient)) * groundSpeed
P_gravity = totalMass * g * sin(atan(gradient)) * groundSpeed
P_acceleration = totalMass * acceleration * groundSpeed
```

Validate finite/non-negative masses, speeds, CdA, Crr, air density, and shelter
range. Keep `GravityMps2` named in `RaceTuning`.

- [ ] **Step 4: Run required-power tests and confirm GREEN**

- [ ] **Step 5: Write failing CP/W'/Pmax and durability tests**

```csharp
[Fact]
public void SupraCriticalWorkConsumesWPrimeAndPeakPowerCapsOutput()
{
    CapabilityResult result = CapabilitySolver.Evaluate(Profile(), Fresh(), 900, 10);
    Assert.Equal(Profile().PeakPowerW, result.RealizablePowerW);
    Assert.True(result.NextState.WPrimeRemainingJ < Fresh().WPrimeRemainingJ);
}

[Fact]
public void HighIntensityLoadReducesLatePowerMoreForLowDurabilityRider()
{
    CapabilityResult durable = CapabilitySolver.Evaluate(DurableProfile(), Late(), 520, 60);
    CapabilityResult fragile = CapabilitySolver.Evaluate(FragileProfile(), Late(), 520, 60);
    Assert.True(durable.RealizablePowerW > fragile.RealizablePowerW);
}
```

- [ ] **Step 6: Observe RED, implement named prototype degradation and W'
  recovery rules, then confirm GREEN**

- [ ] **Step 7: Run all Simulation tests and commit**

Commit: `feat(race): add prototype physics and capability solvers`

---

### Task 2: Deterministic slots, shelter, groups, and dynamic gaps

**Files:**
- Create: `src/Peloton.Simulation/Race/RaceRiderState.cs`
- Create: `src/Peloton.Simulation/Race/RaceGroupState.cs`
- Create: `src/Peloton.Simulation/Race/PositionAndGroupResolver.cs`
- Create: `tests/Peloton.Simulation.Tests/Race/PositionAndGroupResolverTests.cs`

**Interfaces:**
- `PositionAndGroupResolver.Resolve(GroupResolutionInput) -> GroupResolution`
- Input is one immutable rider snapshot ordered by distance descending and
  `WorldEntityId` ascending for ties.
- Output supplies deterministic `PositionSlot`, `GroupId`, `GapAheadM`,
  `ShelterMultiplier`, and split/merge transitions.

- [ ] **Step 1: Write failing behavior tests**

```csharp
[Fact]
public void CrosswindLimitsShelteredSlotsWithoutScriptedSplit()
{
    GroupResolution result = Resolve(widthM: 3.2, crosswindMps: 11.0, riders: TwelveRiders());
    Assert.True(result.Riders.Count(rider => rider.ShelterMultiplier < 1.0) < 12);
    Assert.Contains(result.Riders, rider => rider.ShelterMultiplier == 1.0);
}

[Fact]
public void GrowingGapRemovesShelterAndCreatesASeparateGroup()
{
    GroupResolution result = ResolveWithRearGap(8.0);
    Assert.Equal(1.0, result.Riders.Single(r => r.RiderId == RearId).ShelterMultiplier);
    Assert.NotEqual(result.Riders[0].GroupId, result.Riders[^1].GroupId);
}
```

- [ ] **Step 2: Observe RED**

Run: `dotnet test tests/Peloton.Simulation.Tests --filter PositionAndGroupResolverTests`

- [ ] **Step 3: Implement deterministic capacity, slot, gap, and group rules**

Shelter capacity derives from road width and wind yaw. Group boundaries derive
only from longitudinal gaps. No method accepts `crosswindSplit = true` or a
drop flag.

- [ ] **Step 4: Confirm GREEN and add a mutation-oriented tie-order test**

- [ ] **Step 5: Run Simulation tests and commit**

Commit: `feat(race): resolve deterministic shelter and dynamic groups`

---

### Task 3: One canonical stepped race session and physical scenario proofs

**Files:**
- Create: `src/Peloton.Simulation/Race/RaceCommand.cs`
- Create: `src/Peloton.Simulation/Race/RaceResult.cs`
- Create: `src/Peloton.Simulation/Race/RaceResultChecksum.cs`
- Create: `src/Peloton.Simulation/Race/RaceSession.cs`
- Create: `src/Peloton.Simulation/Race/PrototypeRaceEngine.cs`
- Create: `tests/Peloton.Simulation.Tests/Race/RaceScenarioFactory.cs`
- Create: `tests/Peloton.Simulation.Tests/Race/RacePhysicalProofTests.cs`
- Modify: `tests/Peloton.Simulation.Tests/DeterministicSimulationTests.cs`

**Interfaces:**
- `IRaceEngine.CreateSession(RaceScenario, long seed, IWorldSpySink) -> RaceSession`
- `RaceSession.Step() -> RaceStepResult`
- `RaceSession.ResolveDecision(ResolveRaceDecision) -> RaceDecisionResolution`
- `PrototypeRaceEngine.RunBatch(...)` loops `Step()` and uses declared defaults.

- [ ] **Step 1: Write failing batch-versus-step and same-seed tests**

```csharp
[Fact]
public void BatchIsOnlyALoopOverTheCanonicalStep()
{
    RaceResult batch = Engine.RunBatch(Scenario(), 404, NullWorldSpySink.Instance);
    RaceResult stepped = RunEveryStep(Engine.CreateSession(Scenario(), 404, NullWorldSpySink.Instance));
    Assert.Equal(batch.Checksum, stepped.Checksum);
    Assert.Equal(batch.FinishOrder, stepped.FinishOrder);
}
```

- [ ] **Step 2: Observe RED, implement the phase loop and finish ordering, then
  confirm GREEN**

The engine calculates desired motion, required power, realizable motion,
simultaneous distance updates, group resolution, physiology, and completion in
the fixed phase order. Finish ties use stable rider IDs.

- [ ] **Step 3: Add failing headless proofs for drafting survival, repeated
  attacks, late durability, natural dropping, and crosswind splitting**

Assertions use observable metrics and literal thresholds: finish group, maximum
gap, W' remaining, accumulated energy, group count, and finish order. They do
not assert private implementation calls.

- [ ] **Step 4: Observe each RED failure and make the smallest tuning/behavior
  change that produces the physical cause chain**

- [ ] **Step 5: Remove `StubRaceEngine` tests and confirm no official engine
  code ranks entrants from a seed score**

- [ ] **Step 6: Run Simulation tests and commit**

Commit: `feat(race): run canonical stepped prototype scenarios`

---

### Task 4: Knowledge-bounded tactics, briefing, DecisionRequest, and Spy

**Files:**
- Create: `src/Peloton.Domain/DecisionTracing.cs`
- Create: `src/Peloton.Domain/DecisionRequests.cs`
- Create: `src/Peloton.Simulation/Race/RaceKnowledge.cs`
- Create: `src/Peloton.Simulation/Race/RaceTactics.cs`
- Create: `src/Peloton.Simulation/Race/RaceSpy.cs`
- Create: `tests/Peloton.Simulation.Tests/Race/RaceDecisionAndSpyTests.cs`

**Interfaces:**
- `IWorldSpySink.Emit(DecisionTrace)` is write-only from gameplay's perspective.
- `ChaseDecisionEvaluator.Evaluate(TeamRaceObservation, RaceBriefing) -> ChaseDecision`
- `RaceDecisionRequest` owns ID, authority, race second, trigger, options,
  delegated/default option, and resolution lifecycle.

- [ ] **Step 1: Write failing chase-disagreement and briefing tests**

```csharp
[Fact]
public void TeamsCanDisagreeBecauseObjectivesAndEnergyCostDiffer()
{
    ChaseDecision stageHunters = Evaluate(StageHunterObservation(), ChaseBriefing());
    ChaseDecision gcTeam = Evaluate(GcObservation(), ProtectBriefing());
    Assert.Equal(RaceDecisionOption.CommitSupport, stageHunters.SelectedOption);
    Assert.Equal(RaceDecisionOption.WaitForRivals, gcTeam.SelectedOption);
}

[Fact]
public void ProtectAndChaseBriefingsChangeBehaviorNotPhysicsRules()
{
    RaceResult protect = Run(ScenarioWithBriefing(RaceBriefingKind.Protect));
    RaceResult chase = Run(ScenarioWithBriefing(RaceBriefingKind.Chase));
    Assert.NotEqual(protect.TeamEnergyJ[TeamA], chase.TeamEnergyJ[TeamA]);
    Assert.Equal(protect.PhysicsContractVersion, chase.PhysicsContractVersion);
}
```

- [ ] **Step 2: Observe RED and implement evaluator input that contains no
  `RaceRiderTruthState` reference**

- [ ] **Step 3: Write a failing DecisionRequest test with at least two legal,
  non-dominated strategic options and a RaceLive-compatible blocking result**

- [ ] **Step 4: Implement the five decision gates and deterministic request
  identity; confirm GREEN**

- [ ] **Step 5: Write failing Spy tests**

```csharp
[Fact]
public void SpyOnAndOffProduceTheSameOfficialResult()
{
    RaceResult off = RunWith(NullWorldSpySink.Instance);
    CollectingWorldSpySink spy = new();
    RaceResult on = RunWith(spy);
    Assert.Equal(off.Checksum, on.Checksum);
    Assert.Equal(off.FinishOrder, on.FinishOrder);
    Assert.Contains(spy.Traces, trace => trace.ActorKnownInputs.Count > 0
        && trace.ConsideredOptions.Count >= 2
        && trace.SelectedOption.Length > 0);
}
```

- [ ] **Step 6: Observe RED, emit structured decision-time traces, add JSON
  export and concise Markdown projection, then confirm GREEN**

- [ ] **Step 7: Run Domain and Simulation tests and commit**

Commit: `feat(spy): trace knowledge-bounded race decisions`

---

### Task 5: Validated JSON prototype content

**Files:**
- Create: `src/Peloton.Application/RaceContracts.cs`
- Create: `src/Peloton.Content/JsonRacePrototypeCatalog.cs`
- Create: `content/peloton.race-prototype/pack.json`
- Create: `content/peloton.race-prototype/race-prototype.json`
- Create: `tests/Peloton.Application.Tests/RaceContentTests.cs`

**Interfaces:**
- `IRaceScenarioCatalog.Resolve(string scenarioId) -> RaceScenario`
- Resource kind is `racePrototypeScenarios`; existing `scenarios` loading stays
  unchanged.

- [ ] **Step 1: Write failing loader tests for the valid fixture, out-of-range
  CP/W'/mass/CdA/Crr, duplicate rider/team IDs, missing references, and path
  escape**

- [ ] **Step 2: Observe RED and implement canonical file/reference ordering and
  range validation**

- [ ] **Step 3: Confirm the existing skeleton scenario still resolves and all
  Application tests pass**

- [ ] **Step 4: Commit**

Commit: `feat(content): add validated race prototype fixture`

---

### Task 6: GameApplication, official result, and persistence-safe integration

**Files:**
- Modify: `src/Peloton.Application/Commands.cs`
- Modify: `src/Peloton.Application/Contracts.cs`
- Modify: `src/Peloton.Application/GameApplication.cs`
- Modify: `src/Peloton.Application/SkeletonCareerRunner.cs`
- Modify: `src/Peloton.Infrastructure/ApplicationFactory.cs`
- Modify: `src/Peloton.Domain/WorldState.cs`
- Modify: `src/Peloton.Simulation/WorldChecksum.cs`
- Delete: `src/Peloton.Simulation/StubRaceEngine.cs`
- Modify: `src/Peloton.Persistence/SqliteWorldSaveStore.cs`
- Modify: `tests/Peloton.Application.Tests/GameApplicationTests.cs`
- Modify: `tests/Peloton.Persistence.Tests/SqliteWorldSaveStoreTests.cs`
- Modify: `tests/Peloton.Architecture.Tests/AssemblyBoundaryTests.cs`

**Interfaces:**
- Replace `CompleteStubRaceCommand` with `AdvanceRaceCommand` and
  `RespondToRaceDecisionCommand`.
- `GameApplication.PendingRaceDecision` is a read-only query projection while
  State remains `RaceLive`.
- Persist neutral `RaceSummary` with the same JSON properties as the current
  last-race DTO.

- [ ] **Step 1: Write failing Application tests for pre-race autosave, RaceLive
  pause, save rejection, response authority/option validation, official result
  commit, and the unchanged results/debrief transition sequence**

- [ ] **Step 2: Observe RED and inject `IRaceEngine` plus
  `IRaceScenarioCatalog`; implement commands without exposing watts**

- [ ] **Step 3: Write a failing persistence round-trip asserting the neutral
  official result and SchemaVersion 1**

- [ ] **Step 4: Rename domain/DTO code without changing serialized property
  names or SQLite tables; confirm persistence GREEN**

- [ ] **Step 5: Add an architecture test proving production assemblies contain
  no `StubRaceEngine` and rerun the existing forbidden-team/Godot gates**

- [ ] **Step 6: Update the skeleton career runner to resolve every request with
  its declared delegated/default option and use the real engine**

- [ ] **Step 7: Run Application, Persistence, Architecture, and full solution
  tests; commit**

Commit: `feat(app): make prototype engine authoritative for races`

---

### Task 7: SimRunner prototype command and diagnostics export

**Files:**
- Modify: `tools/Peloton.SimRunner/Program.cs`
- Create: `tests/Peloton.Application.Tests/SimRunnerContractTests.cs`

**Interfaces:**
- Preserve existing `run --scenario ... --years ... --seed ...`.
- Add `race --scenario race.prototype.gate --seed <n>
  [--trace-json <path>] [--trace-markdown <path>]`.
- Output stable keys: `winner`, `checksum`, `decisionCount`,
  `spyNeutral`, `crashed`.

- [ ] **Step 1: Write a failing process-level or callable-runner contract test
  for required output and malformed options**

- [ ] **Step 2: Observe RED, extract a small callable command handler if needed,
  and implement the CLI without duplicating race execution**

- [ ] **Step 3: Run the same command twice and verify identical winner,
  checksum, and decision count**

- [ ] **Step 4: Commit**

Commit: `feat(simrunner): expose race prototype gate`

---

### Task 8: Documentation, self-review, and release gate

**Files:**
- Modify: `KNOWN_DIFFERENCE_FROM_CODE.md`
- Modify: `HANDOFF.md`
- Modify: `CODEBASE_MAP.md`
- Modify: `README.md` only if the executable command list needs it

- [ ] **Step 1: Update known differences**

State that the seed-ranking stub is removed/quarantined from official results.
List prototype limitations: one-second `double`, synthetic content, simplified
shelter/slots/durability/knowledge, no Godot, no §49 owner verdict.

- [ ] **Step 2: Update handoff and codebase map with exact projects, commands,
  test locations, and debugging entry points**

- [ ] **Step 3: Run static self-review**

Run:

```text
rg -n "PlayerTeam|IsHumanTeam|GlobalRandom|new Random\(|GetHashCode\(|HashCode\.Combine" src tests tools
rg -n "StubRaceEngine|CompleteStubRaceCommand" src tests tools
git diff --check origin/main...HEAD
```

The first search may find architecture-test string construction only; inspect
every hit. The second search must find no production official-result path.

- [ ] **Step 4: Run the complete gate**

```text
dotnet format PelotonManager.sln --verify-no-changes --no-restore
dotnet build PelotonManager.sln --no-restore
dotnet test PelotonManager.sln --no-build --no-restore
dotnet run --project tools/Peloton.SimRunner --no-build -- run --scenario scenario.peloton.skeleton --years 10 --seed 91234
dotnet run --project tools/Peloton.SimRunner --no-build -- race --scenario race.prototype.gate --seed 91234
```

- [ ] **Step 5: Run the prototype race command twice and compare stable output;
  record owner §49 questions as `NOT VERIFIED`**

- [ ] **Step 6: Review the diff for scope, save/RNG/Spy impact, then commit docs**

Commit: `docs(race): hand off prototype and playtest gate`

- [ ] **Step 7: Push the branch and create a PR to `main` using every field in
  `GITHUB_WORKFLOW_v0.1.md`; merge only after the high-level and automated gates
  pass, leaving the owner fun gate explicitly open**

