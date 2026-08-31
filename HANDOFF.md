# Peloton Manager — HANDOFF

**Status:** ACTIVE WORKING FILE  
**Purpose:** aktualny stan projektu dla kolejnej sesji AI lub człowieka. Nie jest design docem.

## Read first
1. `VISION.md`
2. ten plik
3. `DOCS.md`
4. dokumenty z `Relevant docs`

## Current milestone
`D-035 sequential core loop — Hub + prep (owner watch still open)`

### Goal
Thin Godot Hub: Advance Day, Race next, inbox, Watch. Prep from the four: who you put, why. Owner still has to watch a stage (step 3); more race decisions wait on that. Do not close §49.

### Status
Skeleton career has 12 named riders, 3 teams, manager off the roster. A 12-day season has Opening Classic, Hill Classic, and Season Finale; all three teams start. Godot main scene is the thin Career Hub (paper/red/black from the HTML lab): Advance Day / Race next, calendar, inbox, prep seats, Watch. Prep shows the four Beskid riders with Leader / Card / Worker and a one-line why; Confirm needs one leader and one card. Standalone `race` CLI keeps the old golden. §49 remains `NOT VERIFIED`. SchemaVersion remains 1.

## What works now
- [x] High-level game design v0.7
- [x] Technical Architecture v0.7 snapshot
- [x] VISION
- [x] Documentation governance
- [x] HANDOFF workflow
- [x] Initial DOCS index
- [x] Organization-scoped knowledge model direction
- [x] Human/AI world symmetry locked; old controller model replaced by ManagerCareer + DecisionAuthority
- [x] Recruitment dossier/agent-contact direction locked
- [x] Design Principles & Anti-Patterns v0.1
- [x] Advance Day / living world direction locked
- [x] AI Manager System v0.2
- [x] Long Save & Performance Design v0.2
- [x] Stable never-reused entity IDs locked
- [x] Manager balance-by-ruleset / 100-year lab direction locked
- [x] Human player identity belongs to ManagerCareer, not a permanent organization
- [x] Changing organizations is core-model compatible
- [x] OrganizationKnowledge vs PersonalKnowledge split locked
- [x] .NET 8 solution and authoritative project boundaries
- [x] Headless JSON world creation with Dynamic + Advanced + Guessed recipe
- [x] Canonical nine GameStates and command guards
- [x] Whole-world deterministic Advance Day
- [x] Versioned seed derivation and deterministic checksum
- [x] SQLite SchemaVersion 1 save/load with content/rules identity
- [x] Pre-race autosave, RaceLive save rejection, and crash recovery
- [x] Prototype race engine (physics, groups, chase decisions, Race Spy) and 10-season SimRunner
- [x] Thin career day loop: Hub projection, race-due Advance Day block, SimRunner `day`
- [x] Hub primary action: Advance Day on normal days, Race next on race-due days; `FollowHubPrimaryActionCommand` enters preparation
- [x] Race preparation projection and Cancel / Confirm / Watch / Simulate commands; Simulate uses the canonical engine, seed derivation, and delegated defaults
- [x] Confirmed prep plan round-trips in the application checkpoint at SQLite SchemaVersion 1
- [x] Headless Watch supervising clock with rate control, decision pause/resume, and RNG-neutral focal-rider motion projection
- [x] Race result and debrief projections after Simulate/Watch; debrief uses committed LastRace facts, not TacticalPlans
- [x] Career Watch from prep uses the D-033 supervising clock on the live RaceLive session
- [x] Godot Watch Race window: Commands + Queries, interpolated icons, decision pause, Results from LastRace
- [x] Godot Watch film duration (30 s–5 min) plus authored route-profile library (3 variants per terrain kind) and a seeded route generator for the map
- [x] Career world roster: 12 named riders, 3 teams, manager person off the roster; career races bind prototype physiology onto those people
- [x] Career calendar of three races per 12-day season (Opening Classic, Hill Classic, Season Finale); all three teams start; inbox still cannot dismiss race deadlines
- [x] Thin Godot Career Hub: Advance Day, Race next, calendar, inbox, prep seats, entry to Watch (look from the HTML lab, not a KPI dashboard)
- [x] Race preparation from the player's four: Leader / Card / Worker with a one-line why
- [x] Headless domain/application/persistence/architecture tests

