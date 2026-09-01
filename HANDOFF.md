# Peloton Manager — HANDOFF

**Status:** ACTIVE WORKING FILE  
**Purpose:** aktualny stan projektu dla kolejnej sesji AI lub człowieka. Nie jest design docem.

## Read first
1. `VISION.md`
2. ten plik
3. `DOCS.md`
4. dokumenty z `Relevant docs`

## Current milestone
`RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md` (D-049) landed. **D-050/D-051/D-052 landed.** **Next:** CdA Road/TT wait (`RACE_ENGINE_DESIGN_v0.2.md` — do not close §49).

### Goal
D-051 landed: desk / Skład / Finanse read `ClubFinanceProjection` (euro) and Skład writes D-044 contract offers. **D-052 landed:** 1 Jan 2026 dates, grouped upcoming races, month calendar, world inbox, employer crest, world market. **Next:** CdA Road/TT. Do not close §49. Do not rebuild Career Hub. Watch film stays optional and off by default.

### Status
Owner (player) directed this on 2026-09-01. **D-049, D-050, D-051, and D-052 landed.** Classified Flat is a bunch sprint (sit-in, then last 250 m at peak power). Official WT starts are event-shaped (TDU 140, monuments 175, Grand Tours 176, other WT 154). After a stage the shell/CLI can show GC / points / KOM / youth / team. SimRunner `compare` puts prototype results next to 2025 analogues (not a script). Skeleton soak still uses the short proof circuit. SQLite SchemaVersion **9**.

Feel probe seed `91234`: Philipsen 1 vs Pogačar 135 on the flattest stored Flat; Pogačar 8 vs Philipsen 133 on the biggest mountain. TDU starts 140. §49 stays open.

Godot career shell (`CareerShell.tscn`) is the main scene: POC v3 chrome. Hub date, Advance Day / Race next, inbox, save/load, skeleton people, desk finance (euro), squad wages, and contract offers stay Application Queries. **D-052** replaces day-number UI, the calendar dump, Beskid crest, laboratory banners, and look-catalog market with world events/dates/riders. Staff / sponsors / scouting stay look catalog until a later slice; leftover toasts say `Jeszcze nie w tej wersji.` Watch Race is an optional overlay, **off by default** (D-043 / D-048). HTML look lab stays the drawing, not a second client. §49 remains `NOT VERIFIED`. `D-032` remains deferred. Prototype still stores **one** CdA per rider (Road vs TT is later).

## Gdzie jest gra (dla właściciela)
Nie ma jeszcze pełnej gry managerskiej.

Działa:
- w Godocie **Nowa gra**: wybór klubu WT, plan sezonu (imprezy + lider), potem biurko z prawdziwym składem i kalendarzem;
- wyścig: **symulacja i wynik** (D-043); wynik można filtrować po każdej ekipie;
- statystyki 1–99 (góry, pagórki, płaskie, TT, sprint, **bruk**, OVR, POT) wyliczane z fizjologii — Pogačar jest góralem, Philipsen sprinterem; w polskim UI to **Bruk**, nie „kocie łby”;
- trasy WT: gęsty profil (~25 m), zapisane w świecie; TDU etap 1 to ~140 km, nie tor 5,4 km;
- kalendarz WT: **jeden dzień na etap** (Tour = 21 etapów), nie jeden wpis na całe Grand Tour;
- w CLI pętla dnia i ten sam człowiek na starcie co w klubie;
- wynik zapisuje się na karierze kolarza (`RiderCareerResult`);
- Advance Day zmienia formę / świeżość / zmęczenie; wyścig używa readiness na CP/Pmax (faza 2);
- przedsezonowy wybór startów i strategia przed Confirm (faza 3);
- kontrakty, wypłaty, wygaśnięcie, **cienkie negocjacje** oferty pensji/daty (D-044);
- paczka WorldTour 2026: 18 ekip WT + wildcards, 8 kolarzy na klub w świecie (200 na CreateWorld), 36 imprez (etapy osobno); **start zależy od wyścigu** (TDU 140, monument 175, Grand Tour 176, inny WT 154);
- na sklasyfikowanym płaskim sprinter może wygrać finisz z peletonu (Philipsen przed Pogačarem w probe `91234`); na górze góral zostaje góralem;
- po etapie widać koszulki: GC / punkty / góry / młodzież / drużynowa (tabela, nie polityka DS w trakcie etapu);
- cienka ekonomia: kasa, sponsor vs płace, notatka o debecie;
- Godot: powłoka kariery (wygląd z HTML); daty od 1 stycznia 2026; herb to wybrany klub; biurko pokazuje max 5 całych wyścigów; kalendarz to siatka miesiąca; inbox ze świata; rynek to kolarze ze świata z filtrem klubu; sztab/sponsorzy/skauting jeszcze nie w świecie;
- paczka Windows do ręcznego playtestu: `playtest/PelotonManager-playtest-windows.zip` (`playtest/CZYTAJ_MNIE.txt`).

