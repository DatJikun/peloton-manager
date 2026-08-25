# Peloton Manager — AI Manager System Design

**Wersja:** 0.2
**Status:** REVIEW  
**Purpose:** zdefiniować sposób działania managerów i organizacji AI tak, aby były symetryczne wobec gracza, wyjaśnialne, zróżnicowane, deterministyczne i możliwe do balansowania przez długie symulacje.

## 1. North star

> **AI manager nie jest botem utrudniającym życie graczowi. Jest uczestnikiem tego samego świata, posiadającym własną wiedzę, cele, ludzi, ograniczenia i błędy oceny.**

AI nie posiada osobnego „systemu transferowego AI”, „systemu wyboru lidera AI” ani losowych eventów typu `steal player from human`.

AI korzysta z tych samych domenowych mechanik co człowiek:

- scouting,
- kontakt z agentami,
- dossier,
- negocjacje,
- kontrakty,
- staff market,
- sponsorzy,
- kalendarz,
- briefing,
- race decisions,
- workload,
- finanse,
- rulesety,
- wiedza organizacyjna.

## 2. ManagerCareer + DecisionAuthority, nie Team Controller

Organizacja i manager to nie to samo. Manager jest realną osobą/karierą zatrudnioną przez organizację.

```text
Organization
    employs → ManagerCareer?

ManagerCareer
    PersonId
    traits / skills / reputation / memory
    Employment
    DecisionAuthority
```

`DecisionAuthority` mówi, skąd pochodzą wybory tej ManagerCareer:

```text
HumanInputAuthority
AIInputAuthority
RemoteHumanAuthority [future]
```

Domena nie posiada `PlayerTeam`, `AITeam` ani managera będącego tylko wrapperem controllera.

Human i AI wysyłają te same Application Commands. Zmiana klubu zmienia employment, nie gatunek organizacji.

## 3. Świat nie jest zbudowany wokół gracza

Jeżeli gracz nie bierze udziału w wyścigu, wyścig nadal się odbywa.

Jeżeli gracz nie obserwuje zawodnika, inne organizacje mogą go obserwować.

Jeżeli gracz nie szuka staffu, AI może go zatrudnić.

Jeżeli gracz nie otwiera rynku transferowego, rynek nadal działa.

> **The human-controlled manager is one actor in the world, not the owner of the world clock.**

## 4. Advance Day

Z perspektywy UX podstawową jednostką czasu kariery jest jeden dzień.

Główny przycisk:

```text
ADVANCE DAY
```

Przykład:

```text
2028-03-12 → 2028-03-13
```

Kliknięcie oznacza: „przetwórz świat do końca bieżącego dnia i zatrzymaj się, jeżeli potrzebna jest moja decyzja”.

### 4.1. Implementacja pozostaje event-driven

`Advance Day` nie oznacza jednego gigantycznego globalnego `DailyUpdate()`.

Wewnątrz dnia scheduler może wykonać:

```text
08:00 Training / recovery events
09:30 Scouting completion
10:00 Agent responses
11:00 AI staff negotiations
12:15 Race A
13:00 Race B
15:30 Race C
18:00 Medical updates
20:00 Media / sponsor events
23:00 Finance / housekeeping
```

System przetwarza wyłącznie zdarzenia, które faktycznie wymagają obliczeń.

## 5. Stop Conditions w Advance Day

Jeżeli w trakcie dnia pojawia się zdarzenie wymagające człowieka, Advance zatrzymuje się.

Przykłady:

- race briefing,
- ważna decyzja live race,
- deadline negocjacji,
- sponsor wymagający odpowiedzi,
- krytyczna kontuzja,
- roster registration deadline,
- decyzja organizacyjna oznaczona jako non-delegated.

Po rozwiązaniu sprawy gracz może kontynuować ten sam dzień.

## 6. Organization Identity vs Manager Strategy

Zachowanie organizacji nie jest wyłącznie charakterem jednego managera.

### Organization Identity

Wolno zmieniające się lub trwałe elementy:

```text
youth tradition
national / regional focus
commercial profile
sponsor dependence
financial risk tolerance
race identity
historical prestige
academy strength
technology culture
ethical culture
```

### Manager Strategy

Bardziej osobiste preferencje aktualnego szefa:

