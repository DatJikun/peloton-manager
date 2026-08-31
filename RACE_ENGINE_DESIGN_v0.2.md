# Peloton Manager — Race Engine Design

**Version:** 0.2  
**Status:** REVIEW  
**Authority:** System design under `DECISIONS.md`, `VISION.md`, `ARCHITECTURE.md`, and `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`  
**Primary source basis:** `RACE_ENGINE_RESEARCH_2026-08-25.md`  
**Purpose:** define the first playable, deterministic and explainable race simulation model without overbuilding physiology before the core race loop proves fun.

---

## 1. Player value

The race engine exists to create:

- believable cycling outcomes,
- meaningful strategic trade-offs,
- situations worth watching,
- decisions that are not obvious,
- consequences that can be explained after the race,
- a world in which AI organizations race under the same information and physical rules as the human organization.

The race engine is not successful merely because its finishing order looks plausible.

It is successful when the player can say:

> "I understand why that happened, I had a meaningful choice, and I want to see the next race."

---

## 2. North star

> **A rider does not drop because a stamina bar reached zero. A rider drops because the current situation demands more than the rider can currently produce, creating a gap that can make the situation even more expensive.**

Canonical conceptual loop:

```text
route / environment
↓
required physical power
↓
current rider capability
↓
position + shelter + group context
↓
rider response / intent
↓
realized speed
↓
gap / position changes
↓
new required physical power
```

This loop should naturally produce:
- gradual climbing losses,
- repeated-attack selection,
- elastic/gaps at the back of groups,
- crosswind splits,
- riders returning after a short gap,
- riders failing to return because loss of shelter increases cost,
- domestiques intentionally easing after their job is finished.

No universal `Dropped = true because Stamina <= 0` rule exists.

---

## 3. Locked race principles

### R-001 — One canonical race model

`Watch Race`, accelerated live race and headless simulation use the same race rules and state transitions.

Presentation mode does not change race physics.

Future performance optimizations may batch or skip mathematically safe intervals, but they may not create a separate "player race engine" with different behavior.

### R-002 — Delegation, not direct leg control

The human manager never sets:
- exact watts,
- effort percentages,
- exact attack duration,
- exact rider acceleration,
- bottle timing every few minutes.

The human sets:
- objectives,
- roles,
- conditional tactical policies,
- risk appetite,
- resource priorities,
- strategic overrides when consulted.

The DS and riders translate those instructions into local race execution.

### R-003 — Rider archetypes emerge

The primary race model does not depend on magic determinant stats such as:

```text
Climbing = 84
Flat = 77
Hills = 81
```

UI may later expose descriptive summaries or estimates, but performance is mainly derived from underlying physiology, physical characteristics, current state, equipment, positioning and race context.

### R-004 — Position is performance

Positioning is not merely a sprint modifier.

Position affects:
- shelter,
- number of accelerations,
- exposure to gaps,
- corner cost,
- crosswind risk,
- access to protected slots,
- ability to respond without first closing distance.

### R-005 — Drafting modifies aero, not total power

Shelter primarily changes the aerodynamic component of required power.

It does not multiply the rider's entire power requirement by an arbitrary draft percentage.

### R-006 — Fatigue is multi-timescale

The model must support distinct processes operating over different time horizons.

Prototype v0 uses only the minimum required subset.

Later systems may include:
- W' balance: seconds–minutes,
- thermal/fluid state: minutes–hours,
- glycogen availability: hours/day,
- durability/acute fatigue: hours–days,
- muscle damage/illness/injury: multi-day.

### R-007 — Race information obeys the normal knowledge boundary

Simulation knows exact internal race state.

DS, riders, AI organizations and the human manager do not automatically know it.

```text
RaceTruth
↓
ObservationSignal
↓
RaceKnowledge / Interpretation
↓
Decision
```

No RaceLive UI may become a hidden-truth debugger.

### R-008 — Interesting decisions are scarce resources

More popups do not create better gameplay.

A human DecisionRequest should exist only when the situation:
- materially matters,
- has at least two defensible strategic responses,
- is not already unambiguously resolved by briefing/delegation,
- is appropriate for the chosen DS autonomy,
- provides enough information for a meaningful judgement.

### R-009 — Same information basis for human and AI

AI DS does not inspect hidden W' balance, true fatigue or private rider state of competitors.

It consumes the same type of observations and estimates that the world can provide to an equivalent human organization.

### R-010 — Race truth is independent of rendering

Opening a panel, changing camera, enabling ticker text or generating commentary cannot consume gameplay RNG or alter the simulation.

---

## 4. Scope of Race Engine v0.1

This document defines the intended architecture and a deliberately smaller prototype.

It does NOT require implementing the full research report immediately.

### First prototype must prove