Właśnie budujemy:
- CdA szosa/deska czeka.

Jeszcze nie:
- nie ma Career Hub — usunięty z repozytorium (D-048); biurko to powłoka `CareerShell.tscn`;
- Watch Race **jest w grze**, ale **domyślnie wyłączony** — FILM: WŁ włącza oglądanie; nie mergujemy starych PR-ów radia/DS;
- nie ma scoutingu, dynamicznego rynku sponsorów ani AI managerów w świecie (ekrany Godota pokazują tylko katalog wyglądu);
- §49 nie jest zaliczone — to ręczny playtest właściciela.

Tryby All / Guessed / None (widać / częściowo / ukryte OVR i POT) zostają. Nie dokładamy czwartej mgły.

„Eventy dnia” = po Advance Day świat coś robi (forma, regeneracja, terminy, kasa), nie tylko +1 na dacie.

Pieniądze: kasa klubu, płace kolarzy, sponsor tytułowy płaci dzienną opłatę — bez ukrytego podatku.

Baza 2026: 18 ekip męskiego WorldTour plus zaproszone ProTeamy / Australia w `scenario.peloton.wt-2026`. Fizjologia i płace są oszacowanymi pasmami na osobę (kapitan / sprinter / pomocnik / neo), nie jedną liczbą na klub. Evenepoel 2026 jedzie w Red Bull. 3 lata licencji WT i niższe ligi są w modelu. Oficjalny start WT ma kształt UCI (7 na ekipę, 8 na Grand Tour, wildcards), nie cap 12 i nie „wszystkie czwórki = 72”. JSON katalogu pozwala na 512 kolarzy; to nie jest twardy sufit fizyki. Symulacja idzie sekundą po sekundzie po każdym kolarzu — na CPU kończy się w sekundy ścienne, nie w czasie rzeczywistym.

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
- [x] Godot career shell: POC v3 chrome; Advance Day / Race next / inbox / calendar / default simulate → result table; look catalog for empty domains
- [x] Career calendar entries (domain system of record) and inbox query (race-due + race-result); archive cannot dismiss race deadlines
- [x] Headless domain/application/persistence/architecture tests

