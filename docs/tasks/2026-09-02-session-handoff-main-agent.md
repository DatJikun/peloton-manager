# Przekazanie sesji agenta głównego — 2026-09-01/02

**Dla:** właściciela i następnego agenta (dowolny model). Pisane, bo kończy się usage
agenta głównego. Wszystko poniżej jest stanem faktycznym na `main` lub na gałęzi
`cursor/godot-ui-html-parity-9a2c` (wskazane wprost). Nie ma tu planów sprzedanych
jako fakty.

Czytaj po tym: `HANDOFF.md` (aktywny plik pracy), `DECISIONS.md` (D-053…D-058),
`KNOWN_DIFFERENCE_FROM_CODE.md` (co silnik robi inaczej niż projekt),
`docs/tasks/2026-09-01-main-agent-godot-ui.md` (tabela luk UI i checklista).

---

## 1. Co zostało zrobione (na `main`)

### 1.1 Higiena repo i CI (D-053)
- Zamknięte 12 przeterminowanych PR-ów (stos konfliktów). Polityka: jedna gałąź →
  zielony gate → merge do `main` w tej samej sesji (D-045), bez stosu PR-ów.
- `.github/workflows/gate.yml` — na każdy push do `main` i każdy PR uruchamia to samo,
  co `HANDOFF.md` „Commands to run first”: `dotnet format --verify-no-changes`,
  `build`, `test`, SimRunner `run` / `race` / `watch` / `compare` / `day` ×2.
- `.github/workflows/playtest-windows.yml` — tag `playtest-*` buduje zip Windows
  (Godot 4.4.1 .NET export) i publikuje go jako **GitHub Release**. Zip **nie jest już
  commitowany** do repo (`.gitignore`, usunięty z drzewa przez agenta nocnego).
- `global.json` przypina SDK 8.0.100 `latestFeature` — lokalnie i w CI te same
  analizatory (wcześniej CI wywalało się na CA1859, lokalnie było zielone).

### 1.2 Silnik wyścigu — D-054 „pozycja jest zarabiana, tempo dają kolarze”
Kontrakt: `RACE_FEEL_POSITION_AND_SELECTION_v0.1.md` (+ dwie poprawki §3.3 i §5.1
dopisane po pierwszym przebiegu implementacji). `PhysicsContractVersion = 2`.
- Start grid po `Positioning` (nie alfabetycznie / po id drużyny).
- Dryf pozycji w grupie (`PositionScoreResolver`), bonusy intencji, finał.
- **§3.3 — dryf nigdy nie holuje za darmo:** kolarz, który w danym kroku nie
  utrzymał tempa (power-limited), może dryfować tylko do tyłu. To była dziura, która
  kasowała selekcję.
- Pace-setter w strefach selekcji (bruk, strome, finał): grupa jedzie tempem
  najszybszego kolarza z przodu, nie stałą prędkością bazową.
- **§5.1 — bruk:** Crr delta 0.018 × `(1.60 − 1.00·Handling)`, osłona na sektorze
  `max(shelter, 0.85)`, **zrywy** na wjeździe/zjeździe z sektora (+2.5 m/s przez 12 s
  realizowane przez `CapabilitySolver`, więc Pmax i W′ decydują, kto się utrzyma).
- Sondy (`tests/Peloton.Application.Tests/PositionAndSelectionProbeTests.cs`):
  sprint TdF s1 i TDU s6 → pięciu sprinterów z różnych ekip (koniec „jedna drużyna w
  top 5”); Hautacam → super-gc. **Roubaix nadal wygrywa Evenepoel** — to treść, nie
  silnik: roster daje MvdP CP 430 / wytrzymałość 0.90, Evenepoel 425 / 0.94 przy 61 kg
  i CdA 0.25. Ścisła sonda Roubaix jest `Skip` z powodem; aktywna sonda sprawdza, co
  silnik uczciwie daje (selekcja, brak sprinterów w top 5, źli technicy poza top 10).
  Kalibrację gwiazd klasyków przekazano do D-057 (punkt 4b w
  `docs/tasks/2026-09-01-night-agent-roster-depth.md`). **Żadnych hacków na
  nazwiska w silniku — nigdy.**
- Goldeny zmienione raz (race winner `1002`, day winner `8`, sumy kontrolne w
  testach same-seed). Gate zielony: 257 passed / 0 failed / 1 skipped.

### 1.3 Kontrakty napisane przez agenta głównego (wdrażał je agent nocny)
- `RACE_CDA_ROAD_TT_v0.1.md` — **D-055**: dwa CdA (Road / TT), ITT/TTT bez osłony,
  schema 10. Wylądowało na `main` (`f251a47`).
