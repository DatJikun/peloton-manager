# Peloton Manager: Rulesets

**Title:** Rulesets

**Version:** 0.1

**Status:** DRAFT

**Purpose:** Define how independent rules modules are selected, composed, validated, applied, and changed during a career without tying gameplay to one era or database.

**Authority/Owner:** Project owner (gameplay and simulation architecture)

**Supersedes:** none

**Superseded by:** none

**Last reviewed:** 2026-08-25

**Related decisions/ADRs:** D-001, D-002, D-004, D-011, D-012, D-013, D-014, D-015, D-028, D-029, D-031

---

## 1. Purpose and scope

Peloton Manager does not have one ruleset selected by a calendar year. A world composes independent rules modules for competition, registration, transfers, economy, equipment, information, and other domains.

Rules modules are versioned data interpreted by known simulation contracts. The same resolved modules govern human and AI actors.

### In scope

- boundary between content, rules, simulation, and presentation;
- module slots and module identity;
- custom scenarios that mix modules from different eras;
- dependency, capability, and cross-module validation;
- deterministic rule evaluation;
- effective-dated changes between seasons or eras;
- grandfathering, conversion, validation, and repair contracts;
- rules compatibility with saves and simulation builds;
- open design questions that must be answered before implementation.

### Out of scope

- full formulas for race physics, physiology, economics, transfers, contracts, ranking, or development;
- production JSON schemas for each domain;
- full save tables or migration code;
- UI layout for rule selection and transition warnings;
- procedural political/governance simulation that invents reforms;
- executable mod scripts;
- hotseat RaceLive resolution.

---

## 2. Four boundaries

| Boundary | Owns | Does not own |
|---|---|---|
| Content | Static definitions, parameters, module declarations, scenario recipes, transition definitions | Runtime state changes or legal command execution |
| Rules | Legal actions, eligibility, constraints, calculations, transition policy, explicit defaults | Authoritative state storage, UI, hidden actor knowledge shortcuts |
| Simulation/Application | World evolution, command handling, scheduler work, state mutation, event emission | Reinterpreting content by filename/year or bypassing rules for the player |
| Presentation | Module summaries, warnings, forecasts, explanations, input collection | Rules truth, validation authority, gameplay RNG |

Examples:

| Question | Owner |
|---|---|
| Which race definition exists? | Content |
| Is this organization eligible and invited under current regulations? | Rules |
| Which organizations accepted, and what happened on race day? | Simulation/Application |
| How eligibility and consequences are explained to the player | Presentation through knowledge-bounded Queries |
| Which equipment definition exists? | Content |
| Is that equipment legal on the effective date? | Rules |
| Which organization acquired and assigned it? | Simulation/Application |

Rules can consume world state and explicit actor context. They cannot read a Godot scene, infer human control from an organization type, or obtain hidden rival information through presentation state.

---

## 3. Rules module identity

A rules module is a content definition addressed by `ContentDefinitionId`.

Example:

```text
ContentDefinitionId = rules.peloton.transfer.open_market_2026
ModuleSlot          = transferRules
RulesContract       = transfer-market
ContractVersion     = 1
```

Minimum module semantics:

```text
RulesModuleDefinition
    ContentDefinitionId
    ModuleSlot
    RulesContractId
    RulesContractVersion
    ModuleSchemaVersion
    Parameters
    RequiresCapabilities[]
    ProvidesCapabilities[]
    CompatibleModuleConstraints[]?
    TransitionSupport[]?
```

The simulation build implements `RulesContractId` and a supported contract-version range. The pack supplies data. A pack cannot add a new executable rule implementation in MVP.

Changing display text or localization does not change rule identity. Changing gameplay semantics requires a new compatible version, a migration, or a new module identity.

---

## 4. Module slots

The initial slot vocabulary should cover independent rule surfaces without forcing every world into one historical preset.

```text
calendarStructure
competitionRules
raceEligibility
registrationRules
rosterRules
invitationRules
rankingAndPoints
transferRules
contractRules
equipmentRules
technologyAvailability
medicalContext
antiDopingRules
integrityCulture
organizationStructure
economyRules
sponsorMarketRules
mediaContext
trainingKnowledge
scoutingInformationEnvironment
raceCommunicationRules
safetyRules
```

This list is a registry direction, not a claim that all modules will be implemented in the architecture skeleton. Later domain designs may split or combine a slot when they define a clearer contract. Such changes require compatibility and migration analysis.

