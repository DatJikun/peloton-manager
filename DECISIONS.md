# Peloton Manager — ACCEPTED DECISIONS

**Status:** ACCEPTED  
**Purpose:** stabilne owner locks, do których mogą odwoływać się kolejne dokumenty i AI.

## D-001 — Simulation creates outcomes
Historyczne wyniki nie są skryptem. Świat liczy rezultaty z aktualnego stanu i rulesetu.

## D-002 — Human/AI world symmetry
Human i AI używają tych samych legalnych domenowych mechanik, rynku, kontraktów, workloadu i zasad wiedzy.

## D-003 — Truth / information / knowledge boundary
Truth należy do Simulation. DomainEvent nie jest automatycznie wiedzą. Informacja przechodzi przez observation/publication rules do knowledge aktora.

## D-004 — Player identity is ManagerCareer
Gracz jest managerem, nie stałą drużyną. Może zmieniać pracodawcę, zostać zwolniony i kontynuować tę samą karierę.

## D-005 — Manager and input authority are separate
`ManagerCareer` jest osobą/karierą. `DecisionAuthority` mówi, czy jej decyzje dostarcza człowiek czy AI.

## D-006 — Advance Day
Jedyną podstawową jednostką postępu UX jest jeden dzień. Runtime pozostaje event-driven i cały świat działa niezależnie od udziału gracza.

## D-007 — Stable IDs never reused
`WorldEntityId` raz użyte w save jest spalane na zawsze. Identity historyczne trwa po retirement/compaction.

## D-008 — RaceLive scope and saving
`RaceLive` obejmuje jeden etap/dzień wyścigowy. Brak mid-race save; przed wejściem wykonywany jest pre-race autosave.

## D-009 — Knowledge ownership
Confidential organization data zostaje z organizacją po zmianie pracy managera. Personal knowledge/relationships są osobną warstwą.

## D-010 — AI has no God-Eye attributes
AI nie czyta `true ability`, `true potential` ani prywatnych danych innych organizacji. Korzysta z evidence i własnej wiedzy.

## D-011 — Sponsor-market economy
Długoterminowy balans ekonomiczny ma wynikać głównie z dynamicznego rynku sponsorów, popytu/podaży, regulaminu i naturalnych ograniczeń, nie z ukrytego globalnego luxury tax.

## D-012 — Stable-value default money
Domyślna gospodarka nie stosuje automatycznej nominalnej inflacji procentowej przez kolejne stulecia. Inflacja jest opcjonalnym jawnym rules module, nie uniwersalnym balanserem.

## D-013 — Determinism scope
Gwarancja: same simulation build + same resolved content/rules + same initial state + same ordered commands = same gameplay result.

## D-014 — Forecast purity
Queries i forecasts nie zmieniają World State, nie konsumują gameplay RNG i nie mogą ujawniać hidden truth poza AccessContext.

## D-015 — Causal-safe compaction
Kompakcja starej historii nie może zmienić przyszłego gameplayu.

## D-016 — Core loop before full balance lab
Headless infrastructure i podstawowe probes powstają wcześnie. Pełny multi-era manager balance lab dopiero po przejściu owner race/core-loop playability gate.

## OPEN — Numeric representation in race engine
Nie zaakceptowano jeszcze `fixed-point everywhere`. Decyzja nastąpi po race-engine research/spike i testach deterministyczności na wspieranych targetach.

## OPEN — Hotseat RaceLive resolution
Management domain ma być hotseat-ready, ale ergonomia/pauzy/checkpointy wielu ludzi podczas RaceLive są deferred.

## D-017 — No stamina-bar race causality
A rider drops because required performance exceeds currently realizable performance and a gap develops; not because one generic stamina resource reaches zero.

## D-018 — Rider archetypes emerge from the model
Primary race performance derives from physiology, physical characteristics, durability, position, drafting, equipment and current state. Terrain labels/summary ratings may exist in UI but are not the main hidden cause of results.

## D-019 — Positioning and drafting are structural
Position affects experienced power demand throughout the race. Drafting primarily modifies the aerodynamic component of required power.

## D-020 — Race decisions cannot consume hidden truth
Briefing, DS logic, AI and human RaceLive decisions operate on observations/interpretations available to the actor, not internal W' balance or other omniscient truth.