1. physically sensible power demand,
2. drafting and position matter,
3. CP/W' can create repeated-attack selection,
4. basic durability can distinguish fresh and late-race ability,
5. dynamic gaps can create natural dropping,
6. crosswind + limited shelter can split a group,
7. AI teams can make different chase decisions because energy has opportunity cost,
8. briefing changes team behavior,
9. at least some live-race decisions are genuinely non-obvious.

If these fail, deeper physiology does not proceed.

---

## 5. Canonical race state layers

Race state is split conceptually into:

```text
RaceDefinition
RaceEnvironmentState
RaceGroupState
RaceRiderTruthState
RaceTeamIntentState
RaceInformationState
RaceDecisionState
RaceResultState
```

### RaceDefinition

Mostly immutable for an edition/stage:
- route geometry,
- distance,
- gradient profile,
- road width,
- surface,
- corners/technical sectors,
- feed zones,
- categorized climbs,
- finish geometry,
- race rules.

### RaceEnvironmentState

Changes during race:
- wind vector,
- air density,
- temperature,
- precipitation,
- road wetness,
- optional future visibility conditions.

### RaceGroupState

For every active group:
- members,
- longitudinal extent,
- density,
- speed,
- front pace,
- road occupancy,
- available shelter pattern,
- group tactical context,
- gaps to adjacent groups.

### RaceRiderTruthState

Exact internal simulation state.

Never exposed directly to normal UI.

### RaceTeamIntentState

Current execution intent produced by briefing + DS + rider autonomy:
- protect,
- conserve,
- chase,
- pull,
- attack,
- bridge,
- hold position,
- move forward,
- support dropped leader,
- lead-out,
- ride economically to finish.

### RaceInformationState

Actor-specific race observations and interpretations.

### RaceDecisionState

Pending DecisionRequests and resolved strategic commands.

---

## 6. Rider truth profile

The long-term race profile may include more variables, but the conceptual truth model is:

### Physiological base

```text
CriticalPowerW
WPrimeJ
PeakPowerW
PowerDurationProfile
WPrimeRecoveryProfile
DurabilityProfile
DayToDayRecovery
```

### Physical

```text
BodyMassKg
BikeSystemMassKg
CdARoad
CdATT
BaseCrrSensitivity
```

### Race skills / behavior

```text
Positioning
Handling
Descending
SurfaceSkill
TacticalAwareness
RiskTolerance
Communication
SelfAssessment
```

These are not necessarily all player-visible numeric attributes.

Their visibility is determined by scouting/knowledge rules.

---

## 7. Prototype rider profile

Prototype v0 intentionally reduces the profile to:

```text
CriticalPowerW
WPrimeJ
PeakPowerW
WPrimeRecovery
DurabilityLowIntensity
DurabilityHighIntensity

BodyMassKg
SystemMassKg
CdARoad
BaseCrr

Positioning
Handling
TacticalAwareness
```

Deferred from the first physics spike:
- glycogen,
- carbohydrate absorption/tolerance,
- fluid deficit,
- thermal load,
- sleep,
- illness,
- muscle damage,
- detailed cobble vibration physiology,
- complex descending risk,
- radio failure.

The architecture must permit them later, but the first race prototype is not allowed to require them.

---

## 8. Power-duration model

### Critical Power and W'

Critical Power and W' are the main first approximation for sustainable and supra-critical work.

For power above CP, a simplified prototype model can consume W':

```text
WPrimeUse = max(0, RealizedPower - EffectiveCP) * dt
```

When power falls below effective CP, W' may recover according to a rider-specific recovery function.

Important:

> W' is a useful tolerance model, not a literal biochemical battery.

The implementation must not promise physiological truth beyond the model's purpose.

### Peak power

CP/W' alone does not cap instantaneous output adequately.

Prototype uses `PeakPowerW` or a short-duration cap so a rider cannot mathematically produce absurd instantaneous power merely because W' remains available.

### Future power-duration curve

Later iterations may fit or derive duration anchors such as:
- 5 s,
- 30 s,
- 1 min,
- 5 min,
- 20 min,
- CP asymptote.

The first spike should avoid redundant parameters that cannot be calibrated.

---

## 9. Durability

Durability represents degradation of later-race performance after accumulated work.

It must distinguish at minimum:

```text
LowIntensityLoad
HighIntensityLoad
```

because equal total kJ at very different intensity should not necessarily produce equal late-race capability.

Prototype concept:

```text
DurabilityState =
    f(
        accumulated_low_intensity_work,
        accumulated_high_intensity_work,
        rider_durability_traits
    )
```

Durability should be duration-sensitive.

Example conceptual effect:

```text
late short power degrades more
late medium power degrades somewhat
CP degrades less
```

Avoid:

```text
all_power *= fatigueMultiplier
```

The exact curves are calibration parameters, not locked by this document.

---

## 10. Later physiology modules

The following are strongly supported by the research but deferred until the core race loop passes its gate.

### Glycogen / fueling

Future state may include:
- glycogen availability,
- exogenous carbohydrate availability,
- feeding success,
- gastrointestinal tolerance,
- late-race high-intensity limitation.