Each required slot resolves to one primary provider unless the slot contract explicitly supports ordered composition. A resolver must not combine two providers merely because both happen to be installed.

---

## 5. Ruleset and scenario recipe

A ruleset preset is a tested selection of modules. A scenario selects a preset or supplies module choices directly.

```text
Scenario recipe
-> content dependency resolution
-> module-slot selection
-> capability and compatibility validation
-> ordered ResolvedRuleset
-> world creation
```

`ResolvedRuleset` contains:

```text
ResolvedRuleset
    Rules resolution contract version
    Selected module identity per slot
    RulesContractId and version per module
    Effective parameter identity/hash
    Required/provided capability set
    Cross-module validation result
    Declared transition schedule/policy identities
    Aggregate rules identity/hash
```

The itemized module list remains available for diagnostics. An aggregate hash alone cannot explain which rule changed.

---

## 6. Custom scenario composition

Custom scenarios may combine modules from different periods and publishers. This is a supported use case.

Illustrative recipe:

```json
{
  "id": "scenario.example.modern_riders_old_rules",
  "kind": "scenario",
  "startDate": "2026-01-01",
  "modules": {
    "riders": "rider-set.peloton.modern_2026",
    "organizations": "organization-set.peloton.modern_2026",
    "calendarStructure": "calendar.peloton.modern_2026",
    "competitionRules": "rules.peloton.competition.1965",
    "transferRules": "rules.peloton.transfer.1965",
    "equipmentRules": "rules.community.equipment.future_open",
    "antiDopingRules": "rules.peloton.antidoping.off",
    "economyRules": "rules.peloton.economy.real_value_1998"
  }
}
```

The resolver accepts the recipe if the selected content and rules satisfy their schemas, references, capabilities, and compatibility constraints. It does not replace modules because the combination looks historically unusual.

An arbitrary valid mix is not guaranteed to be balanced. It must remain deterministic, explainable, and free of invalid state.

---

## 7. Composition and conflict rules

Composition proceeds in stable slot order defined by the rules-resolution contract, not JSON property order.

Validation includes:

- exactly one provider for each required single-provider slot;
- supported rules contract and version;
- all required capabilities present;
- no incompatible capability or module pair;
- parameters inside domain ranges and units;
- referenced definitions available in resolved content;
- transition support for modules scheduled to change;
- cross-module invariants defined by owning contracts.

Examples of cross-module checks:

```text
roster maximum must be compatible with registration rules
equipment availability must include a legal option under equipment rules
calendar structure must provide dates required by transfer windows
ranking rules must recognize the competition categories used by the calendar
contract duration rules must define treatment of existing multi-year contracts
```

### 7.1 Precedence

There is no universal "most specific rule wins" or source-file precedence.

When two modules affect one decision surface, the owning contract defines the combination order. For example, race eligibility may combine competition category, organization license, registration status, and invitation state through one explicit eligibility contract.

If no owning contract defines a combination, the resolver reports a conflict before world creation or transition application.

---

## 8. Rule evaluation contract

Rule evaluation is deterministic and side-effect free until Application commits an accepted Command or scheduler action.

Conceptual flow:

```text
Command or ScheduledWork
-> capture active ResolvedRuleset identity for the effective time
-> gather authoritative state and legal actor context
-> evaluate relevant rules contracts in canonical order
-> return allowed/rejected plus structured reasons and required effects
-> Application commits state mutation and DomainEvents atomically
```

Rules evaluation:

- does not consume presentation or cosmetic RNG;
- uses gameplay RNG only through a simulation service when the owning domain contract explicitly requires it;
- does not depend on unordered collection iteration;
- does not mutate World State while answering a Query or forecast;
- does not read `IsHumanTeam`, `PlayerTeam`, UI state, or World Spy output;
- returns stable reason codes suitable for AI, UI, tests, and DecisionTrace links.

Human and AI Commands pass through the same rules contracts. `DecisionAuthority` affects permission and decision ownership, not the substance of competition, market, or eligibility rules.

---

## 9. No named-event exceptions

Gameplay rules cannot branch on a famous name when the real cause is a general property.

Forbidden direction:

```text
if raceId == race.tour_de_france then apply_special_registration
if currentYear >= 2032 then enable_new_equipment
if organization is human then relax_deadline
```

