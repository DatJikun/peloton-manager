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

**Day events** in designer-speak only means: when you press Advance Day, the world **does work** (recovery, form, contract expiry, later scout reports). It is not a second calendar and not a minigame. Form tick is in. Contract expiry is phase 4.

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
    OrganizationId              // WorldEntityId?; current club from the active RiderContract; null = unattached
    OriginDefinitionId
    physiology                  // fields RaceRiderProfile already needs
    Form01                      // 0..1, default 1; unused by physics until phase 2
    Freshness01                 // 0..1, default 1
    Fatigue01                   // 0..1, default 0
    Loyalty01                   // 0..1, default 0.5; stored; no transfer modifier until a market exists
    Results                     // append-only RiderCareerResult

RiderCareerResult
    RaceContentId
    DayNumber
    Place                       // 1-based; 0 if DNF
    DidNotFinish
```

`Employment` stays manager-only (see phase 4). Phase 1 used `RiderCareer.OrganizationId` as the roster link; phase 4 adds `RiderContract` and allows `OrganizationId` to become null when the contract expires.

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

Headless commands first. Same nine GameStates (D-031). No Career Hub. Godot Watch stays a race window.

#### Pre-season race entry

- `BeginPreSeasonPlanningCommand`: `Management` → `PreSeasonPlanningFlow`. Time does not advance.
- `PreSeasonPlanningProjection`: upcoming calendar races for the season with `Entered` for the **current employer**.
- `SetSeasonRaceEntryCommand(raceContentId, entered)` edits a **draft** only.
- `ConfirmPreSeasonPlanCommand` commits the draft onto world state and returns to `Management`.
- `CancelPreSeasonPlanningCommand` discards the draft and returns to `Management`.
- Entry is keyed by `RaceContentId` for the season (skip Flanders, not “this Tuesday’s copy”).
- **World create:** every organization is entered into every currently scheduled race content id (soak/tests keep racing).
- Persist `OrganizationRaceEntry` (OrganizationId, RaceContentId, Entered). **SchemaVersion 3.**
- Official start list = `RiderCareer` rows whose organization is entered for that race’s `RaceContentId`.
- **Race-due for the player** (Hub Race next / AdvanceDay blocked): today is a calendar race **and** the player’s employer is entered.
- If today is a world race but the player did **not** enter: `AdvanceDay` is legal and must **auto-simulate** that official race with the entered teams (delegated DS defaults), then record results. The world does not wait on a skipped race.
- If nobody entered, skip creating a session (no empty race). Default world-create entries make this rare.

#### Pre-race strategy step

Inside `RacePreparationFlow`, **before** Confirm:

- `SetRacePreparationStrategyCommand(leaderId, supportId, objective, briefingKind)`
- Leader and support must be distinct riders on the employer’s world roster.
- Objective: existing `RaceObjective` (`StageWin` / `GeneralClassification`).
- Briefing: existing `RaceBriefingKind` (`Chase` / `Protect`).
- Projection includes those fields plus `StrategySet`.
- `ConfirmRacePreparationPlanCommand` rejects `PREP_STRATEGY_INCOMPLETE` unless strategy is set.
- `CanStart` / `CanSimulate` still require confirmed plan.
- `WorldRaceScenarioAssembler` must honour the committed strategy for the player’s organization (leader/support/objective/briefing). Other orgs keep template/delegated defaults.
- `RacePreparationCheckpoint` stores the strategy; round-trip through SchemaVersion 3 saves.
- Update `SkeletonCareerRunner` and every test that Confirms prep so they set strategy first (deterministic: first two roster riders by Id as leader/support, StageWin + Chase unless a test says otherwise).

Tests: toggle entry then confirm — player skipped race does not block Advance Day and player riders are absent from that start list; strategy required before Confirm; strategy changes the assembled tactical plan; Cancel discards drafts; no tenth GameState; Spy neutrality still holds.

### Phase 4 — contracts

Rider wage + expiry. Loyalty is stored and queried; it is **not** a transfer modifier until a market exists. No personal sponsors. No marketability loop. Headless only. No tenth GameState. No Career Hub. No Godot planning UI.

#### Slice lock vs manager Employment (OQ-DM-002 for this slice)

- Manager `Employment` stays manager-only. Do **not** reuse it for riders.
- Riders use a new `RiderContract` entity. A generic shared “contract” table is still later.

```text
RiderContract
    Id                  // WorldEntityId
    RiderCareerId
    OrganizationId      // club this contract binds
    AnnualWage          // int, whole game-euros per year; must be > 0
    StartDate           // WorldDate; world create uses day 0
    EndDate             // WorldDate; inclusive last contracted day
