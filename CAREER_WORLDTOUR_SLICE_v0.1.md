# Peloton Manager — Career WorldTour slice

**Title:** Career WorldTour slice  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Owner-directed next slice after Watch Race. Player-language decisions plus the implementation contract.  
**Authority/Owner:** Project owner (player feedback, 2026-08-31)  
**Related decisions:** D-006, D-011, D-012, D-016, D-022, D-031, D-034, D-036–D-042  

---

## 1. For the owner (plain language)

This is not a full game yet. This slice turns the prototype into the start of a **career**: the people in your club are the people who race, results stay on their history, and a 2026 WorldTour database becomes the world.

### 1. Your answers, locked

| # | You said | What we do |
|---|---|---|
| 1 | Most świat–wyścig **musi** być. Wielka historia kariery. Wszystko udokumentowane. | First code. Same person in the club, on the start list, and in the chronicle. Documented here and in `KNOWN_DIFFERENCE_FROM_CODE.md`. |
| 2 | Tak — dzień ma zmieniać kolarza. | `Advance Day` changes form / freshness / fatigue, not only the date. |
| 3 | Przed wyścigiem okienko strategii. W przedsezonie wybór wyścigów. | Two Card Flows, **no extra GameState**: pre-season pick which races you enter; pre-race strategy (roles/plan) inside preparation. |
| 4 | Nie wiesz, bo są tryby z widocznym / częściowym / ukrytym OVR i POT. | Those **are** the knowledge model. We do not add a fourth fog. See §2. |
| 5 | Nie wiesz, co to „eventy dnia”. Plus baza prawdziwego kolarstwa 2026. WorldTour na start. Pamiętać o 3 latach WT i niższych ligach. | Explained in §3. WorldTour pack is in this slice. Lower categories and the 3-year licence are architecture, not the first playable grid. |
| 6 | Kontrakty **absolutnie**. Lojalność? Marketability? Własni sponsorzy? Co jest overkill? | §4. |
| 7 | Żadnych minigier. | Staff never becomes a minigame. |
| 8 | Nie rozumiesz finansów. | Explained in §5. Thin money after wages exist. |
| 9 | (puste) | AI managerów **nie** zaczynamy, dopóki nie poprosisz. |

§49 fun gate stays a **manual** playtest. Career Hub stays rejected. No tenth GameState.

### 2. Item 4 — OVR / POT modes are not a separate mystery system

New Game already has three independent visibility settings (not difficulty):

- **All** — you see attributes (OVR/POT and the rest the UI is allowed to show).
- **Guessed** — ranges and confidence, not naked truth.
- **None** — hidden. You judge people from results, context, your club, scouting later.

The “knowledge spine” is only the **backend of those modes**: the simulation knows the truth; a club does not get God-eye just because the engine does. We are **not** building a scouting/dossier game in this slice. First honest step: race results become public evidence on the career, and later queries respect All / Guessed / None. If All is on, showing OVR is legal. If None is on, showing rival OVR is a bug.

### 3. Item 5 — “Day events” and the 2026 database

**Day events** in designer-speak only means: when you press Advance Day, the world **does work** (recovery, form, contract expiry, later scout reports). It is not a second calendar and not a minigame. Right now Advance Day mostly adds +1 to the date. Form tick is the first real work.

**2026 database — can we do it ourselves?** Yes as a **content pack**, not as a licensed official product.

