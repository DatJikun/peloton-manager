# Peloton Manager — HANDOFF

**Status:** ACTIVE WORKING FILE  
**Purpose:** aktualny stan projektu dla kolejnej sesji AI lub człowieka. Nie jest design docem.

## Read first
1. `VISION.md`
2. ten plik
3. `DOCS.md`
4. dokumenty z `Relevant docs`

## Current milestone
`Milestone 0 — Architecture Skeleton`

### Goal
Bootstrap headless C# world with deterministic time, identity, content/rules identity, GameState isolation, SQLite save/load, and repeatable skeleton seasons.

### Status
`ON MAIN` — Architecture Skeleton merged (`#9`).

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
- [x] Deterministic race stub and 10-season SimRunner
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
`Wybierz jedno osobno scoped następne zadanie (race prototype albo cienki core loop). Nie rozbudowuj StubRaceEngine do prawdziwego Race Engine bez osobnego taska i playtest gate.`

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

## Owner feedback / project experience
Wcześniejszy Ping-Pong Manager był rozwijany przez miesiące z AI i technicznie osiągnął sporo, ale ostatecznie główny gameplay okazał się nudny, ponieważ w trakcie meczu brakowało ciekawych decyzji. W Peloton Managerze jest to jawna lekcja projektowa: nie budować kolejnych warstw na pętli, która nie przeszła ręcznego testu fun/decision density.

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
```

## Things the next AI must NOT do
- Nie traktuj `StubRaceEngine` jako implementacji `RACE_ENGINE_DESIGN_v0.2.md`; przeczytaj `KNOWN_DIFFERENCE_FROM_CODE.md`.
- Nie przenoś logiki gameplayowej do Godot UI.
- Nie twórz `new Random()` w systemach gameplayowych.
- Nie zmieniaj schema save/content bez migration planu.
- Nie traktuj starych dokumentów jako aktualnych bez sprawdzenia statusu.
- Nie rozszerzaj scope'u taska bez wskazania PLAYER VALUE.
- Nie zamykaj OQ-TS-001 ani OQ-DM-001 na podstawie checksumy lub allocatora szkieletowego.

## Handoff summary
Milestone 0 now has a working headless .NET 8 skeleton. A JSON scenario creates one world with recorded content/rules identity; Application owns the nine GameStates and Commands; Advance Day advances every organization; SQLite SchemaVersion 1 preserves the minimal career checkpoint and rejects invalid/corrupt loads; RaceLive requires a pre-race autosave and blocks manual save; SimRunner completes deterministic short seasons without Godot. `StubRaceEngine` is explicitly below the accepted Race Engine contract. Full physics, Knowledge, AI managers, DecisionTrace/Spy, and gameplay systems remain unimplemented.

The paragraph below preserves the pre-bootstrap design context and owner lessons; implementation status is given above and in `CODEBASE_MAP.md`.

Peloton Manager jest na etapie pre-production. Celem jest modularny, deterministyczny manager kolarstwa z matematyczną symulacją i emergent history. Epoki składają się z niezależnych modułów content/rules. Race gameplay jest krytycznym ryzykiem: wcześniejszy projekt managerski właściciela okazał się nudny przez brak ciekawych decyzji w trakcie meczu, więc realizm nie może usprawiedliwiać pasywnej rozgrywki. RNG musi być izolowany per domena, aby kosmetyczne zmiany nie wpływały na gameplay. Truth należy do Simulation, natomiast Knowledge do konkretnych organizacji. Human i AI używają tych samych Application Commands oraz rynku; AI nie posiada magicznego dostępu do ukrytych atrybutów. Wyniki są evidence, a nie bezpośrednim odczytem ability. Dossier jest sprawą rekrutacyjną z kontaktem z agentem, a nie paskiem postępu. UI Godota nie może posiadać logiki świata. Advance Day jest jedyną podstawową jednostką postępu UX, ale scheduler pozostaje event-driven i symuluje cały świat niezależnie od gracza. AI managerowie korzystają z tych samych Commands co człowiek; ich różnorodność wynika z traits, skills, knowledge, staffu, identity organizacji i kontekstu rulesetu. Efektywność cech managerów jest mierzona przez batchowe i 100-letnie symulacje w wielu epokach. Stable IDs nigdy nie są ponownie używane, a stare encje są kompaktowane zamiast kasowane z historii. UI Sitemap, Game States, minimalny Data Model, Content Format, Rulesets, Save Format i Testing są w DRAFT i czekają na owner review. Content resolution zapisuje dokładną tożsamość packów, dependencies i overrides. Rules modules składają świat bez globalnego przełącznika epoki, a ich przejścia są effective-dated. Save jest kontraktem pliku SQLite z wersją schematu, obowiązkową migracją, recovery i dokładną content/rules identity; nie zawiera mid-race snapshotu ani scheduler runtime jako World State. Testing definiuje warstwy, golden families, kanoniczny przepis Dynamic+Advanced+Guessed i gate Milestone 0; nie zamyka fun gate'u automatami. Race research został przełożony na RACE_ENGINE_DESIGN_v0.2, ale gameplay coding i race spike nie należą do bieżącego gate'u.

- `2026-08-25` — Race Spy jest obowiązkowym, RNG-neutral narzędziem debugowym od pierwszego headless race spike; porównuje truth z actor knowledge i generuje reprodukowalne raporty decyzji.

- `2026-08-25` — Spy do całego świata: Race Spy jest specjalizacją wspólnego World Spy / Decision Trace Framework dla kontraktów, sponsorów, staffu, managerów, kalendarza, treningu, finansów, scoutingu, equipmentu i organization strategy.

- `2026-08-25` — AI coding workflow: docs explain contracts/WHY, not every line; coding uses small tasks, Git history, tests, World Spy and concise handoff for the owner.

- `2026-08-26` — Avatar prototype (EXPERIMENT): deterministyczny, warstwowy system portretów kolarzy w `experiments/avatar_prototype/`. Prawdziwy jest cały pipeline (generacja cech z `rider_id`, wagi rzadkości, reguły kompatybilności, starzenie z zachowaniem tożsamości, wykrywanie klonów z solą, kompozytor warstw, cache, wersjonowanie, walidator pakietu). Grafika jest **placeholderem** rysowanym proceduralnie w Pythonie — nie jest docelowym stylem i nie zastępuje pakietu assetów. Eksperyment nie jest wpisany do `DOCS.md`, nie jest kontraktem, nie dotyka `PelotonManager.sln` i czeka na wizualną ocenę właściciela oraz decyzję o kierunku artystycznym i miejscu renderera (Godot layers vs cache PNG).

- `2026-08-26` — Avatar prototype, decyzje właściciela: **widok front**, **portret bez kasku**, **męski peleton + stroje menadżerów**, awatar na karcie zawodnika do ~1/6 strony laptopa, kierunek artystyczny wstępnie **płaski wektor** (do porównania wypieczone cztery profile stylu: `flat`, `flat_outline`, `painted`, `soft`). Styl jest własnością pakietu assetów, nie kodu gry. Rekomendacja renderera (właściciel nie jest programistą, więc to decyzja, nie menu): kompozycja w C#, cache PNG, Godot pokazuje gotową teksturę — jeden tor kodu, testowalny headless, bez logiki świata w UI.

- `2026-08-26` — Avatar prototype dopasowany do UI z PR #18 (konstruktywizm 08e): domyślny profil stylu `poster` (kontur tuszem ~4 px, dwa płaskie tony, minimum detalu skóry), karnacje i kolory włosów przeniesione z `09-avatar-lab.html` (lab następnie odrzucony i usunięty przez właściciela; wartości żyją w `avatarlab/bake/pack.py`), klucze koszulek te same co w labie (`team` / `tour` / `giro` / `vuelta` / `world` / `national`, ze starymi nazwami jako aliasami), plansze oceny renderowane na papierze `#f3ede1` z czarną obwódką. Dodatkowo: lekki uśmiech w każdych ustach, 25 fryzur, `head_crop` dla ikon 48–96 px. Nadal EXPERIMENT, nadal placeholder art, nadal poza `PelotonManager.sln`.