## What is currently being changed
- [x] Architecture cleanup v0.6
- [x] Race Engine Design v0.2 from owner research
- [x] Race Spy / race diagnostic design v0.1
- [x] World Spy / shared Decision Trace Framework v0.1
- [x] AI Development Rules v0.1
- [x] GitHub Workflow v0.1
- [x] Codebase Map (ACTIVE navigation, not a blank template)
- [x] UI Sitemap v0.1 (DRAFT)
- [x] Game States v0.1 (DRAFT)
- [x] Minimal Data Model v0.1 (DRAFT)
- [x] Content Format v0.1 (DRAFT)
- [x] Rulesets v0.1 (DRAFT)
- [x] Save Format v0.1 (DRAFT)
- [x] Testing v0.1 (DRAFT)
- [x] Manager-games + cycling-as-management research (RESEARCH SOURCE, 2026-08-31) — nie zmienia locków
- [x] WT 2026 physiology + contracts/wages research (RESEARCH SOURCE, 2026-09-01)
- [x] WT 2026 card calibration — archetype/wage bands + captain-first squad order
- [x] WT 2026 full pack starts (72) + Evenepoel at Red Bull + identity-constrained route fill
- [x] Docs snapshot 2026-08-31: Godot Watch is real (not a stub); Composer 2.5 coding lock (D-035)
- [x] Career WorldTour slice phase 1 — world–race bind (`RiderCareer`, SchemaVersion 2, career results)
- [x] Career WorldTour slice phase 2 — form / freshness / fatigue on Advance Day and in official races
- [x] Career WorldTour slice phase 3 — pre-season entry + pre-race strategy (`OrganizationRaceEntry`, SchemaVersion 3)
- [x] Career WorldTour slice phase 4 — rider contracts (`RiderContract`, `ClubRosterProjection`, SchemaVersion 4)
- [x] Career WorldTour slice phase 5 — WT 2026 pack (`scenario.peloton.wt-2026`, SchemaVersion 5; start-list cap 12 later lifted to full pack 72)
- [x] Career WorldTour slice phase 6 — thin economy (`CashEur`, `ClubFinanceProjection`, SchemaVersion 6)
- [x] Career WorldTour slice phase 7 — results filter by any org (D-043) + thin contract negotiation (D-044, SchemaVersion 7)
- [x] D-046 / D-047 — derived rider ratings + detailed course engine (`RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md`, SchemaVersion 8)
- [x] D-049 — bunch sprint + UCI-shaped fields + classification jerseys (`RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md`)
- [x] D-050 — New Game club pick + pre-season entries + per-event leaders (WT world in the career shell; SchemaVersion 9)
- [x] D-051 — desk / Skład / Finanse show world cash (euro) and D-044 contract offers on screen
- [x] D-052 — absolute 2026 dates + HTML career-shell look (`CAREER_SHELL_DATES_AND_LOOK_v0.1.md`)
- [ ] Avatar prototype (EXPERIMENT, placeholder art) — czeka na wizualną ocenę właściciela
- [ ] CdARoad vs CdATT (prototype still has one `CdAM2` per rider)
- [ ] Infinite career / season rollover / rider aging — later

## Next task
**CdA Road vs TT** and related race-engine gaps — do not close §49. Do not rebuild Career Hub. Watch film stays off by default. Do not start aging, year-2 routes, or sponsor market in the same tree.

## Known blockers
- None.

## Known failing tests
- None at handoff. Run the commands below again after rebasing or changing packages.

## Merge policy
**D-045.** Właściciel nie jest programistą. Gotową pracę **mergujemy do `main` w tej samej sesji**.
Nie czekamy na osobne „merguj”. Nie zostawiamy stosu otwartych PR-ów — to robi syf konfliktów.

Zielony gate (`dotnet format` / `build` / `test`, plus SimRunner z tego pliku gdy ruszamy kod)
albo docs-only bez złamania locka → `git fetch origin main`, złączyć **jedną** zmianę na aktualny
`main`, wypchnąć `main`. Ta zasada **nadpisuje** domyślne „nie merguj, dopóki właściciel nie poprosi”.

Wstrzymujemy merge tylko przy poważnej rzeczy: padające testy, złamany lock (`PlayerTeam`,
God-eye, mid-race save, cichy dryf designu), odrzucony kierunek (odbudowa Career Hub;
Watch Race jako **domyślny** sposób gry — D-043, film zostaje opcją), albo stub wyścigu udający
prawdziwy Race Engine.

Starych branchy nie zlewamy jeden na drugi. Konflikt = odtworzyć wartość na dzisiejszym `main`.

