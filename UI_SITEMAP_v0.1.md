# Peloton Manager — UI Sitemap

**Title:** UI Sitemap  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Contract-level map of screens, navigation patterns, knowledge visibility rules, and Application Command entry points for the Godot presentation layer.  
**Authority/Owner:** Project owner (gameplay/UX architecture)  
**Supersedes:** none  
**Superseded by:** none  
**Last reviewed:** 2026-08-31  
**Related decisions/ADRs:** D-002, D-003, D-004, D-005, D-006, D-008, D-009, D-010, D-013, D-014, D-017, D-020, D-024, D-027, D-016, D-031; OPEN — Hotseat RaceLive (DECISIONS.md)

---

## 1. Purpose & scope

This document defines **what screens exist**, **how the player moves between them**, **what each screen may show** (knowledge-bounded), and **which Application Commands** screens may trigger. It does **not** specify pixel layouts, visual styling, Godot scene trees, or implementation details.

### In scope

- Top-level sitemap and screen inventory
- Navigation modality: persistent Career shell vs Card Flow vs blocking RaceLive
- `Advance Day` loop and interaction with Decision Requests / Inbox
- Manager career transitions (employed, unemployed, fired, hired)
- Truth vs knowledge rules for all player-facing UI
- Human/AI symmetry at the UI boundary (no privileged human-only data screens)

### Out of scope (defined elsewhere)

- Full state-machine legality matrix → `GAME_STATES_v0.1.md`
- Entity contracts, `AccessContext`, knowledge record semantics → `DATA_MODEL_v0.1.md`
- Race physics and DecisionRequest generation → `RACE_ENGINE_DESIGN_v0.2.md`
- Command envelopes, event taxonomy, determinism → `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`

### Downstream contract documents

| Document | What UI_SITEMAP defers to it |
|---|---|
| `GAME_STATES_v0.1.md` | Exact game-state enum, save/load restrictions per state, illegal transition guards |
| `DATA_MODEL_v0.1.md` | `ManagerCareer`, `DecisionAuthority`, `OrganizationKnowledgeStore`, `PersonalKnowledge`, `RecruitmentCase`, query payload shapes |

These documents refine the contracts without changing the screen inventory or modality rules here.

---

## 2. Presentation layer contract

The Godot client (`Peloton.Client.Godot`) is **pure presentation**:

| Allowed | Forbidden |
|---|---|
| Render query projections | Compute gameplay outcomes |
| Collect player input and emit **Application Commands** | Hold authoritative world truth |
| Local UI state (scroll, sort, selected tab) | `new Random()` or gameplay RNG |
| Presentation settings (race camera, ticker speed) | Rules validation or domain logic |
| Invoke read-only **Queries** and **Forecasts** | Direct mutation of domain entities |

Every gameplay mutation flows:

```text
UI input → CommandEnvelope → Application → Rules → Simulation → DomainEvent
                                              ↓
UI refresh ← Query projection ← Knowledge stores (via AccessContext)
```

Queries and forecasts are read-only, RNG-neutral, and knowledge-bounded (D-014, D-027).

**Look reference:** career-shell chrome and layout start from `HTML_UI_LAB.md` / `peloton-manager-full-ui-poc-v3.html`. Godot `CareerShell.tscn` copies that chrome. The HTML is not a client and does not change this sitemap's domains, Commands, or knowledge rules.

---

## 3. Navigation model & modality

Three navigation patterns coexist. They are **not** interchangeable.

### 3.1 Persistent Career shell (Management mode)

**When:** Player has an active save in day-to-day career play (employed or unemployed).

**Behavior:**

- Persistent top/side navigation between major domains (Hub, Calendar, Squad, Recruitment, Organization, World, Manager, Settings).
- Optional **season context rail** (orientation, not a state machine): e.g. `PRE-SEASON → SPRING → GIRO → TOUR → … → OFF-SEASON` with `TODAY` marker (ARCHITECTURE §72).
- Central primary action: **`ADVANCE DAY`** (D-006).
- Domain screens are **non-blocking** relative to each other; player can browse while the world date is frozen until Advance Day or a blocking overlay fires.
- Save/load available (except when blocked by RaceLive or other states defined in `GAME_STATES`).

