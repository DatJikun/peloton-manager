# Task card — Query slice for career UI (D-035 / session 2026-09-05)

**Owner:** Cursor (Application queries). Godot wiring is Antigravity, later.  
**Branch:** `cursor/ui-query-rider-route-results-aa63`  
**Status:** landed on branch `cursor/ui-query-rider-route-results-aa63` (Application + tests). Godot wiring is a later slice.

```text
FEATURE: Presentation queries for rider identity, course sparkline, race times
GOAL: ClubRoster / market / calendar / results expose world facts the HTML v3 chrome already draws
PLAYER VALUE: Skład shows NED · 31 LAT · KLASYKI; calendar can draw a real profile; results show time and gap

IN SCOPE:
- ClubRosterEntry + MarketRiderProjection: Nationality, Age, RoleLabel, FormPercent, IdentityLine
- CalendarEntryProjection + SeasonEventProjection: CourseSparkline from stored CourseProfile
- GameApplication.StagesForEvent
- RaceResultPlacement: FinishTimeSeconds, GapSeconds, TimeLabel, GapLabel from RiderStageTime
- Application tests

OUT OF SCOPE:
- Godot CareerShellViews / LookCharts wiring (Antigravity)
- Watch Race, Career Hub, D-058 avatars
- Inventing transfer value / monument flags / inbox urgency / finance ledger
- SchemaVersion, LastRace JSON shape, engine feel, DECISIONS.md Godot-vs-web

AUTHORITATIVE DOCS:
- docs/tasks/2026-09-02-session-handoff-main-agent.md §1.4 / §3.2
- CAREER_SHELL_DATES_AND_LOOK_v0.1.md (presentation only)
- RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md (CourseProfile is world truth)
- AI_DEVELOPMENT_RULES_v0.1.md G-001 / G-004 (UI does not invent)

AFFECTED MODULES:
- Peloton.Application projections only
- Peloton.Application.Tests

COMMANDS / QUERIES:
- Queries only. No new commands.

DATA / SAVE IMPACT:
- None. Reads Person.Nationality / BirthYear, RiderCareer.Form01, CourseProfile.Samples, RiderStageTime.

RNG DOMAIN:
- None.

DOMAIN EVENTS:
- None.

WORLD SPY TRACE:
- None.

ACCEPTANCE TESTS:
- CareerUiQueryTests: Alpecin identity lines; market Pogačar GÓRY; TDU sparkline ~140 km / 24 heights; TDU simulate times + gaps; skeleton times if recorded; checksum unchanged by queries

BALANCE PROBES:
- None (do not retune physiology)

MANUAL PLAYTEST:
- After Godot wires fields: Skład card meta, calendar cell sparkline, results time column
- Owner Windows zip still needs a new playtest-* tag (owner)

DOCS TO UPDATE:
- HANDOFF.md Next task + one Recent owner decisions line
- CODEBASE_MAP.md career day / calendar / result rows
- KNOWN_DIFFERENCE_FROM_CODE.md ClubRoster / RaceResultPlacement bullets
```

## Role labels (Polish, uppercase)

| archetype | RoleLabel |
|---|---|
| sprinter | SPRINTER |
| classics | KLASYKI |
| diesel | ALL-ROUND |
| neo | NEO |
| gc, super-gc | GÓRY |
| tt | CZASOWIEC |
| domestique | POMOCNIK |
| puncheur | PUNCHEUR |

No fake transfer **wartość**. Wage is already on the row.

## File split vs Antigravity

| Cursor (this slice) | Antigravity (later) |
|---|---|
| `src/Peloton.Application/**` | `src/Peloton.Client.Godot/**` |
| Application tests | LookCharts / CareerShellViews wiring |
| HANDOFF / CODEBASE_MAP notes for queries | DECISIONS.md Godot-vs-WebView2; branch hygiene |

Watch Race stays optional and off by default (D-043 / D-048).