- First playable grid: **men’s UCI WorldTour 2026**, 18 teams, 2026–2028 licence cycle.
- Public sources: UCI licence list, team/rider lists, published calendars, published route profiles where available.
- Physiology (CP/W'/Pmax), wages, and budgets are **estimated gameplay numbers**, labelled as such. We do not pretend we have a secret ProCyclingStats dump.
- Real names: you asked for them. Commercial licensing of real names/jerseys remains a later legal problem (`Peloton_Manager_design_notes_v1.0.md` §38). The engine must still work with a fictional pack.
- **3-year WorldTour licence** and **ProTeam / Continental** tiers: stored on the organization (division, licence years remaining). First playable season does not require a living promotion/relegation sim. The fields exist so we do not paint ourselves into “only WT forever”.
- Women’s WorldTour is out of this slice (avatar/content direction is men’s peloton for now).

### 4. Item 6 — contracts vs overkill

**In this slice**

- Rider **contract**: club, wage, start, expiry. Without this, the roster is a costume.
- **Loyalty**: one number/trait that makes staying or leaving more or less likely later. Not a relationship minigame.

**Overkill for now (do not build)**

- Personal rider sponsors as a game.
- Marketability as its own loop or minigame.
- Agent-negotiation board game.

**Later, with the sponsor economy (not now)**

- A quiet **marketability** number that sponsors care about. A number is fine. A minigame is not.

### 5. Item 8 — money, in player words

You have a club budget. Riders cost wages. A title sponsor (and later co-sponsors) pays the bills. If you overspend, you get worse sponsors or you cannot keep riders — not because a hidden luxury tax fired.

We implement a **thin** version **after** contracts have wages. Not before the world–race bind.

### 6. Honest order of code

1. World ↔ race bind + career result history (this is the gate for everything else).
2. Form / freshness / fatigue on Advance Day.
3. Pre-season race entry + pre-race strategy window.
4. Rider contracts + thin loyalty.
5. 2026 WorldTour content pack (can be authored in parallel once identities exist).
6. Thin sponsor-market / budget.
7. Not this slice: AI managers, staff minigames, scouting dossiers, living 3-year relegation.

---

## 2. World ↔ race bind (implementation contract)

**PLAYER VALUE:** After a race, you can point at a person in your club and say “he won / he was 12th / he blew up on the climb”, and it is the same person who started.

### Invariants

1. `Person` is the human. `RiderCareer` is the racing career. Race `RiderId` **is** `RiderCareer.Id` (a `WorldEntityId`).
2. Official start lists are built from **world** `RiderCareer` rows of participating organizations, plus the route/tuning from race **content**. The disconnected `peloton.race-prototype` pack must not remain the official start list.
3. `LastRace.WinnerId` and `FinishOrder` are those world IDs.
4. Each starter gets an append-only `RiderCareerResult` (race content id, day, place, DNF flag). This is career history. Compaction may later compact representation, not identity (D-015).
5. Prep squad query uses the player employer’s world roster, not fixture IDs.
6. Human and AI teams use the same bind (D-002).
7. Godot Watch stays presentation: it already consumes `LastRace` / `RaceWatch`. Do not put roster logic in Godot. Do not build Career Hub.
8. SchemaVersion becomes **2**. Schema 1 skeleton saves are pre-production and may refuse to load (document in `KNOWN_DIFFERENCE_FROM_CODE.md`).

### Domain (minimum)

```text
Person
    Id
    Name
    OriginDefinitionId?

RiderCareer
    Id                          // WorldEntityId; used as race RiderId
    PersonId
    OrganizationId              // active club; later replaced by rider Employment/contract
    OriginDefinitionId
    physiology                  // fields RaceRiderProfile already needs
    Form01                      // 0..1, default 1; unused by physics until phase 2
    Freshness01                 // 0..1, default 1
    Fatigue01                   // 0..1, default 0
    Loyalty01                   // 0..1, default 0.5; unused until contracts phase
    Results                     // append-only RiderCareerResult

RiderCareerResult
    RaceContentId
    DayNumber
    Place                       // 1-based; 0 if DNF
    DidNotFinish
```

`Employment` today is manager-only. Do not invent rider employment tables in phase 1; `RiderCareer.OrganizationId` is the roster link until the contract phase.

### Application

- `CreateWorld` materializes Person + RiderCareer + Organization from content (not `"Skeleton Rider N"`).
- `StartRace` / `SimulateRace` / Watch path: build `RaceScenario` from **world roster + race route content**.
- After `CommitOfficialResult`, write `RiderCareerResult` for every starter and keep `LastRace` / calendar result.
- Calendar race entries store a `RaceContentId` (extend `CalendarEntry` — do not keep the title as the only identity).

### Content (phase 1, still small)

Extend `content/peloton.skeleton` (or a dedicated roster resource it references) so each skeleton organization has real physiology riders. Migrating the existing `peloton.race-prototype` rider/team documents into world content is allowed if OriginDefinitionIds stay stable and tests keep determinism.

The prototype **route** (synthetic proof circuit) may remain the first official route until the 2026 calendar pack lands. Route and start list are separate.

### Tests (phase 1 must prove)

- Finish order IDs are `RiderCareer` IDs present in `WorldState` before the race.
- Same seed → same finish order and same career history rows.
- Save/load SchemaVersion 2 round-trips riders and results.
- Prep squad is the employer’s world roster.
- Spy OFF/ON still matches checksum and finish order.
- `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`.
- SimRunner `day --simulate-from-prep --through-results` still runs; output shows a world rider as winner.
- Architecture tests: still no `PlayerTeam`, no `StubRaceEngine`.

### Out of scope for phase 1

Form tick, pre-season picker, strategy window UI, contracts, 2026 names, Godot career shell, D-032, closing §49.

---

## 3. Later phases (same slice, after bind)

### Phase 2 — day state

Advance Day must change stored `Form01` / `Freshness01` / `Fatigue01` for every `RiderCareer`. Official races must use those values. No `new Random()`. No gameplay RNG on this tick (closed formula, D-013). Deep glycogen/thermal stays deferred (D-022).

**Rest tick** (every `AdvanceOneDay`, after org day counters, before or as part of the same world day):

```text
Fatigue01    = clamp01(Fatigue01 * 0.82)
Freshness01  = clamp01(Freshness01 + 0.12 * (1 - Freshness01))
Form01       = clamp01(Form01 + 0.05 * (0.90 - Form01))
```

Form drifts toward 0.90 at rest, not 1.00.

**Race load** (when `RecordRace` appends a starter result):

```text
Fatigue01    = clamp01(Fatigue01 + 0.30)
Freshness01  = clamp01(Freshness01 - 0.25)
Form01       = clamp01(Form01 - 0.08)
```

**Race capability** (`WorldRaceScenarioAssembler.ToRaceProfile`): do not mutate stored CP. Scale the profile fed to the engine:

```text
readiness = (0.70 + 0.30 * Form01) * (0.85 + 0.15 * Freshness01) * (1.0 - 0.25 * Fatigue01)
criticalPowerW' = CriticalPowerW * readiness
peakPowerW'     = max(PeakPowerW * readiness, criticalPowerW')
```

**Also:** `TeamRaceObservation.DecisionAuthorityId` must be a real `WorldState` authority id (the human authority for this slice), not `organizationId + 100`.

Tests: rest days recover fatigue; a race raises fatigue on starters; same seed → same form trajectory and same finish order; Spy OFF/ON still matches; SchemaVersion stays 2; no Career Hub; no tenth GameState.

### Phase 3 — windows

- `PreSeasonPlanningFlow`: pick which calendar races this organization enters. Confirm commits entries. Cancel discards the draft.
- `RacePreparationFlow`: extra strategy step (leader / support / objective / briefing) **before** Confirm. Still the same nine states (D-031). Headless commands first; Godot Watch does not become a Hub.

### Phase 4 — contracts

Rider wage + expiry. Loyalty used only as a modifier when a transfer system exists; until then it is stored and visible under All/Guessed rules.

### Phase 5 — 2026 WorldTour pack

Pack id `peloton.wt-2026`. Men’s 18 WT teams for the 2026–2028 cycle (UCI licence list). Seed team list:

```text
Alpecin–Premier Tech
Bahrain Victorious
Decathlon CMA CGM Team
EF Education–EasyPost
Groupama–FDJ United
Ineos / Netcompany–INEOS (use UCI licence name; alias the other)
Lidl–Trek
Lotto–Intermarché
Movistar Team
NSN Cycling Team
Red Bull–Bora–Hansgrohe
Soudal Quick-Step
Team Jayco AlUla
Team Picnic PostNL          // one-year licence; store licenceYearsRemaining=1
Team Visma | Lease a Bike
UAE Team Emirates XRG
Uno-X Mobility
XDS Astana Team
```

Each organization: country, division=`WorldTour`, licence cycle 2026–2028, bike/groupset when public, title sponsor name, **estimated** budget band. Each rider: name, nationality, birth year, estimated physiology, estimated wage band. Routes: WT calendar races with stage/route parameters where a public profile exists; otherwise a labelled estimated profile. Equipment: bike + groupset as organization equipment, not a tech tree.

### Phase 6 — thin economy

Club cash, wage sum, one title sponsor paying a fee. No luxury tax. No century inflation.

---

## 4. Docs to keep current

`HANDOFF.md`, `CODEBASE_MAP.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`, `DATA_MODEL_v0.1.md` (RiderCareer), `DOCS.md`, this file.