## Owner communication
Nie wysyłamy właścicielowi maili o zmianach. Status jest w czacie agenta. Bez
`@mention`, bez proszenia o GitHub review, bez komentarzy PR tylko po to, by
dostał powiadomienie.

## Recent owner decisions
- `2026-09-01` — **D-052 landed (Composer):** `CareerCalendarDates` (1 Jan 2026 epoch); grouped `SeasonEventProjection` / `UpcomingEvents` / `MarketRiders`; Polish inbox; Godot desk/calendar/rynek/squad crest/dates per `CAREER_SHELL_DATES_AND_LOOK_v0.1.md`.
- `2026-09-01` — **D-052: 1 Jan 2026 dates + HTML look repair.** Calendar dates not “dzień N”; desk max five grouped events; month grid; world inbox in Polish; employer crest; no laboratory banners; Skład sorts + geometric avatars; Rynek is world riders filterable by club. Contract: `CAREER_SHELL_DATES_AND_LOOK_v0.1.md`.
- `2026-09-01` — **Windows playtest zip refreshed** after D-050/D-051 (`playtest/PelotonManager-playtest-windows.zip`). Pack includes `peloton.wt-2026`. Staff/sponsors/scouting stay drawings; market is world riders after D-052.
- `2026-09-01` — **D-051 landed (Composer):** Godot desk/finance show `ClubFinanceProjection` (euro); Skład contract offer via D-044 commands; staff/sponsors/scouting/market stay look catalog.
- `2026-09-01` — **D-050 landed (Composer):** New Game WT club pick (`ListNewGameClubs`, `CreateWorldCommand` employer), pre-season designated leader per event (`SetSeasonRaceLeaderCommand`), `OrganizationRaceEntry.DesignatedLeaderId`, SchemaVersion 9 / checksum v9. Godot opens MainMenu club picker (not auto-skeleton). SimRunner `day --employer`. Default Alpecin CreateWorld unchanged for soak/tests.
- `2026-09-01` — **D-050: club pick, calendar entries, per-event leaders first.** Not one rigid team. Infinite career and aging later. Content stays data-only JSON packs (riders/routes/teams); physics code is the engine, not a Lua mod. CdA and look-catalog finance wait.
- `2026-09-01` — **Player-facing Polish for cobbles is bruk (D-046).** The rating on the card is **Bruk**. Do not say „kocie łby” to the owner. English code stays `Cobbles`.
- `2026-09-01` — **Bunch sprint + real fields + all jerseys + compare with real life (D-049).** Classified Flat is a bunch sprint. UCI 7 (8 on Grand Tours) with wildcards. Jerseys are after-stage tables, not D-032. History analogues are for judgment, not a script.
- `2026-09-01` — **Calibrate 2026 pack from physiology/wage research; lift the 12-starter cap; keep routes diverse.** Research file is a source, not a lock. Keep captain-first cards. Evenepoel 2026 is Red Bull. Official WT Simulate starts the full entered 4-man cards (72). Do not claim a UCI 150–200 field. Do not fake a sprinter win on Flat.
- `2026-09-01` — **Career Hub deleted; Watch film stays off by default (D-048).** Remove `CareerHub.tscn` / host / screen. Desk is the career shell. Watch Race remains optional (`FILM: WYŁ` default). Do not rebuild the PR #4 dashboard. Do not merge leftover Watch radio/DS PRs.
- `2026-09-01` — **D-046 / D-047 landed:** derived 1–99 ratings + WT archetype calibration; dense course catalog at CreateWorld; SchemaVersion 8 / checksum v8. TDU stage 1 ~140 km. Replay onto current `main` (career shell kept).
- `2026-09-01` — **Normal rider stats + real routes (D-046, D-047).** Ratings 1–99 are a view of physiology, not a second magic engine. Courses are dense polylines with a yearly generator under race-identity constraints. Not a five-chunk mock. Contract: `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md`. (D-045 on `main` is the merge-to-main lock.)
- `2026-09-01` — **Merge ready work to `main` in the same session (D-045).** No waiting for „merguj”. No pile of open PRs (that made a conflict mess). Overrides Cloud “don’t merge unless asked”. Watch film stays optional and off by default; do not land leftover Watch radio/DS PRs. Stale conflicting branches are replayed onto current `main`, not stacked.
- `2026-09-01` — **Phase 7 landed (Composer):** `RaceResultForOrganization` (any team); `Begin/Set/Confirm/CancelContractNegotiationCommand`; SchemaVersion 7 / checksum v7. Watch Race UI not expanded.
- `2026-09-01` — **Watch Race is not the default play path (D-043).** Simulate then results; filter classification by any team. Film stays in the game, off by default. Career Hub later deleted (D-048).
- `2026-09-01` — **Thin contract negotiation (D-044):** offer wage + end date to own / unattached / other-club rider. Loyalty in the accept formula. No agent board game. No tenth GameState.
- `2026-08-31` — **Phase 6 specified:** club cash, daily wage vs title-sponsor fee, no luxury tax, SchemaVersion 6. Prep title uses the calendar race name.
- `2026-08-31` — **Phase 5 landed (Composer):** `scenario.peloton.wt-2026` CreateWorld, 18 orgs, 72 riders, 36-race calendar, 12-starter prototype cap, `calendar-from-content`, SchemaVersion 5 / checksum v5. Skeleton soak unchanged.
- `2026-08-31` — **Phase 5 specified:** `scenario.peloton.wt-2026` CreateWorld, 18 orgs, thin 4-rider estimated squads, 36-race content calendar, prototype 12-starter cap, SchemaVersion 5. Skeleton soak stays.
- `2026-08-31` — **Phase 4 landed (Composer):** `RiderContract` wage/expiry, nullable `RiderCareer.OrganizationId`, `ClubRosterProjection`, contract expiry on Advance Day, SQLite SchemaVersion 4 / checksum v4. Independently rechecked: 7/7 `CareerWorldTourPhase4` tests pass.
- `2026-08-31` — **Phase 4 specified:** `RiderContract` (not manager `Employment`), wage + inclusive expiry, nullable club id, SchemaVersion 4. Loyalty stored/queried only. No transfer market this phase.
- `2026-08-31` — **Phase 3 landed (Composer):** pre-season `PreSeasonPlanningFlow` (draft entry by `RaceContentId`), `OrganizationRaceEntry`, player race-due gating, delegated auto-sim on skipped race days, `SetRacePreparationStrategyCommand` + `PREP_STRATEGY_INCOMPLETE`, SQLite SchemaVersion 3 / checksum v3. Gate green: format/build/test (132 tests) + SimRunner gates.
- `2026-08-31` — **Phase 1 landed (Composer):** world–race bind.
- `2026-08-31` — Career WorldTour slice: bind world to race; 2026 WT pack; contracts; no minigames; All/Guessed/None stay the visibility model; AI managers wait (D-036–D-042).
- `2026-08-31` — Composer 2.5 is the default coding subagent; Grok 4.6 High writes docs and reviews (D-035). Owner is a player giving feedback, not a programmer.
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