```text
risk tolerance
youth trust
leader loyalty
form sensitivity
reputation bias
data reliance
staff trust
sponsor priority
long-term planning
transfer aggression
financial discipline
innovation openness
tactical intervention preference
```

Organizacja = tożsamość + aktualni ludzie + zasoby + sytuacja + ruleset.

## 7. Trait composition zamiast kilku archetypów AI

Nie kodujemy dziesięciu zamkniętych typów:

```text
AggressiveAI
YouthAI
DefensiveAI
```

Archetypy mogą być etykietami opisowymi wynikającymi z wartości cech.

Przykład:

```text
riskTolerance = 0.82
youthTrust = 0.74
leaderLoyalty = 0.31
formSensitivity = 0.88
dataReliance = 0.62
longTermPlanning = 0.70
```

UI może później opisać takiego managera jako:

> Aggressive, youth-friendly, form-driven manager.

Ale decyzje wynikają z cech i sytuacji, nie z nazwy archetypu.

## 8. Cechy muszą wpływać na realne decyzje

Każda cecha musi posiadać co najmniej jeden udokumentowany decision surface.

Przykład:

### `formSensitivity`

Wpływa na:
- zmianę zaplanowanego lidera przed wyścigiem,
- wybór składu,
- skłonność do skracania długoterminowego planu po słabych wynikach.

### `leaderLoyalty`

Wpływa na:
- utrzymanie leadership promise,
- liczbę szans po słabszych występach,
- reakcję na eksplozję formy drugiego lidera.

### `dataReliance`

Wpływa na wagę:
- danych fizjologicznych,
- raportów analityków,
- obserwacji scoutów,
- reputacji i wyników.

Jeżeli cecha nie zmienia zachowania w testach, jest kandydatem do usunięcia lub połączenia.

## 9. Brak jednej optymalnej kombinacji cech

System nie powinien tworzyć jednego „meta manager build”, który jest najlepszy zawsze.

Wartość cechy zależy od kontekstu:

- rulesetu,
- epoki,
- technologii,
- jakości informacji,
- rynku transferowego,
- struktury kalendarza,
- ekonomii,
- dostępnego staffu,
- profilu zawodników.

Przykład:

W świecie z bardzo słabym scoutingiem i ograniczonymi danymi ekstremalny `dataReliance` może dawać mniejszą przewagę niż w nowoczesnym środowisku telemetrycznym.

W epoce dużej niepewności kontraktowej `agentRelationships` może być znacznie silniejsze.

W regulaminie mocno ograniczającym rozmiar składu `rosterEfficiency` może nabrać większej wartości.

## 10. Era-dependent trait value

Nie kodujemy:

```text
if year >= 2026:
    dataReliance += power
```

Cechy stają się lepsze lub słabsze **emergentnie**, ponieważ ruleset i środowisko zmieniają dostępne decyzje oraz informację.

To jest kluczowy test modularności.

Ten sam ManagerProfile może mieć inne wyniki w:

```text
1965 preset
2026 preset
custom no-antidoping world
future high-tech world
```

## 11. Knowledge-bounded decision making

AI podejmuje decyzję wyłącznie na podstawie informacji dostępnych jego organizacji.

Nie może bezpośrednio czytać:

```text
truePotential
trueCurrentAbility
hiddenInjuryRisk
futureDevelopment
otherTeamPrivateOffer
```

Może korzystać z:

- własnego scoutingu,
- danych publicznych,
- danych wewnętrznych własnych zawodników,
- informacji agenta,
- reputacji,
- wyników,
- staff opinions,
- niepewnych estimates.

## 12. AI może się mylić logicznie, nie losowo

Zła decyzja AI jest dopuszczalna, jeżeli da się ją wyjaśnić.

Przykład:

> Manager przecenił ostatnią formę zawodnika, ponieważ posiada wysokie `formSensitivity`, słabszy scouting i mało zaufania do długoterminowego planu.

Nie:

```text
AI made random bad choice because random() < 0.08
```

Randomness może wpływać na niepewne obserwacje i świat, ale nie zastępuje modelu decyzyjnego.

## 13. Decision pipeline

Przykładowy pipeline AI:

```text
World Situation
↓
Organization Knowledge
↓
Available Legal Commands
↓
Organization Needs
↓
Manager Traits
↓
Staff Recommendations
↓
Promises / Relationships
↓
Financial / Workload Constraints
↓
Candidate Actions
↓
Contextual Utility / Heuristics
↓
Decision
↓
Application Command
↓
Domain Events
```

