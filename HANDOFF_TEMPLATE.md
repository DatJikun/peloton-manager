# Peloton Manager — HANDOFF

**Status:** ACTIVE WORKING FILE  
**Purpose:** aktualny stan projektu dla kolejnej sesji AI lub człowieka. Nie jest design docem.

## Read first
1. `VISION.md`
2. ten plik
3. `DOCS.md`
4. dokumenty z `Relevant docs`

## Current milestone
`[MILESTONE NAME]`

### Goal
`[GOAL]`

### Status
`NOT STARTED / IN PROGRESS / BLOCKED / READY FOR PLAYTEST / DONE`

## What works now
- [x] High-level game design v0.2
- [x] Technical Architecture v0.2
- [x] VISION
- [x] Documentation governance
- [x] HANDOFF workflow
- [x] Initial DOCS index

## What is currently being changed
- [ ] UI Sitemap
- [ ] Game States

## Next task
`Zaprojektować UI_SITEMAP_v0.1 i GAME_STATES_v0.1 bez rozpoczynania gameplay code.`

## Known blockers
- None.

## Known failing tests
- N/A — gameplay repo jeszcze nie istnieje.

## Recent owner decisions
- `2026-08-24` — Windows jest pierwszym targetem; preferowany stack to Godot .NET + C#.
- `2026-08-24` — New Game i procesy liniowe używają Card Flow / Back / Next.
- `2026-08-24` — RaceLive blokuje normalną nawigację i mid-race save.
- `2026-08-24` — Custom scenarios mogą mieszać niezależne moduły epok i rulesetów.
- `2026-08-24` — Kluczowy system, zwłaszcza race gameplay, musi generować interesujące decyzje; realizm nie broni nudy.
- `2026-08-24` — Właściciel projektu jest głównym sędzią feelu i ręcznych playtestów.

## Owner feedback / project experience
Wcześniejszy Ping-Pong Manager był rozwijany przez miesiące z AI i technicznie osiągnął sporo, ale ostatecznie główny gameplay okazał się nudny, ponieważ w trakcie meczu brakowało ciekawych decyzji. W Peloton Managerze jest to jawna lekcja projektowa: nie budować kolejnych warstw na pętli, która nie przeszła ręcznego testu fun/decision density.

## Relevant docs
```text
VISION.md
DOCS.md
Peloton_Manager_design_notes_v0.2.md
Peloton_Manager_Technical_Architecture_v0.2.md
DOCS_GOVERNANCE.md
```

## Commands to run first
N/A przed utworzeniem repo. Po bootstrapie wpisać tu realne komendy.

## Things the next AI must NOT do
- Nie rozpoczynaj szerokiego gameplay coding przed dokumentami pre-code gate.
- Nie przenoś logiki gameplayowej do Godot UI.
- Nie twórz `new Random()` w systemach gameplayowych.
- Nie zmieniaj schema save/content bez migration planu.
- Nie traktuj starych dokumentów jako aktualnych bez sprawdzenia statusu.
- Nie rozszerzaj scope'u taska bez wskazania PLAYER VALUE.

## Handoff summary
Peloton Manager jest na etapie pre-production. Celem jest modularny, deterministyczny manager kolarstwa z matematyczną symulacją i emergent history. Epoki składają się z niezależnych modułów content/rules. Race gameplay jest krytycznym ryzykiem: wcześniejszy projekt managerski właściciela okazał się nudny przez brak ciekawych decyzji w trakcie meczu, więc realizm nie może usprawiedliwiać pasywnej rozgrywki. RNG musi być izolowany per domena, aby kosmetyczne zmiany nie wpływały na gameplay. UI Godota nie może posiadać logiki świata. Następny krok to UI Sitemap i Game States, potem Data Model i format contentu.