**Why:** Management is exploratory; difficulty comes from decisions, not from hiding navigation (DESIGN_PRINCIPLES §27).

### 3.2 Card Flow (linear / wizard)

**When:** Multi-step processes with a defined start, order, and end.

**Behavior:**

- Sequential cards with **`Back`**, **`Next`**, **`Confirm`**, **`Cancel / Exit Flow`** (HANDOFF, ARCHITECTURE §15).
- Replaces or hides persistent shell navigation for the duration of the flow.
- Each step triggers Commands or collects intent for a single confirming Command at the end (pattern varies per flow; see screen inventory).
- **Forecast before commitment** on important steps when staff knowledge allows (DESIGN_PRINCIPLES §5).

**Mandatory Card Flow domains:**

| Flow | Trigger |
|---|---|
| New Game / Career Setup | Main Menu → New Game |
| Custom Scenario composition | New Game (optional branch) |
| Pre-Season Planning | Season start / HQ prompt |
| Race Preparation | Calendar / HQ race entry |
| Race Results → Debrief | After stage/race completes |
| Multi-step recruitment / contract offer | Dossier, renewal, staff hire, sponsor deal |
| Manager employment change | Job offer, application outcome, dismissal |

**Not Card Flow:** Routine browsing of roster, finances, or world rankings.

### 3.3 Blocking RaceLive

**When:** One race stage / one race day is actively simulated in live presentation mode (D-008).

**Behavior:**

- **Blocks** normal Career shell navigation: no scouting, negotiations, calendar editing, or other management domains (ARCHITECTURE §14).
- **Blocks mid-race save**; **pre-race autosave** runs before entry (D-008).
- Player may: pause, adjust safe presentation settings, respond to race Decision Requests, exit to Main Menu (abandons live session; reload from pre-race autosave).
- Grand Tour: between stages, player returns to Management shell; each stage entry re-enters blocking RaceLive for that stage only.

**Why:** Simpler state model, determinism, no partial event-queue saves (ARCHITECTURE §28).

### 3.4 Advance Day loop

`Advance Day` is the **UX time unit**; runtime remains event-driven (D-006).

```text
Player clicks ADVANCE DAY
    → Application processes ScheduledWork until end-of-day barrier
    → ObservationSignals → knowledge updates
    → DecisionRequests may be created
    → STOP if non-delegated human DecisionRequest OR end of day

If stopped mid-day:
    Player resolves Decision Request (blocking overlay OR Inbox-first — OPEN)
    → may continue same day without advancing calendar date

If end of day reached:
    Hub shows new date; Inbox/feed reflect new projections
```

**Advance Day may interrupt** the player with (non-exhaustive):

- Race briefing requirement (may launch Race Preparation Card Flow)
- Live race Decision Request (during RaceLive only)
- Negotiation / sponsor / registration deadlines
- Critical medical or roster crises marked non-delegated
- Manager employment events (offer, dismissal) when rules require human authority

**Advance Day does not** require the player to open specific screens first; the world simulates regardless (DESIGN_PRINCIPLES §32).

### 3.5 Decision presentation modality (OPEN)

**Default intent:** Blocking overlay for time-critical race decisions; Inbox/Decision Queue entry for management deadlines. Exact routing remains **OPEN** in OQ-UI-001 and OQ-GS-001.

---

## 4. Top-level sitemap (ASCII)