## D-021 — Dynamic gap model
Dropping, returning, elastic effects and many splits should emerge from realized speed differences, changing gaps and changing shelter rather than universal scripted `drop rider` events.

## D-022 — Race prototype before full physiology
The first race spike uses CP/W'/Pmax/basic durability + physical/group/position mechanics. Glycogen, hydration, thermal state and other deep physiology are deferred until the owner race engagement gate passes.



## D-023 — Race Spy is mandatory early infrastructure
Race Engine development must include a passive, RNG-neutral Race Spy that records decision-time knowledge, interpretations, options, chosen actions and relevant truth-level debug context. Unexpected behavior must be explainable without ad-hoc print debugging.

## D-024 — Debug truth never becomes gameplay knowledge
Race Spy may compare Simulation Truth with actor beliefs for developers, but this truth-level information is never exposed to normal human/AI decision code or ordinary RaceLive UI.


## D-025 — World Spy is a cross-system invariant
Every important automated decision domain must emit a structured decision trace compatible with the common World Spy framework. Race Spy is the first specialization, not a one-off debug tool.

## D-026 — Explain actor perspective before judging outcome
Diagnostics must preserve what the actor knew and believed when deciding. A decision that later produced a bad result is not automatically considered irrational or a bug.

## D-027 — Player-facing Why and developer Spy are separate
Developer Spy may compare hidden Simulation Truth with actor knowledge. Normal UI explanations are strictly bounded by the player's AccessContext.


## D-028 — AI coding workflow is mandatory
Implementation follows `AI_DEVELOPMENT_RULES_v0.1.md`. Documentation explains contracts/invariants/WHY rather than every line.

## D-029 — Git history is project memory
Meaningful work uses scoped branches/tasks, descriptive commits and reviewable PR summaries. Large unrelated changes must not be hidden in feature work.

## D-030 — Regression before patch stacking
When practical, bugs receive a reproducible failing test/scenario before root-cause fixes. One-off exceptions are disfavored when a general defect exists.

## D-031 — Canonical game-state list and runtime boundary
The canonical game-state machine contains exactly: `MainMenu`, `NewGameFlow`, `LoadingWorld`, `Management`, `PreSeasonPlanningFlow`, `RacePreparationFlow`, `RaceLive`, `RaceResultsFlow`, and `RaceDebriefFlow`.

Scheduler idle/processing/deterministic-pause status is runtime, not a GameState or World State. Employment, settings, open modals, season review, employment change, and other presentation flows do not add game states. They run inside the applicable canonical state unless a later owner decision changes the list.

## D-032 — Failed designated leader may become support
W wieloetapowym wyścigu, gdy wyznaczony lider nie ma już realistycznych szans na główny cel zespołu (zazwyczaj GC), zespół może przekierować go do wsparcia kolegi z najlepszymi pozostałymi szansami.

Ocena szans jest knowledge-bounded: wynika z obserwacji, klasyfikacji, formy i pewności sztabu, nie z ukrytego truth fizjologii. Human i AI używają tej samej decyzji. Jakość oceny i gotowość do porzucenia pierwotnego planu leadership zależą od cech i staffu (np. `formSensitivity`, `leaderLoyalty`, analog rider/teamwork). Dobre i złe decyzje są legalnym gameplayem, nie bugiem.

Implementacja jest deferred do wieloetapowego/virtual GC. Obecny jednodniowy race prototype tego nie buduje.

## D-033 — Supervising watch clock, smooth simulation
Oglądanie nie jest 1:1 z godzinami etapu, ale też nie jest skokiem „1s oglądania = 100s fizyki”.

Zegar oglądania (Watch Race) jest nadzorujący: gracz wybiera tempo (np. ×1 / ×2 / ×5 / ×20). Symulacja dostosowuje się do tego zegara i pozostaje płynna. Gdyby na mapie trasy stały ikony kluczowych zawodników, ich pozycja ma wynikać z aktualnej prędkości, gapu, shelteru i terenu w danej chwili — bez teleportów.

Fizyka zostaje kanoniczna (`R-001`). Prototype `dt = 1s` to krok referencyjny silnika, nie klatka filmu. Renderer może interpolować pozycje między krokami. `DecisionRequest` pauzuje zegar oglądania. Renderer nie steruje fizyką.

Headless `watch` implements this supervising clock (rates, pause on `DecisionRequest`, RNG-neutral motion). Godot Watch Race is the presentation renderer over the same clock. CLI markdown output is not the owner §49 playtest.