## What is currently being changed
- [x] Architecture cleanup v0.6
- [x] Race Engine Design v0.2 from owner research
- [x] Race Spy / race diagnostic design v0.1
- [x] World Spy / shared Decision Trace Framework v0.1
- [x] AI Development Rules v0.1
- [x] GitHub Workflow v0.1
- [x] Codebase Map template
- [x] UI Sitemap v0.1 (DRAFT)
- [x] Game States v0.1 (DRAFT)
- [x] Minimal Data Model v0.1 (DRAFT)
- [x] Content Format v0.1 (DRAFT)
- [x] Rulesets v0.1 (DRAFT)
- [x] Save Format v0.1 (DRAFT)
- [x] Testing v0.1 (DRAFT)
- [ ] Avatar prototype (EXPERIMENT, placeholder art) — czeka na wizualną ocenę właściciela

## Next task
`D-035 step 3 — owner watches a stage.` Open `src/Peloton.Client.Godot` (main scene is the Hub). Advance Day to the Opening Classic (day 4), check seats on the four, Race next / Open Watch, film at 2 minutes. Then answer:

1. Does the story stick?
2. Does the radio decision have two real outcomes?
3. Do you want another decision in the same race?

Tests do not close this. §49 stays `NOT VERIFIED`. Taste flag while watching: the manager is **Adam Wroński** (not a rider) — keep or rename. If watch is good, sequence 7 (more race decisions) is next. Do not glue on physiology. Do not add dossier, transfers, sponsors, market AI, GC leader, avatars, or radio/DS.

## Known blockers
- None.

## Known failing tests
- None at handoff. Run the commands below again after rebasing or changing packages.

## Merge policy
Właściciel nie jest programistą. Gotową pracę mergujemy bez czekania na osobne „merguj”.
Wstrzymujemy merge tylko przy poważnej rzeczy: padające testy, złamany lock (`PlayerTeam`,
God-eye, mid-race save, cichy dryf designu), odrzucony przez właściciela kierunek (PR #4
Career Hub), albo stub wyścigu udający prawdziwy Race Engine.

## Owner communication
Nie wysyłamy właścicielowi maili o zmianach. Status jest w czacie agenta. Bez
`@mention`, bez proszenia o GitHub review, bez komentarzy PR tylko po to, by
dostał powiadomienie.

## Recent owner decisions
- `2026-08-25` — Nie wysyłać właścicielowi maili o zmianach; status tylko w czacie agenta.
- `2026-08-25` — Mergować, gdy high-level check jest OK; nie czekać na „merguj”. Nie mergować tylko przy poważnych problemach.
- `2026-08-24` — Windows jest pierwszym targetem; preferowany stack to Godot .NET + C#.
- `2026-08-24` — New Game i procesy liniowe używają Card Flow / Back / Next.
- `2026-08-24` — RaceLive blokuje normalną nawigację i mid-race save.
- `2026-08-24` — Custom scenarios mogą mieszać niezależne moduły epok i rulesetów.
- `2026-08-24` — Kluczowy system, zwłaszcza race gameplay, musi generować interesujące decyzje; realizm nie broni nudy.
- `2026-08-24` — Właściciel projektu jest głównym sędzią feelu i ręcznych playtestów.
- `2026-08-24` — Główny postęp kariery to `Advance Day`; runtime pozostaje event-driven.
- `2026-08-24` — Świat AI symuluje się niezależnie od udziału organizacji gracza.
- `2026-08-24` — AI diversity wynika z traits + skills + knowledge + organization identity + context, nie z losowych archetypów.
- `2026-08-24` — Efektywność traits managera może zmieniać się emergentnie między epokami/rulesetami.
- `2026-08-24` — 100-letni save jest obowiązkowym soak/balance testem; entity IDs nigdy nie są używane ponownie.
- `2026-08-25` — Gracz jest managerem, nie organizacją; może zmieniać pracodawcę, być zwalniany i kontynuować tę samą karierę.
- `2026-08-25` — Prywatna wiedza organizacji nie przechodzi automatycznie z managerem do nowego klubu.