## 14. Utility nie może być jedną magiczną liczbą bez trace

Wewnętrzne scoringi są dozwolone.

Ale debug system musi umieć pokazać składniki decyzji.

Przykład:

```text
SIGNING DECISION TRACE
Rider: Luca Rossi

Squad need: +22.0
Estimated ability: +18.4
Estimated potential: +13.1
Youth preference: +6.2
Salary cost: -14.8
Competition risk: -4.1
Agent relation: +2.7
Roster congestion: -7.4

Decision: submit offer
Confidence: medium
```

Liczby są debugowe, nie muszą być widoczne graczowi.

## 15. Explainable AI dla gracza

Gracz nie widzi wewnętrznego utility score konkurencji.

Ale własny staff musi umieć wyjaśnić własne rekomendacje.

Przykład:

> DS rekomenduje zmianę lidera, ponieważ drugi zawodnik ma lepszą bieżącą formę, profil etapu bardziej mu odpowiada, a planowany lider zgłosił problemy zdrowotne.

Każda ważna automatyzacja posiada:

```text
Who decided?
What did they know?
What objective were they optimizing?
Why this action?
Confidence?
```

## 16. Manager memory

Manager może posiadać pamięć długoterminową, ale nie ML model.

Przykładowe doświadczenia:

- success/failure with young leaders,
- repeated scout misses,
- sponsor conflicts,
- transfer negotiation failures,
- successful aggressive race decisions,
- financial crisis,
- staff betrayals / departures.

Pamięć może delikatnie modyfikować zaufanie lub preferencje.

Nie powinna zmieniać charakteru po jednym wydarzeniu.

## 17. Slow personality evolution

Dopuszczalne są powolne zmiany:

```text
riskTolerance 0.71 → 0.69
trustInScoutX 0.82 → 0.63
preferenceForYouth 0.54 → 0.59
```

Silne zmiany wymagają wielu doświadczeń albo wydarzenia o dużej wadze.

Podstawowe cechy osobowości mogą posiadać indywidualną `plasticity`.

## 18. Manager skill i manager preference to różne rzeczy

Nie mieszamy:

```text
aggressive = good
conservative = bad
```

Preferencja określa styl.

Umiejętność określa jakość interpretacji i realizacji.

Dwóch agresywnych managerów może podejmować inne jakościowo decyzje, ponieważ jeden:

- lepiej ocenia ryzyko,
- ma lepsze informacje,
- ma lepszy staff,
- posiada większe doświadczenie.

## 19. Manager + DS hierarchy

Manager/GM odpowiada głównie za:

- transfery,
- staff,
- sponsorów,
- strukturę organizacji,
- kalendarz,
- długoterminową strategię,
- high-level race priorities.

DS odpowiada głównie za:

- rekomendacje składu,
- interpretację briefingu,
- taktyczne decyzje podczas wyścigu,
- reakcję na sytuację race live.

Manager może mieć genialną strategię organizacyjną i słabego DS-a.

DS może świetnie prowadzić przeciętnie zarządzaną organizację.

## 20. Staff mobility

AI może:

- zatrudnić staff gracza,
- stracić staff na rzecz gracza,
- podkupić człowieka innemu AI,
- awansować byłego zawodnika,
- zwolnić pracownika,
- zatrudnić byłego rywala.

Nie istnieje osobny „staff pool dla AI”.

## 21. Career transitions ludzi

Architektura powinna dopuszczać:

```text
Rider → retired person → Coach / Scout / DS / Manager
```

Nie każdy emeryt musi zostać pracownikiem.

Jeżeli były zawodnik wraca jako staff, zachowuje `PersonId` i historię.

## 22. Debug spectator mode

Developer tools powinny pozwalać wejść w:

```text
Spectate Organization
```

Bez zmiany controller ownership.

Debug view może pokazywać:

- aktualne cele AI,
- knowledge state,
- shortlist,
- planned races,
- candidate commands,
- decision trace,
- workload,
- finanse,
- manager traits.

To jest narzędzie diagnostyczne, nie cheat UI release.

## 23. AI decision logging

Każda istotna decyzja AI powinna opcjonalnie logować:

```text
WorldDate
OrganizationId
ManagerId
DecisionType
InputKnowledgeVersion
CandidateActions
ChosenAction
DecisionTrace
RngStream if used
ResultingCommandId
```