## Owner feedback / project experience
Wcześniejszy Ping-Pong Manager był rozwijany przez miesiące z AI i technicznie osiągnął sporo, ale ostatecznie główny gameplay okazał się nudny, ponieważ w trakcie meczu brakowało ciekawych decyzji. W Peloton Managerze jest to jawna lekcja projektowa: nie budować kolejnych warstw na pętli, która nie przeszła ręcznego testu fun/decision density.

2026-08-26: właściciel uznał dwie decyzje prototypu (pościg vs czekanie na rywali) za wstępnie ciekawe. Oglądanie ma być płynnym filmem z nadzorującym zegarem i ikonami według prędkości, nie highlightem ze skokami czasu. Nie zamyka to §49.

## Relevant docs
```text
VISION.md
DECISIONS.md
DOCS.md
AGENTS.md
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
RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md
KNOWN_DIFFERENCE_FROM_CODE.md
RACE_SPY_DEBUGGING_v0.1.md
WORLD_SPY_AND_DECISION_TRACING_v0.1.md
AI_DEVELOPMENT_RULES_v0.1.md
GITHUB_WORKFLOW_v0.1.md
CODEBASE_MAP.md
RACE_ENGINE_RESEARCH_2026-08-25.md
HTML_UI_LAB.md
MANAGER_GAMES_AND_CYCLING_RESEARCH_2026-08-31.md
WT_2026_PHYSIOLOGY_AND_CONTRACTS_RESEARCH_2026-09-01.md
CAREER_WORLDTOUR_SLICE_v0.1.md
HOW_RACE_DAY_WORKS.md
AGENTS.md
CAREER_CLUB_CALENDAR_LEADERS_v0.1.md
CAREER_FINANCE_CONTRACTS_ON_SCREEN_v0.1.md
CAREER_SHELL_DATES_AND_LOOK_v0.1.md
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
dotnet run --project tools/Peloton.SimRunner -- compare --scenario scenario.peloton.wt-2026 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --follow-hub
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --through-races
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep --through-results
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.wt-2026 --seed 91234 --days 1 --employer organization.wt2026.uae
```

