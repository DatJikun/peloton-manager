# Peloton Manager — External AI Review Instructions

## Read in this order

1. `VISION.md`
2. `DECISIONS.md`
3. `HANDOFF.md`
4. `DOCS.md`
5. `ARCHITECTURE.md`
6. `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`
7. `DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md`
8. `Peloton_Manager_design_notes_v1.0.md`
9. `AI_MANAGER_SYSTEM_v0.2.md`
10. `LONG_SAVE_AND_PERFORMANCE_v0.2.md`
11. `RACE_ENGINE_DESIGN_v0.2.md`
`RACE_SPY_DEBUGGING_v0.1.md`
`WORLD_SPY_AND_DECISION_TRACING_v0.1.md`
12. `RACE_ENGINE_RESEARCH_2026-08-25.md`
13. `DOCS_GOVERNANCE.md`


## What we want from you

Do not redesign the game from scratch. Review the current design critically and identify contradictions, hidden assumptions, boring/repetitive systems, architecture blockers, data-model risks, determinism risks, 100-year save risks, AI symmetry violations, multiplayer/hotseat blockers and places where difficulty comes from UI obscurity instead of management depth.

Classify feedback as:

```text
CRITICAL BEFORE CODING
IMPORTANT BEFORE SYSTEM IMPLEMENTATION
SAFE TO DEFER
DISAGREE / NEEDS OWNER DECISION
```

When proposing a change, explain the problem it solves and which current decision it conflicts with. Do not silently replace locked decisions. Prefer systemic rules over hard-coded exceptions.

## Current priorities

Core loop follows `D-035` in `DECISIONS.md`: one thing at a time. Roster → watching → calendar → Hub. Do not parallelize rider database, negotiations, and native HTML.

This tree is through step 4 (calendar of three races per season). Owner §49 playtest is step 3 and is not closed by tests. Thin Godot Hub is step 5. Radio/DS, transfers, sponsors, and avatars wait.

## Core invariants

- Simulation determines outcomes; history does not script winners.
- Human and AI organizations use the same world rules and actions.
- The player is a manager career, not a permanent team.
- ManagerCareer and DecisionAuthority are separate concepts.
- Domain truth becomes actor knowledge only through publication/observation rules.
- Default economy has no hidden global luxury tax or automatic century-scale nominal inflation.
- Knowledge belongs to organizations/people and is not globally omniscient.
- Results are evidence of ability, not ability itself.
- `Advance Day` is the UX time unit; runtime remains event-driven.
- Stable entity IDs are never reused.
- Long saves compact old state rather than deleting historical identity.
- Management difficulty comes from decisions, not hidden UI traps.
- Race gameplay must contain meaningful decisions; realism does not excuse boredom.
- Race dropping is emergent from required power, realizable power, gaps and shelter; no generic stamina-zero rule.
- Race decisions use observations/interpretations, not hidden physiological truth.
- Core-loop work follows `D-035`: one step at a time (Watch film+route → roster → owner watch → calendar → Hub).


## Before writing code

Read:
- `AI_DEVELOPMENT_RULES_v0.1.md`
- `GITHUB_WORKFLOW_v0.1.md`
- `CODEBASE_MAP.md`

Never begin broad coding without a scoped task card and acceptance tests.

## When the repository and Git history are available

Treat the existing code and Git history as additional sources of truth. If the
code conflicts with an accepted document, do not automatically assume that the
code is correct. First determine whether this is an implementation bug, stale
documentation, or a deliberate later decision.