- `2026-08-25` — `ManagerCareer` i `DecisionAuthority` są rozdzielone; organization nie jest Human/AI typem.
- `2026-08-25` — Information pipeline: Truth → ObservationSignal → Knowledge → Decision.
- `2026-08-25` — Forecasts są read-only, RNG-neutral i knowledge-bounded.
- `2026-08-25` — Default economy nie ma globalnego luxury tax ani automatycznej wielowiekowej nominalnej inflacji; balans sponsorów wynika z dynamicznego sponsor market.
- `2026-08-25` — RaceLive = pojedynczy etap/dzień wyścigowy.
- `2026-08-25` — Full multi-era Manager Balance Lab jest deferred do momentu udowodnienia core loop.
- `2026-08-25` — Race Engine core: brak uniwersalnego stamina bara; odpadnięcie wynika z required power vs realizable power, gapu i utraty shelter.
- `2026-08-25` — CP/W'/Pmax + basic durability są fundamentem pierwszego race prototype; deeper glycogen/thermal/hydration są deferred.
- `2026-08-25` — Drafting zmienia głównie aero; positioning wpływa na realny koszt energetyczny w całym wyścigu.
- `2026-08-25` — Race briefing/AI/DS nie mogą używać hidden race truth; działają na observations/interpretations.
- `2026-08-25` — Crosswind splits, repeated-attack selection i dropping powinny być emergentne, nie skryptowane.
- `2026-08-25` — Kanoniczna maszyna ma dziewięć stanów z D-031; scheduler pause, employment i presentation flows nie dodają stanów gry.
- `2026-08-25` — DATA_MODEL_v0.1 pozostaje cienkim kontraktem dnia 1; nie zawiera tabel SQLite ani kopii pełnego save/compaction designu.
- `2026-08-25` — Content packi są wersjonowanym, walidowanym i deterministycznie składanym JSON-em; modding MVP pozostaje data-only.
- `2026-08-25` — Custom scenario może mieszać niezależne content/rules modules, jeśli ich kontrakty i capabilities są kompatybilne.
- `2026-08-25` — Save zmierza do jednego pliku SQLite, ale SAVE_FORMAT definiuje kontrakt i migracje zamiast pełnego DDL domen.
- `2026-08-25` — Rules transitions są effective-dated i wymagają jawnego grandfathering, conversion, validation oraz repair policy.
- `2026-08-25` — TESTING_v0.1.md jest kontraktem warstw, goldenów i gate'ów; nie implementuje testów.
- `2026-08-25` — Kanoniczna ścieżka developmentu/testów to Dynamic + Advanced + Guessed. Trzy osie New Game zostają niezależnymi polami scenariusza, nie 27 osobnymi grami.
- `2026-08-26` — W wieloetapowym wyścigu słaby wyznaczony lider, który nie ma już realistycznych szans, powinien wspierać kolegę z najlepszymi pozostałymi szansami. Ocena knowledge-bounded; AI też podejmuje tę decyzję, czasem dobrze, czasem źle, zależnie od cech (np. teamwork / `formSensitivity` / `leaderLoyalty`). `D-032`, deferred poza obecnym race prototype.
- `2026-08-26` — Wstępny playtest §49: decyzje prototypu „póki co chyba tak”, jeśli oglądanie nie jest godzinami 1:1. Doprecyzowanie: zegar oglądania nadzoruje, symulacja jest płynna (mapa/ikony z prędkości), nie skok 1s=100s. `D-033`. Gate niezamknięty (brak UI).
- `2026-08-26` — Skrzynka nie otwiera wyścigu. Na dniu wyścigu główny guzik postępu (Advance Day) zmienia nazwę na Race next i wchodzi w menu przygotowania. `D-034`.
- `2026-08-31` — Kolejność core loopu: jedna rzecz na raz. Skład → oglądanie → kalendarz → Hub. Radio/DS tylko po oglądaniu, jeśli właściciel chce. Stare PR-y kariery olewać (już w `main`). Potem, nie teraz: dossier, transfery, sponsorzy, AI rynku, lider GC, awatarzy. `D-035`.

## Owner feedback / project experience
Wcześniejszy Ping-Pong Manager był rozwijany przez miesiące z AI i technicznie osiągnął sporo, ale ostatecznie główny gameplay okazał się nudny, ponieważ w trakcie meczu brakowało ciekawych decyzji. W Peloton Managerze jest to jawna lekcja projektowa: nie budować kolejnych warstw na pętli, która nie przeszła ręcznego testu fun/decision density.

2026-08-26: właściciel uznał dwie decyzje prototypu (pościg vs czekanie na rywali) za wstępnie ciekawe. Oglądanie ma być płynnym filmem z nadzorującym zegarem i ikonami według prędkości, nie highlightem ze skokami czasu. Nie zamyka to §49.

2026-08-28: po pierwszym Watch w Godot — etap za szybki, trasa kanciasta i bez szczegółów. Czas filmu zamiast ×N. Profile nie tylko gładsze: baza ~3 kształtów na rodzaj terenu (płasko, podjazd, zjazd, faliste, wiatr/węższa jezdnia) i generator, który z nich składa mapę. Fizyka prototypu zostaje na 3 grubych segmentach, żeby nie ruszać goldenu.