- `2026-08-26` — Avatar prototype: właściciel zaakceptował styl `poster` i odrzucił `09-avatar-lab.html` (usunięty). Dodany skill `.cursor/skills/peloton-avatars/SKILL.md` — instrukcja obsługi dla innych agentów (zamknięte decyzje o guście, niezmienny kadr, tabele przepisów, profile stylu, obowiązkowa bramka bake/validate/selftest/render_demo, pułapki rysunkowe). Przy okazji naprawiony realny błąd kontraktowy: dobór assetów po sumie wag przesuwał twarze istniejących zawodników po każdym dodaniu assetu. Teraz jest wyścig wykładniczy na hashach per asset z logarytmem stałoprzecinkowym (bez libm, identycznie w C#): dodanie assetu o wadze w przenosi tylko w/(W+w) puli i wyłącznie na nowy asset, zero przetasowań; wycofanie assetu (`weight: 0`) rusza tylko tych, którzy go mieli. Docelowo dodatkowo materializacja bloków `identity`/`shape` w save.

- `2026-08-26` — Avatar prototype, korekta rysów (styl `poster` bez zmian): oczy i usta otwarte/pełne jako waga główna plus warianty neutralne i szerokie; brwi różnią się luką, długością, łukiem i zwężeniem; nos ma sylwetkę skóry (wcześniej same kreski i wszystkie wyglądały tak samo); karykaturalne głowy lekko unormalnione w miejscu; 5 dyskretnych szerokości szyi na nowym strumieniu `identity.neck`. Pakiet `0.3.0-placeholder`. Nadal EXPERIMENT, nadal poza `PelotonManager.sln`.

- `2026-08-26` — Avatar prototype, usta: właściciel odrzucił poprzednie usta jako zbyt podobne. Piec ust ma teraz osobne kształty — zamknięte (szerokie/wąskie, grube/chude, wysokie/niskie, uśmiech/proste) oraz otwarte i śmiech z pasem zębów (`open` / `teeth` / `lift`). To cecha tożsamości, nie system emocji w runtime. Pakiet `0.4.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-26` — Avatar prototype, mniej karykatur, więcej normalnych rysów: wycofane (waga 0) zbyt szerokie/cienkie usta oraz skrajne oczy/uszy/nosy; dodane sąsiednie, spokojniejsze przepisy (~1,5× żywych wariantów ust, oczu, uszu, nosów, brwi). Pakiet `0.5.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-26` — Avatar prototype, usta: wargi pełniejsze (piec nie spłaszcza już środka górnej wargi do nitki), szerokość do środków oczu (`hw` ≤ 47), plus więcej odmiennych kształtów (łuk, kwadrat, cięższa dolna, otwarte z mięsem). Pakiet `0.6.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-26` — Avatar prototype: usta w połowie drogi między nitką a zbyt grubą plamą; oczy — więcej kształtów powieki / nachylenia / tęczówki, bez dalszego szerzenia i chudzenia. Pakiet `0.7.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-27` — Avatar prototype: oczy zwężone w miejscu (`hw` 21.6–22.0, cap 22), para nie siada już na skroniach (affine `dx` ±5 zamiast ±9), szerokość nie jest już rozciągana o ±14%. Pakiet `0.8.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-27` — Avatar prototype: oczy otwarte w pionie (`th` 13.2–16.2 / `bh` 11.0–13.6), brwi 8 px wyżej żeby powieka miała miejsce, szerokość bez zmian; affine nie spłaszcza już `scale_y`. Pakiet `0.9.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-27` — Avatar prototype: oczy w połowie drogi między spłaszczeniem a szokiem (`th` 11.4–14.9 / `bh` 9.8–12.6), brwi z powrotem na linii, powieka zasłania górę tęczówki. Pakiet `0.10.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-27` — Avatar prototype: brwi nie odlatują już od oczu (łagodniejszy łuk, `drop` ≥ 0, affine `brow_height` −2..+4 zamiast −9..+5). Pakiet `0.11.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-27` — Avatar prototype: oczy odrobinę spokojniejsze (powieka niżej na tęczówce, `iris_dy` 2.0; `th`/`bh` tuż pod środkiem 0.11.0), wyraźniejsza kreska wokół oczu/nosa/ust/uszu/brwi. Kontur to osobna część `keyline`, żeby szew warg / znaczki nosa nie nadpisywały tuszu. Pakiet `0.12.0-placeholder`. Styl `poster` bez zmian.

- `2026-08-27` — Avatar prototype: usta odrobinę chudsze (skala pieca 1.12 / 1.14, nie nitka); tęczówki i źrenice zróżnicowane rozmiarem (`iris_r` 8.6–12.4, `pupil` 0.28–0.56). Pakiet `0.13.0-placeholder`. Styl `poster` bez zmian.