```text
MAIN MENU
├── Continue Career          → LoadingWorld → Management (or unemployed Career Hub)
├── Load Game                → LoadingWorld → …
├── New Game                 → [Card Flow] NEW GAME / CAREER SETUP
│   ├── Scenario / World Base
│   ├── Custom Scenario / Rules Modules (optional mix of era modules)
│   ├── History Mode (Historical / Dynamic / Chaos)
│   ├── Difficulty + Attribute Visibility (All / Guessed / None)
│   ├── Starting Manager Profile
│   ├── Starting Employment (organization OR unemployed)
│   ├── Summary → CreateWorld Command
│   └── LoadingWorld → Management
├── Challenge Mode           → [Card Flow] variant of New Game (scenario overlay)
├── Settings                 → App settings (persistent)
└── Credits / Exit

MANAGEMENT — PERSISTENT CAREER SHELL
├── CAREER HUB / DASHBOARD (HQ)
│   ├── ADVANCE DAY (primary)
│   ├── Season context rail
│   ├── Problems / risks / recommendations
│   ├── Next events & deadlines
│   └── Feed snippets (projection, not source of truth)
├── INBOX / DECISION QUEUE
│   ├── Action-required items (DecisionRequests)
│   ├── Reports & notifications (NotificationProjection)
│   └── Archive
├── CALENDAR
│   ├── World calendar & team commitments
│   ├── Rider race plans & priorities (A/B/C)
│   ├── Invitations / wildcards
│   ├── Training camps
│   └── [Card Flow] PRE-SEASON PLANNING (seasonal)
├── SQUAD / ROSTER
│   ├── Team roster list
│   ├── Rider profile (knowledge-bounded)
│   ├── Development / form / health summaries
│   └── Season role plan (per rider)
├── RECRUITMENT & SCOUTING
│   ├── Recruitment dashboard (workload, priorities)
│   ├── Scouting assignments & reports
│   ├── Shortlist
│   ├── Dossier / Recruitment Case (case file)
│   ├── Agent contact
│   └── [Card Flow] NEGOTIATION (rider / staff / sponsor branches)
├── ORGANIZATION
│   ├── Staff & departments
│   ├── Facilities / infrastructure (as ruleset provides)
│   ├── Culture / strategy summary
│   ├── Partners: Sponsors, equipment, R&D priorities
│   └── Finances (cash, commitments, forecast)
├── WORLD
│   ├── Results & classifications
│   ├── Rankings & points
│   ├── Organizations & rivals (public knowledge)
│   ├── Rider/staff encyclopedia (knowledge-bounded)
│   └── World chronicle / history
├── MANAGER / CAREER PROFILE
│   ├── ManagerCareer traits, reputation, memory (personal)
│   ├── Employment history
│   ├── Personal relationships (PersonalKnowledge)
│   └── [when unemployed] Job market & applications
├── KNOWLEDGE / INTEL (optional lens)
│   ├── Organization knowledge index (scoped to current employer)
│   ├── Rival assessments (never omniscient)
│   └── Scouting confidence map
├── SETTINGS (in-career)
│   ├── Difficulty interpretation level (where not locked by scenario)
│   ├── Presentation / accessibility
│   └── Save / Load (when state allows)

RACE CLUSTER (stage-scoped)
├── [Card Flow] RACE PREPARATION
│   Overview → Squad → Roles → Objectives → Briefing → Summary → StartRace
├── RACE LIVE (BLOCKING)
│   ├── Live presentation (ticker / 2D groups / profile / gaps)
│   ├── DS observations & recommendations (knowledge-bounded)
│   └── RespondToRaceDecision Commands
├── [Card Flow] RACE RESULTS → DEBRIEF
│   Result → Key moments → DS debrief → Medical notes → Consequences → Management
└── (optional) Fast sim path: SimulateRace → Results/Debrief without RaceLive

POST-SEASON (seasonal Card Flow — extent OPEN)
└── [Card Flow] SEASON REVIEW (optional depth)

DEVELOPER-ONLY (not player sitemap — see DEFERRED)
├── World Spy / Decision Trace viewer
├── Race Spy viewer
├── Spectate Organization (debug)
├── Manager Balance Lab UI
└── Database Editor
```

---

## 5. Screen inventory

**Modality:** `persistent` | `card-flow` | `blocking`  
**AccessContext:** All queries use viewer `PersonId` (ManagerCareer), `CurrentOrganizationId` (if employed), and `DecisionAuthorityId`. Unemployed manager: `CurrentOrganizationId` null; organization knowledge queries return empty or public-only unless explicitly personal/public.

