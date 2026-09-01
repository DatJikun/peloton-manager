# Peloton Manager — Career shell dates and HTML look repair

**Title:** Career shell dates / look  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Owner playtest 2026-09-01: absolute calendar dates from 1 January 2026; desk and calendar match the HTML POC (`peloton-manager-full-ui-poc-v3.html`); no Beskid crest; no laboratory banners.  
**Authority/Owner:** Project owner (player)  
**Related decisions:** D-043, D-044, D-045, D-048, D-050, D-051, D-052  
**Look source:** `peloton-manager-full-ui-poc-v3.html` / `HTML_UI_LAB.md` (not a second client)

---

## 1. For the owner (plain language)

The year always starts on **1 January 2026**. You never see “dzień 247”. You see **5 września 2026**.

The desk shows **at most five upcoming races**, each race once (Tour is one row, not 21 stages). Click a race to open a **race page** with every stage listed.

The calendar is the **month grid** from the HTML look (Pon–Nie, arrows for months). Inbox is real mail from the world, in Polish, or “Brak spraw.” The left crest is **your club** (INEOS Grenadiers if you picked them). The grey laboratory sentences are gone.

Avatars on Skład / Sztab / Rynek are the HTML geometric heads (initials), not empty rows. Skład sorts by clicking column headers. Rynek filters by club and lists real world riders.

---

## 2. Dates (display only)

- Epoch: **day 0 = 1 January 2026**. `WorldDate.DayNumber` does not change. No schema bump.
- Helper (Application, testable): `CareerCalendarDates.ToDate(int dayNumber)` → `DateOnly`; `FormatLong` → `5 września 2026`; `FormatSlab` → `5 WRZ`; `FormatWeekdayShort` → `SO` (pl-PL).
- Top bar copies HTML: slab `5 WRZ` + weekday short; meta `Sobota · 5 września 2026`; year pill `Rok 2026`; race pill uses calendar dates, not “dzień N”.
- Contract end days on Skład use `FormatLong`, not “dzień 10000”.

---

## 3. Race grouping

Extend `CalendarEntryProjection` with `RaceContentId`, `StageIndex` (from domain `CalendarEntry`). No SQLite change.

Query `SeasonEventProjection` (group by `RaceContentId`, else by title):

- `RaceContentId`, `Name` (event title without “ — Stage N”), `StartDay`, `EndDay`, `StageCount`, `Status`

**Desk NADCHODZĄCE WYŚCIGI:** events whose `EndDay >= today`, ordered by `StartDay`, **max 5**. Never list individual stages here. Default selection = first of those five (Tour Down Under on 1 Jan, never Vuelta stage 13).

Click a row → desk detail shows the **event** (name, `20–25 stycznia 2026`, “6 etapów” or one-day). Button **otwórz wyścig ›** opens `View.RaceEvent`.

**View.RaceEvent:** title of the event; list of stages with absolute dates; click a stage for that day’s title/result. Back to calendar/desk. No new GameState.

**Delete `BuildWorldStrip`** from desk and calendar. That dump is what filled the screen.

---

## 4. Calendar screen

Restore the HTML month calendar:

- Nav ‹  **STYCZEŃ 2026**  ›
- Grid Pon…Nie; cells with day number; one chip per **event start** (not every stage)
- Today outlined
- Click chip → event selected; right panel = event summary + **otwórz wyścig ›**
- Opening month follows the current world date

Do not render a 200-row stage list.

---

## 5. Inbox

- World items only. **Remove** `CareerLookCatalog.DeskMail` from the desk.
- Polish: race-due `Dziś jest wyścig: {event name}.` + button that runs `FollowPrimary` (Race next).
- race-result: `{event} zakończony. {result}.` + **Archiwizuj** (`ArchiveInbox`).
- Empty: `Brak spraw.`
- No English “A race is due today.”

---

## 6. Crest, banners, settings stripe

- Sidebar crest = employer `Day.EmployerName` (e.g. INEOS GRENADIERS), subtitle `WORLDTOUR · 2026` (or skeleton). **Never** `CareerLookCatalog.ClubCrest` after a world exists. New Game can say PELOTON until a club is picked.
- **Delete every `LookBanner()`** and stop showing `CareerLookCatalog.Banner` / “rysunek laboratorium” / “liczby nie są światem”.
- Replace leftover `NotInWorld` toasts with `Jeszcze nie w tej wersji.` (no laboratory).
- Settings window: same diagonal team **pas** as the main shell (`LookChrome` stripe, rotate ~−8°). Paper background.

---

## 7. Skład / Sztab / Rynek look

Copy HTML density from `peloton-manager-full-ui-poc-v3.html`:

- **Skład:** clickable `SortHead` columns (name, OVR, POT, Góry, Pagórki, Płaskie, TT, Sprint, Bruk, pensja, koniec). Horizontal `ScrollContainer` so the table does not crush. Rider card uses `ProfileHead` + `LookChrome.Avatar` (HTML geometric: boxed initials; enlarge card avatar toward ~110×130, list mini ~48×58).
- **Sztab:** same two-column pattern as Skład (list with mini avatar | profile with big avatar). Keep look-catalog staff people; no lab banner.
- **Rynek:** list **world** riders not on the employer roster (`MarketRiderProjection`: name, org name, org origin, ratings, wage, contract end). `OptionButton` filter by club (all + each org). Sort headers. Card + **Negocjuj kontrakt** → existing D-044 `BeginContractNegotiation` (poach). No transfer fee. No Beskid transfer list.

Poster PNG pipeline (`experiments/avatar_prototype`) stays an experiment; do not port it in this slice.

---

## 8. Locks

- No tenth GameState, no schema 10, no checksum label change.
- Race checksum must stay `winner=1006` / `5A35E88103E2FBB40325EA8BEF15AAAC2F2E1AB70F4E6DE2BBCE584EC7EE6721`.
- Watch film off by default. No Career Hub. No `PlayerTeam`. No `StubRaceEngine`.
- Do not start CdA, aging, or sponsor market sim.

---

## 9. Tests

Application:

- Day 0 → `1 stycznia 2026`; a known Vuelta stage day formats as a September 2026 calendar date, never “dzień 247”.
- WT grouped events = 36 (not one row per stage). Upcoming from day 0 has 5 rows; first is Tour Down Under; none is a “Stage 13” title.
- Inbox on day 0 (Management) is empty (no look mail).

Godot host:

- `OpenWorldTour("organization.wt2026.ineos")` + confirm plan: `Day.EmployerName` contains `INEOS`.
- Host exposes upcoming events (≤5) and market riders including another club.

---

## 10. Out of scope

CdA split, firing, living sponsor market, scouting missions in world, Python poster bake, GitHub Releases.