2026-08-31: właściciel zablokował kolejność. Najpierw posprzątać Watch (czas filmu + trasa). Potem kadra świata, dopiero wtedy oglądanie. Kalendarz 2–3 wyścigów, cienki Hub w Godocie, przygotowanie z czwórki — później, po kolei. Nie równolegle. `D-035`.

## Relevant docs
```text
VISION.md
DECISIONS.md
DOCS.md
Peloton_Manager_design_notes_v1.0.md
ARCHITECTURE.md
UI_SITEMAP_v0.1.md
GAME_STATES_v0.1.md
DATA_MODEL_v0.1.md
CONTENT_FORMAT_v0.1.md
RULESETS_v0.1.md
SAVE_FORMAT_v0.1.md
TESTING_v0.1.md
DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md
DOCS_GOVERNANCE.md
DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md
AI_MANAGER_SYSTEM_v0.2.md
LONG_SAVE_AND_PERFORMANCE_v0.2.md
RACE_ENGINE_DESIGN_v0.2.md
RACE_SPY_DEBUGGING_v0.1.md
WORLD_SPY_AND_DECISION_TRACING_v0.1.md
AI_DEVELOPMENT_RULES_v0.1.md
GITHUB_WORKFLOW_v0.1.md
CODEBASE_MAP.md
RACE_ENGINE_RESEARCH_2026-08-25.md
```

## Commands to run first
From the repository root:

```text
dotnet format --verify-no-changes
dotnet build PelotonManager.sln
dotnet test PelotonManager.sln
dotnet run --project tools/Peloton.SimRunner -- run --scenario scenario.peloton.skeleton --years 10 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- race --scenario race-scenario.peloton.prototype-v0 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- watch --scenario race-scenario.peloton.prototype-v0 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --follow-hub
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --through-races
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep --through-results
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --watch-from-prep --rate 5
```

Godot Watch Race (Godot 4.4 .NET, not required for headless tests):

```text
src/Peloton.Client.Godot/project.godot
```

`race --scenario race.prototype.gate` is an alias for the same fixture.

## Things the next AI must NOT do
- Nie traktuj race prototype jako ukończonego `RACE_ENGINE_DESIGN_v0.2.md`; przeczytaj `KNOWN_DIFFERENCE_FROM_CODE.md`.
- Nie twierdź, że §49 fun gate przeszedł; Godot Watch istnieje, ale właściciel musi oglądać ręcznie. Testy tego nie zamykają.
- Nie przywracaj `StubRaceEngine` jako źródła oficjalnych wyników.
- Nie przenoś logiki gameplayowej do Godot UI.
- Nie twórz `new Random()` w systemach gameplayowych.
- Nie zmieniaj schema save/content bez migration planu.
- Nie traktuj starych dokumentów jako aktualnych bez sprawdzenia statusu.
- Nie rozszerzaj scope'u taska bez wskazania PLAYER VALUE.
- Nie zamykaj OQ-TS-001 ani OQ-DM-001 na podstawie checksumy lub allocatora szkieletowego.
- Nie skacz po `D-035`. Jedna rzecz na raz. Hub i przygotowanie z czwórki są w. Właściciel musi oglądnąć etap (krok 3) zanim dojdą kolejne decyzje w wyścigu.
- Nie wpuszczaj PR #25 (radio/DS board) dopóki właściciel nie obejrzy etapu i nie poprosi.
- Nie wracaj do starych PR-ów kariery #13–#16 (Hub/inbox/debrief) — to już w `main`.
- Nie buduj teraz dossier/agenta, transferów, sponsorów, AI na rynku, lidera GC ani awatarów na oko.
- Nie rób równolegle „baza zawodników + negocjacje + native HTML”.
- Nie ruszaj goldenu `race` CLI przy kadrze świata.

## Handoff summary
Milestone 0 still supplies the headless .NET 8 spine. The race prototype is the official result path. Career Simulate/Watch bind the prototype onto the skeleton roster (12 named riders, 3 teams, manager off the roster). A 12-day season has three named races; all three teams start. Godot main scene is the thin Career Hub (Advance Day / Race next / inbox / calendar / prep seats / Watch) with the HTML lab look, not a KPI dashboard. Standalone `race` CLI keeps synthetic IDs and the old golden. After Simulate/Watch, result and debrief projections use committed `LastRace` without a second `RunBatch`. SQLite `SchemaVersion` remains 1. Owner §49 remains `NOT VERIFIED`. `D-032` is deferred. Core-loop order is `D-035`: steps 1–2 and 4–6 are in code; step 3 is owner watch; step 7 waits on that watch; step 8 is not now.