## D-034 — Race next is the Hub primary on race day
On a race-due day the Hub primary time-progress control relabels to **Race next** and enters `RacePreparationFlow`. Inbox remains a queue of items and does not launch the race.

Normal Hub primary action stays **Advance Day** (D-006). The `AdvanceDay` command still cannot skip a due race. Race next only opens preparation; starting the race remains a later prep-menu command.

## D-035 — Composer 2.5 is the coding subagent
When this repo is developed with a main Cloud Agent plus subagents, the split is:

- main agent = Grok 4.6 High (not fast): Markdown, design contracts, review;
- Composer 2.5: code.

Coding `Task` launches must set the subagent model to Composer 2.5. They must not inherit the main agent and must not use Composer 2.5 Fast unless the owner asked for speed. Composer is not the primary author of VISION, DECISIONS, ARCHITECTURE, HANDOFF, UI sitemap, GAME_STATES, DATA_MODEL, ADRs, or similar governance docs. Operational detail lives in `AGENTS.md` and `.cursor/rules/composer-coding-subagent.mdc`.

## D-036 — Career riders are the people who race
Official start lists, finish order, and `LastRace` IDs are world `RiderCareer` identities. A disconnected race-only fixture is not the official result path once this bind exists. Race results append to that career’s history. Same person in the club, on the start list, and in the chronicle.

## D-037 — Pre-season entry and pre-race strategy stay in the nine states
Pre-season: the player chooses which races the organization enters (`PreSeasonPlanningFlow`). Pre-race: a strategy step (roles, objective, briefing) sits inside `RacePreparationFlow` before Confirm. Neither adds a tenth GameState (D-031). Career Hub UI is deleted (D-048).

## D-038 — 2026 WorldTour content first; lower tiers are architecture
First real-cycling pack is men’s UCI WorldTour 2026 (18 teams, 2026–2028 licence cycle). Physiology, wages, and budgets may be estimated gameplay numbers and must be labelled as such. Organization records store division and licence-years-remaining so a 3-year WorldTour licence and ProTeam/Continental tiers can exist later. Living promotion/relegation and a full lower-category grid are not required for the first playable season. Commercial licensing of real names is a later problem; the engine must still run on fictional packs.

## D-039 — Rider contracts in; loyalty thin; sponsor-loop overkill
Rider contracts (club, wage, dates) are required. Loyalty is a stored trait, not a minigame. Personal rider sponsors and marketability-as-a-game are overkill for this slice. A quiet marketability number may arrive later with the sponsor economy.

## D-040 — Staff is never a minigame
Staff (DS, coach, medical, recruitment) may modify briefing quality, training, or knowledge. They do not get their own minigames.

## D-041 — AI managers wait for the owner
Do not implement AI managers in this slice. Human/AI symmetry (D-002) still applies to the bind: the same commands and world rules, even if only the human authority is wired.

## D-042 — Attribute visibility is All / Guessed / None
The knowledge spine serves the existing New Game visibility axis. It is not a fourth fog-of-war mode. All may show OVR/POT. Guessed shows ranges and confidence. None does not show rival attributes; results are evidence. Do not build a scouting/dossier game until the owner asks.

## D-043 — Race play path is Simulate + Results; Watch film is optional and off by default
Owner lock 2026-09-01, updated the same day: after prep, the **default** path is **simulate then results**. Results can be **filtered by any organization** (public classification, not God-eye live physiology). **Watch Race stays in the game** as an optional film overlay (`FILM: WYŁ/WŁ`), **off by default**. Turning it on does not change the official result. Do not make watching the default play path. Do not merge leftover radio / DS-dashboard Watch PRs as a second race product. The Godot **career shell** (`CareerShell.tscn`, POC v3 chrome) is the management presentation: Advance Day / Race next, simulate, result table, filter by team. Career Hub UI is **deleted** (D-048).

## D-044 — Thin contract negotiation, not an agent board game
The player can offer a contract (annual wage + inclusive end day) to a rider: own roster (renew), unattached (sign), or another club (thin poach). Accept/reject is a closed formula from current wage and `Loyalty01`. No agent minigame, no counter-offer auction, no transfer fee this pass. Stays inside `Management` (D-031: no tenth GameState).

