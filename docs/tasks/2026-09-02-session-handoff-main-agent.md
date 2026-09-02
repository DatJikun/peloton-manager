# Przekazanie sesji agenta głównego — 2026-09-02

**Dla:** właściciela i następnego agenta (dowolny model). Stan faktyczny na
`origin/main` @ `85b4257`. Nie ma tu planów sprzedanych jako fakty.

Czytaj po tym: `HANDOFF.md` (aktywny plik pracy), `DECISIONS.md` (D-053…D-058),
`KNOWN_DIFFERENCE_FROM_CODE.md` (co silnik robi inaczej niż projekt),
`docs/tasks/2026-09-01-main-agent-godot-ui.md` (tabela luk UI i checklista).

---

## 1. Co zostało zrobione (na `main`)

### 1.1 Higiena repo i CI (D-053)
- Zamknięte 12 przeterminowanych PR-ów. Polityka: jedna gałąź → zielony gate →
  merge do `main` w tej samej sesji (D-045).
- `.github/workflows/gate.yml` — na każdy push do `main` i każdy PR: to samo co
  `HANDOFF.md` „Commands to run first”.
- `.github/workflows/playtest-windows.yml` — tag `playtest-*` buduje zip Windows
  (Godot 4.4.1 .NET) i publikuje **GitHub Release**. Zip **nie** jest w repo.
- `global.json` przypina SDK 8.0.100 `latestFeature`.

### 1.2 Silnik wyścigu — D-054 „pozycja jest zarabiana, tempo dają kolarze”
Kontrakt: `RACE_FEEL_POSITION_AND_SELECTION_v0.1.md`. `PhysicsContractVersion = 2`.
- Start grid po `Positioning`. Dryf w grupie (`PositionScoreResolver`).
- **§3.3:** kolarz power-limited w danym kroku może dryfować tylko do tyłu.
- Pace-setter w strefach selekcji (bruk, strome, finał).
- **§5.1 bruk:** Crr delta 0.018 × `(1.60 − 1.00·Handling)`, osłona
  `max(shelter, 0.85)`, zrywy +2.5 m/s / 12 s przez `CapabilitySolver`.
- Sondy: sprint TdF s1 i TDU s6 → pięciu sprinterów z różnych ekip; Hautacam →
  super-gc. **Roubaix nadal wygrywa Evenepoel** po bumpie CP gwiazd (D-057 4b).
  Ścisła sonda jest `Skip` (`PositionAndSelectionProbeTests`, powód: top 5 przy
  seed `91234` to `super-gc|super-gc|super-gc|tt|gc`). Żadnych hacków na nazwiska.

### 1.3 Kontrakty z nocy 2026-09-01/02
- **D-055** CdA Road/TT, ITT/TTT, schema 10 — na `main`.
- **D-056** rollover sezonu / starzenie / emerytury / neo-pro / kontrakty AI /
  inbox / `seasons`, schema 11 — na `main`.
- **D-057** 22 kolarzy na klub WT (452 w świecie) + bump CP/wytrzymałości
  klasyków — na `main`. Sonda Roubaix nadal `Skip` (treść, nie silnik).
- **D-058** awatary w C# (`Peloton.Avatars`) — **nie wylądowało**. Zadanie:
  `docs/tasks/2026-09-01-night-agent-avatars-csharp.md`.

### 1.4 Godot UI = język HTML v3 (na `main`, `85b4257`)
Merge: `Merge Godot UI HTML v3 parity: chrome, flat tables, desk/squad/market/finance/calendar/result; WT simulate fix`.
CI `gate` na tym pushu: **success** (`33642782814`, ~49 min).

Composer 2.5, trzy przebiegi, weryfikacja zrzutami z **uruchomionego** Godota 4.4.1
.NET na VM (Xvfb), nie z opisu. Zrzuty: `/opt/cursor/artifacts/godot-ui/`
(`html-*.png` = wzorzec, `godot-0*.png` = przed, `after-*.png` = po).

