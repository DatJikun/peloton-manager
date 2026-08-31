# Peloton Manager — DOCS

**Status:** REVIEW  
**Purpose:** indeks aktywnej dokumentacji i canonical filenames.

| Document | Status | Purpose |
|---|---|---|
| `VISION.md` | REVIEW | North star, priorytety, anti-goals |
| `DECISIONS.md` | ACCEPTED | Stabilne owner locks / decision IDs |
| `ARCHITECTURE.md` | REVIEW | Canonical current architecture |
| `Peloton_Manager_Technical_Architecture_v1.0.md` | REVIEW SNAPSHOT | Wersjonowany export canonical architecture |
| `Peloton_Manager_design_notes_v1.0.md` | REVIEW | Główny high-level game design |
| `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md` | REVIEW | Determinism, event taxonomy, barriers, info pipeline |
| `DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md` | REVIEW | UX/game-design prawa i anti-patterns |
| `AI_MANAGER_SYSTEM_v0.2.md` | REVIEW | AI managers, authority, traits, explainability, scheduler |
| `LONG_SAVE_AND_PERFORMANCE_v0.2.md` | REVIEW | 100-year saves, IDs, compaction, growth metrics |
| `DOCS_GOVERNANCE.md` | REVIEW | Lifecycle i hierarchy of truth |
| `HANDOFF.md` | ACTIVE | Żywy stan projektu |
| `AGENTS.md` | ACTIVE | Cursor Cloud instructions, Composer 2.5 coding split (D-035) |
| `UI_SITEMAP_v0.1.md` | DRAFT | Ekrany, nawigacja, modality, knowledge-bounded UI |
| `GAME_STATES_v0.1.md` | DRAFT | Kanoniczne stany, przejścia, save/load i runtime barriers |
| `DATA_MODEL_v0.1.md` | DRAFT | Minimalne byty, IDs, employment, authority, knowledge i event contracts |
| `RACE_ENGINE_DESIGN_v0.2.md` | REVIEW | Race physics, rider capability, groups, gaps, DS decisions, prototype gate |
| `KNOWN_DIFFERENCE_FROM_CODE.md` | ACTIVE | Prototype limits versus accepted Race Engine and data-model contracts |
| `RACE_SPY_DEBUGGING_v0.1.md` | REVIEW | Developer race traces, truth-vs-knowledge diagnostics, reproducible AI decision reports |
| `WORLD_SPY_AND_DECISION_TRACING_v0.1.md` | REVIEW | Shared explainability/debug framework for AI decisions across all systems |
| `AI_DEVELOPMENT_RULES_v0.1.md` | REVIEW | Mandatory coding/documentation/testing/Git rules for AI development |
| `GITHUB_WORKFLOW_v0.1.md` | REVIEW | Branch, commit, PR and merge workflow |
| `CODEBASE_MAP.md` | ACTIVE | Fast navigation map of code ownership and debugging entry points |
| `RACE_ENGINE_RESEARCH_2026-08-25.md` | RESEARCH SOURCE | Research basis for physiology, physics, DS information and tactics |
| `CONTENT_FORMAT_v0.1.md` | DRAFT | JSON content packs, manifesty, IDs, dependencies, overrides i deterministic resolution |
| `RULESETS_v0.1.md` | DRAFT | Składane moduły reguł, compatibility i effective-dated transitions |
| `SAVE_FORMAT_v0.1.md` | DRAFT | SQLite save contract, schema versions, migrations, content identity i recovery |
| `TESTING_v0.1.md` | DRAFT | Test layers, golden scenarios, probes, invariants, soak and playtest gates |
| `CAREER_WORLDTOUR_SLICE_v0.1.md` | DRAFT | Owner 2026-08-31 career slice: world–race bind, WT 2026 pack, contracts, no minigames |

## Read order for a new AI session

```text
VISION.md
↓
DECISIONS.md
↓
HANDOFF.md
↓
DOCS.md
↓
ARCHITECTURE.md
↓
DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md
↓
relevant system design docs
```

Cursor Cloud also always applies `AGENTS.md`. Live code layout is `CODEBASE_MAP.md`. Prototype gaps versus contracts are `KNOWN_DIFFERENCE_FROM_CODE.md`.

## Current snapshot (docs vs code)

These DRAFT contracts exist and still await owner REVIEW. They are not a reason to ignore the code:

1. `UI_SITEMAP_v0.1.md`, `GAME_STATES_v0.1.md`, `DATA_MODEL_v0.1.md`
2. `CONTENT_FORMAT_v0.1.md`, `RULESETS_v0.1.md`, `SAVE_FORMAT_v0.1.md`, `TESTING_v0.1.md`
3. `RACE_ENGINE_DESIGN_v0.2.md` — REVIEW; official results already use `PrototypeRaceEngine` below this contract

Already in code (thin versus those contracts): Milestone 0 spine, nine GameStates, SQLite SchemaVersion 1, skeleton Advance Day, prototype race, CLI Hub/inbox/prep/Watch, Godot Watch Race presentation. See `HANDOFF.md`.

Not in code yet, though named in `DATA_MODEL_v0.1.md`: `RiderCareer` as a world career bound to race, `OrganizationKnowledgeStore`, `PersonalKnowledge`, `RecruitmentCase`. Career persons and race-prototype riders are still separate. AI managers, sponsors, training, and a full legal rules engine are not implemented.

Remaining system design (do not treat as the next coding task unless `HANDOFF.md` says so):

- Owner slice: `CAREER_WORLDTOUR_SLICE_v0.1.md` (world–race bind first)
- Rider Performance / Training / Development
- Calendar / Recruitment / Contracts / Economy-Sponsors

Do not close the owner §49 fun gate with automations. Do not build the rejected Career Hub.

## Data Model must include from day one

- `Person`,
- `ManagerCareer` + employment,
- `DecisionAuthority`,
- `Organization`,
- `AccessContext`,
- `OrganizationKnowledgeStore`,
- `PersonalKnowledge/RelationshipMemory`,
- `RecruitmentCase`,
- `ContentDefinitionId` vs `WorldEntityId`,
- event taxonomy identities,
- knowledge provenance/portability,
- no `PlayerTeam`.