Correct direction:

```text
competition category + organizer policy + license + invitation rules
effective-dated equipment rules transition
same deadline contract for every authority
```

A named event may have explicit content properties that feed general rules. A race-specific tradition is valid content only when the rules contract defines what that property means for any event using it.

---

## 10. Difficulty and information

Difficulty must come from decisions, pressure, uncertainty, opposition, and consequences. It cannot come from hidden controls, misleading budgets, missing explanations, or human-only penalties.

Rules may define scenario difficulty parameters when they describe real world behavior, such as:

- tolerance or strictness of an organization board;
- market competition intensity under the same information rules;
- delegation defaults;
- staff recommendation detail where this does not reveal hidden truth;
- scenario-locked constraints stated before world creation.

Difficulty cannot:

- grant AI access to true ability, true potential, hidden health, or rival private state;
- change a legal action only because its authority is human;
- alter a result because a screen was not opened;
- hide required information through UI traps;
- consume a different ruleset without recording it in resolved identity.

Attribute visibility and gameplay difficulty are separate settings. Both remain explicit in the scenario/save recipe.

---

## 11. Rules changes during a career

World creation compatibility is not enough. A module that may change later must define a transition policy.

Rules never change because the simulation reaches a hard-coded year. A transition is explicit content/domain state with an effective time.

```text
RulesTransition
    Stable transition identity
    Source module identity
    Target module identity
    Announced/scheduled time
    Effective time
    Affected scopes
    Grandfathering policy
    Conversion policy
    Validation policy
    Repair policy
    Ordering phase
    Lifecycle status
    Source decision/event references
```

The exact domain identity belongs to the future expanded Data Model. The contract above identifies what save/load and scheduler behavior must preserve.

### 11.1 Transition lifecycle

```text
Proposed or authored transition
-> validate target module and content availability
-> validate transition policy against current world
-> schedule effective work
-> publish legal ObservationSignals when actors may know about it
-> reach deterministic effective-date barrier
-> apply conversion and rule identity change atomically
-> validate post-transition invariants
-> emit DomainEvents and HistoricalRecord links
```

A transition cannot leave half the world under the old module and half under the new one unless the module explicitly defines effective-dated or grandfathered scopes.

### 11.2 Grandfathering

Grandfathering states which existing objects keep old treatment. Examples:

- signed contracts remain valid until expiry;
- registered equipment remains legal for a stated period;
- points already earned keep their historical value;
- roster limits apply at the next registration boundary rather than retroactively.

Absence of a grandfathering rule does not mean "invalidate everything". It is a validation error when existing state needs a decision.

### 11.3 Conversion

Conversion transforms authoritative state required by the new rules. It uses exact, versioned logic and stable ordering.

Examples:

- map old competition categories to new categories;
- convert ranking balances under a declared formula;
- update registration eligibility dates;
- close, preserve, or migrate a rule-specific pending obligation.

Conversion does not reuse entity IDs, rewrite historical outcomes, or consume unrelated gameplay RNG.

### 11.4 Validation and repair

Pre-transition validation produces a deterministic list of conflicts. Repair policy states how each supported conflict is resolved.

Repair may be:

- automatic and fully specified by the transition;
- a normal actor decision represented by `DecisionRequest`;
- deferred to a legal future boundary;
- impossible, which rejects the transition before partial mutation.

The system cannot silently choose a favorable repair for the human organization. AI and human actors receive the same legal repair actions and knowledge boundaries.

---

## 12. Seasonal and era transitions

Season boundaries are common transition points, but the contract supports any deterministic effective date.

Illustrative sequence:

```text
2031-10-15: regulation change announced
2031-10-15: transition validated and scheduled
2031-10-15 onward: actors receive information under publication rules
2032-01-01, RulesTransition phase: conversion applies
2032-01-01, later phases: new rules govern Commands and ScheduledWork
```

Rules effective at the start of a processing phase govern that phase. A pause or UI timing difference cannot move work across the effective boundary.

An era label may summarize the active module set for UI/history. It is not a global switch and cannot replace the itemized `ResolvedRuleset`.

---

## 13. Pending work and decision requests

A rules transition must account for pending domain objects:

- contracts and offers;
- registrations and invitations;
- transfer negotiations;
- scheduled races and season plans;
- unresolved `DecisionRequest` objects;
- scheduler work created under the old module;
- delayed investigations or sponsor obligations.