- `LookChrome` / `LookIcons` / `LookFormat` — CSS-odpowiednik HTML v3.
- `project.godot`: `stretch/mode=canvas_items`, `aspect=expand`, baza 1600×900.
- Ekrany przebiegu: szyna + nagłówek, Biurko, Skład, Rynek, Finanse, Kalendarz,
  Wynik. Naprawa: `SimulateRace` / `OpenWatch` biorą
  `World.TryGetTodaysRaceContentId()`, nie `RacePreparationDefaults.PrototypeScenarioId`.
- Plan sezonu / Nowa gra / Ustawienia: **dziedziczą chrome**, bez osobnego
  przebiegu pikseli. Inbox na Biurku jest; osobnego ekranu Inbox nie ma.

Czego Query nie ma, UI **nie zmyśla:** sparkline trasy na kalendarzu, flaga
monumentu, pilność `InboxItemProjection`, księga finansowa, czas/strata w
`RaceResultPlacement`, narodowość / wiek / rola / forma / wartość na
`ClubRosterEntry` i `MarketRiderProjection`. Ranking i notatki sztabu =
`CareerLookCatalog`.

### 1.5 Release Windows
Tag `playtest-2026-09-02` i Release istnieją
(https://github.com/DatJikun/peloton-manager/releases/tag/playtest-2026-09-02).
**Ten zip jest sprzed merge UI** (workflow ~10:45 UTC; UI wylądowało ~14:33 UTC).
Właściciel, który ma zobaczyć nowy chrome, potrzebuje **nowego** tagu `playtest-*`.

---

## 2. Co jest w toku

Nic kodującego na otwartej gałęzi. Drzewo robocze: `main` @ `85b4257`.
Otwartych PR-ów: **0**.

Właściciel: ręczny playtest Windows. Fun gate §49 nadal `NOT VERIFIED`.

Agent nocny D-058 (awatary) — trzy podejścia padły; lista zadań nadal aktualna.

---

## 3. Co będzie dalej (kolejność)

1. **Nowy tag playtest** (`playtest-2026-09-02-ui` albo data dnia), jeśli
   właściciel ma zagrać chrome HTML v3. Workflow sam buduje zip i Release.
   Nie commituj zipa do repo.
2. **Query dla UI** (mały slice w `Peloton.Application`, prezentacyjny):
   narodowość i wiek (`RiderCareer`), etykieta roli/archetypu, profil trasy na
   wpisach kalendarza, czas/strata w wyniku. Wtedy UI pokaże sparkline,
   „POL · 27 LAT · PUNCHEUR”, czasy. Osobna gałąź, nie w tym samym drzewie co
   D-058.
3. **Plan sezonu / Nowa gra / Ustawienia** — tylko jeśli właściciel powie, że
   chrome tam jest za słaby; dziś dziedziczą `LookChrome`.
4. **Roubaix:** nie ruszaj silnika. Albo dalsza kalibracja treści (nie nazwiska
   w kodzie), albo zostaw `Skip`.
5. **D-058 awatary w C#** — od nowa, wg istniejącego pliku zadania.
6. **Fun gate §49** — decyzja właściciela po zagraniu, nie agenta.

Nie robić: Career Hub, Watch jako domyślna ścieżka, `StubRaceEngine` jako wynik
oficjalny, `PlayerTeam`/God-eye, nieziarnowany RNG, save w środku wyścigu,
nowy system bez PLAYER VALUE.

---

## 4. Architektura (dokładnie, stan na `85b4257`)

### 4.1 Warstwy i projekty (`PelotonManager.sln`, .NET 8)

Kierunek (pilnuje `tests/Peloton.Architecture.Tests`):

```
Peloton.Domain
├── Peloton.Rules
│   └── Peloton.Simulation
│       └── Peloton.Application
├── Peloton.Application
│   ├── Peloton.Content        packi JSON
│   └── Peloton.Persistence    SQLite
└── Peloton.Infrastructure     composition root

Peloton.Client.Godot → Application + Infrastructure (tylko composition)
Peloton.SimRunner    → Infrastructure + Application + Content + Simulation
```

Godot **nigdy** nie sięga do Simulation/Domain poza typami zwracanymi przez
Application. Domain/Rules/Simulation/Persistence nie referencją Godota.

| Projekt | Co tam jest |
|---|---|
| `src/Peloton.Domain` | Encje: `RiderCareer` (CP, W′, Pmax, masa, `CdARoadM2`/`CdATtM2`, Crr, Positioning, Handling, wytrzymałości, wiek, `IsRetired`, `RetiredFromOrganizationId`), `RiderContract`, Organization, ManagerCareer, `CalendarEntry`, `CourseProfile`, `SeasonYear`. Zero zależności. |
| `src/Peloton.Rules` | Tożsamość modułu reguł. Nie ma pełnego legal engine. |
| `src/Peloton.Simulation` | `PrototypeRaceEngine`, `RaceSession` (krok 1 s, `PhysicsContractVersion` 2), `CapabilitySolver`, `PositionAndGroupResolver`, `PositionScoreResolver`, `BunchSprintResolver`, `RaceTuning` (D-054), Course generator/compiler, ITT 60 s / TTT 4. kolarz (D-055), `SeasonRolloverExecutor` (D-056). Seed z zewnątrz. |
| `src/Peloton.Application` | `GameApplication` = jedyne wejście UI. Commands + Queries. `WorldRaceScenarioAssembler`, rollover/aging/neo-pro/AI contracts/inbox (D-056), `RiderRatings`. |
| `src/Peloton.Persistence` | SQLite, plik, **SchemaVersion 11**. Save tylko między dniami. |
| `src/Peloton.Content` | Loadery JSON: skeleton, WT 2026, race-prototype. |
| `src/Peloton.Infrastructure` | Składa porty Application → Content / Persistence / Simulation. |
| `src/Peloton.Client.Godot` | Prezentacja. Opis w §4.3. |
| `src/Peloton.Avatars` | **Nie istnieje** (D-058). |
| `tools/Peloton.SimRunner` | `run` / `race` / `watch` / `compare` / `day` / `seasons`. |
| `tools/*.py` | Roster WT 2026 (Python 3, bez paczek). |
| `content/peloton.skeleton` | Fikcyjny pack, goldeny. |
| `content/peloton.wt-2026` | 452 kolarzy, 18 klubów WT × 22 + wildcardy × 8, kalendarz, race-identities. |
| `content/peloton.race-prototype` | Scenariusz `race` / `watch`. |
| `tests/*` | Domain / Simulation / Application (sondy, soak, goldeny) / Persistence / Architecture / Client.Godot (host bez edytora). |

### 4.2 Pętla gry (D-043, stany z `GAME_STATES_v0.1.md`)

```
MainMenu (Nowa gra: wybór klubu WT)
  → CreateWorld(pack, seed, employer)
  → PreSeasonPlanningFlow (Jedziemy / lider na imprezę)
  → ZATWIERDŹ SEZON
  → Management = Biurko
       ADVANCE DAY  (dzień kalendarzowy: forma, kasa, kontrakty, inbox;
                     31.12 → rollover D-056 → znów plan sezonu)
       dzień wyścigu: Race next → RacePreparationFlow (skład, cel)
         → SYMULUJ (domyślnie) → RaceResultsFlow (tabela, filtr ekipy)
         → Debrief → Biurko
         Watch (film) istnieje, FILM: WYŁ domyślnie (D-048)
```

RNG tylko z seeda świata. Brak save w trakcie wyścigu. CTA w nagłówku zmienia
etykietę według stanu (`ZATWIERDŹ SEZON` / `ADVANCE DAY` / `JEDŹ WYŚCIG` /
`DALEJ`).

### 4.3 Klient Godota — plik po pliku (`src/Peloton.Client.Godot`)

Scena główna: `CareerShell.tscn` (root `CareerShellScreen`). Godot 4.4.1 .NET.

**Powłoka kariery**

| Plik | Rola |
|---|---|
| `CareerShell.tscn` | Jedyna scena startowa. |
| `CareerShellScreen.cs` | Szkielet: tło (pasma), szyna (Crest, NavItem ×10, NavSection, Ustawienia, ManagerFoot), nagłówek dnia (Anton data, pigułki ROK / WYŚCIG ZA, ZESPÓŁ, CTA), kontener treści. Routing `View` (Desk, Squad, Staff, Calendar, Sponsors, Finance, Scouting, Market, History, Help, Manager, RaceEvent). Stany `MainMenu` / `PreSeasonPlanningFlow` / `RaceResultsFlow` nadpisują treść (`BuildNewGame` / `BuildSeasonPlan` / `BuildRaceResults`). Toast `Jeszcze nie w tej wersji.` Okno Ustawień (FILM WŁ/WYŁ). Overlay Watch. |
| `CareerShellHost.cs` | Stan i komendy **bez Godota**: trzyma `GameApplication`, autosave, `SimulateRace` / `OpenWatch` (dzisiejszy `TryGetTodaysRaceContentId`), filtr wyniku, New Game / plan sezonu / kontrakty. Testowany w `Peloton.Client.Godot.Tests`. |
| `CareerShellViews.cs` | Budowa sekcji z projekcji: Biurko (lista wyścigów + `DateChip`, panel Wyścig, Inbox, Skład–ocena `Table`, Ranking/Finanse/Notatki), Skład (`Table` + karta z `Stat` ×2 i `ContractFrame`), Kalendarz (czarne nagłówki, dziś = niebieska ramka), Finanse (BUDŻET + KASA DNIA; bez zmyślonej księgi), Rynek (`Table` + karta), Wynik (`WYNIK · …`, `ZAMKNIJ ›`, podświetlenie klubu), Przygotowanie, Plan sezonu, Nowa gra, Ustawienia. Sztab / Sponsorzy / Skauting / Historia = katalog wyglądu. |
| `LookChrome.cs` | Biblioteka wyglądu = CSS HTML v3. Paleta: Paper `f3ede1`, Red `d11f1f`, Black `0c0c0d`, Gray `6f6f72`, White `fffdf7`, Hair `d9d2c0`, Team `2050c8`. Fonty Anton + PT Sans. `Body` 14, `Meta` 10 wersaliki (`FontVariation.SpacingGlyph`), `Title` 30, `Number` 26. Widgety: `Pill`, `SectionBar`, `Card`/`Frame` (3 px + cień 6 px), `Table` (`GridContainer`, ~34 px, linia włoskowa, wybrany wiersz czarny), `NavItem`, `Crest`, `ManagerFoot`, `DateChip`, `Stat`, `Kv`, `Chip`, `ContractFrame`, `CompactSelect`, `Solid`/`Primary`/`Ghost`, `InboxRow`, `Avatar` (geometryczny). |
| `LookIcons.cs` | Stroke 20 px w `_Draw`. Klucze: `home`, `person`, `id-card`, `calendar`, `tag`, `wallet`, `magnifier`, `arrows-swap`, `clock`, `question`, `sliders`. Plus `LookCrest` (ukos Team/Black). |
| `LookFormat.cs` | Czyste stringi (testowalne bez Godota): pigułka odliczania, chip daty, meta imprezy, inicjały managera. |
| `LookCharts.cs` | `LookSparkline`, `LookRouteProfile`, `LookRaceMap`, `LookEqualCell` (siatka kalendarza). Biurko/Kalendarz użyją profilu, gdy Query go da; Watch już używa. |
| `CareerLookCatalog.cs` | Rysunek: OVR/POT/ranking/notatki/sztab/sponsorzy/skauting. **Nie prawda o świecie.** |
| `ClientStub.cs` | Jedna stała statusu (kompilacja). |
| `project.godot` | `main_scene=CareerShell.tscn`, viewport 1600×900, `stretch=canvas_items` / `aspect=expand`, clear color Paper. |
| `fonts/` | `Anton-Regular.ttf`, `PTSans-Regular.ttf`, `PTSans-Bold.ttf` (+ OFL). |
| `export_presets.cfg` | Export Windows (playtest). |

**Watch (opcja, WYŁ)**

| Plik | Rola |
|---|---|
| `WatchRace.tscn` | Scena overlay. |
| `WatchRaceScreen.cs` | UI filmu: mapa, zegar, obserwacje, pauza, decyzje, wynik. |
| `WatchRaceHost.cs` | Host bez logiki świata: tick zegara nadzorującego, rate, pauza prezentacji. |
| `WatchRaceMapView.cs` | Rysunek trasy + ikony kolarzy. |
| `WatchRouteProfile.cs` | Polyline kursu → punkty ekranu. |
| `WatchMotionInterpolator.cs` | Lerp pozycji między klatkami 1 s (RNG-neutral). |
| `WatchFilmDuration.cs` | 30/60/120/180/300 s; default 120. |
| `WatchObservationText.cs` | Teren / osłona / przerwa jako tekst. |
| `WatchContentPath.cs` | Szukanie `content/` w edytorze i w zipie playtest. |

**Testy** (`tests/Peloton.Client.Godot.Tests/`, bez edytora Godota):
`CareerShellHostTests`, `CareerLookCatalogTests`, `LookFormatTests`,
`WatchRaceHostTests`, `WatchMotionInterpolatorTests`, `WatchFilmDurationTests`,
`WatchRouteProfileTests`, `WatchContentPathTests`.

Zasady: zero logiki świata w Godocie; liczby z projekcji; Polski; **Bruk**;
daty kalendarzowe; wygląd = `peloton-manager-full-ui-poc-v3.html`
(`HTML_UI_LAB.md`).

### 4.4 Jak weryfikować UI na VM (sprawdzone 2026-09-02)

Godot **nie** jest w obrazie Cloud. Po restarcie VM trzeba pobrać ponownie.

```
URL:  https://github.com/godotengine/godot/releases/download/4.4.1-stable/Godot_v4.4.1-stable_mono_linux_x86_64.zip
BIN:  /tmp/godot-setup/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64
Hint: tools/pack-windows-playtest.sh pokazuje oczekiwaną ścieżkę.
DISPLAY=:1   (Xvfb)
```

Uruchomienie (tmux sesja `godot-shell`, żeby nie zabić procesu):

```
cd /workspace/src/Peloton.Client.Godot
dotnet build
$GODOT --path . --fullscreen
```

Zrzut i klik:

```
ffmpeg -f x11grab -video_size 1920x1200 -i :1.0 -frames:v 1 out.png
# albo /tmp/gshot.sh out.png
DISPLAY=:1 xdotool mousemove X Y click 1
```

Wzorzec HTML (Chrome headless, 1600×900 — to baza stretch Godota):

```
google-chrome --headless=new --no-sandbox --window-size=1600,900 \
  --virtual-time-budget=6000 --screenshot=out.png \
  file:///workspace/peloton-manager-full-ui-poc-v3.html
```

Żeby przełączyć ekran HTML: kopia pliku z dopisanym `<script>` klikającym
`.nav-item`. Porównuj `after-*.png` (Godot fullscreen 1920×1200) z `html-*.png`.
`Peloton.Client.Godot.Tests` **nie** zastępują zrzutów — testują host, nie piksele.

### 4.5 Gate (zawsze, przed merge)

Z katalogu repo. To samo robi `.github/workflows/gate.yml`.

```
dotnet format PelotonManager.sln --verify-no-changes
dotnet build PelotonManager.sln
dotnet test PelotonManager.sln
  # Application ma soak 5 sezonów (Category=Soak) po suite; kilka minut;
  # Application tests cap = 2 wątki.
dotnet run --project tools/Peloton.SimRunner -- run --scenario scenario.peloton.skeleton --years 10 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- race --scenario race-scenario.peloton.prototype-v0 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- watch --scenario race-scenario.peloton.prototype-v0 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- compare --scenario scenario.peloton.wt-2026 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --follow-hub
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --through-races
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep --through-results
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.wt-2026 --seed 91234 --days 400 --employer organization.wt2026.uae
dotnet run --project tools/Peloton.SimRunner -- seasons --scenario scenario.peloton.wt-2026 --years 5 --seed 91234 --employer organization.wt2026.uae
```

Docs-only bez złamania locka: high-level check, bez pełnego soak. Zielone →
`git fetch origin main`, jedna zmiana na czubek, merge, `git push origin main`.
Sprawdź `gh run list --workflow gate`.

---

## 5. Role i lock współpracy (D-035, D-045)

Agent główny pisze md i przegląda; **Composer 2.5** koduje (`model: composer-2.5`
na każdym `Task` kodującym — nie `inherit`, nie `-fast`). Gotowe = na `main` w
tej samej sesji. Bez maili, bez `@mention`. Właściciel nie jest programistą —
raport po polsku, w czacie.