| Screen | Purpose | Knowledge-bounded data shown | Primary actions (Application Commands) | Entry points | Exit / next | Modality |
|---|---|---|---|---|---|---|
| Main Menu | Launch, continue, configure app | None (meta) | none (navigation only) | App start | New/Load/Settings | persistent |
| App Settings | Audio, language, controls | None | none | Main Menu | Main Menu | persistent |
| New Game — Scenario | Pick base scenario / challenge | Scenario metadata (content) | none until Confirm | Main Menu | Next card | card-flow |
| New Game — Custom Modules | Mix era/ruleset modules | Module manifest, compatibility warnings | none until Confirm | New Game flow | Next / Back | card-flow |
| New Game — History Mode | Historical / Dynamic / Chaos | Mode descriptions | none until Confirm | New Game flow | Next / Back | card-flow |
| New Game — Difficulty & Visibility | Beginner/Advanced/Expert; All/Guessed/None | Setting descriptions (not hidden truth) | none until CreateWorld | New Game flow | Next / Back | card-flow |
| New Game — Manager Profile | Starting traits/skills/reputation | Profile preview from content | none until CreateWorld | New Game flow | Next / Back | card-flow |
| New Game — Starting Employment | Pick org or start unemployed | Public org summaries, vacancy knowledge | none until CreateWorld | New Game flow | Next / Back | card-flow |
| New Game — Summary | Review world recipe | Composed scenario summary | `CreateWorld` / start save | New Game flow | LoadingWorld | card-flow |
| Loading World | Load save or bootstrap world | Progress meta | none | Load / New Game | Management / error | blocking (transient) |
| Career Hub / Dashboard | Orient; central Advance Day | Org problems, deadlines, next events, staff recommendations, workload summaries — all via org + personal knowledge; forecasts where allowed | `AdvanceDay`; open Decision Request handlers | Default after load; end of flows | Any shell tab; Card Flows | persistent |
| Inbox / Decision Queue | Surface action items | NotificationProjection + DecisionRequest summaries; underlying case/offer IDs | `RespondTo…` / domain Commands per item; never "delete deadline" | Hub alert; shell nav | Resolve item or defer if delegable | persistent |
| Calendar | Plan season & commitments | Calendar entries, rider plans, provenance ("why on calendar") | `AcceptRaceInvitation`, `WithdrawFromRace`, `SetSeasonPriority`, camp/training Commands | Shell nav | Race Prep; Pre-Season Flow | persistent |
| Pre-Season Planning | Annual plan with audit | Overload warnings, sponsor objective gaps, forecast impacts | Plan Commands (priorities, tentative entries, camps) | Calendar; Hub seasonal prompt | Confirm plan → Management | card-flow |
| Squad List | Roster overview | Roster, roles, form/health/morale summaries (org knowledge) | Assign role Commands; navigate to profile | Shell nav | Rider profile | persistent |
| Rider Profile | Evaluate rider | Attributes per visibility rules; scout/coach/medical interpretations; contract; results as evidence | Contract/role Commands; open dossier if recruitment case | Squad; World; Recruitment | Back | persistent |
| Staff & Departments | People and capacity | Staff profiles, department quality, workload, responsibilities | `HireStaff`, dismiss, reassign Commands | Organization nav | Staff negotiation Card Flow | persistent |
| Recruitment Dashboard | Priorities & overload | Workload %, active cases, projected workload forecasts | `OpenRecruitmentCase`, set priority, pause case | Shell nav | Dossier; negotiations | persistent |
| Scouting View | Assign observation | Scout reports, regions, confidence, contradictions | Scouting assignment Commands | Recruitment | Report detail | persistent |
| Dossier / Recruitment Case | Case file for one subject | Knowledge refs, agent state, competition hints (not full rival truth) | `ContactAgent`, `StartNegotiation`, case priority Commands | Recruitment; Rider profile | Negotiation Card Flow | persistent |
| Agent Contact | Structured agent conversation | Agent statements (sourced, confidence); not Simulation Truth | `ContactAgent` (GaugeInterest, AskAvailability, …) | Dossier | Back to dossier | persistent or card-step |
| Negotiation — Rider Contract | Multi-step offer | Terms, reasons, relationship, market hints; forecast payroll impact | `SubmitContractOffer`, counter, withdraw Commands | Dossier; Inbox deadline | Sign / fail → Management | card-flow |
| Negotiation — Staff | Hire key staff | Same pattern; recruitment workload forecast | Staff contract Commands | Staff; Inbox | Confirm → Management | card-flow |
| Negotiation — Sponsor | Sponsor deal | Business goals text; markets; forecast revenue | Sponsor contract Commands | Organization Partners | Confirm → Management | card-flow |
| Finances | Cash vs budget clarity | Cash, committed costs, payroll, sponsor income tiers, regulatory limits — STATE/WHY/FORECAST | Budget allocation Commands (as rules allow) | Organization; Hub | Back | persistent |
| Partners / Sponsors | Active deals & pipeline | Sponsor goals (human-readable), equipment partnerships, R&D project status | Sponsor search/negotiation entry; R&D priority Commands | Organization | Negotiation Card Flow | persistent |
| Organization Strategy | Identity & direction | Organization identity, management strategy summary, sponsor dependence | Strategy priority Commands (high-level) | Organization | Back | persistent |
| World — Results | Read world outcomes | Public results, classifications, revision status | none | Shell nav | Race debrief link | persistent |
| World — Rankings | Standings | Public ranking rules output | none | Shell nav | Entity profiles | persistent |
| World — Encyclopedia | Browse actors | Public + org-scoped knowledge per AccessContext | none | Shell nav | Rider/Org profile | persistent |
| World Chronicle | Emergent history | HistoricalRecords, era labels | none | World nav | Back | persistent |
| Manager Career Profile | Player identity | ManagerCareer traits, reputation, employment history, personal memory | `ResignManager` (if allowed) | Shell nav | Job market when unemployed | persistent |
| Job Market (unemployed) | Find employment | Public vacancies, approaches, reputation fit | `ApplyForManagerRole`, `AcceptManagerContract` | Hub when unemployed; Manager profile | Employment Card Flow | persistent |
| Employment Offer Card Flow | Hire/fire transitions | Offer terms, expectations; no former employer confidential data | `AcceptManagerContract`, `DismissManager`, board Commands | Inbox; Job market | Management with new org context | card-flow |
| Knowledge / Intel View | Compare assessments | Org knowledge index; rival estimates; confidence | none (read-only) | Shell nav (optional) | Back | persistent |
| Race Preparation | Pre-race briefing setup | Race overview, route labels as summaries; rival assessments; squad form | Briefing-related Commands; `SetRaceBriefing`; `StartRace` | Calendar; Hub; Advance Day stop | RaceLive or fast sim | card-flow |
| RaceLive | Watch / intervene in stage | Observations, gaps, DS recommendations — **never** hidden race truth | `RespondToRaceDecision`; pause; presentation only | Race Preparation; Advance Day | Results flow or exit (autosave rollback) | **blocking** |
| Race Results | Immediate outcome | Official result, gaps, classifications | `FinishDebrief` entry or auto-advance | RaceLive end; fast sim | Debrief Card Flow | card-flow |
| Race Debrief | Explain performance | Plan vs execution, DS decisions, staff hypotheses — knowledge-bounded | `FinishDebrief`; optional follow-up Commands | Results card | Management | card-flow |
| Fast Sim Summary | Skip live presentation | Key Race Story from same canonical engine | `SimulateRace` (from prep) | Race Preparation | Results/Debrief | card-flow or persistent |
| Season Review | Close season loop | Season aggregates, development narratives, finance trace | Season acknowledgment Commands (if any) | End of season trigger | Management | card-flow (OPEN depth) |
| In-Career Settings | Player preferences | Non-gameplay settings | Save/Load when legal | Shell nav | Back | persistent |
| Challenge Mode Setup | Overlay objectives | Challenge definition, locked settings | Same as New Game + challenge manifest | Main Menu | New Game branch | card-flow |