Godot career shell (Godot 4.4 .NET, not required for headless tests). Main scene is `CareerShell.tscn`. Default race day is simulate → results; film is a setting:

```text
src/Peloton.Client.Godot/project.godot
```

`race --scenario race.prototype.gate` is an alias for the same fixture.

## Things the next AI must NOT do
- Nie traktuj race prototype jako ukończonego `RACE_ENGINE_DESIGN_v0.2.md`; przeczytaj `KNOWN_DIFFERENCE_FROM_CODE.md`.
- Nie twierdź, że §49 fun gate przeszedł; Godot Watch istnieje, ale właściciel musi oglądać ręcznie.
- Nie przywracaj `StubRaceEngine` jako źródła oficjalnych wyników.
- Nie przenoś logiki gameplayowej do Godot UI.
- Nie twórz `new Random()` w systemach gameplayowych.
- Nie zmieniaj schema save/content bez migration planu.
- Nie traktuj starych dokumentów jako aktualnych bez sprawdzenia statusu (`HANDOFF.md`, `CODEBASE_MAP.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`).
- Nie twierdź, że Godot jest pustym stubem — `CareerShell.tscn` jest oknem kariery; Watch Race zostaje opcjonalnym filmem, domyślnie wyłączonym.
- Nie ustawiaj Watch Race jako domyślnej ścieżki gry (D-043). Nie odbudowuj Career Hub (D-048). Nie merguj starych PR-ów radia/DS Watch na `main`.
- Nie zostawiaj gotowej pracy na otwartym PR (D-045). Zielony gate → merge do `main` w tej samej sesji. Nie zlewaj stosu starych branchy jeden na drugi.
- Nie mów właścicielowi „kocie łby”; polska nazwa statystyki to **bruk** (D-046). W kodzie zostaje `Cobbles`.
- D-051 jest na ekranach. Nie zaczynaj rynku sponsorów, skautingu ani starzenia w tym samym drzewie co CdA. Nie zamykaj §49.
- Nie twórz osobnej ścieżki kariery dla Continental. Dywizja i 3-letnia licencja UCI to dane na `Organization` + scenariusz (`playerStartDivisions`); awansów nie kodujemy w D-050.
- Nie przywracaj cap 12 na oficjalnym starcie WT. Feel probe seed `91234` pokazuje sprintera przed góralem na sklasyfikowanym płaskim; nie zamykaj §49.
- Nie zapisuj OVR/POT/kasy/skautingu z `CareerLookCatalog` do World, SQLite ani Commandów. To nie true ability.
- Nie odpalaj kodujących subagentów z `inherit` (to Grok); kod to Composer 2.5 (D-035).
- Nie rozszerzaj scope'u taska bez wskazania PLAYER VALUE i bez decyzji właściciela.
- Nie zamykaj OQ-TS-001 ani OQ-DM-001 na podstawie checksumy lub allocatora szkieletowego.

