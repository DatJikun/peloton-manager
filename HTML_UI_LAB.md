# Peloton Manager — HTML look lab

**Status:** LOOK REFERENCE  
**Purpose:** visual chrome and career-shell layout the owner accepted as the starting look for most management screens. This is not the game, not a second client, and not a contract for numbers or true attributes.  
**Canonical file now:** `peloton-manager-full-ui-poc-v3.html`  
**Chrome ancestor:** `08e-constructivist-desk.html`  
**Race card (shell, not RaceLive):** `14-race.html`

Open the HTML in a browser. Do not wire it to Application Commands. Do not port it as a playable frontend. Godot `.NET` remains the client; it copies this look.

## What to copy into Godot

- Palette: paper `#f3ede1`, red `#d11f1f`, black `#0c0c0d`, gray `#6f6f72`, white `#fffdf7`, team `#2050c8`.
- Type: Anton for display / slab titles; PT Sans for body, tables, and buttons (Anton lacks Polish glyphs).
- Chrome: 3 px black frames, offset black shadow (`6px 6px 0`), team-blue panel headers, one primary CTA per career screen (`Advance Day`).
- Shell: left sidebar (crest, nav, settings, manager chip) + top bar (date slabs, year pill, employer name, primary CTA) + white panels on a 12-column grid.
- Density: tables, kv rows, tags, mail cards — not hero metrics, not staff-recommendation widgets, not workload %.
- Race-day desk: upcoming races list + route profile + inbox. Race objective was already removed from the desk panel at the owner's request.
- RaceLive is a **blocking** screen (see `UI_SITEMAP_v0.1.md` §3.3). It is not this HTML. Use the owner's RACE LIVE mockup / Godot Watch for that window.

## Sidebar labels in the POC vs sitemap domains

POC names are look labels. `UI_SITEMAP_v0.1.md` stays the domain contract.

| POC | Sitemap domain |
|---|---|
| Biurko | Career Hub / Dashboard |
| Skład | Squad / Roster |
| Sztab | Organization → staff |
| Kalendarz | Calendar |
| Sponsorzy | Organization → partners |
| Finanse | Organization → finances |
| Skauting | Recruitment & Scouting |
| Rynek transferowy | Recruitment (market / negotiation later as Card Flow) |
| Historia zespołu | World → chronicle (org-scoped in the POC) |
| Pomoc | optional help; not in-game tutorial copy on other screens |
| Ustawienia | Settings |
| Karta managera | Manager / Career Profile |

## Demo only (do not ship as truth)

Names (Beskid–Vetter, Kowalczyk, …), invented races, OVR/POT bars, fatigue %, star ratings, cash figures, and CSS geometric avatars are placeholder fiction.

- Player-facing ratings must stay **knowledge-bounded** (D-003, D-010, D-014). Do not treat POC OVR as true ability.
- The race engine has no generic stamina bar; do not read `fatigue %` as physics.
- Poster avatars live in `experiments/avatar_prototype/` (`poster` style). Do not keep the POC's CSS dummy heads in the game.
- Advance Day in the POC is a fake label flicker. In the game the Hub CTA is `Advance Day`, or **Race next** on a race-due day.
- Settings save/load buttons are demo toasts.

## Not this file

- World logic, Commands, SQLite, RNG.
- Replacing Godot with an HTML client.
- Shipping this dashboard as the playable game (rejected Career Hub UI, PR #4).
- In-game AI tutorial sentences on every screen.
- Photoreal portraits, watts, invented race commentary.

## Older HTML files

| File | Role |
|---|---|
| `08e-constructivist-desk.html` | Chrome origin (constructivist desk). |
| `10-dashboard-constructivist.html` | Earlier dashboard pass. |
| `12-dashboard-team-mid.html` | Team-color density; v3 extends this. |
| `14-race.html` | Race **card** inside the shell, not RaceLive. |
| `archive/` | Rejected look variants. Do not implement from those. |