---

## 6. Navigation & transition rules

### 6.1 Legal high-level transitions

```text
MainMenu ↔ LoadingWorld ↔ Management
MainMenu → NewGameFlow (card) → LoadingWorld → Management
Management → PreSeasonPlanningFlow (card) → Management
Management → RacePreparationFlow (card) → RaceLive (blocking)
RaceLive → RaceResultsFlow (card) → RaceDebriefFlow (card) → Management
Management → [hosted Employment Change presentation] → Management (same or new org context)
Management ↔ Unemployed Hub variant (employment data changes; GameState remains Management)
```

Employment Change, Unemployed Hub, Settings and Season Review are presentation routes hosted by a canonical state. They are not additional GameState values (D-031, `GAME_STATES_v0.1.md`).

Illegal (must be rejected by game state machine, not merely hidden buttons):

- Management domain navigation **during** RaceLive (D-008)
- `SaveGame` during RaceLive (D-008)
- Any screen that shows another organization's confidential knowledge without AccessContext path
- UI paths that mutate state without Commands

### 6.2 Blocking states

| State | Blocks | Allows |
|---|---|---|
| RaceLive | Shell nav, management Commands, mid-race save | Pause, presentation settings, race Decision Responses, exit to menu |
| LoadingWorld | Most interaction | Cancel if bootstrap allows |
| New Game / employment Card Flow | Shell (by design) | Back/Next within flow |
| Non-delegated DecisionRequest | Advance Day completion until resolved OR explicit delegate | Depends on request type |

