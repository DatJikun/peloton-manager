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
| `UI_SITEMAP_v0.1.md` | DRAFT | Ekrany, nawigacja, modality, knowledge-bounded UI |
| `GAME_STATES_v0.1.md` | DRAFT | Kanoniczne stany, przejścia, save/load i runtime barriers |
| `DATA_MODEL_v0.1.md` | DRAFT | Minimalne byty, IDs, employment, authority, knowledge i event contracts |
| `RACE_ENGINE_DESIGN_v0.2.md` | REVIEW | Race physics, rider capability, groups, gaps, DS decisions, prototype gate |
| `RACE_SPY_DEBUGGING_v0.1.md` | REVIEW | Developer race traces, truth-vs-knowledge diagnostics, reproducible AI decision reports |
| `WORLD_SPY_AND_DECISION_TRACING_v0.1.md` | REVIEW | Shared explainability/debug framework for AI decisions across all systems |
| `AI_DEVELOPMENT_RULES_v0.1.md` | REVIEW | Mandatory coding/documentation/testing/Git rules for AI development |
| `GITHUB_WORKFLOW_v0.1.md` | REVIEW | Branch, commit, PR and merge workflow |
| `CODEBASE_MAP.md` | TEMPLATE | Fast navigation map of code ownership and debugging entry points |
| `RACE_ENGINE_RESEARCH_2026-08-25.md` | RESEARCH SOURCE | Research basis for physiology, physics, DS information and tactics |
| `CONTENT_FORMAT_v0.1.md` | DRAFT | JSON content packs, manifesty, IDs, dependencies, overrides i deterministic resolution |
| `RULESETS_v0.1.md` | DRAFT | Składane moduły reguł, compatibility i effective-dated transitions |
| `SAVE_FORMAT_v0.1.md` | DRAFT | SQLite save contract, schema versions, migrations, content identity i recovery |
| `TESTING_v0.1.md` | DRAFT | Test layers, golden scenarios, probes, invariants, soak and playtest gates |
| `AI_DEVELOPMENT_RULES_v0.1.md` | REVIEW | Reguły implementacji przez AI |

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

## Immediate design order

1. `UI_SITEMAP_v0.1.md` - DRAFT
2. `GAME_STATES_v0.1.md` - DRAFT
3. minimalny `DATA_MODEL_v0.1.md` dla Person/ManagerCareer/Organization/AccessContext/IDs/events - DRAFT
4. `RACE_ENGINE_DESIGN_v0.2.md` - DONE; next: prototype contracts after minimal Data Model
5. pełniejszy Rider Performance / Training / Development design
6. Calendar / Recruitment / Contracts / Economy-Sponsors designs
7. `CONTENT_FORMAT_v0.1.md`, `RULESETS_v0.1.md`, `SAVE_FORMAT_v0.1.md` - DRAFT
8. `TESTING_v0.1.md` - DRAFT; last named pre-code documentation gate before owner REVIEW

Nie zamykamy dużej persistence/content infrastructure przed wczesnym race/core-loop designem.

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
