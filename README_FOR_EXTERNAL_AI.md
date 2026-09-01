# Peloton Manager — External AI Review Instructions

## Read in this order

1. `VISION.md`
2. `DECISIONS.md`
3. `HANDOFF.md` (live snapshot)
4. `DOCS.md`
5. `CODEBASE_MAP.md` and `KNOWN_DIFFERENCE_FROM_CODE.md`
6. `ARCHITECTURE.md`
7. `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`
8. `DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md`
9. `Peloton_Manager_design_notes_v1.0.md`
10. `AI_MANAGER_SYSTEM_v0.2.md`
11. `LONG_SAVE_AND_PERFORMANCE_v0.2.md`
12. `RACE_ENGINE_DESIGN_v0.2.md`
13. `RACE_SPY_DEBUGGING_v0.1.md`
14. `WORLD_SPY_AND_DECISION_TRACING_v0.1.md`
15. `RACE_ENGINE_RESEARCH_2026-08-25.md`
16. `MANAGER_GAMES_AND_CYCLING_RESEARCH_2026-08-31.md`
17. `DOCS_GOVERNANCE.md`


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

Pre-production, but the architecture skeleton and race prototype already exist in code. `HANDOFF.md` and `CODEBASE_MAP.md` are the live snapshot. `UI_SITEMAP_v0.1.md`, `GAME_STATES_v0.1.md`, and `DATA_MODEL_v0.1.md` remain DRAFT contracts; several identities from the data model are not in code yet (knowledge stores, recruitment). Rider careers are bound to race via `RiderCareer`.

Genre/management research lives in `MANAGER_GAMES_AND_CYCLING_RESEARCH_2026-08-31.md` (source, not a lock).

Godot career shell (`CareerShell.tscn`) presents Advance Day / Race next / simulate / results (D-043). Watch Race is **deferred** as the play path: optional film only; do not expand watching UI. It is not Career Hub. Owner fun gate `RACE_ENGINE_DESIGN_v0.2.md` §49 is `NOT VERIFIED` — do not close it with automations.

Do not begin a new gameplay system until `HANDOFF.md` Next task (or the owner) says so. Do not treat stale README sentences as current. If code and an accepted document conflict, follow `DOCS_GOVERNANCE.md`: decide whether it is a bug, stale docs, or a later decision.

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


## Before writing code

Read:
- `AGENTS.md`
- `AI_DEVELOPMENT_RULES_v0.1.md`
- `GITHUB_WORKFLOW_v0.1.md`
- `CODEBASE_MAP.md`

Never begin broad coding without a scoped task card and acceptance tests.

## When the repository and Git history are available

Treat the existing code and Git history as additional sources of truth. If the
code conflicts with an accepted document, do not automatically assume that the
code is correct. First determine whether this is an implementation bug, stale
documentation, or a deliberate later decision.