### 6.3 Advance Day vs player location

- Player may be on any persistent screen when initiating Advance Day from Hub (or global Advance if Hub provides global affordance — **OPEN**).
- Scheduler runs headlessly; UI refreshes queries after processing pauses.
- Events that occur on races the player does not attend still update world state and may appear as Inbox/World results later.

### 6.4 Fired, hired, and employer change

| Event | UI reroute |
|---|---|
| Player dismissed (`DismissManager`) | Employment ends; `CurrentOrganizationId` cleared; confidential org knowledge no longer queryable; Hub switches to unemployed variant; Inbox retains personal items |
| Player accepts new job | Employment Card Flow confirms; shell context switches organization; roster/finances/recruitment refresh from new org knowledge; **no** import of former employer confidential stores (D-009) |
| Player resigns | Similar to dismissal (voluntary); reputation consequences via world simulation |
| Former employer continues | World/Calendar still simulate former org under AI authority; player sees public outcomes only |
| Approach while employed | Inbox Decision Request; may open negotiation Card Flow without changing AccessContext until accepted |

PersonalKnowledge and relationships portable per DATA_MODEL rules follow the ManagerCareer person; organization confidential data does not (D-009, HANDOFF).

### 6.5 Inbox vs systems of record

- Deadlines, offer IDs, case status live in domain objects (`RecruitmentCase`, negotiations, registrations).
- Inbox is **NotificationProjection** only (ARCHITECTURE §17).
- Marking read / archiving mail must not cancel deadlines or offers.

---

## 7. Truth vs knowledge in the UI

### 7.1 Canonical pipeline (UI boundary)

```text
Simulation Truth (never rendered in normal UI)
    ↓ ObservationSignal + publication rules
OrganizationKnowledge / PersonalKnowledge / PublicKnowledge
    ↓ Interpretation / Forecast (read-only)
Screen projection
```

Locked: D-003, D-010, D-020, D-024, D-027.

### 7.2 Per-screen rules

| Data class | UI rule |
|---|---|
| True ability, true potential, hidden injury | Never shown in normal UI; AI uses same restriction (D-010) |
| Rider attributes (rivals) | Filtered by scenario `attributeVisibility` (All / Guessed / None) |
| Scout/coach/medical text | Interpretations with source, confidence, staleness |
| Agent statements | Shown as sourced information, not fact (DESIGN_PRINCIPLES §9) |
| Results | Evidence with context; not auto-mapped to ability (VISION §10) |
| Route labels (Flat/Hills/Mountain) | Summaries; detailed decisive sectors when knowledge allows (DESIGN_PRINCIPLES §13) |
| RaceLive telemetry | Observations only — no hidden W', true fatigue bars (D-020, R-007) |
| Forecasts | Ranges/confidence from AccessContext; opening forecast twice changes nothing (D-014) |
| Player-facing Why? | Built from actor-legal knowledge at decision time (D-027) |
| Developer Spy / Race Spy | May compare truth vs belief; **never** wired to normal Queries (D-024) |