Fueling must not behave like instant stamina refill.

### Thermal state

Future state separates:
- thermal load,
- fluid deficit.

They may interact but are not the same variable.

### Between-stage recovery

Future stage-race state may include:
- muscle glycogen recovery,
- cumulative fatigue,
- muscle damage,
- sleep quality,
- illness/infection state,
- residual heat/dehydration,
- morale/stress where relevant.

A rider should not receive a random universal `dayForm = ±5%` roll.

Day condition should mostly emerge from causal state plus bounded stochastic events.

---

## 11. Physical power requirement

Prototype foundation:

```text
P_required =
    P_aero
  + P_rolling
  + P_gravity
  + P_acceleration
```

Later drivetrain efficiency may be represented explicitly.

### Aerodynamic component

Conceptual form:

```text
P_aero ≈ 0.5 * rho * CdA_effective * air_speed^3
```

where:
- `rho` = air density,
- `air_speed` comes from rider velocity relative to wind,
- `CdA_effective` includes equipment/position and shelter.

Crosswind uses a vector-relative wind model or an explicitly documented approximation.

### Rolling resistance

Conceptual form:

```text
P_rolling ≈ Crr_effective * mass * g * cos(theta) * speed
```

Surface and equipment modify effective Crr.

### Gravity

Conceptual form:

```text
P_gravity ≈ mass * g * sin(theta) * speed
```

### Acceleration

Conceptual form:

```text
P_acceleration ≈ effective_mass * acceleration * speed
```

This is essential for:
- exits from corners,
- elastic effects,
- repeated positioning changes,
- attacks,
- crosswind surges.

---

## 12. Route representation

The race engine should not operate only on a decorative profile bitmap.

A route is composed of simulation-relevant segments.

Possible segment fields:

```text
StartDistanceM
LengthM
GradientProfile / gradient
RoadWidthM
SurfaceType
CornerDensity
Technicality
Exposure
Direction / heading
Altitude
OptionalHazards
```

Wind direction is evaluated relative to local road heading.

A route label such as "flat" or "hilly" is a summary for UI, not a simulation rule.

---

## 13. Drafting and shelter

Drafting is one of the largest physical advantages in road cycling and must be structural to the simulation.

### Rule

```text
CdA_effective = BaseCdA * ShelterMultiplier
```

The multiplier depends on:
- relative position,
- spacing,
- group shape,
- local density,
- wind yaw,
- road width,
- rider movement.

Prototype does not need CFD.

It needs a calibrated shelter approximation that preserves the qualitative effect.

### Sheltered slots

For crosswind and constrained roads, the simulation can model a finite number/distribution of useful sheltered positions.

Conceptually:

```text
ShelterCapacity =
    f(
        road_width,
        wind_yaw,
        group_speed,
        local_group_shape
    )
```

Riders outside useful shelter pay significantly more aerodynamic cost.

This allows echelons/splits to emerge without a scripted `CrosswindEvent`.

---

## 14. Positioning model

Positioning is represented as actual race state plus rider skill.

A rider skill does not directly grant free watts.

It affects probabilities/ability to:
- secure a desired slot,
- move forward before a critical sector,
- avoid being trapped behind a weakening rider,
- minimize unnecessary accelerations,
- choose sheltered side in crosswind,
- enter climbs/corners near desired position.

Moving forward has a cost.

The engine must avoid:

```text
Positioning 90 => permanent free +10% performance
```

Instead good positioning changes the rider's experienced power-demand history.

---

## 15. Group model

A group is not merely a list of riders with one timestamp.

At minimum it tracks:
- ordering / longitudinal position,
- approximate lateral/shelter slot,
- group speed,
- group length,
- density,
- front contributors,
- local road capacity,
- tactical intent at front,
- gaps inside/around the group.

### Prototype geometry

Use a deterministic 2D slot/lane abstraction rather than full collision geometry.

The purpose is to model:
- shelter,
- road width,
- movement cost,
- group stretch,
- echelon capacity,
- gaps.

Not visual bicycle steering.

Full 3D collision/trajectory simulation is explicitly out of scope.

---

## 16. Canonical per-step phases

Prototype uses a fixed race simulation timestep for simplicity.

Recommended spike value:

```text
dt = 1 second
```

This is a prototype choice, not yet a permanent production lock.

A tick must be phase-based so outcome does not depend on whichever rider happens to be iterated first.

### Phase A — Snapshot

Create/read an immutable logical snapshot of race state for decisions during this step.

### Phase B — Observation / local perception

Riders and staff derive permitted observations from the snapshot.

### Phase C — Intent

Each rider/team generates intent based on:
- current briefing,
- DS command,
- rider role,
- local situation,
- autonomy,
- observed information.

Examples:
- hold wheel,
- move forward,
- pull,
- conserve,
- attack,
- bridge,
- wait,
- support.

