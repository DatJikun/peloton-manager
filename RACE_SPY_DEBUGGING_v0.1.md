# Peloton Manager — Race Spy & Race Debugging

**Version:** 0.1  
**Status:** REVIEW  
**Authority:** Race specialization of `WORLD_SPY_AND_DECISION_TRACING_v0.1.md`, under `RACE_ENGINE_DESIGN_v0.2.md`, `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`, `DECISIONS.md`  
**Purpose:** make every important race behavior inspectable without changing the simulation, so AI tactics, rider behavior, information flow and race physics can be debugged and balanced rather than guessed at.

---

## 0. Framework relationship

Race Spy is not a standalone logging architecture.

It is the Race-domain specialization of the shared `World Spy & Decision Trace Framework`.

It reuses:
- common `DecisionTrace` identity,
- truth vs known-input semantics,
- trace levels,
- reproducibility conventions,
- assertion infrastructure,
- structured-first logging.

Race-specific rider/group/physics samples extend the framework.

---

## 1. North star

> **If the simulation makes an important decision or produces an important split, developers must be able to answer: what happened, what did the actor know, what options did it consider, why did it choose this action, and what happened because of it?**

Race Spy is a developer/testing instrument.

It is not normal player omniscience.

---

## 2. Why Race Spy is mandatory

Peloton Manager deliberately avoids:
- scripted race outcomes,
- one universal tactical algorithm,
- AI access to Simulation Truth,
- direct player watt controls,
- random "AI stupidity" rolls.

That means unexpected race behavior can emerge from many layers:

```text
physics
physiology
position
group geometry
information
staff interpretation
briefing
DS traits
manager policy
utility evaluation
uncertainty
seeded close-choice stochasticity
```

Without Race Spy a developer may only see:

> Team B did not chase.

That is insufficient.

Race Spy must be able to distinguish:

```text
A. Team B correctly judged the break harmless.
B. Team B expected Team C to chase.
C. Team B wanted to chase but had no suitable domestique.
D. Team B misjudged the rider because its information was poor.
E. Team B's DS overvalued preserving resources.
F. The decision model contains a bug.
G. Hidden Simulation Truth leaked into AI input.
```

---

## 3. Hard invariants

### RS-001 — Passive observer

Race Spy never mutates World State.

It cannot:
- issue gameplay commands,
- change intent,
- change RNG,
- pause simulation by itself,
- change event ordering.

### RS-002 — RNG neutral

Enabling or disabling Race Spy must produce the same gameplay result.

Logging cannot consume gameplay RNG.

### RS-003 — Renderer neutral

Race Spy works in:
- headless SimRunner,
- watched RaceLive,
- accelerated race,
- automated test.

The renderer is not required.

### RS-004 — Truth and actor knowledge are separate

Race Spy may show Simulation Truth because it is a developer tool.

But every AI/DS decision trace must separately record:

```text
TruthSnapshot          [debug only]
ActorKnownInputs       [what actor was allowed to know]
ActorInterpretations   [what actor believed]
```

These must never be silently merged.

### RS-005 — No retroactive explanation

The reason shown for a decision must come from the decision-time trace.

Do not regenerate an explanation after the race using information learned later.

### RS-006 — Bounded retention

Verbose Race Spy data is diagnostic data, not permanent 100-year history.

Normal career saves do not retain every race tick.

Selected reports/traces may be exported for testing.

---

## 4. Observation levels

Race Spy should support explicit levels.

### OFF

No diagnostic tracing beyond mandatory deterministic checksums/errors.

### DECISIONS

Recommended default for development.

Records:
- significant DS decisions,
- team tactical decisions,
- DecisionRequests,
- objective switches,
- chase decisions,
- attacks,
- resource sacrifices.

### TACTICAL

Adds:
- group splits/merges,
- major positioning changes,
- rider drops/returns,
- major shelter changes,
- important local rider intent changes.

### VERBOSE

Adds high-resolution diagnostic samples:
- required power,
- realizable power,
- W' state,
- gap/shelter transitions,
- slot resolution,
- utility components.

Verbose mode is for short reproducible scenarios, not full-season permanent logging.

---

## 5. Core trace types

Race Spy should not create one giant untyped log.

Suggested trace types:

```text
RaceSpyDecisionTrace
RaceSpyRiderStateSample
RaceSpyGroupTrace
RaceSpyInformationTrace
RaceSpyPhysicsTrace
RaceSpyIncidentTrace
RaceSpyAssertionFailure
RaceSpyCheckpoint
```