This tree joins that career loop onto `main` without dropping the HTML UI lab. The paragraph below preserves the pre-bootstrap design context and owner lessons; implementation status is given above and in `CODEBASE_MAP.md`.

Peloton Manager jest na etapie pre-production. Celem jest modularny, deterministyczny manager kolarstwa z matematyczną symulacją i emergent history. Epoki składają się z niezależnych modułów content/rules. Race gameplay jest krytycznym ryzykiem: wcześniejszy projekt managerski właściciela okazał się nudny przez brak ciekawych decyzji w trakcie meczu, więc realizm nie może usprawiedliwiać pasywnej rozgrywki. RNG musi być izolowany per domena, aby kosmetyczne zmiany nie wpływały na gameplay. Truth należy do Simulation, natomiast Knowledge do konkretnych organizacji. Human i AI używają tych samych Application Commands oraz rynku; AI nie posiada magicznego dostępu do ukrytych atrybutów. Wyniki są evidence, a nie bezpośrednim odczytem ability. Dossier jest sprawą rekrutacyjną z kontaktem z agentem, a nie paskiem postępu. UI Godota nie może posiadać logiki świata. Advance Day jest jedyną podstawową jednostką postępu UX, ale scheduler pozostaje event-driven i symuluje cały świat niezależnie od gracza. AI managerowie korzystają z tych samych Commands co człowiek; ich różnorodność wynika z traits, skills, knowledge, staffu, identity organizacji i kontekstu rulesetu. Efektywność cech managerów jest mierzona przez batchowe i 100-letnie symulacje w wielu epokach. Stable IDs nigdy nie są ponownie używane, a stare encje są kompaktowane zamiast kasowane z historii. UI Sitemap, Game States, minimalny Data Model, Content Format, Rulesets, Save Format i Testing są w DRAFT i czekają na owner review. Content resolution zapisuje dokładną tożsamość packów, dependencies i overrides. Rules modules składają świat bez globalnego przełącznika epoki, a ich przejścia są effective-dated. Save jest kontraktem pliku SQLite z wersją schematu, obowiązkową migracją, recovery i dokładną content/rules identity; nie zawiera mid-race snapshotu ani scheduler runtime jako World State. Testing definiuje warstwy, golden families, kanoniczny przepis Dynamic+Advanced+Guessed i gate Milestone 0; nie zamyka fun gate'u automatami. Race prototype v0 jest oficjalną ścieżką wyników, ale nadal poniżej pełnego kontraktu `RACE_ENGINE_DESIGN_v0.2.md`; §49 pozostaje do ręcznego playtestu właściciela.

- `2026-08-25` — Race Spy jest obowiązkowym, RNG-neutral narzędziem debugowym od pierwszego headless race spike; porównuje truth z actor knowledge i generuje reprodukowalne raporty decyzji.

- `2026-08-25` — Spy do całego świata: Race Spy jest specjalizacją wspólnego World Spy / Decision Trace Framework dla kontraktów, sponsorów, staffu, managerów, kalendarza, treningu, finansów, scoutingu, equipmentu i organization strategy.

- `2026-08-25` — AI coding workflow: docs explain contracts/WHY, not every line; coding uses small tasks, Git history, tests, World Spy and concise handoff for the owner.

- `2026-08-26` — Avatar prototype (EXPERIMENT): deterministyczny, warstwowy system portretów kolarzy w `experiments/avatar_prototype/`. Prawdziwy jest cały pipeline (generacja cech z `rider_id`, wagi rzadkości, reguły kompatybilności, starzenie z zachowaniem tożsamości, wykrywanie klonów z solą, kompozytor warstw, cache, wersjonowanie, walidator pakietu). Grafika jest **placeholderem** rysowanym proceduralnie w Pythonie — nie jest docelowym stylem i nie zastępuje pakietu assetów. Eksperyment nie jest wpisany do `DOCS.md`, nie jest kontraktem, nie dotyka `PelotonManager.sln` i czeka na wizualną ocenę właściciela oraz decyzję o kierunku artystycznym i miejscu renderera (Godot layers vs cache PNG).