### Phase D — Desired motion / effort

Intent is translated into desired:
- speed,
- acceleration,
- position change,
- power demand.

The human manager never performs this translation manually.

### Phase E — Physics / capability solve

For each rider:
- calculate local required power,
- evaluate current physiological capability,
- determine realizable power/speed.

### Phase F — Position/group resolution

Resolve all riders together:
- slot competition,
- movement,
- group stretch,
- internal gaps,
- group splitting/merging.

Use deterministic tie-break rules.

### Phase G — Physiology update

Update:
- W' balance,
- durability loads,
- accumulated work,
- later deferred states.

### Phase H — Incidents / rule effects

Apply deterministic or seeded incident systems permitted at this phase.

### Phase I — Information publication

Convert truth changes into:
- Radio Tour signals,
- team radio reports,
- visual observations,
- timing gaps,
- staff interpretations.

### Phase J — Decision detection

Evaluate whether a human DecisionRequest should be raised.

### Phase K — Historical/debug trace

Record only required operational/debug information according to retention policy.

---

## 17. Desired power vs realizable power

The engine must distinguish:

```text
DesiredAction
DesiredPower / DesiredSpeed
RealizablePower
RealizedSpeed
```

Example:

A rider wants to follow a group at 50 km/h.

Local position and wind require 520 W for the next interval.

The rider can currently produce only 470 W at the required duration without violating the physiological model.

The engine does not immediately mark the rider dropped.

It produces a lower realized speed.

The gap grows.

That gap may reduce shelter.

The next second becomes more expensive.

---

## 18. Dynamic gaps and natural dropping

Core equation concept:

```text
gap_next =
    gap_now
  + (speed_ahead - rider_speed) * dt
```

As a gap grows:
- local shelter can deteriorate,
- required aero power can rise,
- bridge cost rises because catching requires higher speed than the target group,
- a temporary weakness can become a full split.

This feedback loop is central:

```text
gap increases
→ shelter decreases
→ required power increases
→ rider loses more speed
→ gap increases
```

On steep climbs the feedback is weaker because gravity dominates and speed is lower.

Therefore:
- flat/crosswind dropping can look explosive,
- climbing dropping can look gradual.

No separate universal `DropRider()` script is necessary.

---

## 19. Attack model

An attack is not a special dice roll that creates a gap.

It is an intent that causes a rider to:
- move to a usable position if necessary,
- accelerate,
- accept a high short-duration cost,
- attempt to create a speed difference.

Whether the attack succeeds depends on:
- attacker's available short-duration capability,
- current W' state,
- durability,
- position,
- gradient,
- aero/wind,
- reaction delay and ability of others,
- willingness of others to respond,
- group tactical incentives.

Repeated attacks should be capable of producing selection even when the first attack fails to create a permanent gap.

---

## 20. Pacing

A rider/team can have a target tactical mode, not a direct player watt command.

Examples:
- controlled tempo,
- hard tempo,
- conserve,
- maximal sustainable chase,
- short violent attack,
- bridge,
- pull then release.

Local rider execution converts this into power/speed based on:
- capability,
- terrain,
- position,
- team instruction,
- self-assessment,
- tactical awareness.

Future TT pacing may use a specialized optimizer/heuristic under the same physical model.

---

## 21. Briefing as conditional policy

Pre-race briefing is a conditional plan, not a scripted timeline.

Example:

```text
PRIMARY OBJECTIVE
Protect GC Leader

BREAK POLICY
Allow low-threat break
Send Rider F if composition matches team goal

GC THREAT POLICY
If a credible GC threat attacks:
    prefer wheels / rival contribution if safe
    commit domestique if threat becomes material

CROSSWIND POLICY
Move team forward before exposed sector
Authorize echelon pressure if conditions are favorable

SECOND LEADER
Preserve as tactical card unless GC leader is in immediate danger
```

### Critical knowledge rule

Briefing conditions may not use hidden truth such as:

```text
if leader.WPrimeBalance > 45%
```

Instead they use information available to the staff, for example:

```text
if leader_state_estimate is Strong
and staff_confidence >= Medium
```

The simulation may use true W' internally to resolve performance.

The decision layer does not.

---

## 22. Order / Guideline / Freedom

Briefing instructions retain the already accepted distinction:

### ORDER

Strong instruction.

DS breaks it only in exceptional situations.

### GUIDELINE

Preferred behavior, but DS may override when context changes.

### FREEDOM

Delegated to DS/rider judgement.

This affects:
- frequency of consultation,
- DS autonomy,
- DecisionRequests,
- later debrief accountability.

---

## 23. Race knowledge model

### RaceTruth

Exact:
- W' balance,
- current physiological limits,
- actual injury,
- exact local power,
- exact hidden rival state,
- exact intent.

### Possible ObservationSignals