- `CAREER_SEASON_ROLLOVER_AND_AGING_v0.1.md` — **D-056**: przełom roku w Advance
  Day, starzenie fizjologii, emerytury, neo-prosi, cykl kontraktów AI, schema 11.
  Wylądowało etapami (`d0ba148` … `5371459` + poprawka `41e8203`), soak 5 sezonów,
  komenda SimRunner `seasons`.
- **D-057** — 22 kolarzy na klub WT (452 w świecie), wylądowało (`0022542`,
  `47af8c9`). Czy agent nocny zrobił kalibrację klasyków (4b) — **do sprawdzenia**
  (`compare --seed 91234`, `case=roubaix-2025`; czy sonda ścisła jest odblokowana).
- **D-058** — awatary w C# (`Peloton.Avatars`): **nie wylądowało**, trzy podejścia
  agenta nocnego skończyły się błędem. Zadanie w
  `docs/tasks/2026-09-01-night-agent-avatars-csharp.md` jest nadal aktualne.

### 1.4 Listy zadań (`docs/tasks/`)
Cztery pliki z 2026-09-01: agent główny (UI), nocny nr 1 (D-055/D-056/md/release),
nr 2 (D-057), nr 3 (D-058). Każdy ma granice plików, żeby agenty się nie gryzły.

---

## 2. Co jest w toku (gałąź `cursor/godot-ui-html-parity-9a2c`)

**Cel:** powłoka kariery w Godocie wygląda jak `peloton-manager-full-ui-poc-v3.html`.
Praca Composera 2.5 w trzech przebiegach, każdy weryfikowany zrzutami z realnie
uruchomionego Godota na VM (nie z opisu). Zrzuty: `/opt/cursor/artifacts/godot-ui/`
(`html-*.png` = wzorzec, `godot-0*.png` = przed, `after-*.png` = po).

Zrobione na gałęzi (7 commitów + merge `main`):
- `LookChrome.cs` — skala typografii z HTML (body 14, meta 10 wersaliki z
  `FontVariation.SpacingGlyph`, Anton tytuły 30 / liczby 26), `Pill` (ROK 2026,
  WYŚCIG ZA N DNI), `SectionBar` z linkiem po prawej, `Crest`, `NavItem` z ikoną,
  `NavSection`, `ManagerFoot`, `Table` (płaska tabela na `GridContainer`: nagłówki
  meta, strzałka sortowania, ~34 px wiersze, linia włoskowa, wybrany wiersz czarny,
  micro-linia, chipy), `CompactSelect`, `ContractFrame`, `Stat` (paski), `DateChip`.
- `LookIcons.cs` — 11 ikon nawigacji rysowanych w `_Draw` + herb (`LookCrest`).
- `LookFormat.cs` — czyste helpery tekstowe (testowalne bez Godota).
- `project.godot` — `stretch/mode=canvas_items`, `aspect=expand`, baza 1600×900:
  gra skaluje się jednolicie jak HTML (wcześniej `disabled` → wszystko było o 17 %
  za małe na 1920).
- Ekrany: Szyna + nagłówek, **Biurko** (lista wyścigów z chipem daty, panel Wyścig,
  Inbox, Skład–ocena jako `Table`, Ranking/Finanse/Notatki), **Skład** (tabela +
  karta zawodnika z dwoma kolumnami pasków i ramką KONTRAKT), **Rynek**, **Finanse**
  (BUDŻET + KASA DNIA + uczciwy pusty stan księgi), **Kalendarz** (czarne nagłówki
  dni, dziś = niebieska ramka, chipy wyścigów), **Wynik** (tabela miejsc z
  podświetleniem naszego klubu, `WYNIK · <wyścig>` + `ZAMKNIJ ›`).
- **Naprawiony błąd ścieżki gry:** `CareerShellHost.SimulateRace` / `OpenWatch`
  używały na sztywno `RacePreparationDefaults.PrototypeScenarioId` → na WorldTour
  `RACE_SIMULATION_FAILED`. Teraz `World.TryGetTodaysRaceContentId()`; test
  regresyjny w `CareerShellHostTests`.
- Testy Godota: 36 zielone. Format/build zielone. Application tests — uruchamiane
  przez agenta głównego w gate przed merge (patrz §3).

**Czego UI nie pokazuje, bo Query tego nie ma (nie zmyślono):** profil trasy na
wpisach kalendarza (brak sparkline/wykresu), flaga monumentu (wszystkie chipy
niebieskie), pilność w `InboxItemProjection` (brak czerwonej ramki), księga
operacji finansowych, czas/strata w `RaceResultPlacement`, narodowość / wiek /
rola / forma / wartość w `ClubRosterEntry` i `MarketRiderProjection`. Ranking i
notatki sztabu to nadal `CareerLookCatalog` (rysunek, nie prawda).

---

## 3. Co będzie dalej (kolejność)