---

## 6. Decision trace

Suggested structure:

```text
RaceSpyDecisionTrace
{
    RaceId
    SimulationTime
    DecisionId

    ActorPersonId
    OrganizationId
    ActorRole

    DecisionType
    Trigger

    BriefingContext
    CurrentObjective

    ActorKnownInputs[]
    ActorInterpretations[]
    Confidence

    ConsideredOptions[]
    UtilityBreakdown[]

    SelectedOption
    SelectionReason

    StochasticTieBreak?
    RandomDomain?
    RandomKey?

    CommandsEmitted[]

    RelatedEntities[]
    RelatedGroups[]

    TruthSnapshotRef        // debug only
}
```

Important:

`UtilityBreakdown` is diagnostic.

The player-facing game does not need to expose exact hidden utility numbers.

---

## 7. Example: chase decision

Race Spy report:

```text
12:42:18 — TEAM 17 — CHASE_DECISION

Trigger:
Break gap increased from 02:10 to 03:05.

Known to Team 17:
- Rider 402 in break estimated as strong finisher.
- Virtual GC threat estimated LOW.
- Team 21 has a top sprinter.
- Team 21 has 4 domestiques in peloton.
- Own domestiques A/B have already spent substantial energy.

DS interpretation:
"Break is dangerous for stage objective,
but Team 21 has stronger incentive to chase."

Options considered:

1. Commit Rider A + Rider B
   Sporting benefit: HIGH
   Energy cost: HIGH
   Tomorrow cost: MEDIUM
   Expected rival contribution: LOW/MEDIUM

2. Wait for Team 21
   Sporting benefit: MEDIUM
   Energy cost: LOW
   Risk of gap growth: MEDIUM

3. Attack from peloton
   Sporting benefit: LOW
   Energy cost: MEDIUM

Selected:
WAIT_FOR_RIVAL

Reasons:
- preserve lead-out resources,
- expected Team 21 intervention,
- GC unaffected.

Confidence:
0.64
```

Then Race Spy can later link outcome:

```text
Outcome +5 min:
Team 21 also waited.
Gap increased to 05:12.
Decision became costly.
```

That does not mean the original decision was irrational.

---

## 8. Truth-vs-belief comparison

Developer view may optionally display:

```text
ACTOR BELIEF
Rider 402 condition: Strong
confidence: Medium

SIMULATION TRUTH
Rider 402:
W' balance: low
durability loss: high
actual late-race capability: deteriorating
```

Diagnostic conclusion:

```text
AI did NOT cheat.
Its decision was based on a plausible but wrong estimate.
```

This is essential for validating organization-scoped uncertainty.

---

## 9. "Why did this rider drop?"

Race Spy should build a causal chain from state transitions.

Example:

```text
Rider 88 dropped at km 141.8

Cause chain:

1. Entered exposed sector at position 61.
2. Sheltered capacity approximately 34 riders.
3. Effective aero cost increased.
4. Corner exit required 612 W for 8 s.
5. Rider had reduced short-duration capability after high-intensity load.
6. Realized speed fell 0.8 m/s below group.
7. Gap increased to 3.4 m.
8. Shelter decreased further.
9. Bridge demand exceeded current realizable short-duration power.
10. Gap became self-reinforcing.
```

This is far more useful than:

```text
Dropped: stamina depleted
```

---

## 10. "Why did this attack fail?"

Example:

```text
Attack Rider 201 — km 37.4

Attacker:
- good position,
- strong W' reserve,
- high desired acceleration.

Failure reasons:
- road speed already high,
- aero cost very high,
- two rivals immediately responded,
- group behind had high chase incentive,
- attack lasted too little to create shelter-breaking gap.

Result:
maximum gap 4.1 s
caught after 52 s
```

A later repeated attack may succeed because opponent state has changed.

---

## 11. "Why didn't the DS ask the player?"

DecisionRequest diagnostics should expose gates:

```text
MaterialityGate: PASS
ChoiceGate: PASS
DelegationGate: FAIL
Reason:
Briefing ORDER already covers this situation.

InformationGate: PASS
NoveltyGate: PASS

DecisionRequest created: NO
DS resolved automatically.
```

or:

```text
ChoiceGate: FAIL
Reason:
All evaluated alternatives dominated by immediate leader rescue.

DecisionRequest created: NO
```

This is mandatory for debugging popup fatigue and missing consultations.

---

## 12. Group Spy