- official time gap,
- visible group split,
- rider says "legs are bad",
- rider says "I am fine",
- teammate reports rival looks weak,
- TV shows rider at back,
- Radio Tour reports attack,
- staff notices abnormal behavior,
- timing data becomes available,
- equipment issue is reported.

### Interpretation

Staff converts observations into estimates:

```text
LeaderCondition:
    Strong / Normal / Uncertain / Struggling

Confidence:
    Low / Medium / High

Reason:
    rider report
    visible position loss
    recent workload
    staff judgement
```

Different staff can interpret the same evidence differently.

---

## 24. Rider self-assessment

A rider has imperfect awareness of their own future capability.

`SelfAssessment` affects:
- accuracy of reports,
- recognition of imminent failure,
- willingness to report weakness,
- quality of pacing judgement.

This supports believable situations:
- rider says they are okay and cracks,
- rider reports bad legs but stabilizes,
- experienced rider correctly warns the DS before a crisis.

It is not random deception unless personality/context supports it.

---

## 25. DS information channels

Era/rules dependent channels may include:
- Radio Tour,
- team radio,
- television feed,
- official timing/gaps,
- staff observations,
- rider computers,
- later/custom telemetry.

The exact information environment belongs to scenario/rules modules.

Modern racing must still not default to an omniscient F1-style dashboard.

---

## 26. Team tactical utility

AI DS and automated human-team execution should reason about expected sporting value and cost.

Example:

```text
Action: Chase break

Possible benefit:
- restore bunch sprint probability
- protect GC
- satisfy stage objective

Cost:
- domestique work
- reduced later lead-out resources
- higher late-race fatigue
- higher next-day fatigue
- reveals team commitment
```

The exact utility equation is not locked.

It must:
- use organization knowledge, not truth,
- be explainable,
- reflect manager/DS traits,
- preserve uncertainty,
- allow multiple rational teams to disagree.

---

## 27. Tactical threat

A rider can matter because competitors cannot safely ignore them.

Threat assessment may consider:
- estimated rider quality,
- virtual GC,
- route suitability,
- remaining distance,
- current gap,
- teammates ahead/behind,
- current condition estimate,
- uncertainty,
- likely behavior of other teams.

Do NOT copy a simplistic product such as:

```text
Threat = Gap * RiderQuality * Terrain
```

as the final formula.

Those variables describe decision surfaces, not mathematically compatible units.

---

## 28. "Who chases?" as game theory

A key target behavior is that a catchable break may survive because no team individually wants to pay enough.

Each team independently estimates:

```text
BenefitOfChase
CostOfChase
ExpectedOtherTeamsContribution
RiskOfWaiting
```

Possible emergent outcomes:
- one team shoulders the chase,
- multiple teams cooperate,
- teams alternate,
- one team free-rides,
- everyone waits,
- a team attacks instead of pulling,
- the break survives despite sufficient combined physical strength behind.

This behavior must not be reduced to:

```text
peloton_gap -= X seconds per km after 30 km to go
```

---

## 29. Crosswind model

A crosswind split should emerge from:

```text
wind vector
+ road heading
+ road width
+ group speed
+ shelter capacity
+ team positioning
+ team intent
+ rider positioning ability
+ repeated accelerations
```

A tactical team may anticipate an exposed section and spend energy moving forward before it.

That preparation itself has a cost.

The split is not triggered merely because weather has `Crosswind = true`.

---

## 30. Sprint model direction

The first race spike does not need a complete sprint engine, but final direction is:

Sprint outcome depends on:
- late-race short-duration capability,
- remaining W',
- durability,
- position,
- lead-out,
- drafting,
- timing,
- aero,
- route/finish geometry.

Not:

```text
Winner = max(SprintStat)
```

---

## 31. Climbing model direction

Short climb:
- W',
- high short-duration power,
- CP/kg,
- positioning,
- current durability.

Long climb:
- CP/kg,
- durability,
- pacing,
- W' mainly for changes in pace/attacks.

This should allow two riders with similar fresh "climbing reputation" to excel at different climbing durations.

---

## 32. Time-trial direction

TT uses the same physical and physiological model.

Important variables:
- CP / power-duration,
- CdA,
- Crr,
- mass/gradient,
- wind,
- pacing,
- durability,
- ability to hold aero position.

A rider with less absolute power may beat a more powerful rider through lower aerodynamic cost.

Detailed TT pacing is deferred until road-race core is stable.

---

## 33. Cobbles / rough surfaces direction

Prototype can model rough surfaces primarily through:
- increased effective Crr,
- positioning difficulty,
- handling requirements,
- higher acceleration variance,
- incident probability.

Later it may add:
- vibration fatigue,
- equipment pressure trade-offs,
- mechanical reliability,
- rider-specific surface durability.

Do not create `Cobbles = +15 performance` as the only mechanism.

---

## 34. DecisionRequest gate

A RaceLive human consultation is created only when all relevant gates pass.

Conceptual gates:

```text
MaterialityGate
ChoiceGate
DelegationGate
InformationGate
Novelty/CooldownGate
```