1. **Domknięcie UI (ta sesja, jeśli starczy usage):** merge `origin/main` do gałęzi
   (zrobione: `9faca03`), pełny gate z `HANDOFF.md`, merge do `main`, `git push`,
   sprawdzenie `gh run list --workflow gate`. Jedna linia w `HANDOFF.md` „Recent
   owner decisions”, wiersz Godota w `CODEBASE_MAP.md`, checklista w
   `docs/tasks/2026-09-01-main-agent-godot-ui.md`. Jeśli sesja padła przed merge:
   następny agent robi dokładnie to (gałąź jest wypchnięta).
2. **Release playtest:** tag `playtest-2026-09-02` → workflow buduje zip → Release.
   Właściciel dostaje link do Release, nie plik w repo.
3. **Query dla UI (mały slice w `Peloton.Application`, prezentacyjny):** dodać do
   projekcji to, czego brakuje w §2 — narodowość i wiek kolarza (są w
   `RiderCareer`), rola/archetyp jako etykieta, profil trasy dla wpisów kalendarza
   (jest na świecie po CreateWorld), czas/strata w wyniku. Wtedy UI pokaże
   sparkline, wykres, „POL · 27 LAT · PUNCHEUR”, czasy.
4. **Kalibracja klasyków (D-057 4b)** jeśli agent nocny jej nie zrobił, potem
   odblokowanie ścisłej sondy Roubaix.
5. **D-058 awatary w C#** — od nowa, wg istniejącego pliku zadania.
6. **Fun gate §49** `RACE_ENGINE_DESIGN_v0.2.md` — nadal `NOT VERIFIED`; to decyzja
   właściciela po zagraniu, nie agenta.

Nie robić: Career Hub, Watch jako domyślna ścieżka, `StubRaceEngine` jako wynik
oficjalny, `PlayerTeam`/God-eye, nieziarnowany RNG, save w środku wyścigu.

---

## 4. Architektura (dokładnie, stan na dziś)

### 4.1 Warstwy i projekty (`PelotonManager.sln`, .NET 8)
```
src/Peloton.Domain          encje i wartości: RiderCareer (CP, W′, Pmax, masa, CdA Road/TT,
                            Crr, Positioning, Handling, wytrzymałości, wiek, IsRetired),
                            RiderContract, Organization, ManagerCareer, CalendarEntry,
                            SeasonYear. Zero zależności.
src/Peloton.Simulation      silnik wyścigu: RaceSession (krok 1 s, PhysicsContractVersion 2),
                            CapabilitySolver (CP/W′/Pmax/wytrzymałość → moc osiągalna),
                            PositionAndGroupResolver (grupy, osłona), PositionScoreResolver
                            (dryf, intencje, bruk), BunchSprintResolver, RaceTuning (stałe
                            D-054), RouteSurface/EffectiveCrr. Deterministyczny, seed z zewnątrz.
src/Peloton.Application     GameApplication = jedyne wejście dla UI: Commands (CreateWorld,
                            AdvanceDay, PrepareRace, SimulateRace, kontrakty, oferty…) i
                            Queries/projekcje (DeskProjection, ClubRosterProjection,
                            MarketRiderProjection, ClubFinanceProjection, InboxItemProjection,
                            SeasonEventProjection, RaceResultPlacement). WorldRaceScenarioAssembler
                            (świat → scenariusz wyścigu, start grid po Positioning). Season
                            rollover / aging / neo-pros / kontrakty AI (D-056). SeasonInboxSupport.
src/Peloton.Persistence     SQLite, plik, SchemaVersion 11 (D-056). Save tylko między dniami.
src/Peloton.Client.Godot    Godot 4.4.1 .NET, tylko prezentacja (opis w 4.3).
tools/Peloton.SimRunner     CLI: run / race / watch / compare / day / seasons — gate i sondy.
tools/*.py                  generatory rostera WT 2026 (Python 3, bez paczek).
content/peloton.skeleton    fikcyjny pack (proof circuit, 12 kolarzy) — goldeny.
content/peloton.wt-2026     pack WorldTour 2026: roster.json (452), organizations, calendar,
                            race-identities (profile tras generowane przy CreateWorld),
                            historical-comparisons (sondy compare).
content/peloton.race-prototype  scenariusz prototypowy dla race/watch.
tests/*                     Domain / Simulation / Application (sondy, soak, goldeny) /
                            Persistence / Architecture (zależności warstw) / Client.Godot
                            (host bez edytora).
```
Zależności: Domain ← Simulation ← Application ← (Persistence, Client.Godot, SimRunner).
Test architektury pilnuje kierunku. Godot **nigdy** nie sięga do Simulation/Domain
poza typami zwracanymi przez Application.