For a selected group:

```text
GroupId
time
members
speed
length
density
road width
wind yaw
shelter capacity
front contributors
team intents
internal gaps
split risk
```

Developer can answer:

> Why is this peloton suddenly expensive?

Example:

```text
road narrowed 7.2m → 4.1m
crosswind yaw increased
front speed +4.8 km/h
effective sheltered capacity fell
rear acceleration variance increased
```

---

## 13. Rider Spy

For a selected rider Race Spy may graph/sample:

```text
required power
realized power
effective CP
W' balance
durability state
position
shelter
gap ahead
gap behind
current intent
team instruction
self-assessment
reported condition
```

Important:

Normal RaceLive UI does not receive this truth-level view.

---

## 14. Information Spy

A dedicated information timeline records what became known and when.

Example:

```text
12:11:04 Truth:
Rider 55 begins mechanical problem.

12:11:08 Rider perception:
Rider notices drivetrain issue.

12:11:12 TeamRadioSignal:
"gear problem"

12:11:13 Team knowledge:
mechanical suspected, severity unknown.

12:11:18 TV signal:
Rider visibly dropping.

12:11:22 Rival Team 8 interpretation:
possible fatigue/mechanical, low confidence.
```

This lets us find impossible AI knowledge leaks.

---

## 15. Briefing Spy

Show which briefing rules currently influence behavior.

Example:

```text
Active policy:
SECOND_LEADER_PRESERVE

Source:
Pre-race briefing / GUIDELINE

Current evaluation:
Second leader ahead in group of 7.
Primary leader safe.
No immediate GC emergency.

Effect:
DS will not call Rider B back yet.
```

If DS overrides:

```text
Override:
Primary leader mechanical.

Reason:
Emergency condition dominates guideline.

DecisionRecord:
...
```

---

## 16. Trait contribution

When practical, Race Spy should show how traits changed interpretation/choice.

Do not reduce the entire AI personality to fixed percentage bonuses.

Example:

```text
DS traits affecting decision:

RiskTolerance:
made waiting more acceptable.

LeaderLoyalty:
increased cost of exposing GC leader.

FormSensitivity:
increased weight of today's rider report.

DataReliance:
low effect because available telemetry is weak.
```

This is especially important for long-run manager balance testing.

---

## 17. Close-choice stochasticity

If the AI decision model uses seeded stochasticity among near-equivalent choices, Race Spy must expose it.

Example:

```text
Option A estimated utility: 71 ± uncertainty
Option B estimated utility: 70 ± uncertainty

Difference below decision confidence threshold.

Seeded close-choice resolution used:
RandomDomain = RaceAIDecision
RandomKey = ...
Selected = Option B
```

Never report:

```text
AI randomly chose B.
```

The stochasticity must have a defined role and deterministic key.

---

## 18. Race Spy assertions

Race Spy should actively flag impossible or suspicious behavior.

Examples:

### HiddenTruthLeak

AI decision input references a field not present in actor AccessContext / race knowledge.

### UnexplainedDecision

Significant AI command has no DecisionRecord/reason.

### PresentationAffectsSimulation

Race checksum differs with renderer/Race Spy configuration.

### InvalidKnowledgeTimestamp

Actor uses information before publication time.

### DuplicateDecisionResolution

Same DecisionRequest resolves more than once.

### ImpossibleCommand

Actor emits command inconsistent with rules/state.

### UtilityNaN

Evaluation produces invalid numeric value.

### NonCanonicalOrder

Observed processing order violates race phase ordering.

---

## 19. Outcome linking

A decision is not judged only by final result.

Race Spy may attach outcome windows:

```text
+30 seconds
+5 minutes
next major race event
finish
next stage [if relevant]
```

Example:

```text
Decision:
Commit domestiques to chase.

+5 min:
gap reduced 01:40.

Finish:
sprinter finished 3rd.

Next stage:
both domestiques have increased fatigue.
```

This helps measure actual strategic costs.

---

## 20. Race report

At race finish Race Spy can generate a structured developer report.

Suggested sections:

```text
1. Race summary
2. Decisive physical moments
3. Group splits / merges
4. Major team decisions
5. Major rider failures / recoveries
6. Information mistakes
7. DS decisions by organization
8. Player DecisionRequests and gate diagnostics
9. AI disagreements
10. Suspicious/assertion events
11. Key energy/resource costs
12. Determinism checksum
```

---

## 21. AI disagreement report

Especially useful scenario:

```text
Same rider attack:
Team A assessment: CRITICAL THREAT
Team B assessment: MODERATE THREAT
Team C assessment: LOW THREAT
```

Race Spy explains:

```text
Team A:
rider estimated high ability,
GC priority high.

Team B:
similar ability estimate,
but expects Team A to chase.

Team C:
poor scouting,
believes attacker cannot sustain move.
```

This proves different behavior comes from knowledge/goals/traits rather than arbitrary team archetypes.

---

## 22. Race Spy and 100-year balance lab

Full verbose Race Spy is not used for every race in a 100-year run.

Instead long simulations may record:
- aggregate decision statistics,
- sampled decision traces,
- assertion failures,
- outlier races,
- tagged suspicious behavior.

Examples of automatic capture triggers:

```text
favorite loses unexpectedly
break wins by extreme margin
one team spends abnormal energy
AI repeatedly refuses rational chase
same manager tactic dominates era
major group split occurs
impossible hidden-knowledge assertion fires
```

The simulator can then preserve the relevant Race Spy trace for inspection.

---

## 23. Outlier capture

A major goal is:

> Do not manually search through 100 years for the weird race.

SimRunner should be able to mark races such as:

```text
OUTLIER:
Top favorite lost 17 minutes on flat stage.

Capture:
race seed
resolved content/rules
start state/checkpoint
commands
Race Spy tactical trace
```

Then the race can be reproduced exactly.

---

## 24. Reproduction bundle

For a failed/suspicious race, Race Spy should eventually export a minimal reproducibility package containing:

```text
SimulationBuildVersion
ResolvedContentHash
RulesetHash
RaceId
RaceSeed
Required start-state snapshot
Command sequence
Relevant DecisionRecords
Race Spy trace
Expected checksum / observed checksum
```

This becomes an excellent handoff artifact between AI coding sessions.

---

## 25. Performance

Race Spy must be designed to minimize distortion of profiling.

Therefore:
- tracing is configurable,
- verbose samples can be downsampled,
- strings should be projected after simulation where possible,
- core traces should prefer structured IDs/codes/data,
- report prose is generated from structured trace.

Race Spy OFF and DECISIONS modes must be benchmarked separately.

---

## 26. Storage

Normal save:
- no permanent verbose race trace.

Development artifact:
- JSONL / structured debug format is preferred for traces,
- optional human-readable Markdown/HTML report generated from structured data.

Important permanent historical race decisions follow normal Race History retention rules.

Race Spy is not the historical database.

---

## 27. Privacy / normal UI boundary

Race Spy must be inaccessible from a standard non-debug career unless deliberately exposed as a developer mode.

Otherwise it would destroy:
- fog of war,
- scouting uncertainty,
- race uncertainty,
- competitive information.

Player-facing post-race "Why?" is generated from organization-legal information.

Race Spy's truth comparison is developer-only.

---

## 28. Prototype implementation gate

Race Spy should exist very early in the headless Race Engine spike.

Minimum implementation before complex AI tactics:

```text
- selected rider state trace,
- selected group state trace,
- team DecisionRecord trace,
- DecisionRequest gate trace,
- truth-vs-known-input separation,
- deterministic race checksum,
- Markdown/JSON report generation.
```

Do not wait until race AI is "finished".

The tool is how race AI becomes finishable.

---

## 29. Prototype report questions

Every P0–P6 Race Engine spike should be answerable through Race Spy:

### P1 Mountain pacing
Why did rider X lose contact?

### P2 Repeated attacks
How much did each attack change later available performance?

### P3 Crosswind
Which riders lost shelter and why?

### P4 Closeable gap
Why did one rider return and another fail?

### P5 Who chases?
Why did each team choose chase/wait/attack?

### P6 Briefing
Which briefing rule changed behavior?

If Race Spy cannot answer these, the prototype is not considered debuggable.

---

## 30. Definition of success

Race Spy v0.1 is successful when an unexpected race outcome can be investigated without:
- reading arbitrary code paths,
- adding ad-hoc print statements,
- guessing hidden AI state,
- rerunning until the bug disappears.

For any major decision the developer can reconstruct:

```text
WHAT HAPPENED
WHAT THE ACTOR KNEW
WHAT THE ACTOR BELIEVED
WHAT IT WANTED
WHAT OPTIONS IT CONSIDERED
WHY IT CHOSE THIS
WHAT COMMAND IT EMITTED
WHAT HAPPENED NEXT
```

That is the standard.