### MaterialityGate

Could materially affect:
- GC,
- stage win,
- major objective,
- leader survival,
- valuable resource expenditure,
- tomorrow's race state.

### ChoiceGate

At least two options are defensible given current knowledge.

If one option dominates overwhelmingly, DS should normally act without creating fake interaction.

### DelegationGate

Briefing/autonomy has not already delegated the decision completely.

### InformationGate

Player has enough information to make a judgement.

"Choose randomly because the game hid everything" is not difficulty.

### Novelty/CooldownGate

Avoid asking essentially the same question repeatedly.

Several simultaneous race problems should be aggregated when sensible.

---

## 35. Race decision categories

Target categories for the first playable important races:

1. **Threat response**
   - chase / wait / use rider ahead / trust another team.

2. **Resource sacrifice**
   - burn domestiques now or preserve them.

3. **Objective switch**
   - abandon sprint plan, protect GC, back breakaway rider, switch leader.
   - later multi-stage: if the designated leader no longer has a realistic
     knowledge-bounded chance at the team objective, redirect that rider to
     support the teammate with the best remaining chance (`D-032`).

4. **Two-card strategy**
   - preserve second leader / use as satellite / call back.

5. **Crosswind commitment**
   - spend energy positioning/forcing split or conserve.

6. **Incident recovery**
   - send riders back after crash/mechanical or preserve race position.

7. **Opportunity approval**
   - DS identifies a high-value attack/bridge opportunity that conflicts with conservative briefing.

Not every race must contain every category.

Minor races may contain no human consultation.

---

## 36. Human decision options

Human options should be strategic language.

Good:

```text
Commit two domestiques
Wait and force rivals to react
Protect the second leader
Trust your DS
Switch objective to Rider B
```

Bad:

```text
Set Rider 4 to 487 W
Attack for 31 seconds
Effort = 92%
Drink now
```

---

## 37. DS autonomy

DS autonomy changes:
- how often they consult,
- how aggressively they interpret guidelines,
- which decisions they resolve themselves,
- confidence required before overriding briefing.

High-quality DS:
- better interprets observations,
- estimates costs/threats better,
- may identify opportunities earlier.

Low-quality DS:
- may misread information,
- overpay in energy,
- react late,
- incorrectly expect another team to chase.

Bad DS decisions remain attributable and explainable.

---

## 38. DecisionRecord

Every significant automated race choice should be traceable in debug.

Suggested structure:

```text
DecisionRecord
DecisionId
RaceId
SimulationTime
ActorPersonId
OrganizationId
DecisionType
KnownInputs
ConsideredOptions
SelectedOption
Reasons
Confidence
RelatedBriefingRule
OutcomeLinks
```

Player-facing debrief exposes only information appropriate to the organization.

Debug mode may expose deeper truth comparisons.

---

## 39. Live vs fast simulation

There is one canonical race engine.

### Watch Race

- renderer receives race ViewState (route, rider positions, speeds, observed gaps),
- simulation can pause on human DecisionRequest,
- Godot Watch picks film duration (30 s–5 min, default 2 min) and derives rate;
  headless CLI still uses ×1 / ×2 / ×5 / ×20 (`D-033`): the watch clock is supervisory
  and simulation stays continuous so map icons follow actual speed,
- renderer may interpolate between physics steps for smoothness,
- renderer does not drive physics and must not teleport riders across quiet time.

### Fast / background simulation

- no visual rendering required,
- human-owned decision points resolve through briefing/delegation when the race is intentionally simulated,
- AI organizations use normal DS logic,
- same race rules and state transitions.

### Required parity test

At minimum:

```text
same build
same race seed
same start state
same command/decision sequence
renderer attached vs headless
→ same gameplay result
```

---

## 40. Race performance optimization policy

Do not design a second macro engine before profiling the canonical model.

Prototype first.

A 1-second tick over approximately 200 riders for a several-hour race is a manageable baseline for a headless spike and gives us a correctness reference implementation.

Only after profiling may we introduce:
- event compression,
- batching stable riding intervals,
- adaptive internal resolution,
- cached group calculations.

Any optimization must have parity/regression tests against the reference model.

---

## 41. Determinism and numeric representation

Existing global determinism contract applies.

### Required now

- stable ordering,
- stable RNG derivation,
- no runtime-dependent hash seeds,
- no unordered collection iteration affecting outcomes,
- phase/barrier processing,
- gameplay RNG isolated from presentation.

### Still open

`fixed-point everywhere` is NOT accepted by this document.

Before production lock, prototype must test supported targets and compare:
- repeatability,
- cross-machine/platform behavior where required,
- performance,
- error accumulation,
- calibration burden.

Possible outcomes:
- standard `double` with constrained deterministic math,
- fixed-point for selected race subsystems,
- broader fixed-point representation.

This remains an explicit OPEN decision.

---

## 42. Race command semantics