Log może być ograniczany w release, ale musi istnieć w development buildach i headless tests.

## 24. AI anti-patterns

Zakazane bez jawnego uzasadnienia:

```text
if opponent == Human then become more aggressive
AI receives hidden true potential
AI gets free money to fix budget mistakes
AI instantly signs replacement staff
AI ignores recruitment workload
AI ignores calendar obligations
AI teleports riders between plans
AI performs random transfer to look alive
AI buys based on one global OVR
```

## 25. Headless manager balance lab

100-letnie i batchowe symulacje służą także do analizy cech managerów.

Przykład:

```text
peloton-sim manager-lab \
  --scenario modern_2026 \
  --runs 5000 \
  --years 100
```

Raportuje m.in.:

- win rate organizacji według trait deciles,
- awanse/spadki,
- solvency,
- rider development outcomes,
- transfer ROI,
- sponsor retention,
- staff churn,
- race success,
- dynasty frequency,
- survival rate managerów,
- correlations między cechami a wynikami.

## 26. Trait balance nie oznacza równości

Celem nie jest, aby każda cecha dawała identyczny win rate.

Celem jest uniknięcie:

- jednej zawsze dominującej cechy,
- jednej cechy praktycznie bez wpływu,
- kombinacji automatycznie wygrywającej każdą epokę,
- traitu, który działa odwrotnie niż opis bez sensownego powodu.

Niektóre style mogą być trudniejsze, bardziej ryzykowne lub zależne od warunków.

## 27. Trait effectiveness by environment

Balance report musi umieć porównywać ten sam trait w różnych world configurations.

Przykład:

```text
dataReliance effectiveness:
1965 historical rules      = low/moderate
2026 modern rules          = high
2045 data-rich custom      = very high
no telemetry custom world  = low
```

To nie powinno być ręcznie przypisanym tierem.

Raport mierzy emergentny rezultat.

## 28. Interaction analysis

Cechy mogą mieć synergie i konflikty.

Przykład:

```text
high youthTrust + high longTermPlanning
high transferAggression + low financialDiscipline
high formSensitivity + low leaderLoyalty
high dataReliance + poor analytics department
```

Headless lab powinien analizować nie tylko pojedyncze cechy, ale również pary i wybrane kombinacje.

## 29. Ruleset regression matrix

Przy zmianie rulesetu można uruchomić macierz:

```text
Manager Profiles × Rulesets × Seeds
```

Cel:

- sprawdzić, czy stary balans nie został przypadkowo zniszczony,
- zobaczyć, czy nowe zasady zmieniają strategie w logiczny sposób,
- wykrywać cechy, które stały się nieużyteczne.

## 30. Evolution of the meta is allowed

Jeżeli w świecie proceduralnie zmieniają się przepisy, sprzęt lub dostęp do danych, optymalna strategia managera może się zmienić.

To jest pożądane.

Manager, który dominował w latach 2030., może gorzej odnajdywać się w regulacjach 2040., jeśli nie adaptuje sposobu działania.

Historia managerów może dzięki temu posiadać własne epoki.

## 31. Manager adaptation to changing world

Manager może dostosowywać konkretne strategie bez natychmiastowej zmiany głębokiej osobowości.

Przykład:

- konserwatywny manager nadal jest konserwatywny,
- ale po zmianie zasad może częściej inwestować w określony staff,
- ponieważ jego knowledge model wykazał, że stary sposób przestał działać.

Adaptation ≠ personality rewrite.

## 32. Manager career evaluation

Gra może w przyszłości generować historyczne oceny managerów:

- titles,
- major wins,
- transfer successes,
- academy graduates,
- financial crises,
- staff legacy,
- scandals,
- eras built,
- adaptability.

Nie potrzebujemy jednego `Manager OVR` opisującego ich historyczne znaczenie.

## 33. Hotseat readiness

Hotseat nie jest MVP, ale ten design powinien pozwolić na:

```text
Human DecisionAuthority A → ManagerCareer A → Organization A
Human DecisionAuthority B → ManagerCareer B → Organization B
```

Każda organizacja posiada własny Knowledge Store.

Przed zmianą aktywnego gracza UI musi ukryć prywatne informacje poprzedniej organizacji.

## 34. Online multiplayer readiness