### 4.2 Pętla gry (D-043)
`CreateWorld(pack, seed)` → plan sezonu (Jedziemy / lider) → `ZATWIERDŹ SEZON` →
Biurko → `ADVANCE DAY` (dzień kalendarzowy; wydatki, kontrakty, inbox; 31.12 →
rollover D-056) → dzień wyścigu: przygotowanie (skład, cel) → **`SYMULUJ`** →
tabela wyniku → Biurko. Watch (film) istnieje, **domyślnie wyłączony** (D-048).
RNG tylko z seeda świata; brak save w trakcie wyścigu.

### 4.3 Klient Godota (`src/Peloton.Client.Godot`)
```
CareerShell.tscn          jedna scena, root = CareerShellScreen (Control)
CareerShellScreen.cs      szkielet ekranu: tło (pasma), szyna boczna (Crest, NavItem×10,
                          NavSection, spacer, Ustawienia, ManagerFoot), nagłówek dnia
                          (slab daty Anton, pigułki ROK / WYŚCIG ZA, blok ZESPÓŁ, ADVANCE DAY),
                          kontener treści; routing „która sekcja” + toast „Jeszcze nie w tej wersji.”
CareerShellHost.cs        stan i komendy (bez Godota): trzyma GameApplication, autosave,
                          SimulateRace / OpenWatch (dzisiejszy wyścig ze świata), filtry
                          wyniku. Testowany w Peloton.Client.Godot.Tests.
CareerShellViews.cs       budowa każdej sekcji z projekcji: Biurko, Skład, Sztab*, Kalendarz,
                          Sponsorzy*, Finanse, Skauting*, Rynek, Historia*, Pomoc, Plan sezonu,
                          Nowa gra, Przygotowanie, Wynik, Ustawienia. (* = katalog wyglądu,
                          nie działa, toast.)
LookChrome.cs             biblioteka wyglądu = odpowiednik CSS HTML v3: kolory (Paper f3ede1,
                          Red d11f1f, Black 0c0c0d, Gray 6f6f72, White fffdf7, Hair d9d2c0,
                          Team 2050c8), fonty (Anton, PT Sans), Body/Meta/Title/Number, Pill,
                          SectionBar, Card/Frame (3 px ramka + twardy cień 6 px), Table,
                          NavItem, Crest, ManagerFoot, DateChip, Stat, Kv, Chip, ContractFrame,
                          CompactSelect, Solid/Primary/Ghost przyciski.
LookIcons.cs              ikony stroke 20 px w _Draw + herb.
LookFormat.cs             czyste formatowanie tekstu (testy).
LookCharts.cs             wykresy (profil trasy) — używane w Watch; Biurko/Kalendarz użyją,
                          gdy Query da profil.
CareerLookCatalog.cs      rysunek: OVR/POT/ranking/notatki (NIE prawda o świecie).
Watch*.cs, WatchRace.tscn film wyścigu, opcja.
project.godot             1600×900 baza, stretch canvas_items/expand, main_scene CareerShell.
```
Zasady: żadnej logiki świata w Godocie; wszystkie liczby z projekcji; Polski;
„Bruk”; daty kalendarzowe; wygląd = HTML v3 (`HTML_UI_LAB.md`).

### 4.4 Jak weryfikować UI na VM (działa, sprawdzone)
```
Godot: /tmp/godot-setup/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64
       (pobrany z GitHub releases; po restarcie VM trzeba pobrać ponownie)
DISPLAY=:1 (Xvfb działa), tmux sesja godot-shell:
  cd src/Peloton.Client.Godot && dotnet build && $GODOT --path . --fullscreen
Zrzut:  ffmpeg -f x11grab -video_size 1920x1200 -i :1.0 -frames:v 1 out.png  (/tmp/gshot.sh)
Klik:   DISPLAY=:1 xdotool mousemove X Y click 1
HTML:   google-chrome --headless=new --no-sandbox --window-size=1600,900
        --virtual-time-budget=6000 --screenshot=out.png file:///workspace/peloton-manager-full-ui-poc-v3.html
        (żeby przełączyć ekran: kopia HTML z dopisanym <script> klikającym .nav-item)
```

### 4.5 Gate (zawsze, przed merge)
```
dotnet format PelotonManager.sln --verify-no-changes
dotnet build PelotonManager.sln
dotnet test PelotonManager.sln           # Application ma soak 5 sezonów — kilka minut
dotnet run --project tools/Peloton.SimRunner -- run --scenario scenario.peloton.skeleton --years 10 --seed 91234
... race / watch / compare / day (pełna lista w HANDOFF.md)
```
CI robi to samo (`gate.yml`). Zielone → merge do `main` → push.

---

## 5. Role i lock współpracy (D-035, D-045)
Agent główny pisze md i przegląda; **Composer 2.5** koduje (`model: composer-2.5`
na każdym `Task` kodującym). Gotowe = na `main` w tej samej sesji. Bez maili, bez
`@mention`. Właściciel nie jest programistą — raport po polsku, w czacie.
