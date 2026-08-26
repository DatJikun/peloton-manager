# Peloton Manager — HANDOFF

**Status:** ACTIVE WORKING FILE  
**Purpose:** aktualny stan projektu dla kolejnej sesji AI lub człowieka. Nie jest design docem.

## Read first
1. `VISION.md`
2. ten plik
3. `DOCS.md`
4. dokumenty z `Relevant docs`

## Current milestone
`Race Engine Prototype v0`

### Goal
Replace seed-ranked official race results with one deterministic, phase-based, one-second-step engine that proves the nine prototype mechanics and exposes knowledge-bounded decisions through Race Spy.

### Status
On branch `feature/race-engine-prototype`. Tasks 1–8 implemented. Owner §49 fun/decision-density gate remains `NOT VERIFIED`. Architecture Skeleton remains on `main` (`#9`).

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
- [x] Headless `race` SimRunner command with Spy neutrality and optional trace export
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

## Next task
`Owner playtest §49 nadal otwarty do oglądania na ekranie. Wstępny werdykt 2026-08-26: decyzje OK, jeśli sekundy oglądania są przeskalowane (D-033), nie wall-clock. Nie zamykaj gate'u automatami. Nie implementuj D-032 w jednodniowym prototypie.`

## Known blockers
- None.

## Known failing tests
- None at handoff. Run the commands below again after rebasing or changing packages.

## Merge policy
Właściciel nie jest programistą. Gotową pracę mergujemy bez czekania na osobne „merguj”.
Wstrzymujemy merge tylko przy poważnej rzeczy: padające testy, złamany lock (`PlayerTeam`,
God-eye, mid-race save, cichy dryf designu), odrzucony przez właściciela kierunek (PR #4
Career Hub), albo stub wyścigu udający prawdziwy Race Engine.

## Recent owner decisions
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
- `2026-08-26` — Wstępny playtest §49: decyzje prototypu „póki co chyba tak”, **jeśli** sekundy decyzji są przeskalowanym czasem oglądania, nie rzeczywistym czasem etapu. `D-033`. Gate niezamknięty (brak UI).

## Owner feedback / project experience
Wcześniejszy Ping-Pong Manager był rozwijany przez miesiące z AI i technicznie osiągnął sporo, ale ostatecznie główny gameplay okazał się nudny, ponieważ w trakcie meczu brakowało ciekawych decyzji. W Peloton Managerze jest to jawna lekcja projektowa: nie budować kolejnych warstw na pętli, która nie przeszła ręcznego testu fun/decision density.

2026-08-26: właściciel uznał dwie decyzje prototypu (pościg vs czekanie na rywali) za wstępnie ciekawe, pod warunkiem że czas oglądania jest przeskalowany. Nie zamyka to §49.

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
```

`race --scenario race.prototype.gate` is an alias for the same fixture.

## Things the next AI must NOT do
- Nie traktuj race prototype jako ukończonego `RACE_ENGINE_DESIGN_v0.2.md`; przeczytaj `KNOWN_DIFFERENCE_FROM_CODE.md`.
- Nie twierdź, że §49 fun gate przeszedł; jest tylko wstępny werdykt z 2026-08-26, bez UI, pod warunkiem D-033.
- Nie przywracaj `StubRaceEngine` jako źródła oficjalnych wyników.
- Nie przenoś logiki gameplayowej do Godot UI.
- Nie twórz `new Random()` w systemach gameplayowych.
- Nie zmieniaj schema save/content bez migration planu.
- Nie traktuj starych dokumentów jako aktualnych bez sprawdzenia statusu.
- Nie rozszerzaj scope'u taska bez wskazania PLAYER VALUE.
- Nie zamykaj OQ-TS-001 ani OQ-DM-001 na podstawie checksumy lub allocatora szkieletowego.

## Handoff summary
Milestone 0 still supplies the headless .NET 8 spine. The race prototype on this branch is now the official result path: `PrototypeRaceEngine` plus `content/peloton.race-prototype`, Application commands `StartRaceCommand` / `AdvanceRaceCommand` / `RespondToRaceDecisionCommand`, and SimRunner `race`. A pending DecisionRequest stays in `RaceLive`. Spy OFF/ON must match checksum and finish order. `StubRaceEngine` is gone from production assemblies. SQLite `SchemaVersion` remains 1. Owner §49 remains `NOT VERIFIED` as a closed gate. 2026-08-26 owner note: prototype decisions are provisionally interesting if watch time is scaled (`D-033`). `D-032` (failed GC leader becoming support) is deferred.

The paragraph below preserves the pre-bootstrap design context and owner lessons; implementation status is given above and in `CODEBASE_MAP.md`.

Peloton Manager jest na etapie pre-production. Celem jest modularny, deterministyczny manager kolarstwa z matematyczną symulacją i emergent history. Epoki składają się z niezależnych modułów content/rules. Race gameplay jest krytycznym ryzykiem: wcześniejszy projekt managerski właściciela okazał się nudny przez brak ciekawych decyzji w trakcie meczu, więc realizm nie może usprawiedliwiać pasywnej rozgrywki. RNG musi być izolowany per domena, aby kosmetyczne zmiany nie wpływały na gameplay. Truth należy do Simulation, natomiast Knowledge do konkretnych organizacji. Human i AI używają tych samych Application Commands oraz rynku; AI nie posiada magicznego dostępu do ukrytych atrybutów. Wyniki są evidence, a nie bezpośrednim odczytem ability. Dossier jest sprawą rekrutacyjną z kontaktem z agentem, a nie paskiem postępu. UI Godota nie może posiadać logiki świata. Advance Day jest jedyną podstawową jednostką postępu UX, ale scheduler pozostaje event-driven i symuluje cały świat niezależnie od gracza. AI managerowie korzystają z tych samych Commands co człowiek; ich różnorodność wynika z traits, skills, knowledge, staffu, identity organizacji i kontekstu rulesetu. Efektywność cech managerów jest mierzona przez batchowe i 100-letnie symulacje w wielu epokach. Stable IDs nigdy nie są ponownie używane, a stare encje są kompaktowane zamiast kasowane z historii. UI Sitemap, Game States, minimalny Data Model, Content Format, Rulesets, Save Format i Testing są w DRAFT i czekają na owner review. Content resolution zapisuje dokładną tożsamość packów, dependencies i overrides. Rules modules składają świat bez globalnego przełącznika epoki, a ich przejścia są effective-dated. Save jest kontraktem pliku SQLite z wersją schematu, obowiązkową migracją, recovery i dokładną content/rules identity; nie zawiera mid-race snapshotu ani scheduler runtime jako World State. Testing definiuje warstwy, golden families, kanoniczny przepis Dynamic+Advanced+Guessed i gate Milestone 0; nie zamyka fun gate'u automatami. Race prototype v0 jest oficjalną ścieżką wyników, ale nadal poniżej pełnego kontraktu `RACE_ENGINE_DESIGN_v0.2.md`; §49 pozostaje do ręcznego playtestu właściciela.

- `2026-08-25` — Race Spy jest obowiązkowym, RNG-neutral narzędziem debugowym od pierwszego headless race spike; porównuje truth z actor knowledge i generuje reprodukowalne raporty decyzji.

- `2026-08-25` — Spy do całego świata: Race Spy jest specjalizacją wspólnego World Spy / Decision Trace Framework dla kontraktów, sponsorów, staffu, managerów, kalendarza, treningu, finansów, scoutingu, equipmentu i organization strategy.

- `2026-08-25` — AI coding workflow: docs explain contracts/WHY, not every line; coding uses small tasks, Git history, tests, World Spy and concise handoff for the owner.