## Handoff summary
Milestone 0 still supplies the headless .NET 8 spine. The race prototype is the official result path: `PrototypeRaceEngine` plus `content/peloton.race-prototype`, Application commands `StartRaceCommand` / `AdvanceRaceCommand` / `RespondToRaceDecisionCommand` / `BeginRaceWatchCommand` / `AdvanceRaceWatchCommand` / `AbandonRaceLiveCommand`, and SimRunner `race`. A pending DecisionRequest stays in `RaceLive`. SimRunner `watch` and career `day --watch-from-prep` keep the D-033 supervising clock. Godot (`src/Peloton.Client.Godot`) presents the career shell (`CareerShell.tscn`, POC v3 chrome, desk queries, default simulate → result table, plus `CareerLookCatalog` for empty domains). Watch film is optional and **off by default** (D-043 / D-048). Career Hub UI is deleted. Renderer does not drive physics. Look-catalog OVR is not World; desk/squad/finance cash and contract offers are. The owner look drawing remains `peloton-manager-full-ui-poc-v3.html` (`HTML_UI_LAB.md`). After Simulate/Watch, `RaceResultProjection` and `RaceDebriefProjection` present the committed result without a second `RunBatch`. Spy OFF/ON must match checksum and finish order. `StubRaceEngine` is gone from production assemblies. SQLite `SchemaVersion` is **9**. Owner §49 remains `NOT VERIFIED`. `D-032` is deferred. D-049 bunch sprint / UCI fields / jerseys are in. D-050 New Game club pick / pre-season leaders are in. D-051 desk finance and squad offers are in. D-052 is the next coding task. Prototype CdA is still one number.

This tree joins that career loop onto `main` without dropping the HTML UI lab. The paragraph below preserves the pre-bootstrap design context and owner lessons; implementation status is given above and in `CODEBASE_MAP.md`.

Peloton Manager jest na etapie pre-production. Celem jest modularny, deterministyczny manager kolarstwa z matematyczną symulacją i emergent history. Epoki składają się z niezależnych modułów content/rules. Race gameplay jest krytycznym ryzykiem: wcześniejszy projekt managerski właściciela okazał się nudny przez brak ciekawych decyzji w trakcie meczu, więc realizm nie może usprawiedliwiać pasywnej rozgrywki. RNG musi być izolowany per domena, aby kosmetyczne zmiany nie wpływały na gameplay. Truth należy do Simulation, natomiast Knowledge do konkretnych organizacji. Human i AI używają tych samych Application Commands oraz rynku; AI nie posiada magicznego dostępu do ukrytych atrybutów. Wyniki są evidence, a nie bezpośrednim odczytem ability. Dossier jest sprawą rekrutacyjną z kontaktem z agentem, a nie paskiem postępu. UI Godota nie może posiadać logiki świata. Advance Day jest jedyną podstawową jednostką postępu UX, ale scheduler pozostaje event-driven i symuluje cały świat niezależnie od gracza. AI managerowie korzystają z tych samych Commands co człowiek; ich różnorodność wynika z traits, skills, knowledge, staffu, identity organizacji i kontekstu rulesetu. Efektywność cech managerów jest mierzona przez batchowe i 100-letnie symulacje w wielu epokach. Stable IDs nigdy nie są ponownie używane, a stare encje są kompaktowane zamiast kasowane z historii. UI Sitemap, Game States, minimalny Data Model, Content Format, Rulesets, Save Format i Testing są w DRAFT i czekają na owner review. Content resolution zapisuje dokładną tożsamość packów, dependencies i overrides. Rules modules składają świat bez globalnego przełącznika epoki, a ich przejścia są effective-dated. Save jest kontraktem pliku SQLite z wersją schematu, obowiązkową migracją, recovery i dokładną content/rules identity; nie zawiera mid-race snapshotu ani scheduler runtime jako World State. Testing definiuje warstwy, golden families, kanoniczny przepis Dynamic+Advanced+Guessed i gate Milestone 0; nie zamyka fun gate'u automatami. Race prototype v0 jest oficjalną ścieżką wyników, ale nadal poniżej pełnego kontraktu `RACE_ENGINE_DESIGN_v0.2.md`; §49 pozostaje do ręcznego playtestu właściciela.