Online multiplayer jest `DEFERRED`.

Przygotowanie polega wyłącznie na:

- command-based world mutation,
- deterministic simulation,
- organization-scoped knowledge,
- brak ExactlyOneHumanPlayer,
- jawne ownership.

Nie implementujemy networkingu przed stabilnym single-player.

## 35. Acceptance criteria AI Manager v0.2

System design jest gotowy do implementacji dopiero gdy potrafimy odpowiedzieć:

- jakie cechy istnieją w MVP,
- które decisions każda cecha zmienia,
- jak AI pozyskuje wiedzę,
- jak generuje candidate actions,
- jak staff wpływa na decyzję,
- jak explain trace jest zapisywany,
- jak manager memory działa bez ML,
- jak testujemy traits przez wiele rulesetów,
- jak Human i AI korzystają z tych samych Commands.

## 36. Najważniejsza zasada

> **AI nie ma wyglądać na różnorodne dlatego, że losuje zachowanie. Ma być różnorodne dlatego, że różni ludzie, z różną wiedzą i priorytetami, podejmują inne decyzje w tym samym świecie.**

---

## Manager labor market and career mobility

AI managers are persistent people in the world, not permanent properties of teams. They may renew, resign, be dismissed, become unemployed, apply for jobs, receive approaches and move between organizations. The human manager follows the same model.

Employment and `DecisionAuthority` may change during the save. The organization left by the human remains fully active under an AI-managed employment/authority state.

Staff and riders do not automatically follow a manager to a new club. They must be approached through the normal market and contracts, compensation, refusal and counteroffers still apply.

Manager reputation should be multidimensional, including surfaces such as youth development, sporting results, recruitment, financial discipline, sponsor relations, staff management, loyalty, tactical reputation and ethical/scandal history. Different organizations value these dimensions differently.



## 37. Scheduled AI cognition

AI nie wykonuje pełnego `Think()` dla każdej organizacji każdego dnia.

Decyzje są wyzwalane przez scheduler:
- event-driven triggers,
- deadline'y,
- zaplanowane review cycles,
- zmiany potrzeb organizacji.

Przykład:

```text
WeeklyRosterReview
RecruitmentNeedTriggered
ContractRenewalWindow
SponsorPipelineReview
PreRaceSelectionReview
```

To chroni `Advance Day` przed globalnym daily loop i pozwala profilować koszt każdej domeny.

## 38. Explainable stochasticity

Losowość nie zastępuje modelu decyzyjnego.

Dopuszczalne jest seedowane rozstrzygnięcie, kiedy kilka opcji ma zbliżoną ocenę przy wysokiej niepewności. `DecisionRecord` zapisuje wtedy uncertainty i powody.

Nie istnieje `random bad decision chance`.

## 39. Manager Balance Lab scope

Infrastruktura headless, podstawowe probes i metryki są wymagane wcześnie.

Pełna macierz:

```text
ManagerProfiles × ManyRulesets × ManySeeds × 100 years
```

jest `DEFERRED` do czasu, aż single-scenario core loop i race engagement przejdą owner playtest gate.

W official scenarios badamy również behavioral diversity po 20/50/100 latach, aby adaptation nie zbiegała wszystkich managerów do jednej polityki.

Custom modded worlds nie mają gwarancji braku dominującego traitu; mają być mierzalne i wyjaśnialne.

## 40. Sponsor/economy adaptation

Managerowie i organizacje reagują na dynamiczny sponsor market. Nie dostają sztucznego catch-up bonusu ani ukrytego luxury tax.

Wartość umiejętności komercyjnych może emergentnie zmieniać się między epokami, gdy zmienia się geografia sponsorów, popularność sportu, reputacja i dostępność kapitału.


---

## World Spy integration

Important manager/organization AI decisions must emit the shared `DecisionTrace` contract from `WORLD_SPY_AND_DECISION_TRACING_v0.1.md`.

This includes at minimum:
- recruitment,
- renewals,
- staff hiring/firing,
- manager job moves,
- sponsor decisions,
- season/calendar strategy,
- major finance/resource decisions,
- organization strategy changes.

The trace must preserve actor knowledge at decision time and may separately link developer-only Simulation Truth.

This is the main mechanism for debugging whether a manager trait is:
- useful,
- irrelevant,
- overpowered,
- interacting incorrectly with a ruleset,
- receiving information it should not have.