- `2026-08-26` — Avatar prototype, decyzje właściciela: **widok front**, **portret bez kasku**, **męski peleton + stroje menadżerów**, awatar na karcie zawodnika do ~1/6 strony laptopa, kierunek artystyczny wstępnie **płaski wektor** (do porównania wypieczone cztery profile stylu: `flat`, `flat_outline`, `painted`, `soft`). Styl jest własnością pakietu assetów, nie kodu gry. Rekomendacja renderera (właściciel nie jest programistą, więc to decyzja, nie menu): kompozycja w C#, cache PNG, Godot pokazuje gotową teksturę — jeden tor kodu, testowalny headless, bez logiki świata w UI.

- `2026-08-26` — Avatar prototype dopasowany do UI z PR #18 (konstruktywizm 08e): domyślny profil stylu `poster` (kontur tuszem ~4 px, dwa płaskie tony, minimum detalu skóry), karnacje i kolory włosów przeniesione z `09-avatar-lab.html` (lab następnie odrzucony i usunięty przez właściciela; wartości żyją w `avatarlab/bake/pack.py`), klucze koszulek te same co w labie (`team` / `tour` / `giro` / `vuelta` / `world` / `national`, ze starymi nazwami jako aliasami), plansze oceny renderowane na papierze `#f3ede1` z czarną obwódką. Dodatkowo: lekki uśmiech w każdych ustach, 25 fryzur, `head_crop` dla ikon 48–96 px. Nadal EXPERIMENT, nadal placeholder art, nadal poza `PelotonManager.sln`.

- `2026-08-26` — Avatar prototype: właściciel zaakceptował styl `poster` i odrzucił `09-avatar-lab.html` (usunięty). Dodany skill `.cursor/skills/peloton-avatars/SKILL.md` — instrukcja obsługi dla innych agentów (zamknięte decyzje o guście, niezmienny kadr, tabele przepisów, profile stylu, obowiązkowa bramka bake/validate/selftest/render_demo, pułapki rysunkowe). Przy okazji naprawiony realny błąd kontraktowy: dobór assetów po sumie wag przesuwał twarze istniejących zawodników po każdym dodaniu assetu. Teraz jest wyścig wykładniczy na hashach per asset z logarytmem stałoprzecinkowym (bez libm, identycznie w C#): dodanie assetu o wadze w przenosi tylko w/(W+w) puli i wyłącznie na nowy asset, zero przetasowań; wycofanie assetu (`weight: 0`) rusza tylko tych, którzy go mieli. Docelowo dodatkowo materializacja bloków `identity`/`shape` w save.

- `2026-08-26` — Skill `peloton-avatars` przetestowany na obcym agencie (dodanie fryzury wyłącznie z instrukcji). Test wykrył realny defekt: literówka w kluczu przepisu (`excludes_tags` zamiast `excludes`) przechodziła całą bramkę i publikowała asset bez reguły blokującej. Naprawione: `check_recipe` odrzuca nieznane klucze, nieznane style i nieznane tagi z podpowiedzią; doszedł `scripts/asset_usage.py` (udział assetu w puli + licznik naruszeń blokady); `asset_pack_version` dostaje odcisk liczony ze stylu, tabeli assetów i bajtów wszystkich PNG (wcześniej `flat` i `flat_outline` miały tę samą wersję, czyli kolizję cache), a `asset_table_hash` pilnuje, żeby plansza porównania stylów nie mieszała świeżych i nieświeżych pakietów. Self-test: 45 asercji.
- `2026-08-27` — Właściciel kazał złączyć pętlę kariery z `main`. Jedno drzewo: PrototypeRaceEngine + Hub/inbox/prep/Watch razem z HTML labem (`08e` / `10` / `12` / `14-race.html`). Godot Watch i §49 nadal otwarte.
- `2026-08-28` — Pierwszy Watch Race w Godot (D-033): okno oglądania etapu, nie Career Hub. §49 nadal `NOT VERIFIED`.
- `2026-08-28` — Watch: czas filmu (domyślnie 2 min) zamiast ×N w Godot; mapa z biblioteki profili (3 warianty na rodzaj terenu) i generatora. CLI nadal ×1/×2/×5/×20. Fizyka prototypu bez zmiany goldenu.
- `2026-08-31` — `D-035`: kolejność core loopu zablokowana. To drzewo wpuszcza krok 1 (czas filmu + trasa). Radio/DS, kalendarz, Hub Godot, kadra i reszta czekają na swoje kroki. Stare PR-y kariery olewać.
