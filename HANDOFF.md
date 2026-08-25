# Peloton Manager — HANDOFF

**Status:** ACTIVE WORKING FILE  
**Purpose:** aktualny stan projektu dla kolejnej sesji AI lub człowieka. Nie jest design docem.

## Read first
1. `VISION.md`
2. ten plik
3. `DOCS.md`
4. dokumenty z `Relevant docs`

## Current milestone
`Pre-production architecture & UX design`

### Goal
Doprowadzić dokumentację do poziomu, przy którym można rozpocząć Architecture Skeleton bez zgadywania fundamentalnych kontraktów.

### Status
`IN PROGRESS`

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

## What is currently being changed
- [x] Architecture cleanup v0.6
- [x] Race Engine Design v0.2 from owner research
- [x] Race Spy / race diagnostic design v0.1
- [x] World Spy / shared Decision Trace Framework v0.1
- [x] AI Development Rules v0.1
- [x] GitHub Workflow v0.1
- [x] Codebase Map template
- [x] UI Sitemap v0.1 (DRAFT)
- [ ] Game States

## Next task
`UI_SITEMAP_v0.1 jest w DRAFT (do owner review). Następnie GAME_STATES_v0.1, potem minimalny DATA_MODEL_v0.1. RACE_ENGINE_DESIGN_v0.1 jest gotowy do późniejszego headless spike po skeletonie.`

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

## Owner feedback / project experience
Wcześniejszy Ping-Pong Manager był rozwijany przez miesiące z AI i technicznie osiągnął sporo, ale ostatecznie główny gameplay okazał się nudny, ponieważ w trakcie meczu brakowało ciekawych decyzji. W Peloton Managerze jest to jawna lekcja projektowa: nie budować kolejnych warstw na pętli, która nie przeszła ręcznego testu fun/decision density.

## Relevant docs
```text
VISION.md
DECISIONS.md
DOCS.md
Peloton_Manager_design_notes_v1.0.md
ARCHITECTURE.md
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
N/A przed utworzeniem repo. Po bootstrapie wpisać tu realne komendy.

## Things the next AI must NOT do
- Nie rozpoczynaj szerokiego gameplay coding przed dokumentami pre-code gate.
- Nie przenoś logiki gameplayowej do Godot UI.
- Nie twórz `new Random()` w systemach gameplayowych.
- Nie zmieniaj schema save/content bez migration planu.
- Nie traktuj starych dokumentów jako aktualnych bez sprawdzenia statusu.
- Nie rozszerzaj scope'u taska bez wskazania PLAYER VALUE.

## Handoff summary
Peloton Manager jest na etapie pre-production. Celem jest modularny, deterministyczny manager kolarstwa z matematyczną symulacją i emergent history. Epoki składają się z niezależnych modułów content/rules. Race gameplay jest krytycznym ryzykiem: wcześniejszy projekt managerski właściciela okazał się nudny przez brak ciekawych decyzji w trakcie meczu, więc realizm nie może usprawiedliwiać pasywnej rozgrywki. RNG musi być izolowany per domena, aby kosmetyczne zmiany nie wpływały na gameplay. Truth należy do Simulation, natomiast Knowledge do konkretnych organizacji. Human i AI używają tych samych Application Commands oraz rynku; AI nie posiada magicznego dostępu do ukrytych atrybutów. Wyniki są evidence, a nie bezpośrednim odczytem ability. Dossier jest sprawą rekrutacyjną z kontaktem z agentem, a nie paskiem postępu. UI Godota nie może posiadać logiki świata. Advance Day jest jedyną podstawową jednostką postępu UX, ale scheduler pozostaje event-driven i symuluje cały świat niezależnie od gracza. AI managerowie korzystają z tych samych Commands co człowiek; ich różnorodność wynika z traits, skills, knowledge, staffu, identity organizacji i kontekstu rulesetu. Efektywność cech managerów jest mierzona przez batchowe i 100-letnie symulacje w wielu epokach. Stable IDs nigdy nie są ponownie używane, a stare encje są kompaktowane zamiast kasowane z historii. Następny krok to UI Sitemap i Game States, potem minimalny Data Model. Race research został dostarczony i przełożony na RACE_ENGINE_DESIGN_v0.1. Wczesny headless race/core-loop spike ma nastąpić po minimalnym Data Modelu i przed zamknięciem dużej persistence/content infrastruktury.

- `2026-08-25` — Race Spy jest obowiązkowym, RNG-neutral narzędziem debugowym od pierwszego headless race spike; porównuje truth z actor knowledge i generuje reprodukowalne raporty decyzji.

- `2026-08-25` — Spy do całego świata: Race Spy jest specjalizacją wspólnego World Spy / Decision Trace Framework dla kontraktów, sponsorów, staffu, managerów, kalendarza, treningu, finansów, scoutingu, equipmentu i organization strategy.

- `2026-08-25` — AI coding workflow: docs explain contracts/WHY, not every line; coding uses small tasks, Git history, tests, World Spy and concise handoff for the owner.