Each owning contract declares whether old work:

- completes under captured old rules;
- is converted to the new contract;
- is cancelled through an explicit domain event and compensation policy;
- requires a legal repair decision.

Runtime pause is not a rules state. `DecisionRequest` remains a domain object, and the canonical `GameState` list remains unchanged (D-031).

---

## 14. History and explainability

World history must reconstruct which rules applied when an outcome occurred.

Permanent or effective-dated records retain enough identity to answer:

```text
Which competition/ranking/equipment rule applied?
When was the change announced and effective?
Which existing objects were grandfathered?
Which conversion or repair changed this entity?
```

Important automated transition and repair decisions produce normal `DecisionRecord` and World Spy compatible `DecisionTrace` links. Spy remains passive and cannot decide or repair a transition.

Player-facing explanations use public, organization, or personal Knowledge available through `AccessContext`. They do not expose developer truth or hidden political/market state.

---

## 15. Rules and persistence

A save preserves:

- active `ResolvedRuleset` identity and itemized modules;
- parameters that affect future simulation;
- pending transition definitions/state;
- effective dates and stable ordering fields;
- grandfathering/conversion results that remain causal;
- pending repair obligations and Decision Requests;
- historical rule identities needed to interpret outcomes.

The save does not rely on the currently installed default preset. Load resolves exact recorded identities through `CONTENT_FORMAT_v0.1.md` and `SAVE_FORMAT_v0.1.md`.

Compaction may summarize old rule history, but it cannot remove an identity or hook that affects future gameplay (D-015). HOT/WARM/COLD policy remains in `LONG_SAVE_AND_PERFORMANCE_v0.2.md`.

---

## 16. Rules schema and contract evolution

Three versions may move independently:

```text
ContentSchemaVersion
RulesModuleSchemaVersion
RulesContractVersion implemented by the simulation build
```

A new field that the old rules contract ignores cannot silently acquire gameplay meaning under the same resolved identity. Changes follow content compatibility policy and, for existing saves, save migration policy.

A build may load a module only when it supports the recorded rules contract version or has an explicit compatibility adapter/migration. Unsupported modules produce a clear incompatibility before the world attaches.

Cross-version bit-identical replay is not promised. D-013 applies to the same simulation build plus the same resolved content/rules, state, and ordered Commands.

---

## 17. Failure behavior

| Failure | Required result |
|---|---|
| Missing module provider | Reject scenario/world creation before allocation. |
| Unsupported rules contract version | Reject resolution or load with module and version named. |
| Cross-module incompatibility | Report conflicting slots/capabilities; do not substitute a preset. |
| Invalid transition against current world | Keep source rules active and world unchanged. |
| Conversion failure | Roll back the atomic transition and preserve recovery evidence. |
| Missing target content during load | Follow save recovery/content mismatch policy; never use current defaults. |
| Ambiguous rule precedence | Fail validation; source or collection order cannot decide it. |
| Repair needs an actor decision | Create/preserve a domain Decision Request at a deterministic barrier. |

Failure diagnostics include resolved content identity, source/target rule identities, transition identity, world time, and stable affected entity references where safe.

---

## 18. Locked decisions

| Decision | Rules consequence |
|---|---|
| D-001 | Rules compute legal behavior and outcomes; they do not script historical winners. |
| D-002 | Human and AI actors use the same active modules and legal actions. |
| D-004 | Rules address ManagerCareer, employment, authority, and organizations without a permanent player team. |
| D-011 | Sponsor-market behavior is an explicit domain/rules model, not a hidden player-balancing tax. |
| D-012 | Nominal inflation is an optional explicit EconomyRules module, not the default global clock. |
| D-013 | Resolved rules identity and deterministic evaluation order are part of reproducibility. |
| D-014 | Rules-backed Queries and forecasts are pure, RNG-neutral, and knowledge-bounded. |
| D-015 | Transitions and compaction preserve future causal state. |
| D-031 | Rules transitions, scheduler status, and repair presentation do not add GameState values. |

---

## 19. Open questions