- `2026-09-01` — Paczka WT: pasma archetypu i płacy na osobę; czwórka = kapitan / karta / dwaj pomocnicy; default lider to `.leader`, nie `.card` alfabetycznie. Nadal estimated (`D-038`). Research zaktualizowany.

- `2026-09-01` — Research źródłowy `WT_2026_PHYSIOLOGY_AND_CONTRACTS_RESEARCH_2026-09-01.md`: pasma mocy i mas sezonu 2026 oraz minima/średnie/top pensji UCI+dziennikarstwo vs cienka paczka `peloton.wt-2026`. Nie zmienia locków. D-038 (estimated, labelled) zostaje.
- `2026-09-01` — Evenepoel 2026 w Red Bull; pełny start WT = 72; generator tras trzyma min/max tożsamości (Kopenhaga płaska). Feel probe: góry rozróżniają; płaskie nadal nie dają sprintu. Cap 12 nie wraca. Karty kapitana z `main` zostają.

- `2026-08-31` — Research źródłowy `MANAGER_GAMES_AND_CYCLING_RESEARCH_2026-08-31.md`: kolarstwo poza rowerem (sponsor-vehiculum, licencje UCI, kalendarz szczytów, rynek bez okna FIFA) oraz gatunek menedżerów (FM/CM/OOTP/PCM/F1/MM). Nie zmienia locków; potwierdza VISION/DECISIONS i uzupełnia lukę obok researchu wyścigu.

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
- `2026-08-31` — Dokumenty zsynchronizowane ze stanem kodu: Godot Watch nie jest stubem; `CODEBASE_MAP.md` ACTIVE; Composer 2.5 lock (D-035). Czekamy na feedback właściciela, bez nowego systemu z urzędu.
- `2026-08-31` — Owner slice: most świat–wyścig, historia kariery, WT 2026, kontrakty, bez minigier (D-036–D-042). `CAREER_WORLDTOUR_SLICE_v0.1.md`.
- `2026-08-31` — Właściciel wskazał `peloton-manager-full-ui-poc-v3.html` jako dobry wstęp wyglądu **większości** ekranów kariery (konstruktywizm 08e, niebieska szyna, Biurko + sidebar). To look lab, nie druga gra. RaceLive zostaje osobnym oknem. Nie wdrażać OVR/POT z PoC jako true ability.
- `2026-08-31` — Właściciel kazał przenieść ten wygląd do Godota. `CareerShell.tscn` kopiuje chrome; Biurko/kalendarz/skrzynka/ludzie biorą Query ze szkieletu. Puste działy zostają puste. Watch Race nadal blokuje powłokę.
- `2026-08-31` — Właściciel kazał dodać brakujące działy. Godot pokazuje katalog wyglądu (Beskid–Vetter, OVR, kasa, skauci) jako rysunek; belka dnia i Advance Day / Race next zostają ze świata. Negocjacje i oferty nie zapisują się.
- `2026-09-01` — Właściciel: Watch Race na razie przesunięte. D-045: gotową pracę mergować do `main` od razu (bez stosu PR-ów i konfliktów). Nie zlewamy starych branchy Watch na `main`.
- `2026-09-01` — Pętla powłoki kariery (Advance Day / Race next / simulate → tabela wyniku) złącza się na `main` razem z WorldTour. Watch film zostaje opcją, nie ścieżką gry.
- `2026-09-01` — Właściciel: usuń Career Hub całkowicie. Watch Race zostaje w grze, domyślnie wyłączony (D-048).
