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

Headless komenda `watch` jest na razie skrótem decyzji (start / pauza / meta), nie modelem Watch Race.

## D-034 — Race next is the Hub primary on race day
On a race-due day the Hub primary time-progress control relabels to **Race next** and enters `RacePreparationFlow`. Inbox remains a queue of items and does not launch the race.

Normal Hub primary action stays **Advance Day** (D-006). The `AdvanceDay` command still cannot skip a due race. Race next only opens preparation; starting the race remains a later prep-menu command.