```

Invariants:

- World create: exactly one `RiderContract` per `RiderCareer`.
- At most one **active** contract per rider. Active means `StartDate.DayNumber <= CurrentDate.DayNumber` and `EndDate.DayNumber >= CurrentDate.DayNumber`.
- Do not delete expired contracts. They remain history.
- `EndDate >= StartDate`.
- No overlapping contracts this phase. No renew/sign/release commands this phase (that is a market).

#### Roster link

- `RiderCareer.OrganizationId` is `WorldEntityId?`: the current club copied from the active contract.
- `WorldEntityId` cannot be 0, so unattached is **null**, not a sentinel.
- `GetRiderCareersForOrganization` returns riders whose `OrganizationId` equals that club (unattached riders drop off).
- Official start lists, prep squad, and strategy leader/support use that same roster filter. Assembler must skip `OrganizationId is null`.
- Unattached riders still exist, still receive the rest tick, still keep career history. They do not start races.

#### Expiry (Advance Day)

`AdvanceOneDay` order:

1. Organization day counters (unchanged).
2. Rest tick on every `RiderCareer` (unchanged).
3. `CurrentDate = CurrentDate.NextDay()`.
4. **Then** expire: for each `RiderContract` with `EndDate.DayNumber < CurrentDate.DayNumber`, if that rider’s `OrganizationId` still equals the contract’s club, `DetachFromClub()` (`OrganizationId = null`).

Inclusive last day: a rider whose `EndDate` is day 5 is still on the roster on day 5 (can race that day). After the Advance that moves the world to day 6, they are unattached.

`CaptureDayNotes` (after Advance Day in `GameApplication`) adds one deterministic note per rider who expired on this Advance, ordered by `OriginDefinitionId`:

```text
{Person.Name}'s contract expired.
```

Do not mention expiry when nobody expired.

#### World create / content

Extend `content/peloton.skeleton/skeleton-roster.json` and `RiderDefinition` / `RiderDocument` with required fields:

- `annualWage` (int)
- `contractEndDay` (int, inclusive)
- optional `loyalty01` (0..1); default 0.5 if omitted

Catalog validation fails if `annualWage` is missing or `<= 0`, or if `contractEndDay` is missing or `< 0`.

Skeleton wages (estimated game-euros/year — copy exactly):

| OriginDefinitionId | annualWage | contractEndDay | loyalty01 |
|---|---|---|---|
| `rider.race-prototype.alpha-leader` | 280000 | 10000 | 0.80 |
| `rider.race-prototype.alpha-support-1` | 160000 | 10000 | default |
| `rider.race-prototype.alpha-support-2` | 110000 | 10000 | default |
| `rider.race-prototype.alpha-card` | 90000 | 10000 | default |
| `rider.race-prototype.beta-leader` | 280000 | 10000 | default |
| `rider.race-prototype.beta-support-1` | 160000 | 10000 | default |
| `rider.race-prototype.beta-support-2` | 110000 | 10000 | default |
| `rider.race-prototype.beta-card` | 90000 | 10000 | default |
| `rider.race-prototype.gamma-leader` | 280000 | 10000 | default |
| `rider.race-prototype.gamma-support-1` | 160000 | 10000 | default |
| `rider.race-prototype.gamma-support-2` | 110000 | 10000 | default |
| `rider.race-prototype.gamma-card` | 90000 | 10000 | default |

`contractEndDay: 10000` is past the 10-season soak (120 days) so default races keep a 12-rider peloton. Do **not** put a short contract on the skeleton roster; expiry is proven with a constructed `WorldState` (see tests).

`CreateWorld` allocates a `RiderContract` id per rider (after person + career ids), `StartDate = day 0`, `EndDate = contractEndDay`, `AnnualWage` from content, `OrganizationId` = mapped club. Copy wage/end/loyalty into domain; do not re-derive wage from CP at runtime.

#### Query

Headless `ClubRosterProjection` on `GameApplication` (employer roster + unattached is not listed here):

```text
ClubRosterEntry
    RiderCareerId
    Name
    OriginDefinitionId
    AnnualWage
    ContractEndDay
    Loyalty01