### 7.3 AccessContext scenarios

| Viewer state | Organization knowledge | Personal knowledge | Public knowledge |
|---|---|---|---|
| Employed manager | Current employer store | Manager person store | Always |
| Unemployed manager | None (org-scoped) | Manager person store | Always |
| After job change | New employer only | Retained per portability rules | Always |

Hotseat (future): active `DecisionAuthority` switches viewer; UI must not leak other human's private knowledge (ARCHITECTURE §93).

---

## 8. Human/AI symmetry & no PlayerTeam

### 8.1 Confirmation

- There is **no** `PlayerTeam` type and **no** human-only screens that expose simulation truth or rival private data (D-002, D-004, ARCHITECTURE §77).
- The human view is one `DecisionAuthority` bound to a `ManagerCareer` (D-005).
- Organizations are the same entity type whether managed by human or AI input authority.
- Every Command available in UI is a legal Application Command AI may also emit (symmetry at action layer).

### 8.2 What symmetry does **not** require

- Identical **screens** for AI (AI needs no Godot UI).
- Identical **wording** of recommendations (AI orgs use same knowledge, not same staff quality).
- Showing the player AI's internal utility scores (debug only via World Spy).

### 8.3 Delegation model in UI

- DS/staff automation uses same Commands with `AIInputAuthority` or delegated defaults.
- UI shows **who** owns automated decisions (DESIGN_PRINCIPLES §12).
- Player can override via briefing, autonomy settings, and race Decision Responses where authority allows — not direct rider control (RACE_ENGINE R-002).

---

## LOCKED DECISIONS

| ID | UI implication |
|---|---|
| D-002 | Same Commands/queries for human and AI organizations; no cheat screens |
| D-003 | Screens render knowledge, not Simulation Truth |
| D-004 | ManagerCareer-centric nav; employment change is normal routing |
| D-005 | DecisionAuthority separate from ManagerCareer identity in AccessContext |
| D-006 | Advance Day is Hub primary action; event-driven backend |
| D-008 | RaceLive blocks nav and save; one stage/day scope |
| D-009 | Job change does not carry former employer confidential UI data |
| D-010 | No true ability/potential in UI for rivals |
| D-013 | UI order of Commands must not rely on client-side race conditions |
| D-014 | Forecasts on screens are read-only previews |
| D-017–D-021 | Race UI shows observations/gaps, not stamina-zero causality |
| D-024 | Race Spy truth never in RaceLive UI |
| D-027 | Player Why? ≠ developer Spy |
| D-016 | No full Balance Lab UI until core loop gate |

---

## OPEN QUESTIONS

| # | Question | Notes / deadline |
|---|---|---|
| OQ-UI-001 | **DecisionRequest routing:** blocking modal vs Inbox-first for management deadlines? | Affects Hub vs Inbox primacy; decide before production Decision Queue routing |
| OQ-UI-002 | **Unemployed Career Hub layout:** dedicated minimal shell vs full shell with empty org panels disabled? | D-004 requires unemployed path; layout not locked |
| OQ-UI-003 | **Global Advance Day:** callable only from Hub or from any persistent screen? | D-006 UX detail |
| OQ-UI-004 | **Season Review Card Flow depth:** mandatory full flow vs optional summary panel? | Seasonal loop polish |
| OQ-UI-005 | **Negotiation UI pattern:** one shared negotiation shell vs per-domain Card Flows? | Must respect "not one generic minigame" (ARCHITECTURE §18) |
| OQ-UI-006 | **Knowledge/Intel nav:** top-level tab vs sub-views under Recruitment/World? | Information architecture only |
| OQ-UI-007 | **Hotseat RaceLive** pause/checkpoints (DECISIONS OPEN) | Deferred; UI must not assume single human authority forever |
| OQ-UI-008 | **Fast sim default:** always offer Watch vs Simulate on Race Preparation summary? | Player agency vs time cost |
| OQ-UI-009 | **Starting unemployed:** allowed in all scenarios or scenario-gated? | New Game employment card |