## D-045 — Merge ready work to main in the same session
Owner lock 2026-09-01: the owner is not a programmer and does not want a pile of open PRs. When the gate is green, **merge into `main` in the same session**. Do not wait for a separate „merguj”. Do not leave finished work on a feature branch.

Green means: right topic; `dotnet format` / `dotnet build` / `dotnet test` (and SimRunner commands from `HANDOFF.md` when code changed) actually ran and passed, or the change is docs-only with no lock break; no `PlayerTeam` / God-eye / mid-race save / unseeded gameplay RNG; no silent design drift.

Do **not** merge when tests fail, save/load or determinism is broken, a lock is violated, the owner already rejected the direction (rebuilding Career Hub; Watch Race as the **default** play path, D-043), or the change would ship the wrong product (`StubRaceEngine` as official results).

This lock **overrides** Cursor Cloud defaults that say “do not merge unless the user asks”. The standing ask is: merge to `main` when there are no errors.

Land **one change onto current `main`**. Fetch `origin/main` first. Do not merge a stack of stale branches into each other — that is how conflict piles happen. If an old PR conflicts, replay the player-value change onto today’s `main`; do not force the ancient branch through.

Watch Race stays in the game, **off by default** (D-043 / D-048). Do not merge leftover Watch radio/DS dashboard PRs onto `main`. Do not rebuild Career Hub.

## D-046 — Player-facing rider ratings are a view of physiology
Owner lock 2026-09-01: the player needs normal strengths/weaknesses (Climb, Hills, Flat, TT, Sprint, Cobbles, OVR, POT). Those 1–99 numbers are **derived** from stored physiology (CP, W′, Pmax, mass, CdA, durability, positioning, handling). They are never the hidden cause of a race result. Form/freshness/fatigue stay day condition, not talent. WorldTour content must actually differ by archetype (stop copy-paste lab numbers). Visibility remains All / Guessed / None (D-042). **Polish player-facing name for Cobbles is Bruk** (not „kocie łby”). English identifiers stay `Cobbles`. Contract: `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md`.

## D-047 — Detailed courses + yearly identity-constrained generator
Owner lock 2026-09-01: routes are dense polylines (~25 m samples), stored in the world at New Game, not five labelled fragments and not the 5.4 km proof circuit for WorldTour. A generator builds realistic new courses each year inside race-identity constraints (Tour de France stage mix, monument character, distances). Stage races use one calendar racing day per stage. Official Simulate uses that day’s stored course. Skeleton soak keeps the short proof circuit. Watch film stays optional and off by default. Contract: `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md`.

## D-048 — Career Hub UI is deleted; Watch film stays, off by default
Owner lock 2026-09-01: remove Career Hub from the product. Delete `CareerHub.tscn` / `CareerHubHost` / `CareerHubScreen` and do not rebuild that dashboard (PR #4). The desk is the career shell. Watch Race remains an optional film in that shell, **off by default**; the player can turn it on. Simulate then results stays the default race-day path.

## D-049 — UCI-shaped fields, bunch-sprint flats, classification jerseys
Owner lock 2026-09-01: official WorldTour start lists follow UCI team size (7 riders; 8 on Grand Tours) and event invites (wildcards), not a 4-man card as the whole peloton. Classified Flat finishes are bunch sprints (peak / W′ / position), not a CP time trial. Stage races expose GC, points, KOM, youth, and team classifications as **after-stage queries**. D-032 (mid-race GC leadership transfer) stays deferred. History analogues are for owner judgment (D-001), not a scripted result. Contract: `RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md`.

## D-050 — Pick club, calendar entries, and per-event leaders before deeper management
Owner lock 2026-09-01: the next playable slice is **not** CdA split, sponsor market, scouting, or wiring look-catalog finance. First the player must, in the career shell on the WorldTour world:

1. **Choose which organization they manage** at New Game (not hardcoded Alpecin).
2. **Choose which calendar events that club enters** (pre-season, per event — Tour is one tick, not 21).
3. **Choose a designated leader for each entered event**, so the year is not one default captain on every start.

Race-day prep may still override. Do not grow a 28-man roster, aging, season rollover, or infinite career in the same tree. Manager identity stays `ManagerCareer` (D-004): the club is a job, not a permanent skin. Watch film stays off by default. Do not rebuild Career Hub. Contract: `CAREER_CLUB_CALENDAR_LEADERS_v0.1.md`.