```

Exact numbers this phase (Godot All/Guessed/None filtering waits for UI). Skeleton `attributeVisibility` stays Guessed; that does not hide wages in this headless query.

No new GameApplication **commands** except what already exists. Expiry is a world rule on Advance Day, not a Card Flow.

#### Persistence / checksum

- SQLite `SchemaVersion` **4**. Schema 1–3 saves may refuse to load.
- World checksum label `peloton-world-checksum-v4`.
- Persist `RiderContract` rows and nullable `RiderCareer.OrganizationId`.
- Checksum includes every contract (id, rider, org, wage, start day, end day) ordered by contract id, and writes `OrganizationId.Value` or `0` when null.
- Ten-season golden checksums will change; tests compare same-seed equality, not a hardcoded hex.

#### Tests (`CareerWorldTourPhase4Tests`)

- CreateWorld: 12 contracts; alpha-leader wage 280000, loyalty 0.80; others loyalty 0.5; every career `OrganizationId` matches its contract club.
- `ClubRosterProjection` for the red employer lists four riders with those wages.
- SchemaVersion 4 save/load round-trips contracts, wages, loyalty, nullable org id; checksum matches.
- Constructed `WorldState` (test helper; do not go through CreateWorld): one rider, contract `EndDate = 0`. After one `AdvanceOneDay`, date is 1, `OrganizationId` is null, `GetRiderCareersForOrganization` is empty, contract row still exists. After that Advance, a race assemble (or a start-list helper) must not include the rider.
- Constructed world with `EndDate = 5`: still on roster at day 5; unattached after the Advance to day 6.
- Unattached riders still receive the rest tick (fatigue still drops).
- Default CreateWorld 10-season runner still completes with 10 races / day 120; still 12 riders on clubs (nobody expired).
- Spy OFF/ON still matches checksum and finish order where those tests already exist.
- `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`.
- SimRunner `run` / `race` / `day --simulate-from-prep --through-results` still pass.
- Architecture tests: still no `PlayerTeam`, no `StubRaceEngine`.

#### Out of scope for phase 4

Transfer market, renew/sign/release commands, wage negotiation, personal sponsors, marketability, club cash, title sponsors, Godot Hub, tenth GameState, AI managers, wiring `peloton.wt-2026` to CreateWorld, closing §49.

### Phase 5 — 2026 WorldTour pack

Pack id `peloton.wt-2026`. Men’s 18 WT teams for the 2026–2028 cycle (existing `organizations.json` is the team list; Picnic `licenceYearsRemaining=1`). **Honesty:** physiology, wages, and budgets are **estimated gameplay bands**, labelled in `content/peloton.wt-2026/README.md`. Real names are a thin public identity layer, not a licensed UCI dump and not a 28-rider official roster. Commercial licensing remains later. The engine must still run on `scenario.peloton.skeleton`.

Do **not** put 18×28 riders into `PrototypeRaceEngine`. Do **not** replace the skeleton SimRunner `run` gate.

#### What “wired” means

New scenario id `scenario.peloton.wt-2026`. `CreateWorldCommand("scenario.peloton.wt-2026", seed)` must succeed.

Skeleton `scenario.peloton.skeleton` stays the 10-season soak and default `run` path. Existing CareerWorldTour phase 1–4 tests stay on skeleton.

#### Content

Extend `content/peloton.wt-2026/`:

- Keep `organizations.json` / `calendar.json` (36 races).
- Add `scenario.json` (`scenario.peloton.wt-2026`, `startDate: "2026-01-01"`, Dynamic/Advanced/Guessed, all 18 org ids).
- Add `roster.json` (manager + 72 riders). Manager employer: `organization.wt2026.alpecin`, name `WT Manager`.
- `pack.json` resources: `scenarios`, `roster`, existing `organizations` and `calendar`.
- Rules module `calendarStructure` parameterIdentity: `calendar-from-content` (not `days-per-season:12`).
- Competition module may reuse `rules.peloton.race.prototype-v0`.

Each org already has country, division, licence, bike, groupset, title sponsor, estimated budget. Load those into domain `Organization` (not a tech tree).

Each rider JSON: `id`, `name`, `organizationId`, `nationality` (ISO3), `birthYear`, physiology fields already used by `RiderDefinition`, `annualWage`, `contractEndDay` (10000), optional `loyalty01`.

Rider id pattern: `rider.wt2026.{teamSlug}.{role}` with roles `leader`, `support-1`, `support-2`, `card`. Team slugs match org ids after `organization.wt2026.`.

**Thin squad names (copy exactly):**

| teamSlug | role | name | nationality | birthYear |
|---|---|---|---|---|
| alpecin | leader | Mathieu van der Poel | NED | 1995 |
| alpecin | support-1 | Søren Wærenskjold | NOR | 2000 |
| alpecin | support-2 | Quinten Hermans | BEL | 1995 |
| alpecin | card | Jasper Philipsen | BEL | 1998 |
| bahrain | leader | Santiago Buitrago | COL | 1999 |
| bahrain | support-1 | Matej Mohorič | SLO | 1994 |
| bahrain | support-2 | Pello Bilbao | ESP | 1990 |
| bahrain | card | Phil Bauhaus | GER | 1994 |
| decathlon | leader | Ben O'Connor | AUS | 1995 |
| decathlon | support-1 | Paul Seixas | FRA | 2006 |
| decathlon | support-2 | Bruno Armirail | FRA | 1994 |
| decathlon | card | Bryan Coquard | FRA | 1992 |
| ef | leader | Richard Carapaz | ECU | 1993 |
| ef | support-1 | Ben Healy | IRL | 2000 |
| ef | support-2 | Neilson Powless | USA | 1996 |
| ef | card | Marijn van den Berg | NED | 1999 |
| fdj | leader | David Gaudu | FRA | 1996 |
| fdj | support-1 | Romain Grégoire | FRA | 2003 |
| fdj | support-2 | Valentin Madouas | FRA | 1996 |
| fdj | card | Paul Penhoët | FRA | 2001 |
| ineos | leader | Carlos Rodríguez | ESP | 2001 |
| ineos | support-1 | Thomas Pidcock | GBR | 1999 |
| ineos | support-2 | Filippo Ganna | ITA | 1996 |
| ineos | card | Oscar Onley | GBR | 2002 |
| lidl-trek | leader | Giulio Ciccone | ITA | 1994 |
| lidl-trek | support-1 | Tao Geoghegan Hart | GBR | 1995 |
| lidl-trek | support-2 | Toms Skujiņš | LAT | 1991 |
| lidl-trek | card | Jonathan Milan | ITA | 2000 |
| lotto | leader | Arnaud De Lie | BEL | 2002 |
| lotto | support-1 | Maxim Van Gils | BEL | 1999 |
| lotto | support-2 | Lennert Van Eetvelt | BEL | 2001 |
| lotto | card | Victor Campenaerts | BEL | 1991 |
| movistar | leader | Enric Mas | ESP | 1995 |
| movistar | support-1 | Oier Lazkano | ESP | 1996 |
| movistar | support-2 | Iván García Cortina | ESP | 1995 |
| movistar | card | Fernando Gaviria | COL | 1994 |
| nsn | leader | Biniam Girmay | ERI | 2000 |
| nsn | support-1 | Joseph Blackmore | GBR | 2003 |
| nsn | support-2 | Alexey Lutsenko | KAZ | 1992 |
| nsn | card | Ethan Vernon | GBR | 2000 |
| redbull | leader | Primož Roglič | SLO | 1989 |
| redbull | support-1 | Florian Lipowitz | GER | 2000 |
| redbull | support-2 | Jai Hindley | AUS | 1996 |
| redbull | card | Jordi Meeus | BEL | 1998 |
| soudal | leader | Remco Evenepoel | BEL | 2000 |
| soudal | support-1 | Mikel Landa | ESP | 1989 |
| soudal | support-2 | Yves Lampaert | BEL | 1991 |
| soudal | card | Tim Merlier | BEL | 1992 |
| jayco | leader | Simon Yates | GBR | 1992 |
| jayco | support-1 | Eddie Dunbar | IRL | 1996 |
| jayco | support-2 | Michael Matthews | AUS | 1990 |
| jayco | card | Dylan Groenewegen | NED | 1993 |
| picnic | leader | Max Poole | GBR | 2003 |
| picnic | support-1 | Frank van den Broek | NED | 2000 |
| picnic | support-2 | Pavel Bittner | CZE | 2002 |
| picnic | card | Tobias Lund Andresen | DEN | 2002 |
| visma | leader | Jonas Vingegaard | DEN | 1996 |
| visma | support-1 | Matteo Jorgenson | USA | 1999 |
| visma | support-2 | Wout van Aert | BEL | 1994 |
| visma | card | Olav Kooij | NED | 2001 |
| uae | leader | Tadej Pogačar | SLO | 1998 |
| uae | support-1 | João Almeida | POR | 1998 |
| uae | support-2 | Isaac del Toro | MEX | 2003 |
| uae | card | Jhonatan Narváez | ECU | 1997 |
| unox | leader | Tobias Halland Johannessen | NOR | 1999 |
| unox | support-1 | Jonas Abrahamsen | NOR | 1995 |
| unox | support-2 | Rasmus Tiller | NOR | 1996 |
| unox | card | Alexander Kristoff | NOR | 1987 |
| astana | leader | Christian Scaroni | ITA | 1997 |
| astana | support-1 | Diego Ulissi | ITA | 1989 |
| astana | support-2 | Clément Berthet | FRA | 1997 |
| astana | card | Cees Bol | NED | 1995 |

**Estimated physiology / wage (write into JSON; do not re-derive at runtime):**

Shared: `systemMassKg=8`, `cdAM2=0.29`, `baseCrr=0.004`, `wPrimeRecoveryJPerSecond` from role, positioning/handling/tacticalAwareness from role.

| role | CP | W' | Pmax | Wrec | lowD | highD | mass | pos | han | tac | base wage |
|---|---|---|---|---|---|---|---|---|---|---|---|
| leader | 410 | 30000 | 1000 | 42 | 0.88 | 0.86 | 67 | 0.86 | 0.80 | 0.84 | 800000 |
| support-1 | 375 | 26000 | 900 | 40 | 0.82 | 0.80 | 71 | 0.80 | 0.78 | 0.80 | 280000 |
| support-2 | 360 | 23000 | 870 | 38 | 0.79 | 0.76 | 73 | 0.76 | 0.74 | 0.76 | 180000 |
| card | 385 | 28000 | 1080 | 40 | 0.83 | 0.82 | 70 | 0.84 | 0.80 | 0.82 | 350000 |

Budget band from the rider’s org (`organizations.json` `budgetBand`):

| band | CP delta | wage multiplier |
|---|---|---|
| elite | +8 | 1.35 |
| high | 0 | 1.00 |
| mid | −8 | 0.75 |
| tight | −15 | 0.55 |

`criticalPowerW = roleCP + delta`. `peakPowerW = max(rolePmax + delta, criticalPowerW)`. `annualWage = round(baseWage * multiplier)` to nearest 1000. `contractEndDay = 10000`. `loyalty01` default 0.5.

#### Domain / recipe

- `Organization`: persist `Country`, `Division`, `LicenceYearsRemaining`, `TitleSponsor`, `Bike`, `Groupset`, `EstimatedBudgetEur`. Skeleton CreateWorld uses empty country, division `Skeleton`, licence 0, budget 0.
- `Person`: persist optional `Nationality` and `BirthYear` (WT riders set them; skeleton may leave null).
- `WorldState.GeneratePeriodicRaces`: `true` for skeleton (current `EnsureUpcomingRaceEntry` behaviour); `false` for WT (season is the 36 content races only).
- `WorldRecipe` carries org metadata, calendar definitions, `GeneratePeriodicRaces`, and `DefaultRaceTemplateId` (`race-scenario.peloton.prototype-v0`).
- Calendar: `DayNumber = (race.start date − scenario startDate).Days`. Title = race name. `RaceContentId` = calendar race id (`race.wt2026.tour_down_under`, …). First race (Tour Down Under, 2026-01-20) is **day 19**.

#### Race-due

Stop using “day % CalendarPeriodDays” as the only race-due signal.

`IsCalendarRaceDue` = there is a `CalendarEntry` of kind Race on `CurrentDate.DayNumber` whose result is not yet recorded (`LastCompletedRaceDay != CurrentDate.DayNumber`), and `CurrentDate.DayNumber > 0`.

`NextRaceDayNumber` = soonest Race entry with `DayNumber >= CurrentDate` that is still due; if none, `CurrentDate.DayNumber`.

Skeleton still works: it already places entries on day 12, 24, … and `GeneratePeriodicRaces` keeps adding them. WT does not add extra races after Guangxi.

#### Assembler (required for WT riders)

Today the assembler indexes prototype origin ids (`rider.race-prototype.*`) and will throw on WT ids.

- If every template starting-order origin id exists in the world (skeleton): keep the current path.
- Else (WT):
  - Starters: **cap 12**. Take the player employer’s roster (up to 4), then other **entered** orgs by `OriginDefinitionId`, up to 4 riders each, until 12. Order riders by `OriginDefinitionId`.
  - Starting positions: that order, spacing 0.7 as today.
  - Scripted template commands that reference missing origin ids: skip.
  - Tactical plans: one per org that has a starter. Player org uses committed strategy when present; others use first two roster riders by Id, `StageWin` + `Chase`.
- `BuildOfficialRaceScenario`: `RaceContentId` values like `race.wt2026.omloop` are **not** route files. Resolve the **route/tuning** template via `WorldRecipe.DefaultRaceTemplateId` (prototype circuit). Honesty: route geometry is still the synthetic proof circuit, labelled estimated — not Flanders cobbles.

This cap is an explicit prototype limit (`KNOWN_DIFFERENCE_FROM_CODE.md`), not a UCI 176-rider field.

World create still enters every org into every scheduled race (pre-season can skip). The cap, not entry, keeps the engine alive.

#### Persistence

SQLite **SchemaVersion 5**. Checksum label `peloton-world-checksum-v5`. Schema 1–4 may refuse to load. Include new org fields, person nationality/birth year, `GeneratePeriodicRaces`, and the larger calendar. Skeleton worlds also save as v5.

#### Tests (`CareerWorldTourPhase5Tests`)

- `CreateWorld` WT: 18 orgs; Picnic `licenceYearsRemaining == 1`; UAE/Visma/Ineos/RedBull/Lidl-Trek are `elite` budget; 72 riders; 72 contracts; 36 calendar races; TDU at day 19; Alpecin is employer; Pogačar / van der Poel names present.
- Save/load SchemaVersion 5 round-trips WT world checksum.
- Advance to day 19 (or `PrepareRace` on that day): official simulate produces a 12-rider start list of world `RiderCareer` ids; winner is one of those ids; career results append.
- Assembler does not throw; start list length 12; player Alpecin riders are included when Alpecin is entered.
- Skeleton CreateWorld + 10-season runner **unchanged in behaviour** (still 12 skeleton riders, still 10 races / day 120). Update schema-version assertions from 4 → 5 where they read the live store constant.
- Catalog validation still fails on missing WT rider wage/end day.

#### Gate

Same skeleton commands as today, **plus**:

`dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.wt-2026 --seed 91234 --days 20 --simulate-from-prep --through-results`

That should pass TDU (day 19) and print a world WT rider as winner. Do not add a 10-year WT soak.

#### Out of scope for phase 5

Real cobbled/mountain route profiles, 28-rider rosters, 150-rider pelotons, living promotion/relegation, Women’s WT, Godot Hub, tenth GameState, AI managers, transfer market, club cash / title-sponsor economy (phase 6), closing §49, restoring `StubRaceEngine`.

### Phase 6 — thin economy

Club cash, wage sum, one title sponsor paying a fee. **No luxury tax (D-011). No century inflation (D-012).** No personal rider sponsors. No marketability minigame. No transfer market. Headless only. No tenth GameState. No Career Hub.

Also fix the phase-5 cosmetic: `RacePreparationProjection.Title` must be today’s calendar entry title (WT: `Santos Tour Down Under`), not the hardcoded `"Skeleton race"`. Skeleton entries stay `"Skeleton race"`.

#### Player meaning

You have cash in the club. Riders cost wages every day. The title sponsor pays a fee every day. If wages outrun the sponsor, cash goes negative (overdrawn). That is the warning. This slice does **not** auto-fire riders, auto-change sponsors, or levy a hidden tax.

#### Domain

On `Organization` (mutable cash only):

```text
CashEur                     // long; may be negative; start 0
TitleSponsorAnnualFeeEur    // long >= 0
```

`TitleSponsor` name already exists. `EstimatedBudgetEur` stays the labelled content budget; it is the source of the fee at world create, not a second bank.

`WorldState.FinancialYearDays`:
- skeleton (`GeneratePeriodicRaces`): `CalendarPeriodDays` (12)
- WT (`calendar-from-content`): **365**

Active wage bill for an org = sum of `RiderContract.AnnualWage` whose `RiderCareer.OrganizationId` is that org (expired/unattached riders are not paid).

Daily integers, floor division, no RNG:

```text
dailySponsor = Floor(TitleSponsorAnnualFeeEur / FinancialYearDays)
dailyWages   = Floor(activeWageBill / FinancialYearDays)
CashEur     += dailySponsor - dailyWages
```

#### AdvanceOneDay order (extend phase 4)

1. Organization day counters  
2. Rest tick  
3. `CurrentDate = NextDay()`  
4. Contract expiry (`DetachFromClub`)  
5. **Then finance tick** for every organization (expired riders already unpaid)

`CaptureDayNotes`: if the employer `CashEur < 0`, add `The club is overdrawn.` Do not mention cash when solvent.

#### World create

- WT: `TitleSponsorAnnualFeeEur = EstimatedBudgetEur` from `organizations.json` (Alpecin 18_000_000, Picnic 12_000_000, UAE 50_000_000, …). `CashEur = 0`. Title sponsor name already loaded.
- Skeleton: if `EstimatedBudgetEur == 0`, set `TitleSponsorAnnualFeeEur = 2_000_000` and `TitleSponsor = "Skeleton Sponsor"` when empty. `CashEur = 0`. This keeps 10-season soak solvent (wage bill 640_000 vs fee 2_000_000 per 12-day year).

No new spend/sign/sponsor-market **commands**. Cash only moves on Advance Day.

#### Query

`ClubFinanceProjection` on `GameApplication` (employer, Management only, same style as `ClubRoster`):

```text
CashEur
WageBillAnnual
TitleSponsorName
TitleSponsorAnnualFeeEur
DailySponsor
DailyWages
DailyNet
Overdrawn    // CashEur < 0
```

SimRunner `day` Hub print may include `cash=` and `overdrawn=`. Do not put this in Godot.

#### Persistence

SQLite **SchemaVersion 6**. Checksum label `peloton-world-checksum-v6`. Schema 1–5 may refuse to load. Persist `CashEur`, `TitleSponsorAnnualFeeEur`, `FinancialYearDays`. Ten-season checksums change because cash ticks every day; tests keep same-seed equality, not a hardcoded hex.

#### Tests (`CareerWorldTourPhase6Tests`)

- WT CreateWorld: Alpecin `CashEur == 0`, fee `18_000_000`, `FinancialYearDays == 365`, wage bill equals the four Alpecin contracts, `DailyNet == Floor(fee/365) - Floor(wages/365)`.
- After one `AdvanceDay` on WT: Alpecin cash equals that `DailyNet`. Same seed → same cash.
- Constructed/skeleton world with fee 0 and positive wages: after AdvanceDay, cash negative; Hub notes contain `The club is overdrawn.`
- SchemaVersion 6 save/load round-trips cash, fee, and checksum.
- Skeleton 10-season runner still completes (10 races, day 120); employer cash is finite; cash never jumps by a tax (only the locked daily formula).
- WT prep on TDU: `RacePreparationProjection.Title` is `Santos Tour Down Under` (not `Skeleton race`).
- No `PlayerTeam`. No `StubRaceEngine`.

#### Gate

Same as phase 5 (skeleton format/build/test/`run`/`race`/`day`, plus WT `day --days 20 --simulate-from-prep --through-results`).

#### Out of scope for phase 6

Dynamic sponsor market, co-sponsor slots, luxury tax, inflation, marketability, personal rider sponsors, transfer/renew commands, auto-firing overdrawn clubs, Godot Hub, tenth GameState, AI managers, closing §49.

---

## 4. Docs to keep current

`HANDOFF.md`, `CODEBASE_MAP.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`, `DATA_MODEL_v0.1.md` (RiderCareer), `DOCS.md`, this file.

---

## 5. Owner follow-up 2026-09-01 — Simulate + filter + negotiate

**Landed 2026-09-01** (SchemaVersion 7). Headless commands and queries only; no Godot Hub, no Watch Race expansion.

### Watch Race

Docs had **Career Hub** as rejected (PR #4). Watch Race was built as the first Godot window after D-033. The owner now rejects Watch Race as the way you play a race. **D-043:** Simulate, then results. Filter the classification by **any** team. Do not grow Godot Watch.

### Results filter

`RaceResultPlacement` includes `OrganizationId` and organization name. Queries may return the full order or only one org’s riders (with their race places). Legal for every organization. Results are public evidence (D-042). This is not live race God-eye.

### Thin negotiation (D-044)

Inside `Management` only (no tenth GameState):

- `BeginContractNegotiationCommand(riderCareerId)`
- `SetContractOfferCommand(annualWage, contractEndDay)`
- `ConfirmContractOfferCommand` — accept or `CONTRACT_OFFER_REJECTED`
- `CancelContractNegotiationCommand`

Target: own rider (renew), unattached (sign), or another club’s rider (poach). No transfer fee. No agent board. Confirm replaces the active club link: old contract remains history with `EndDate = current day` if it was still active; new `RiderContract` starts today; `OrganizationId` becomes the employer.

Accept formula (no RNG):

```text
currentWage = active contract AnnualWage, or 0 if unattached
threshold   = currentWage == 0
              ? 100000
              : floor(currentWage * (1.10 - 0.20 * Loyalty01))
accept if offerWage >= threshold AND offerEndDay > CurrentDate.DayNumber
```

High loyalty → cheaper to keep. Unattached floor 100_000. SchemaVersion **7**. Checksum v7.

Tests: renew own rider; reject too-low offer; poach updates club; cancel discards; results filter returns only that org’s finishers; no tenth state; skeleton soak still runs.
