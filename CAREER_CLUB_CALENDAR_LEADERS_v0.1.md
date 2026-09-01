# Peloton Manager — Club pick, calendar entries, per-event leaders

**Title:** Career club / calendar / leaders  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Owner lock D-050 (2026-09-01): the year is not one rigid team. Player picks the club, which WorldTour events to enter, and who leads each entered event.  
**Authority/Owner:** Project owner (player)  
**Related decisions:** D-004, D-031, D-037, D-038, D-043, D-045, D-048, D-049, D-050  

---

## 1. For the owner (plain language)

You start a career by **choosing a WorldTour club** (UAE, Alpecin, Visma, … — the 18 WT teams). Then you tick **which of the 36 races** that club rides this year (Tour is one tick, not 21). For each race you entered you name **one leader**.

Race morning can still change the leader for that day. Skipping a race means your riders stay home; the rest of the world still races.

**Not in this slice:** 28-man roster, aging, year 2027, CdA split, sponsor market, scouting. Staff / sponsors / finance screens stay a drawing.

**Assumptions (say if wrong):** only the 18 WT clubs are pickable (not wildcard ProTeams / Australia). All invited events start ticked on; you uncheck what you skip. One leader per *event* (one GC leader for the whole Tour).

---

## 2. Player flow

```text
MainMenu
  → New Game: list 18 WT clubs (name, country, title sponsor)
  → CreateWorld (scenario.peloton.wt-2026, seed, chosen org)
  → PreSeasonPlanningFlow: 36 events, entered?, leader?
  → Confirm → Management (career shell on that world)
```

Load save skips New Game. Cancel New Game stays on the picker (no skeleton auto-create).

Polish UI copy:

| English (code) | Polish (player) |
|---|---|
| New Game | Nowa gra |
| Choose team | Wybierz zespół |
| Season plan | Plan sezonu |
| Entered | Jedziemy |
| Skip | Pomijamy |
| Leader | Lider |
| Confirm season | Zatwierdź sezon |

---

## 3. Implementation contract

### 3.1 CreateWorld employer

`CreateWorldCommand(string ScenarioId, long Seed, string? EmployerOrganizationOriginId = null)`

- `null` → recipe `Manager.OrganizationId` (Alpecin / skeleton red). **Existing tests and soak stay green.**
- Non-null on WT: must be an organization in the recipe with `Division == "WorldTour"`. Else reject `EMPLOYER_NOT_PLAYABLE`.
- Manager `Employment` attaches to that org. Do not rewrite rider contracts of other clubs.

Query without a world:

```text
NewGameClubProjection(OriginId, Name, Country, TitleSponsor, Division)
GameApplication.ListNewGameClubs(scenarioId)  // WT: Division WorldTour only, name order
```

### 3.2 Pre-season entries + leaders

`OrganizationRaceEntry(OrganizationId, RaceContentId, Entered, WorldEntityId? DesignatedLeaderId)`

World create: same invite rules as today; `DesignatedLeaderId = null` (runtime default = squad-order captain).

Draft in `PreSeasonPlanningFlow` (no tenth GameState):

- `SetSeasonRaceEntryCommand(raceContentId, entered)` — already exists
- **New** `SetSeasonRaceLeaderCommand(raceContentId, leaderCareerId)`
  - Leader must be on the employer roster
  - Reject `PREP_STRATEGY_RIDERS_INVALID` otherwise
- `PreSeasonRaceEntryProjection` adds `DesignatedLeaderId`, `DesignatedLeaderName`
- Confirm writes entered + leader. Cancel discards draft.
- Entry stays **per event id** (`race.wt2026.tdf`), not per stage.

### 3.3 Race-day default strategy

`RacePreparationSupport.SetDefaultStrategy`:

1. Today's `RaceContentId`
2. If employer entry has `DesignatedLeaderId` still on roster → that leader
3. Else first rider in `RiderSquadOrder`
4. Support = next distinct squad rider
5. Objective `StageWin`, briefing `Chase` unless already set

Race-day `SetRacePreparationStrategyCommand` may override **this stage** and does not rewrite the season plan.

### 3.4 Persistence

SQLite **SchemaVersion 9**. Checksum label `peloton-world-checksum-v9`. Include `DesignatedLeaderId` (0 if null) in `WorldChecksum` after `Entered`. Schema 1–8 may refuse to load.

Standalone SimRunner `race` / `watch` gate checksum **unchanged**.

### 3.5 Godot career shell

- `_Ready` must **not** `OpenSkeleton()`. Start at `MainMenu` / New Game club list.
- After club pick: `CreateWorld` WT + seed `91234` + chosen origin id, then `BeginPreSeasonPlanningCommand`.
- Pre-season screen: world events (36), toggle Jedziemy/Pomijamy, leader dropdown from `ClubRosterProjection`.
- CTA on that flow: **Zatwierdź sezon** → `ConfirmPreSeasonPlanCommand`.
- Management: top bar employer is world name; date meta is WorldTour not „Szkielet”.
- **Desk upcoming + Calendar view use `host.Calendar` / pre-season projection**, not `CareerLookCatalog` months.
- **Desk squad + Squad view use `ClubRosterProjection`** (name, 1–99 including **Bruk**, wage, contract end). Look-catalog Beskid riders stay off those tables.
- Staff / sponsors / finance / scouting / market / history / ranking / staff notes stay look catalog + toast.
- Keep `OpenSkeleton` on the host for tests. Add `OpenWorldTour(employerOriginId, seed)`.
- Watch film stays off by default. No Career Hub.

### 3.6 SimRunner

`day` gains optional `--employer <organization.wt2026.*>`.

Print `employer=` (already) and after pre-season confirm, compact `entry=` / `leader=` lines when a plan was applied in tests (not required on every soak).

### 3.7 Tests (required)

`tests/Peloton.Application.Tests/CareerWorldTourPhase8Tests.cs` (or `CareerClubCalendarLeadersTests.cs`):

1. Default `CreateWorld` WT still employs Alpecin.
2. `CreateWorld(..., "organization.wt2026.uae")` employs UAE; Alpecin riders stay at Alpecin.
3. ProTeam / National / unknown origin → `EMPLOYER_NOT_PLAYABLE`.
4. `ListNewGameClubs` returns 18 WT, no Australia national.
5. Skip Lombardia: player riders absent; Advance Day not blocked; world still races.
6. Set van der Poel as Roubaix leader → `SetDefaultStrategy` on that day uses him; Philipsen remains default on a classified Flat unless set.
7. Cancel pre-season restores previous entries/leaders.
8. Schema 9 save/load round-trips `DesignatedLeaderId`.
9. Skeleton `CreateWorld` + 10-season runner still succeeds.
10. Godot: `_Ready` path / host `OpenWorldTour`; calendar/squad tests read world names not Beskid; Career Hub files still gone.

Feel probe seed `91234` and `compare` stay valid with default Alpecin.

---

## 4. Non-goals

- Aging, season rollover, 28-man roster, CdARoad/CdATT
- Dynamic sponsor market, scouting, AI managers
- Per-stage Tour leadership plan (D-032 stays deferred)
- Rebuilding Career Hub; Watch as default play path
- Closing §49

---

## 5. Docs to update when landing

`HANDOFF.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`, `CODEBASE_MAP.md`, `DOCS.md`, `playtest/CZYTAJ_MNIE.txt`, `HOW_RACE_DAY_WORKS.md` (employer filter names from world).