---

## DEFERRED

| Item | Reason |
|---|---|
| Full **Manager Balance Lab** UI | D-016; headless SimRunner first |
| **Database Editor** tool | ARCHITECTURE §29; JSON + validator first |
| Deep **training / physiology** screens (glycogen, thermal, hydration) | D-022; after race engagement gate |
| **World Spy / Race Spy** player-facing viewers | Developer infrastructure (D-025, D-024); optional debug overlay only |
| **Spectate Organization** debug UI | AI_MANAGER_SYSTEM §22 |
| **Hotseat** UI (authority switch, privacy) | Multiplayer-later (ARCHITECTURE §92–95) |
| **Online multiplayer** lobby / sync UI | Explicitly out of MVP |
| **Workshop / mod browser** UI | Data-only modding first |
| Full **R&D** project management UI | Organization system depth post-MVP |
| **Doping / integrity** decision UI | Post-MVP module |
| **Media** deep simulation UI | Lightweight in first versions (design notes §15) |
| **Procedural rules reform** UI | World evolution notifications may suffice initially |

---

## NON-GOALS

- Pixel-perfect mockups, color systems, or Godot scene hierarchy in this document
- **`PlayerTeam`** or human-only privileged data panels
- Hidden-truth race debug overlays in normal gameplay (stamina truth, rival W')
- **Inbox as system of record** for deadlines or offers
- **Interest/Agreement progress bars** as primary negotiation UX (DESIGN_PRINCIPLES §7)
- **Dossier 100%** gate before signing (DESIGN_PRINCIPLES §8)
- **Disabled buttons** as substitute for market timing (DESIGN_PRINCIPLES §6)
- **Difficulty via UI obscurity** — hidden checkboxes, unreadable budgets (DESIGN_PRINCIPLES §27)
- **Mid-race save** UI
- **Direct rider control** in RaceLive (watts, attack timing) (R-002)
- **Separate AI transfer UI** or `StealFromHuman` affordances
- **World Spy** driving gameplay decisions or consuming RNG

---

## IMPLEMENTATION NOTES

- Canonical filenames: this doc is `UI_SITEMAP_v0.1.md`; future accepted version may become `UI_SITEMAP.md` per DOCS.md index.
- `GAME_STATES_v0.1.md` enumerates the canonical states from ARCHITECTURE §14 and aligns them with the modality rules here.
- Each screen should map to one or more Queries; mutations only via Commands enumerated in `Peloton.Application` (see ARCHITECTURE §11).
- Presentation-mode race settings must not call gameplay RNG (R-010).
- Season context rail is orientation only; calendar content drives accuracy (ARCHITECTURE §72).
- Custom scenarios: module-mix validation errors surface in New Game Custom Modules card before `CreateWorld` (ARCHITECTURE §7.3).

---

## TEST / PLAYTEST CRITERIA

- New Game cannot skip Card Flow steps (state machine test, ARCHITECTURE §32).
- RaceLive: attempting shell navigation or SaveGame is rejected.
- Reload after RaceLive exit restores pre-race autosave, not mid-race state.
- Rider profile for rival org shows ≤ knowledge allowed for viewer org; switching jobs changes visible rival dossier content appropriately.
- Forecast panel refresh does not change world checksum (Spy OFF vs ON identical gameplay).
- Unemployed manager: org-scoped recruitment/finance screens show no former employer confidential data.
- Advance Day from Hub processes world events when player never opened Calendar.
- Player-facing Why? on debrief never includes debug utility or hidden attributes.
- All listed primary actions correspond to Application Commands, not local state mutation.