| ID | Question | Decision deadline |
|---|---|---|
| OQ-RS-001 | Final initial registry of required and optional module slots | Before rules resolver implementation |
| OQ-RS-002 | Which slots permit multiple ordered providers rather than exactly one | Before module composition implementation |
| OQ-RS-003 | Common version-constraint language for cross-module compatibility | Before first public rules schema |
| OQ-RS-004 | Whether official mid-career reforms must be authored at world creation or may arrive through a later compatible content artifact | Before dynamic reform implementation |
| OQ-RS-005 | Exact transaction/recovery boundary for a failed large rules conversion | Before persistence implementation of transitions |
| OQ-RS-006 | Which repair classes create DecisionRequests versus applying automatic policy | Before first mutable roster/contract rule transition |
| OQ-RS-007 | How difficulty presets map to explicit rules, AI decision support, and presentation without breaking symmetry | Before difficulty implementation |

---

## 20. Deferred

- numeric formulas and data schemas for each module slot;
- procedural reform politics and voting;
- AI strategy adaptation details for each rules change;
- historical catalog of real regulations;
- balance certification for official era combinations;
- hotseat decision ownership during simultaneous RaceLive rule effects;
- online distribution of new rule artifacts to existing saves;
- editor UI for module composition and transition authoring.

---

## 21. Non-goals

- one monolithic `Era` enum that controls all mechanics;
- hard-coded Tour de France, rider, organization, country, or year exceptions;
- `PlayerTeam`, `IsHumanTeam`, or human-only legal rules;
- arbitrary executable rules supplied by mods;
- Godot scenes or UI widgets as rule authority;
- World Spy output as a rule input;
- a complete economy, transfer, calendar, contract, or race design in this document;
- automatic balancing of every valid custom module mix;
- silent repair, silent module substitution, or silent reinterpretation of old fields;
- a second game-state machine for rule transitions.

---

## 22. Implementation notes

- Keep rule contracts in headless domain/rules assemblies with no Godot dependency.
- Keep module parameters in resolved immutable content; keep mutable transition and world effects in World State.
- Use stable reason codes for validation, command rejection, transition conflicts, and repairs.
- Capture the effective rules identity at domain boundaries that can outlive a module change.
- Test rules with both HumanInput and AIInput authorities through the same Commands.
- Do not use runtime-dependent hashes or collection order in module resolution/evaluation.
- Record units and exact numeric representations in each future domain schema.

---

## 23. Migration impact

This DRAFT defines contracts only. It changes no implemented module or save.

A future rules schema or contract change must state:

- source and target module/contract versions;
- affected scenario presets and custom combinations;
- treatment of active world state and pending work;
- transition, grandfathering, conversion, validation, and repair behavior;
- content and save migration requirements;
- determinism, balance, and long-save regression coverage.

---

## 24. Test and playtest criteria

### Resolution

- The same scenario and pack artifacts produce the same itemized module set, order, and rules identity.
- JSON property, filesystem, archive, and dictionary order do not affect resolution.
- Missing, duplicate, unsupported, and incompatible module providers fail with stable diagnostics.
- A compatible mixed-era custom scenario resolves without being normalized to one era.
- Human and AI authorities see the same active legal rules.

### Evaluation

- The same state, resolved rules, and ordered Command produce the same decision/effects under the same build.
- A rejected rule evaluation leaves World State and gameplay RNG unchanged.
- Queries and forecasts leave world checksum and gameplay RNG unchanged.
- No rule changes because the player opens a screen or because an organization is human-controlled.
- A named race with the same rule-relevant properties as another event follows the same general contract.

### Transitions

- A transition applies exactly at its deterministic effective phase.
- Pausing, rendering, or reloading before the boundary does not change its effects.
- Existing contracts, rosters, equipment, rankings, and pending work follow declared grandfathering/conversion rules.
- A failed transition rolls back without partial state or ID reuse.
- Automatic and actor-owned repairs use the same legal actions for human and AI authorities.
- Historical outcomes retain the rules identity effective when they occurred.

### Persistence and causality

- Save/load preserves active module identity, pending transitions, ordering, and repair obligations.
- Loading never replaces recorded rules with the currently installed default preset.
- Compacted and uncompacted worlds produce the same gameplay-relevant future after a rule transition under identical build/content/commands.
- Spy OFF and Spy DECISIONS produce identical transition and gameplay state.

### Playtest

- New Game explains a module conflict in terms of consequences, not raw implementation details.
- Difficulty changes stated decision pressure or support without hiding legal controls or granting AI hidden truth.
- An unusual valid custom scenario behaves according to its selected modules even when it diverges from historical expectations.