Race commands are strategic domain commands, not direct component mutation.

Examples:

```text
SetRaceBriefing
ApproveDSRecommendation
RejectDSRecommendation
ChangeRacePriority
CommitSupportResources
SwitchProtectedLeader
AuthorizeCrosswindPressure
DelegateRaceDecision
```

Command ordering follows the canonical deterministic scheduler/command contract.

Commands never mutate Godot scene state as the source of truth.

---

## 43. Race events vs race information

A race DomainEvent is truth-level world output.

Examples:
- AttackInitiated,
- GroupSplit,
- RiderCrashed,
- MechanicalOccurred,
- RiderAbandoned,
- FinishCrossed.

These events do NOT automatically become knowledge for all organizations.

Publication/observation rules create:
- RadioTourSignal,
- TeamRadioSignal,
- VisualSignal,
- TimingSignal,
- StaffInterpretation.

This preserves fog of war during races.

---

## 44. RaceLive state boundary

`RaceLive` covers one race day / one stage.

After finish:

```text
RaceLive
→ Results
→ Debrief
→ Management
```

For stage races the next stage is a later RaceLive session after normal world/calendar processing.

Race transient state is compacted after the race according to long-save policy.

---

## 45. Race result persistence

Permanent/warm race history stores:
- official achieved result,
- exact times/gaps where appropriate,
- classifications,
- important incidents,
- key tactical events,
- significant DecisionRecords or summaries,
- state changes needed for future simulation,
- historical context.

Do not persist forever:
- every second of every rider's instantaneous power,
- every internal utility candidate,
- all transient slot calculations.

---

## 46. Prototype Spike v0

The first executable race spike should be headless.

### Scenario P0 — Basic pace line / group

Goal:
- several riders follow one pace,
- shelter reduces aero cost,
- power demand changes correctly with gradient/wind.

Pass if:
- riders in shelter spend materially less than front/solo riders,
- no unexplained group instability,
- same seed/input gives same result.

### Scenario P1 — Mountain pacing

Setup:
- small group on sustained climb,
- one rider has lower sustainable late-race capability.

Expected:
- rider initially follows,
- power deficit produces gradually increasing gap,
- rider is not instantly marked dropped.

### Scenario P2 — Repeated attacks

Setup:
- riders with similar CP but different W' / recovery,
- repeated attacks separated by insufficient recovery.

Expected:
- first move may fail,
- later move can create selection,
- result emerges from accumulated supra-CP cost.

### Scenario P3 — Crosswind split

Setup:
- flat road,
- strong crosswind,
- constrained road width,
- team increases pace from front.

Expected:
- shelter capacity becomes limited,
- poorly positioned riders pay higher aero/acceleration cost,
- splits can emerge without scripted split event.

### Scenario P4 — Closeable gap

Setup:
- rider loses a few meters after acceleration.

Expected:
- rider may return if reserve is sufficient,
- or fail if shelter loss makes bridge too expensive.

### Scenario P5 — Who chases?

Setup:
- break ahead,
- three teams behind with different objectives/resources.

Expected across seeds/profiles:
- one team sometimes commits,
- sometimes cooperation emerges,
- sometimes nobody wants to pay,
- the strongest combined chase does not automatically form.

### Scenario P6 — Briefing changes behavior

Run identical race state with different briefing:

A:
```text
preserve GC team
```

B:
```text
aggressively hunt stage
```

Expected:
- team energy use and tactical actions differ materially,
- physics remain identical.

---

## 47. Prototype metrics

Collect at minimum:

### Physics
- average/peak rider power,
- time above CP,
- W' usage/recovery,
- energy spent by position,
- shelter advantage,
- speed vs gradient/wind.

### Group
- number of groups,
- split frequency,
- merge frequency,
- average group length,
- gap formation rate,
- position changes.

### Tactical
- attacks,
- chase initiations,
- chase contributors,
- energy paid per team,
- break success rate,
- objective switches.

### Gameplay
- number of DecisionRequests,
- decision categories,
- repeated/similar prompts,
- percentage of decisions with a dominant option,
- player-visible reason quality.

### Performance
- simulation wall time,
- allocations,
- memory,
- rider-step throughput.

---

## 48. Race balance probes

Named probes should eventually include:

```text
CanRepeatedAttacksBreakEquivalentCPRiders
CanStrongDraftLetWeakerRiderSurviveFlat
CanPoorPositionWasteMeaningfulEnergy
CanCrosswindSplitWithoutScript
CanRiderReturnFromSmallGap
CanGapBecomeSelfReinforcing
CanLongClimbDropBeGradual
CanNobodyChooseToChase
CanTwoLeadersCreateTacticalLeverage
CanBriefingChangeOutcomeWithoutChangingPhysics
CanBadDSMakeExplainableMistake
CanRivalAIActWithoutHiddenTruth
DoesWatchingRaceChangeResult
```

---

## 49. Race engagement gate

Before adding full:
- glycogen,
- fueling,
- heat,
- detailed cobbles,
- complex incidents,
- broad content,

the owner manually plays/watches prototype races.

Required questions:

```text
Was I interested while watching?
Did the race tell a coherent story?
Did I face at least one decision with two reasonable answers?
Did I understand the cost of my choice?
Did the DS feel like a person with responsibility?
Could I explain why the decisive split/attack worked?
Did the next test produce a different tactical problem?
Would I willingly watch another important race?
```

If not, stop adding simulation detail.

Fix the decision loop.

---

## 50. Deferred systems

Explicitly deferred from Race Engine v0 prototype:

- complete fueling strategies,
- GI distress,
- detailed hydration,
- core temperature model,
- altitude physiology,
- illness interactions,
- sleep model,
- full crash physics,
- detailed mechanical model,
- radio hardware failures,
- detailed cobble vibration physiology,
- 3D collision geometry,
- advanced TT optimizer,
- sprint train micro-positioning,
- race-neutral-service detail,
- historical era-specific communication implementations,
- multi-stage GC leadership transfer: a failing designated leader becoming
  support for the teammate with the best remaining chance (`D-032`).

Their future place in the architecture is reserved. The current prototype does
not evaluate virtual GC, remaining-stage probability, or teamwork/loyalty
policy for that switch.

---

## 51. Open questions

### O-RACE-001 — Numeric representation

`double`, selected fixed-point or broader fixed-point after deterministic spike tests.

### O-RACE-002 — Final timestep / adaptive resolution

Prototype uses 1 second.

Production choice comes after profiling and parity testing.

### O-RACE-003 — Exact power-duration representation

How much is directly parameterized versus derived from CP/W'/Pmax.

### O-RACE-004 — Durability function

Need real-data calibration and gameplay validation.

### O-RACE-005 — Shelter approximation

Need a computationally cheap model calibrated against plausible group savings without pretending to be CFD.

### O-RACE-006 — Position resolution

Need deterministic slot competition that looks organic and does not create excessive churn.

### O-RACE-007 — Incident model

Must eventually integrate crashes/mechanicals without turning race outcome into arbitrary RNG.

### O-RACE-008 — Background optimization

Only after canonical reference implementation profiling.

---

## 52. Anti-patterns

Never implement:

```text
if rider.Stamina <= 0:
    rider.Drop()
```

Never:

```text
if crosswind:
    trigger scripted split
```

Never:

```text
if 30km_to_finish:
    peloton_gap -= fixed_seconds
```

Never:

```text
AI reads rival.TrueWPrime
```

Never:

```text
player sets rider watts directly
```

Never:

```text
WatchRace uses FullEngine
SimRace uses RandomResultGenerator
```

Never make DecisionRequests simply to keep the player busy.

---

## 53. Implementation order after architecture skeleton

Recommended order:

```text
1. Route segment + environment primitives
2. Rider physical profile
3. Required-power solver
4. CP/W'/Pmax reference capability
5. One-rider validation tests
6. Group + shelter model
7. Dynamic gaps
8. Position/slot model
9. Basic durability
10. Rider local intent/autonomy
11. Team intent / briefing
12. AI chase utility
13. Race observations / information
14. DecisionRequest gate
15. Minimal RaceLive renderer
16. Owner engagement gate
```

Do not start with:
- animations,
- detailed media,
- full Grand Tour physiology,
- dozens of terrain types.

---

## 54. Definition of success for Race Engine v0.1

Race Engine v0.1 is design-complete enough for a prototype when:

- all prototype scenarios have explicit acceptance tests,
- hidden truth and race knowledge are separated,
- live/headless parity rule is preserved,
- no direct rider effort control enters the human command API,
- group/drafting/gap mechanics are defined,
- CP/W'/durability roles are defined without claiming false physiological precision,
- DecisionRequest gating is defined,
- deferred physiology is clearly separated,
- numeric representation remains an explicit measured decision rather than ideology.

The next milestone is not "more realism".

The next milestone is:

> **prove that a small version of this engine produces a race worth watching and decisions worth making.**

---

## 55. Race Spy is mandatory debug infrastructure

Detailed design:

`RACE_SPY_DEBUGGING_v0.1.md`

Race Spy is a passive, RNG-neutral developer observer for race physics, information and decisions.

It must be available during the early headless race spike, not added after AI tactics are considered complete.

For major race behavior it must distinguish:

```text
Simulation Truth
Actor Known Inputs
Actor Interpretation
Considered Options
Selected Decision
Commands Emitted
Subsequent Outcome
```

Required early uses:
- explain why a rider dropped,
- explain why an attack failed/succeeded,
- explain why a team chased or refused to chase,
- show which briefing rule was active,
- show why a DecisionRequest was or was not created,
- detect hidden-truth leaks,
- capture reproducible outlier races.

Enabling Race Spy must never change race result or consume gameplay RNG.

