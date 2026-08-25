# Peloton Manager — Technical Architecture & Modularity Design

**Wersja:** 1.0
**Status:** REVIEW — cleanup po zewnętrznym architecture review; gotowy do dalszego pre-production designu
**Powiązany dokument:** `Peloton_Manager_design_notes_v0.7.md`  
**Cel:** zdefiniować techniczne zasady projektu tak, aby Peloton Manager mógł być rozwijany przez lata, intensywnie iterowany z pomocą AI, obsługiwać długie save'y, historyczne i niestandardowe bazy danych, modding, edytor bazy, challenge mode oraz dokładanie nowych systemów bez przepisywania fundamentów gry.

---

## 1. Najważniejsza decyzja architektoniczna

Peloton Manager ma być **data-driven modular monolith**.

To oznacza:
- jedną aplikację i jeden spójny model świata,
- brak mikroserwisów i zbędnej infrastruktury sieciowej,
- wyraźnie oddzielone moduły domenowe,
- logikę symulacji niezależną od UI,
- zawartość świata definiowaną przez dane,
- zasady gry komponowane z niezależnych modułów,
- ten sam silnik dla career, historical databases, custom databases, challenge mode i modów.

**Godot ma prezentować grę. Godot nie może być miejscem, w którym ukryta jest właściwa logika świata.**

---

## 2. Docelowy stack technologiczny

Preferowany kierunek:
- **Godot 4.x .NET**, stabilna wersja przypięta przy rozpoczęciu projektu,
- **C#**,
- aktualna wspierana wersja **.NET LTS** przypięta przy rozpoczęciu projektu,
- **JSON** dla danych gry, rulesetów, scenariuszy i modów,
- **SQLite** dla save'ów,
- Windows jako pierwszy oficjalny target,
- brak technicznej zależności symulacji od Windowsa.

Dokument nie przypina dziś numeru Godota ani .NET. W dniu bootstrapu repozytorium powstaje osobny ADR z dokładnymi wersjami.

---

## 3. Fundamentalne invariants projektu

1. UI nie posiada prawdy o świecie. UI tylko prezentuje query/view state.
2. Żaden ważny system nie zależy od konkretnego ekranu Godota.
3. Race engine działa bez uruchamiania interfejsu.
4. `Advance Day` działa bez uruchamiania interfejsu.
5. Kontrakt deterministyczności brzmi: **same simulation build + same resolved content/rules + same initial state + same ordered command sequence = same gameplay result**.
6. Oglądany i nieoglądany wyścig korzystają z tego samego canonical race modelu; presentation mode nie zmienia fizyki ani zasad.
7. Historia nie skryptuje zwycięzców. Wyniki powstają z modelu symulacyjnego.
8. Rok kalendarzowy nie jest ukrytym globalnym switchem mechanik.
9. Reguły epoki są danymi/modułami, nie porozrzucanymi `if (year >= ...)`.
10. Trwałe `WorldEntityId` nigdy nie są ponownie używane w ramach save'a.
11. Save posiada wersję schematu, migracje oraz resolved-content identity/hash.
12. Content pack posiada wersję schematu, stabilne `ContentDefinitionId` i walidację.
13. Challenge mode nie posiada osobnego silnika gry.
14. Database Editor używa tego samego formatu contentu co gra.
15. Human i AI podlegają tym samym legalnym akcjom, kosztom i ograniczeniom świata, z jawnymi wyjątkami wyłącznie tam, gdzie wymagane jest uproszczenie wydajnościowe.
16. Nie wolno przechowywać ważnego deadline'u wyłącznie w mailu/newsie/powiadomieniu.
17. `DomainEvent` nie jest automatycznie wiedzą aktora. Informacja przechodzi przez zasady publikacji/obserwacji.
18. Queries i forecasts są read-only, knowledge-bounded i nie konsumują gameplay RNG.
19. Nie można zapisywać gry podczas aktywnego `RaceLive`; przed wejściem powstaje autosave.
20. `RaceLive` oznacza pojedynczy dzień wyścigowy / etap, nie cały wieloetapowy wyścig.
21. Każda istotna decyzja gracza daje się testować bez renderowania UI.
22. Gracz jest `ManagerCareer`, nie organizacją.
23. Manager/persona i źródło decyzji (`DecisionAuthority`) są osobnymi pojęciami.
24. Truth należy do Simulation; knowledge jest scoped do ludzi/organizacji/publicznego świata.
25. Compaction może zmienić reprezentację historii, ale nie może zmienić przyszłej symulacji.
26. Domyślna ekonomia nie używa ukrytego globalnego luxury tax ani automatycznej wielowiekowej inflacji nominalnej jako narzędzia balansu.

## 4. Matematyka jest źródłem rezultatu

> **Świat nie ma wiedzieć, jaki wynik „powinien” się wydarzyć. Ma policzyć, jaki wynik wynika ze stanu świata.**

Nie powinno istnieć:
- `PogacarHistoricalTourBonus`,
- ręcznie narzucona przewaga historycznego zwycięzcy,
- skrypt „ten zawodnik powinien wygrać”,
- osobny model wyników dla oglądanego i symulowanego wyścigu.

Historyczność jest warunkiem początkowym. Jeżeli baza startowa opisuje bardzo mocnego zawodnika, zwykle będzie mocny dlatego, że jego parametry, rozwój i otoczenie prowadzą do wysokiej wydajności.

---

## 5. Performance pipeline zawodnika

Race engine nie powinien operować na jednym atrybucie typu `Climbing = 92`.

Przykładowy abstrakcyjny pipeline:

```text
Rider Base Profile
    ↓
Current Development State
    ↓
Training Adaptation
    ↓
Form / Freshness / Fatigue
    ↓
Health / Injury / Illness
    ↓
Psychological & Morale Effects
    ↓
Equipment Effects
    ↓
Environmental Effects
    ↓
Team Support / Drafting / Position
    ↓
Tactical Effort
    ↓
Legal / Illegal Performance Modifiers
    ↓
Instantaneous Race Capability
    ↓
Terrain + physics-inspired race model
    ↓
Speed / energy use / time gaps / events
```

Nie wszystkie elementy muszą być ekstremalnie fizjologiczne. Celem jest wiarygodna matematyczna zależność, nie medyczny symulator człowieka.

---

## 6. Doping jako moduł wpływający na model, nie magiczny buff wyniku

System dopingu, jeśli zostanie zaimplementowany po MVP, wpływa na **wewnętrzne źródła wydajności**, a nie bezpośrednio na rezultat.

Nie:

```text
doping = +12% do czasu
```

Nie:

```text
if doped:
    win_chance += 30
```

Raczej abstrakcyjnie:

```text
illegalPerformanceState
    → zmiana zdolności wysiłkowych / regeneracji / obciążenia
    → wpływ na przebieg wysiłku
    → wpływ na prędkość i zdolność utrzymania tempa
    → wynik czasowy wynikający z race engine
```

Gra nie modeluje realnych procedur stosowania ani ukrywania środków. Model opisuje skutki sportowe i organizacyjne na abstrakcyjnym poziomie.

### 6.1. Kluczowy przypadek modularności

Custom scenario może wyglądać tak:

```json
{
  "scenarioId": "scenario.custom.modern_no_antidoping",
  "startDate": "2026-01-01",
  "content": {
    "riderDatabase": "riders.modern_2026",
    "teamDatabase": "teams.modern_2026",
    "raceDatabase": "races.modern_2026",
    "equipmentEra": "equipment.modern_2026",
    "organizationEra": "organization.modern_2026",
    "competitionRules": "rules.road_2026",
    "transferRules": "transfers.road_2026",
    "medicalContext": "medicine.modern_2026",
    "antiDopingRules": "antidoping.none",
    "integrityCulture": "integrity.permissive_custom"
  }
}
```

W takim świecie:
- istnieją zawodnicy i sprzęt z 2026,
- kalendarz może być z 2026,
- system antydopingowy może być całkowicie wyłączony,
- nie istnieje wykrywanie wynikające wyłącznie z założenia „rok = 2026”,
- jeżeli nielegalne wspomaganie zmienia możliwości zawodnika, race engine liczy wynik z nowych możliwości,
- osiągnięty czas może przez to znacząco odbiegać od rzeczywistej historii,
- silnik nie nakłada sztucznego limitu „to nierealne dla 2026”, jeśli skonfigurowany świat matematycznie do tego prowadzi.

To jest **wspierany przypadek użycia**, nie exploit.

---

## 7. Epoka nie jest jednym rulesetem

Nie powinno istnieć jedno monolityczne:

```text
Era = 1960
```

Epoka jest presetem złożonym z niezależnych modułów.

### 7.1. Domeny content/rules modules

```text
Rider Database
Team Database
Staff Database
Race Database
Calendar Structure
Competition Rules
Transfer Rules
Registration Rules
Roster Rules
Invitation Rules
Ranking / Points Rules
Equipment Era
Technology Availability
Medical Context
Anti-Doping Rules
Integrity Culture
Organization Structure
Economic Model
Sponsor Market
Media Context
Training Knowledge
Scouting Information Environment
Race Communication Rules
Safety Rules
```

### 7.2. Preset historyczny

```text
Historical 1965
```

może wskazywać:

```text
riders = riders.1965
teams = teams.1965
races = races.1965
competitionRules = rules.1965
equipment = equipment.1965
medicine = medicine.1965
antiDoping = antidoping.1965
organization = organization.1965
economy = economy.1965
informationEnvironment = information.1965
```

### 7.3. Custom database

Modder może złożyć:

```text
riders = riders.2026
rules = rules.1965
equipment = equipment.2035_custom
antiDoping = antidoping.off
economy = economy.1998
```

Jeśli konfiguracja jest kompatybilna, silnik ją uruchamia. Jeśli nie, walidator wyjaśnia konkretny konflikt przed startem save'a.

---

## 8. Content packs

Każdy zestaw danych istnieje jako content pack z manifestem.

```text
content/
├── core/
├── modern_2026/
├── historical_1998/
├── fictional_base/
└── community/
```

Przykład:

```json
{
  "id": "peloton.content.modern_2026",
  "name": "Modern Cycling 2026",
  "version": "1.0.0",
  "schemaVersion": 1,
  "dependencies": ["peloton.core"],
  "provides": [
    "riders.modern_2026",
    "teams.modern_2026",
    "races.modern_2026",
    "equipment.modern_2026"
  ]
}
```

Zasady:
- namespaced ID,
- jawne dependencies,
- jawne wersje,
- deterministyczny load order,
- walidacja referencji,
- wykrywanie konfliktów ID,
- jasne zasady override,
- możliwość oznaczenia niekompatybilności,
- w MVP wyłącznie data modding, bez wykonywania dowolnego kodu moda.

---

## 9. Stabilne ID

Nazwa jest treścią dla człowieka. ID jest prawdą dla systemu.

```json
{
  "organizationId": "org.uae_legacy"
}
```

Nazwa organizacji i sponsor mogą się zmieniać. ID nie.

Przykładowe namespaces:

```text
rider.*
staff.*
org.*
sponsor.*
race.*
raceedition.*
route.*
country.*
equipment.*
rules.*
scenario.*
challenge.*
event.*
```

---

## 10. Warstwy kodu

```text
src/
├── Peloton.Domain/
├── Peloton.Simulation/
├── Peloton.Rules/
├── Peloton.Application/
├── Peloton.Persistence/
├── Peloton.Content/
├── Peloton.Infrastructure/
└── Peloton.Client.Godot/

tools/
├── Peloton.SimRunner/
├── Peloton.ContentValidator/
└── Peloton.DatabaseEditor/

tests/
├── Peloton.Domain.Tests/
├── Peloton.Simulation.Tests/
├── Peloton.Rules.Tests/
├── Peloton.Persistence.Tests/
└── Peloton.Integration.Tests/
```

### Peloton.Domain
Model świata: Rider, StaffMember, Organization, TeamSeason, Contract, Sponsor, Race, RaceEdition, Calendar, Injury, TrainingState, ScoutingKnowledge, RecruitmentCase, Negotiation, WorldDate, HistoryRecord.

### Peloton.Rules
Odpowiada m.in.:
- czy drużyna musi wystartować,
- czy zawodnik może zostać zarejestrowany,
- czy można zawrzeć kontrakt,
- jak działa ranking,
- jakie wyposażenie jest dopuszczone,
- jak działa antydoping w aktywnym świecie.

### Peloton.Simulation
Race engine, rozwój, trening, forma, zdrowie, AI zespołów, transfer market, sponsor market, recruitment workload, finanse, media events, world evolution, retirement, generation of new riders.

### Peloton.Application
Przyjmuje intencje gracza przez Commands.

### Peloton.Persistence
Save, load, autosave, migracje, integrity checks, backup.

### Peloton.Content
JSON loading, schema validation, dependency resolution, content packs, ID registry, scenario composition.

### Peloton.Client.Godot
Rendering, UI, input, audio, animacje, live race visualization.

---

## 11. Commands zamiast bezpośrednich zmian stanu

UI, AI i przyszłe zdalne źródła wejścia nie modyfikują domeny bezpośrednio. Wysyłają Commands do Application layer.

Przykład:

```text
SubmitContractOfferCommand
```

nie:

```text
rider.Contract.Salary = ...
```

Canonical command envelope:

```text
CommandEnvelope
    CommandId
    RequestedAtSimulationTime
    IssuerPersonId
    ActingOrganizationId?   // null np. dla bezrobotnego managera
    AuthorityContext
    Payload
```

Application layer:
1. przyjmuje command,
2. ustala `AccessContext` i authority,
3. waliduje go przez Rules,
4. nadaje canonical processing order,
5. wykonuje system,
6. aktualizuje authoritative World State,
7. emituje `DomainEvent`,
8. zwraca rezultat/query projection.

Przykładowe Commands:

```text
AdvanceDay
AcceptRaceInvitation
WithdrawFromRace
SetSeasonPriority
OpenRecruitmentCase
ContactAgent
StartNegotiation
SubmitContractOffer
HireStaff
SetRaceBriefing
StartRace
RespondToRaceDecision
FinishDebrief
ApplyForManagerRole
OfferManagerContract
AcceptManagerContract
DismissManager
ResignManager
```

Command nie jest sortowany wyłącznie po `EntityId`. Canonical ordering jest własnością authority/schedulera i musi zostać jednoznacznie zapisany w kontrakcie deterministyczności.

## 12. Event taxonomy i Domain Events

`DomainEvent` opisuje fakt, że authoritative World State został zmieniony przez domenę.

Przykłady:

```text
RaceFinished
RiderInjured
ContractOfferSubmitted
ContractSigned
SponsorSigned
StaffHired
RiderRetired
DopingViolationDetected
InvestigationOpened
ResultStatusChanged
```

**DomainEvent nie jest powiadomieniem i nie jest automatycznie wiedzą AI.**

Obowiązują rozdzielone typy:

```text
ScheduledWork         // coś ma zostać wykonane w określonym czasie
CommandEnvelope       // intencja aktora
DomainEvent           // fakt zmiany authoritative state
ObservationSignal     // informacja możliwa do zauważenia/otrzymania
DecisionRequest       // trwała potrzeba decyzji
DecisionRecord        // audyt podjętej decyzji
HistoricalRecord      // trwały zapis historycznego znaczenia/outcome
NotificationProjection// prezentacja dla Inbox/UI
```

Każdy typ ma osobne identity, zasady persistence i idempotency.

Nie używamy event sourcingu jako jedynego źródła świata. Authoritative World State jest zapisywany jawnie; eventy służą do reakcji, audytu, informacji i historii zgodnie z własnym kontraktem.

## 13. Centralny World Scheduler, canonical order i Advance Day

Czas świata obsługuje jeden deterministyczny scheduler.

`Advance Day` jest kontraktem UX. Runtime nadal jest event-driven.

Przykład `ScheduledWork`:

```text
2027-03-05 12:00 — RaceStageSimulation
2027-03-05 18:00 — RecoveryUpdate
2027-03-06 08:00 — ScoutReportDue
2027-03-06 12:00 — AIRecruitmentReview
2027-03-07 09:00 — SponsorMeetingDeadline
2027-03-08 10:00 — RaceBriefingRequired
```

Canonical processing key musi być stabilny. Minimalny model:

```text
SimulationTimestamp
→ ProcessingPhase
→ AuthorityAssignedSequence
→ StableWorkId / CommandId tie-break
```

`EntityId` nie jest głównym mechanizmem rozstrzygania kolejności biznesowej.

Scheduler posiada jawne barrier semantics. Jeżeli wiele zdarzeń ma ten sam timestamp, rules określają, które fazy kończą się przed utworzeniem `DecisionRequest` i kiedy symulacja może zostać zatrzymana. Decyzja człowieka nie może retroaktywnie zmieniać już wykonanej „równoczesnej” fazy.

`ADVANCE DAY`:
1. ustala koniec bieżącego dnia,
2. pobiera najbliższe `ScheduledWork`,
3. wykonuje je w canonical order,
4. emituje Domain Events,
5. przepuszcza informację przez publication/observation pipeline,
6. tworzy ewentualne `DecisionRequest`,
7. zatrzymuje się na niedelegowanej decyzji albo końcu dnia.

`DecisionRequest` jest trwałym obiektem z ownerem, czasem utworzenia, deadline'em i polityką delegated/default resolution.

## 14. Game State Machine

```text
MainMenu
NewGameFlow
LoadingWorld
Management
PreSeasonPlanningFlow
RacePreparationFlow
RaceLive
RaceResultsFlow
RaceDebriefFlow
SeasonReviewFlow
Settings
```

Każdy stan jawnie określa:
- dostępne akcje,
- dozwolone przejścia,
- możliwość save/load,
- dostępność głównej nawigacji.

### RaceLive

`RaceLive` obejmuje **jeden dzień wyścigowy / jeden etap**. Grand Tour wraca pomiędzy etapami do normalnego świata kariery.

W `RaceLive`:
- nie działa główna nawigacja managera,
- nie można wejść w scouting ani negocjacje,
- nie można zapisać gry,
- można pauzować,
- można zmieniać bezpieczne ustawienia prezentacji,
- można wyjść do menu,
- ponowne wejście w save odtwarza stan sprzed rozpoczęcia wyścigu.

---

## 15. Card Flow / Wizard

Sekwencyjny UI stosujemy tam, gdzie proces ma początek, kolejność i koniec.

### New Game

```text
Scenario / World Base
↓
World / Custom Rules
↓
Historical / Dynamic / Chaos
↓
Beginner / Advanced / Expert
↓
All / Guessed / None
↓
Starting Manager Profile
↓
Starting Employment / Organization
↓
Summary
↓
Create World
```

### Race Preparation

```text
Race Overview
↓
Squad
↓
Roles
↓
Objectives
↓
Briefing
↓
Summary
↓
Start Race
```

### Race Completion

```text
Immediate Result
↓
Key Moments
↓
DS Debrief
↓
Medical / Recovery Notes
↓
Consequences
↓
Return to Management
```

Podstawowe akcje: `Back`, `Next`, `Confirm`, `Cancel / Exit Flow`.

---

## 16. Główna nawigacja Management Mode

Card Flow nie zastępuje całej gry.

Proponowane główne obszary:

### HQ
Dashboard, problemy, next event, rekomendacje, feed, `ADVANCE`.

### Calendar
Kalendarz światowy, zobowiązania, zaproszenia, plany zawodników, priorytety A/B/C, training camps.

### Team
Roster, role, forma, zdrowie, morale, rozwój, plan sezonu, historia.

### Staff
Kluczowi pracownicy, departamenty, odpowiedzialności, workload, kultura.

### Recruitment
Scouting, dossiers, shortlist, negotiations, renewals, workload.

### Finances
Cash, budget, payroll, commitments, forecast, sponsor income.

### Partners
Sponsorzy, sprzęt, manufacturer partnerships, R&D.

### World
Wyniki, rankingi, zawodnicy, zespoły, rekordy, historia, kronika świata.

### Inbox
Wiadomości, raporty, sprawy wymagające decyzji, archiwum.

---

## 17. Inbox nie jest bazą danych systemów

Mail jest prezentacją stanu.

Jeśli system negocjacji posiada `offerId`, `deadline`, `status`, `terms`, to Inbox tylko prezentuje te dane. Usunięcie lub przeczytanie maila nie może usunąć deadline'u z systemu.

Ta sama zasada dotyczy sponsorów, zaproszeń, medycyny, rejestracji, ostrzeżeń i workload.

---

## 18. Negocjacje

Wspólna infrastruktura, różne domenowe reguły.

Wspólne:
- strony,
- etap rozmów,
- deadline,
- knowledge state,
- relacja,
- oferta,
- counteroffer,
- czas odpowiedzi,
- agent/reprezentant,
- konkurencja,
- zakończenie procesu.

Rider contract:
- salary,
- duration,
- sporting role,
- leadership expectations,
- race opportunities,
- bonusy,
- promises.

Staff contract:
- salary,
- duration,
- responsibility,
- authority,
- department.

Sponsor:
- funding,
- duration,
- markets,
- naming,
- race priorities,
- reputation clauses,
- performance expectations.

Equipment partnership:
- funding,
- supplied equipment,
- exclusivity,
- R&D cooperation,
- testing access,
- contractual expectations.

Nie tworzyć jednego generycznego systemu, który zamienia każdą negocjację w tę samą minigrę.

---

## 19. Pre-season planning

Pre-season to specjalny Card Flow / planning mode.

Gracz przygotowuje plan:
- wyścigi zespołu,
- obowiązkowe wydarzenia,
- planowane zgłoszenia,
- tentative events,
- cele sponsora,
- programy liderów,
- priorytety A/B/C,
- training camps,
- rezerwy i alternatywy.

Plan nie jest więzieniem. W trakcie sezonu można go zmieniać.

Formalne konsekwencje wycofania z wydarzenia wynikają z aktywnego rulesetu, nie z uniwersalnego UI.

---

## 20. Race engine

### Input
- lista startowa,
- route profile,
- pogoda/środowisko,
- rider states,
- equipment,
- team tactics,
- briefing,
- DS models,
- rules,
- seed.

### Output
- finishing order,
- exact times,
- gaps,
- classifications,
- rider state changes,
- fatigue,
- incidents,
- injuries,
- tactical events,
- event log,
- decision requests,
- historical records.

### Live i fast simulation

Nie istnieją dwa race engine.

`Simulate Race` i `Watch Race` korzystają z tego samego modelu. Live mode renderuje postęp i może zatrzymać symulację przy decision point. Fast simulation rozwiązuje decision point przez briefing, AI i autonomię DS-a.

---

## 21. Race decisions i autonomia DS-a

Gracz nie steruje bezpośrednio nogami zawodnika.

Przykład:

```text
42 km to finish
Major rival attacks.

DS recommendation:
Use two support riders immediately.

Expected cost:
High fatigue for both domestiques.

Options:
Approve
Preserve support
Prioritize second leader
Your call
```

Częstotliwość konsultacji zależy od briefingu, DS autonomy, ważności sytuacji, osobowości DS-a i ustawień guidance.

---

## 22. Difficulty

Difficulty nie powinno ukrycie boostować AI.

### Beginner
Więcej wyjaśnień, rekomendacji, ostrzeżeń, bezpieczne defaulty i częstsze Stop Conditions.

### Advanced
Standardowa ilość interpretacji.

### Expert
Minimalne prowadzenie i większa odpowiedzialność gracza za interpretowanie danych.

---

## 23. Attribute Visibility

Niezależne od difficulty.

### All
Pełne atrybuty według zasad widoczności.

### Guessed
Zakresy i confidence.

### None
Brak surowych atrybutów obcych zawodników. Ocena przez wyniki, kontekst, scouting, trenerów, dane klubu, zdrowie, media i reputację.

`None` jest pełnoprawnym sposobem gry.

---

## 24. History Development Mode

Niezależny od rulesetów.

### Historical
Profile talentu mocniej trzymają się rzeczywistego archetypu.

### Dynamic
Historia jest warunkiem początkowym, ale kariery mogą mocno odejść.

### Chaos
Znacznie większa wariancja potencjału i rozwoju.

`Historical / Dynamic / Chaos` nie oznacza `rules 1960 / rules 2026`.

---

## 25. R&D i equipment partnerships

R&D jest systemem organizacyjnym.

Gracz:
- wybiera partnera,
- negocjuje,
- ustala priorytety projektu,
- przydziela budżet,
- wybiera kierunek,
- ocenia rezultat.

Race engine dostaje wynikową charakterystykę sprzętu.

Brak klasycznego drzewka `Aero II = +5 speed`.

---

## 26. Save system

Save: SQLite.

Save zawiera m.in.:
- world state,
- datę,
- seed,
- aktywne content packi i wersje,
- aktywne rules modules,
- historię,
- obiekty świata,
- relacje,
- kontrakty,
- kalendarz,
- stan AI,
- konfigurację,
- schema version.

Każda zmiana schema wymaga jawnej migracji `v1 → v2 → v3`.

---

## 27. Save integrity

- transakcyjny zapis,
- temp save + commit,
- rotacja autosave,
- validation kluczowych struktur,
- wykrywanie brakujących content packów,
- jasne komunikaty niekompatybilności,
- recovery save,
- pre-race autosave,
- sezonowy archive autosave.

---

## 28. Brak save'a podczas RaceLive

Świadoma decyzja:
- wejście tworzy autosave,
- `Save Game` jest niedostępne,
- `Exit to Main Menu` kończy sesję race view,
- ponowne wejście zaczyna od pre-race autosave.

Powód: prostszy model stanu, mniej edge case'ów, łatwiejsza deterministyczność i brak zapisywania połowicznej kolejki zdarzeń.

---

## 29. Database Editor

Nie implementować na początku.

Etap 1: JSON + schema.  
Etap 2: Content Validator z czytelnymi błędami.  
Etap 3: Database Editor używający tych samych schematów.

Edytor nie ma własnego formatu.

---

## 30. Challenge Mode

Challenge to scenario overlay, nie osobny engine.

```json
{
  "id": "challenge.save_the_team",
  "baseScenario": "scenario.modern_2026",
  "startDate": "2026-01-01",
  "startingEmployment": {
    "managerSource": "human-career",
    "organizationId": "org.example"
  },
  "overrides": {
    "startingCash": 2100000
  },
  "objectives": [
    {
      "type": "RemainSolvent",
      "until": "2028-12-31"
    }
  ],
  "failureConditions": [
    {
      "type": "Bankruptcy"
    }
  ],
  "lockedSettings": {
    "historyMode": "Dynamic",
    "difficulty": "Advanced",
    "attributeVisibility": "Guessed"
  },
  "seed": 54819321
}
```

---

## 31. Headless SimRunner

Obowiązkowe narzędzie.

```text
peloton-sim run --scenario modern_2026 --years 10
peloton-sim run --scenario modern_2026 --years 50 --seed 1234
peloton-sim batch --scenario modern_2026 --runs 1000 --years 20
```

Raportuje m.in. crashe, czas symulacji, liczebność peletonu, ekonomię, bankructwa, rozkład zwycięstw, rekordy, kontuzje, transfery i invalid states.

---

## 32. Testy automatyczne

### Determinism
`same state + same seed + same actions = same result`

### Persistence
`save → load = equivalent world`

### Long Simulation
`50 seasons without invalid state`

### Contracts
Brak nielegalnych overlapów, prawidłowe daty i reguły.

### Race
Poprawne start listy, klasyfikacje, DNF i identyczny engine dla live/fast sim.

### Content
Brak broken refs i duplicate IDs.

### State Machine
RaceLive blokuje save, New Game nie omija kart, nielegalne przejścia są kontrolowane.

### Scheduler
Deterministyczna kolejność i brak podwójnego wykonania eventów.

---

## 33. Architecture tests

Przykłady:

```text
Peloton.Domain cannot reference Godot
Peloton.Simulation cannot reference Peloton.Client.Godot
Peloton.Rules cannot depend on UI
Peloton.Persistence cannot own gameplay decisions
```

AI nie może dla wygody przepychać logiki do sceny Godota.

---

## 34. Logging i debug

Od początku:
- structured logs,
- event log,
- seed w logach,
- export debug report,
- trace zawodnika,
- trace wyścigu,
- trace negocjacji,
- trace scheduler.

Debug panel może pokazywać:
- World Date,
- Seed,
- Queued Events,
- Rider Internal State,
- Active Rules Modules,
- Scenario,
- Loaded Content Packs.

---

## 35. AI coding workflow

Repozytorium ma być zrozumiałe bez historii rozmów.

Obowiązkowe dokumenty:

```text
README.md
ARCHITECTURE.md  // canonical copy of current versioned architecture
GAME_STATES.md
DATA_MODEL.md
CONTENT_FORMAT.md
RULESETS.md
SAVE_FORMAT.md
TESTING.md
AI_DEVELOPMENT_RULES.md
ROADMAP.md
```

Każde większe zadanie dla AI zawiera:
1. zakres,
2. moduł,
3. dozwolone zależności,
4. oczekiwane zachowanie,
5. edge cases,
6. testy akceptacyjne,
7. czego nie zmieniać.

Zakaz: refactor całej architektury jako efekt uboczny małego feature'a.

Każdy feature musi przechodzić testy i aktualizować dokumentację, jeśli zmienia kontrakt systemu.

---

## 36. Architecture Decision Records

```text
docs/adr/
0001-use-godot-dotnet.md
0002-sqlite-save-format.md
0003-no-mid-race-save.md
0004-data-only-modding-first.md
0005-deterministic-race-engine.md
```

ADR: Context / Decision / Consequences / Alternatives considered.

Dzięki temu inne AI nie próbuje później „naprawić” świadomej decyzji.

---

## 37. Compatibility Matrix

Content może deklarować capabilities:

```json
{
  "requiresCapabilities": [
    "race.communication.radio",
    "contracts.multi_year"
  ],
  "providesCapabilities": [
    "antidoping.none"
  ]
}
```

Lepsze od uzależniania wszystkiego od konkretnych nazw packów.

---

## 38. Scenario Composition

Scenario jest receptą na świat.

```json
{
  "id": "scenario.modern_default",
  "startDate": "2026-01-01",
  "modules": {
    "riders": "riders.modern_2026",
    "teams": "teams.modern_2026",
    "staff": "staff.modern_2026",
    "calendar": "calendar.modern_2026",
    "competitionRules": "rules.road_2026",
    "transferRules": "transfers.road_2026",
    "registrationRules": "registration.road_2026",
    "equipment": "equipment.modern_2026",
    "medicine": "medicine.modern_2026",
    "antiDoping": "antidoping.modern_2026",
    "economy": "economy.modern_2026",
    "organization": "organization.modern_2026"
  },
  "defaults": {
    "historyMode": "Dynamic",
    "difficulty": "Advanced",
    "attributeVisibility": "Guessed"
  }
}
```

---

## 39. World evolution po starcie save'a

Zmieniać mogą się:
- przepisy,
- ranking,
- sprzęt,
- medycyna,
- organizacja,
- ekonomia,
- media,
- technologia.

Zmiana jest jawna i historyzowana.

```text
2031-10-15
RuleChangeScheduled:
EquipmentRules v4
Effective: 2032-01-01
```

Nie używać sekretnego `if currentYear == 2032`.

---

## 40. Nowi zawodnicy po historycznej bazie

Po wyczerpaniu historycznych roczników:
- generator tworzy kohorty,
- talent pool wynika z world model,
- regiony mogą ewoluować,
- infrastruktura krajów może wpływać na produkcję talentu,
- wygenerowani zawodnicy są pełnoprawną częścią historii.

---

## 41. Brak specjalnych klas „Historical Rider”

Nie tworzyć osobnych bytów:

```text
HistoricalRider
GeneratedRider
CustomRider
```

Jeden `Rider` może mieć metadata:

```text
source = Historical | Generated | Custom
```

ale systemy używają tego samego modelu.

---

## 42. Wyniki i kronika

Ważny wynik ma trwały zapis strukturalny.

Nie tylko:

```text
"Martin won Tour de France"
```

Raczej:

```text
EventType: GrandTourWon
RaceEditionId
RiderId
OrganizationId
Date
Time
Margin
Context
```

Tekst narracyjny jest generowany z danych.

---

## 43. UI: jedno źródło prawdy

Ta sama informacja w kilku ekranach pochodzi z jednego modelu/query.

`Rider Current Team` nie jest kopiowane osobno do profilu, kontraktu, newsa i race screen.

---

## 44. Query layer

UI:
- Commands zmieniają świat,
- Queries odczytują świat.

Przykłady:

```text
GetRiderProfile
GetTeamRoster
GetRecruitmentDashboard
GetRaceOverview
GetWorldRankings
GetInboxItems
GetCalendarMonth
GetFinancialForecast
```

---

## 45. Performance i skala

Docelowo:
- tysiące zawodników,
- setki organizacji,
- tysiące wyników,
- dziesiątki lat save'a.

Zasady:
- świat nie aktualizuje się co render frame,
- scheduler/event-driven simulation,
- render nie jest zegarem świata,
- batch processing tam, gdzie ma sens,
- indeksowanie danych historycznych,
- profiling przed przedwczesną optymalizacją.

---

## 46. Brak logiki świata w `_process()`

Dozwolone:
- animacje,
- UI transitions,
- input,
- interpolation race visualization.

Niedozwolone:
- rozwój peletonu,
- kontrakty,
- transfer AI,
- world calendar,
- ekonomia sezonu.

---

## 47. Localization ready

Dane powinny rozdzielać ID od tekstu.

```json
{
  "nameKey": "race.tour_de_france.name"
}
```

Nie trzeba mieć wielu języków w MVP. Trzeba nie zamknąć tej drogi.

---

## 48. Database i kwestie licencyjne

Architektura musi obsługiwać bez zmiany silnika:
- bazę fikcyjną,
- historycznie inspirowaną,
- licencjonowaną,
- community database.

Core gry nie zakłada istnienia konkretnych realnych nazwisk.

---

## 49. MVP techniczny

Pierwszy vertical slice ma udowodnić architekturę.

Minimum:
- Godot + C# bootstrap,
- Domain,
- Rules,
- Application Commands,
- Content JSON,
- scenario loader,
- SQLite save/load,
- World Scheduler,
- Advance Time,
- minimalny roster,
- minimalny kalendarz,
- prosty race engine,
- race preparation Card Flow,
- RaceLive,
- results,
- pre-race autosave,
- brak mid-race save,
- jeden prosty system scoutingu,
- jedna negocjacja kontraktowa,
- event log,
- headless simulation,
- determinism test.

Nie trzeba jeszcze: pełnego dopingu, wielu epok, Database Editora, Workshop, pełnego R&D, rozbudowanych mediów ani proceduralnych reform regulaminów.

Architektura nie może ich blokować.

---

## 50. Milestone 0 — Architecture Skeleton

Cel:

```text
uruchomić świat bez grafiki,
przesunąć czas,
zasymulować prosty wyścig,
zapisać,
wczytać,
otrzymać identyczny stan,
pokazać wynik w minimalnym UI Godota.
```

Acceptance:
1. `dotnet test` przechodzi.
2. Headless runner symuluje 10 sezonów bez crasha.
3. Ten sam seed daje ten sam wynik.
4. Godot nie jest wymagany do testu race engine.
5. JSON scenario wskazuje aktywne modules.
6. Zmiana jednego rules module nie wymaga zmiany race UI.
7. Save zapisuje schema version i content manifest.
8. RaceLive blokuje SaveGame.

Dopiero potem szerzej budujemy UI i kolejne systemy.

---

## 51. Osobne dokumenty potrzebne przed właściwym kodowaniem

### `UI_SITEMAP_v0.1.md`
Wszystkie główne ekrany, nawigacja, Card Flows, HQ, Race Mode, Inbox.

### `GAME_STATES_v0.1.md`
Pełna state machine, przejścia, save restrictions.

### `DATA_MODEL_v0.1.md`
Rider, Organization, Contract, Race, Staff, Sponsor, Equipment, Injury, WorldEvent.

### `CONTENT_FORMAT_v0.1.md`
JSON schemas, IDs, manifests, dependencies, overrides.

### `RACE_ENGINE_DESIGN_v0.2.md`
Trasa, grupy, wysiłek, fatigue, drafting, taktyka, czasy, decision points.

### `RULESETS_v0.1.md`
Rodzaje modułów rules, interfejsy i kompatybilność.

### `SAVE_FORMAT_v0.1.md`
SQLite schema, migrations, backup strategy.

### `TESTING_STRATEGY_v0.1.md`
Unit tests, simulations, invariants, regression scenarios.

### `AI_DEVELOPMENT_RULES_v0.1.md`
Zasady pracy modeli nad repo, dokumenty wejściowe, zakres tasków, zakazane skróty architektoniczne.

---

## 52. Najważniejsza konsekwencja projektu

Peloton Manager nie ma jednej „prawdziwej” konfiguracji kolarstwa zaszytej w kodzie.

Silnik posiada:
- ludzi,
- organizacje,
- model osiągów,
- race engine,
- czas,
- zdarzenia,
- reguły,
- dane.

Scenariusz mówi, **jaki świat z tych elementów zbudować**.

Możliwe:

```text
Modern 2026
```

```text
Historical 1965
```

oraz:

```text
2026 riders
+ 1965 competition rules
+ no anti-doping
+ custom equipment
+ fictional sponsors
+ Chaos development
+ Expert
+ No Attributes
```

Silnik nie ocenia, czy taki świat jest historycznie sensowny.

Ma go poprawnie, deterministycznie i matematycznie zasymulować.

---

## 53. Definicja architektury w jednym zdaniu

> **Peloton Manager jest deterministycznym, data-driven symulatorem świata kolarskiego, w którym interfejs, scenariusze, historyczne epoki, challenge i mody są różnymi sposobami konfiguracji i obserwowania tego samego modularnego silnika.**

---

# Zmiany architektoniczne v0.2

Poniższe sekcje są częścią obowiązującej architektury i mają pierwszeństwo przed starszymi, mniej precyzyjnymi zapisami v0.1, jeżeli wystąpi konflikt.

## 54. North Star i hierarchy of truth

Repozytorium musi posiadać krótki `VISION.md`.

Hierarchia dokumentów przy konflikcie:

```text
VISION.md
↓
zaakceptowane ADR
↓
ARCHITECTURE.md
↓
zaakceptowane system design docs
↓
GAME_STATES / DATA_MODEL / RULESETS / CONTENT_FORMAT
↓
HANDOFF.md
↓
ROADMAP / bieżące taski
↓
kod
```

Kod nie jest automatycznie źródłem prawdy tylko dlatego, że istnieje. Jeżeli kod różni się od zaakceptowanego designu, należy ustalić, czy kod jest błędny, dokument jest nieaktualny, czy świadomie zmieniono decyzję.

## 55. Fun / decision density invariant

Doświadczenie z wcześniejszego projektu managerskiego pokazuje, że głęboki i poprawny system może nadal być nudny, jeżeli gracz nie podejmuje w nim ciekawych decyzji.

> **Realizm, symulacja i modularność nie usprawiedliwiają systemu, który nie generuje interesujących decyzji albo interesującej obserwacji konsekwencji.**

Każdy duży gameplay system przed uznaniem za gotowy musi odpowiedzieć:

1. Jakie decyzje podejmuje gracz?
2. Dlaczego co najmniej dwie opcje mogą być rozsądne?
3. Jak system komunikuje konsekwencje?
4. Co może pójść nie tak?
5. Jak wynik tej decyzji wpływa na przyszłość?
6. Czy system jest nadal interesujący po 20, 100 i 500 użyciach?
7. Czy da się delegować rutynę bez usuwania sensownych decyzji?

Jeżeli odpowiedź brzmi „system jest realistyczny, ale decyzja jest oczywista”, system nie jest skończony.

## 56. Race engagement gate

Race engine jest szczególnie narażony na problem „dobry symulator, słaba gra”. Przed rozbudową grafiki, contentu lub liczby typów wyścigów musi zostać udowodnione, że oglądanie ważnego wyścigu i interakcja z DS-em są interesujące.

Minimalne pytania testowe:

```text
Czy w trakcie ważnego wyścigu występują decyzje, których wynik nie jest oczywisty?
Czy briefing realnie zmienia zachowanie zespołu?
Czy DS czasami proponuje sensowne odejście od planu?
Czy gracz rozumie, dlaczego został zapytany o decyzję?
Czy decyzje mają koszt alternatywny?
Czy kolejny wyścig może wygenerować inny typ problemu?
Czy simowanie mało ważnego wyścigu korzysta z tego samego silnika?
```

Nie wolno maskować braku decyzji częstszymi popupami.

## 57. Rozdzielone deterministyczne strumienie RNG

Jeden globalny PRNG jest zabroniony. Zmiana kosmetyczna, news, imię lub kolejność renderowania nie może przesunąć losowości transferów, kontuzji ani race engine.

Świat posiada `MasterSeed`, a losowość jest izolowana przez stabilne domeny i scopes.

```text
Race
Development
Injury
Health
Transfers
Negotiations
AIDecisions
Sponsors
Economy
WorldEvents
Weather
Names
Portraits
NewsWording
```

Seed derivation **nie może używać** runtime-dependent hashy takich jak `.GetHashCode()` / `HashCode.Combine()`.

Projekt posiada jawnie wersjonowany `StableSeedDerivationAlgorithm` działający na kanonicznie serializowanych wartościach:

```text
MasterSeed + RandomDomain + ScopeId + Purpose + OptionalEventId
```

W zależności od systemu RNG może być:
- stanowy i serializowany,
- albo adresowalny/counter-based przez stabilny klucz i draw index.

Wybór konkretnego PRNG i hash zostanie zamknięty w ADR przed pierwszym gameplayowym użyciem RNG.

Test izolacji:

```text
extra cosmetic news draw
must not change
race / injury / transfer outcomes
```

## 58. Randomness service i numeric determinism contract

Gameplay code nie tworzy ad-hoc `new Random()`.

Losowość przechodzi przez jawny kontrakt, np.:

```text
IRandomService
    Draw(RandomAddress address)

RandomAddress
    Domain
    ScopeId
    Purpose
    EventId?
    DrawIndex?
```

Debug trace potrafi wskazać adres/stream losowania.

### Numeric determinism

Nie lockujemy dziś zasady „fixed-point everywhere”.

Exact domains używają reprezentacji exact/integer tam, gdzie to naturalne:
- pieniądze w najmniejszych jednostkach,
- ID,
- daty/ticki,
- punkty/rankingi,
- liczniki.

Race physics/performance numeric policy zostanie wybrany po `RACE_ENGINE` spike. Może użyć fixed-point w krytycznych fragmentach albo kontrolowanego floating point, ale musi przejść determinism tests na wspieranych targetach.

Nie obiecujemy bit-identycznego wyniku między różnymi simulation builds po zmianie algorytmu, chyba że przyszły ADR jawnie wprowadzi versioned simulation routing.

## 59. Organization Identity i Management Strategy

AI drużyny nie powinno być jednym algorytmem optymalizującym `best value`.

Organizacja posiada trwałą tożsamość, np.:

```text
developmentTradition
nationalFocus
commercialProfile
financialRiskTolerance
historicalPrestige
sponsorDependence
youthPreference
transferAggression
raceIdentity
```

Aktualne kierownictwo posiada osobną strategię, np.:

```text
Youth Builder
Superteam Builder
GC Obsession
Classics Specialist
Stage Hunter
Sponsor First
Data Driven
Traditionalist
Aggressive Recruiter
Financial Rebuilder
```

Zmiana managera / GM może zmienić strategię bez resetowania historii organizacji.

## 60. AI nie zna prawdy tylko dlatego, że jest AI

AI przeciwników nie powinno automatycznie znać ukrytych atrybutów, potencjału lub przyszłości zawodników. Jeżeli gracz używa fog of war, AI również operuje na modelu wiedzy odpowiednim do swojej organizacji, chyba że jawnie udokumentowane uproszczenie jest konieczne wydajnościowo.

## 61. Talent kontra system wsparcia

Wyjątkowy zawodnik może być naprawdę wyjątkowy. Systemy wspierające nie mogą skalować liniowo bez końca.

> **Talent tworzy gwiazdy. Organizacja zwiększa prawdopodobieństwo wykorzystania talentu, zmniejsza straty i poprawia marginesy.**

Staff, infrastruktura, R&D, medycyna, analityka i scouting powinny często używać diminishing returns.

## 62. Natural constraints first

Jeżeli ograniczenie może wiarygodnie wynikać z finansów, rynku, regulaminu, workloadu, czasu, reputacji, informacji, relacji lub preferencji ludzi, najpierw używamy tych mechanizmów zamiast arbitralnego hard capu.

## 63. R&D i partnerzy muszą tworzyć trade-off

Nie istnieje uniwersalnie najlepszy partner sprzętowy. Kontrakty konkurują m.in. na osiach: cash, equipment quality, specjalizacja, customization, testing support, R&D influence, exclusivity, marketing obligations i reputation. Jeżeli jeden partner zawsze daje więcej pieniędzy i lepszy sprzęt, system wymaga poprawy.

## 64. Executable Design Questions / Balance Probes

Headless SimRunner ma odpowiadać na pytania projektowe, nie tylko wykrywać crashe.

Przykłady:

```text
CanAContinentalTeamReachWorldTour
CanYouthOnlyTeamBuildDynasty
CanRichTeamKeepFiveGenerationalRiders
CanHistoricalProdigyFlopInDynamicMode
CanLateBloomerReachEliteLevel
CanAICyclingEconomyRemainSolventForThirtyYears
CanSponsorLossDestroyFormerSuperteam
CanCleanTeamBeatDopedTeam
DoSupportSystemsShowDiminishingReturns
DoesBestTeamWinTooOften
CanWeakScoutMissFutureChampion
CanAggressiveRecruitmentOverloadCreateNaturalFailure
```

Każdy probe powinien definiować scenariusz, liczbę symulacji, obserwowane metryki, akceptowalny przedział i wynik ostatniego runu.

## 65. Golden simulation scenarios

Tworzymy stabilne scenariusze regresyjne:

```text
golden/
├── deterministic_race.json
├── contract_market_small_world.json
├── ten_season_economy.json
├── injury_recovery.json
└── custom_rules_frankenstein_world.json
```

## 66. Owner playtest gate

Testy automatyczne mogą udowodnić, że gra działa. Nie mogą udowodnić, że gra jest ciekawa.

Każdy większy gameplay milestone kończy się ręcznym playtestem właściciela projektu. Notujemy:

```text
What was fun?
What was boring?
What felt repetitive?
What decision felt meaningful?
What information was missing?
What did UI make harder than it should?
What would make me press Advance one more time?
```

Jeżeli fundamentalna pętla milestone'u jest nudna, nie rozbudowujemy na niej kolejnych systemów.

## 67. Milestone exit criteria

Każdy milestone posiada trzy bramki:

### Technical Gate
Build działa, testy przechodzą, brak krytycznej korupcji danych, invariants zachowane.

### Simulation Gate
Wymagane balance probes wykonane, długie symulacje stabilne, brak krytycznych anomalii.

### Playability Gate
Właściciel rozegrał wymagany zakres, decyzje są czytelne, pętla nie jest monotonna i nie ma krytycznego UX blockera.

Milestone nie jest `DONE`, jeśli przechodzi tylko pierwszą bramkę.

## 68. Document lifecycle

Dozwolone statusy:

```text
DRAFT
REVIEW
ACCEPTED
IMPLEMENTED
DEPRECATED
ARCHIVED
```

Większe design docs powinny posiadać sekcje: `LOCKED DECISIONS`, `OPEN QUESTIONS`, `DEFERRED`, `IMPLEMENTATION NOTES`, `KNOWN DIFFERENCES FROM CODE`.

## 69. HANDOFF jako żywy stan projektu

Repozytorium posiada jeden aktywny `HANDOFF.md`. Nie jest design docem. Opisuje aktualny milestone, działające elementy, next task, blokery, failing tests, ostatnie decyzje, dokumenty do przeczytania, komendy i feedback z playtestu.

## 70. Dokument index

Repozytorium posiada `DOCS.md`, aby AI nie musiało zgadywać, który markdown jest aktualny.

## 71. Open Questions muszą być jawne

Jeżeli decyzja nie została podjęta, dokument nie może udawać, że została. Open question może posiadać moment, przed którym musi zostać zamknięte.

## 72. Season context rail

Management UI powinno stale pomagać orientować się w sezonie, np.:

```text
PRE-SEASON → SPRING → GIRO → TOUR → VUELTA → WORLDS → OFF-SEASON
                              ▲
                            TODAY
```

Nie jest to sztywna state machine. To warstwa orientacji i accessibility zależna od aktywnego kalendarza.

## 73. Metrics before polish

Przed polerowaniem systemu należy umieć zmierzyć jego zachowanie. Race może mierzyć np. meaningful decision points, favourite win rate i comeback frequency; recruitment czas procesów i overload; development rozkład peak age i flop rate; economy insolvency rate i superteam sustainability.

## 74. Model changes require migration thinking

Każda zmiana trwałych danych odpowiada na pytania:

```text
Czy dotyka save schema?
Czy dotyka content schema?
Czy wymaga migration?
Czy zmienia determinism?
Czy zmienia golden simulation?
Czy zmienia mod compatibility?
```

## 75. Feature task template

Każdy większy task dla AI powinien używać szablonu:

```text
FEATURE:
GOAL:
PLAYER VALUE:
IN SCOPE:
OUT OF SCOPE:
AFFECTED MODULES:
ALLOWED DEPENDENCIES:
DATA CHANGES:
SAVE/MIGRATION IMPACT:
RNG DOMAIN:
DOMAIN EVENTS:
COMMANDS:
QUERIES:
EDGE CASES:
ACCEPTANCE TESTS:
BALANCE PROBES:
MANUAL PLAYTEST:
DOCS TO UPDATE:
```

`PLAYER VALUE` jest obowiązkowe.

## 76. Final pre-coding rule

Przed pierwszym większym gameplay codingiem co najmniej w statusie `REVIEW` muszą być: `VISION.md`, `ARCHITECTURE.md`, `DOCS.md`, `HANDOFF.md`, `UI_SITEMAP.md`, `GAME_STATES.md`, `DATA_MODEL.md`, `CONTENT_FORMAT.md`, `RULESETS.md`, `SAVE_FORMAT.md`, `TESTING.md`, `AI_DEVELOPMENT_RULES.md`. Otwarte pytania mogą pozostać, ale muszą być jawne.

---

# Zmiany architektoniczne v0.3

Poniższe sekcje są obowiązującym kontraktem i mają pierwszeństwo przed mniej precyzyjnymi zapisami wcześniejszych wersji.

## 77. Player identity = ManagerCareer, nie PlayerTeam

Domena nie posiada specjalnej klasy `PlayerTeam`.

Gracz jest `ManagerCareer` — trwałą karierą osoby, która może zmieniać pracodawcę, być zwolniona albo bezrobotna.

```text
Person
└── ManagerCareer
       └── Employment → Organization?
```

Organization nie zmienia typu, gdy przejmuje ją człowiek albo AI.

## 78. Manager employment vs DecisionAuthority

`ManagerCareer` i źródło decyzji są osobnymi pojęciami.

```text
ManagerCareer
    current employment
    traits / skills / reputation / relationships

DecisionAuthority
    HumanInputAuthority
    AIInputAuthority
    RemoteHumanAuthority      [future]
```

Manager nadal jest tą samą osobą niezależnie od tego, kto dostarcza decyzję.

Wakat może istnieć jako brak managera lub jawny acting/interim role; nie wymaga tworzenia specjalnego typu organizacji.

## 79. Symetryczne akcje, koszty i ograniczenia Human/AI

Human i AI korzystają z tych samych legalnych Commands i podlegają tym samym domenowym kosztom:
- kontraktom,
- workloadowi,
- deadline'om,
- roster rules,
- budżetom,
- informacjom,
- konkurencji rynkowej.

AI może używać uproszczeń obliczeniowych wyłącznie wtedy, gdy zachowane są obserwowalne możliwości, koszty i ograniczenia. Każde takie uproszczenie wymaga jawnego invariant/testu porównawczego.

Nie istnieje funkcja typu `StealPlayerFromHuman()`. AI kontaktuje agenta i składa normalną ofertę.

## 80. Knowledge ownership i AccessContext

Prywatna wiedza jest scoped i posiada właściciela.

Minimalne warstwy:

```text
PublicKnowledge / PublicEvidence
OrganizationKnowledgeStore
PersonalKnowledge / RelationshipMemory
```

Query nie przyjmuje już wyłącznie `ViewerOrganizationId`. Używa `AccessContext`:

```text
AccessContext
    ViewerPersonId?
    CurrentOrganizationId?
    DecisionAuthorityId?
    PermissionScope
```

Dzięki temu poprawnie działa:
- bezrobotny human manager,
- zmiana pracodawcy,
- hotseat,
- staff mobility,
- spectator/debug,
- publiczne vs prywatne informacje.

Zmiana pracy nie kopiuje confidential data byłego pracodawcy.

## 81. Information provenance

Każda istotna prywatna informacja powinna wiedzieć, skąd pochodzi.

Przykład:

```text
SourceType:
    PublicResult
    ScoutObservation
    CoachAssessment
    MedicalStaff
    AgentStatement
    MediaReport
    TeamData
    Rumor
```

Opcjonalnie:

```text
confidence
observedAt
expiresAt
sourcePersonId
```

Pozwala to budować sprzeczne informacje i explainability.

## 82. Simulation Truth nie jest automatycznie dostępne staffowi

Silnik posiada wewnętrzny rzeczywisty stan zawodnika potrzebny do obliczeń.

Staff nie otrzymuje go jako magicznej liczby.

Trener, scout i lekarz działają jako systemy interpretacji danych.

Ich raport może zależeć od:

- competence,
- specialization,
- sample size,
- quality of evidence,
- bias/personality,
- information environment,
- era technology.

To umożliwia błędną, ale logiczną ocenę zawodnika.

## 83. Evidence vs ability invariant

> **Result is evidence of ability, not ability itself.**

Model danych nie może automatycznie wnioskować:

```text
badSeason => rider regressed
```

Development State i Race Results są osobnymi bytami.

Interpretation systems mogą próbować wyjaśniać zależność, ale nie powinny jej zakładać.

## 84. Era-dependent information environment

Moduł epoki powinien móc określić dostępność i jakość informacji.

Przykładowe capabilities:

```text
training.power_meter
training.heart_rate
training.lab_testing
race.live_radio
race.live_power_data
medical.advanced_diagnostics
scouting.video_analysis
scouting.large_results_database
```

Custom world może mieszać te capabilities niezależnie od rosteru.

## 85. Dossier / Recruitment Case model

Dossier jest read model / agregatem informacji o sprawie rekrutacyjnej.

Nie jest zasobem typu XP.

Przykład:

```text
RecruitmentCase
    OrganizationId
    SubjectId
    Status
    Priority
    AssignedStaff
    OpenedAt
    KnowledgeReferences[]
    AgentContactState
    NegotiationIds[]
    MarketCompetitionKnowledge[]
    Notes[]
```

Brak RecruitmentCase nie musi blokować `ContactAgent`, jeżeli aktywne rules pozwalają na kontakt.

Akcja może automatycznie utworzyć minimalną sprawę.

## 86. ContactAgent jako normalna komenda

Przykładowo:

```text
ContactAgentCommand
    OrganizationId
    RiderId
    Purpose
```

Purpose może obejmować:

```text
GaugeInterest
AskAvailability
AskRoleExpectations
AskFinancialRange
AskMarketSituation
OpenNegotiations
```

Odpowiedź nie jest Simulation Truth.

Jest nową informacją ze źródłem `AgentStatement`.

## 87. Agent behavior

Agent jest aktorem rynku.

Może posiadać:

- relacje,
- reputację,
- preferencje,
- client strategy,
- negotiation style,
- willingness to disclose,
- tendency to pressure timelines.

Nie należy modelować „kłamstwa” jako prostego coin flipa.

Agent podejmuje decyzję w kontekście interesu klienta i swojej strategii.

## 88. Market competition jest rzeczywistym stanem

Jeżeli trzy zespoły negocjują z zawodnikiem, istnieją trzy rzeczywiste procesy.

Agent może poinformować czwarty zespół:

```text
"Several teams are interested"
```

ale wiedza czwartego zespołu nie musi zawierać dokładnych nazw, ofert ani etapu rozmów.

Informacja publiczna i prywatna są oddzielone od rzeczywistego market state.

## 89. AI może podkupować staff i zawodników

Nie jest to specjalna funkcja utrudniająca graczowi życie.

Staff i zawodnicy są uczestnikami rynku.

AI może:

- skontaktować się z ich reprezentacją,
- zaoferować lepszą rolę,
- zaoferować więcej pieniędzy,
- wykorzystać relację,
- wykorzystać kryzys w obecnym klubie,
- próbować przejąć człowieka przed przedłużeniem.

Gracz otrzymuje takie informacje tylko wtedy, gdy jego organizacja realnie może je znać.

## 90. Explainable AI decision record

Ważne decyzje AI/staff powinny móc zostawić debugowalny rekord.

Przykład:

```text
DecisionRecord
    DecisionId
    ActorId
    OrganizationId
    DecisionType
    InputsKnownToActor[]
    ConsideredOptions[]
    SelectedOption
    Reasons[]
    Confidence
    Timestamp
```

Nie wszystkie szczegóły są pokazywane graczowi.

Developer/debug build powinien móc je odczytać.

UI tworzy z nich odpowiednie `Why?` zależnie od wiedzy i difficulty.

## 91. Human-readable `Why?` contract

Jeżeli gra automatycznie:

- wybiera lidera,
- proponuje skład,
- odradza transfer,
- ustala rekomendowany trening,
- sugeruje wycofanie z wyścigu,
- rekomenduje decyzję w race live,

to system powinien posiadać uzasadnienie możliwe do przedstawienia człowiekowi.

Nie oznacza to ujawnienia ukrytych atrybutów.

Uzasadnienie używa informacji, które posiada organizacja.

## 92. Multiplayer-ready, multiplayer-later

Hotseat i networking są poza MVP.

Architektura nie może jednak zakładać:

```text
ExactlyOneHumanOrganization == true
```

Nie implementujemy teraz:

- lobby,
- matchmaking,
- network replication,
- race synchronization,
- anti-cheat.

Projektujemy tylko domenę tak, aby przyszły drugi Human `DecisionAuthority` nie wymagał nowych reguł rynku.

## 93. Hotseat privacy boundary

Przyszły hotseat wymaga zmiany aktywnego `DecisionAuthority` bez ujawnienia prywatnej wiedzy drugiego gracza.

UI query layer zawsze wykonuje zapytania przez `AccessContext`.

```text
GetRiderProfile(riderId, accessContext)
```

Ten sam profil może zwrócić różne dane dla różnych osób/organizacji, a bezrobotny manager nadal posiada publiczną i osobistą wiedzę.

To jest wymagane nawet w single-player, ponieważ ten sam mechanizm obsługuje fog of war.

## 94. Future remote multiplayer authority

Jeżeli kiedyś powstanie multiplayer online, preferowany kierunek to authority host/server nad World State.

Klienci wysyłają Commands.

Authority:

- waliduje Rules,
- wykonuje Simulation,
- zapisuje Events,
- rozsyła dozwolone rezultaty.

Deterministyczny engine i Command architecture są przygotowaniem do takiego modelu, ale nie wymaganiem implementacji MVP.

## 95. Race multiplayer pozostaje otwartym problemem

Live Race przy wielu human `DecisionAuthority` tworzy pytania:

- kto może pauzować,
- jak synchronizować speed,
- jak obsłużyć równoczesne decision points,
- co widzą różne zespoły,
- jak działa radio/delay informacji.

Nie rozwiązujemy tego przed działającym single-player race loop.

Status: `DEFERRED`.

## 96. Nowe architecture tests

Do testów granic dochodzą:

```text
HumanInputAuthority and AIInputAuthority use the same Application Commands.
AI decision code cannot read hidden Simulation Truth directly.
Rider knowledge differs between organizations.
Cosmetic/public profile query cannot expose private knowledge.
ContactAgent creates information, not absolute truth.
An AI team can sign a player from another AI team through the same contract pipeline.
An AI team can hire staff from the human organization through the same market pipeline.
No domain service depends on ExactlyOneHumanPlayer.
```

## 97. Nowe balance probes

```text
CanAIOutbidHumanForRider
CanHumanOutbidAIForRider
CanAIOutbidAIForRider
CanAIMisjudgeFutureChampion
CanHumanAndAIDisagreeOnSameRider
CanImprovingRiderLookStagnantInResults
CanStagnantRiderProduceCareerBestSeason
CanAgentMarketSignalBeUsefulWithoutBeingPerfect
DoesRecruitmentCompetitionEmergeWithoutArtificialPlayerTargeting
```

## 98. Locked architecture principles v0.3

> **Truth belongs to the simulation. Knowledge belongs to organizations.**

> **Human and AI organizations interact with the world through the same rules and actions.**

> **Results are evidence of ability, not ability itself.**

> **No gameplay domain assumes exactly one human-controlled organization.**

> **Automation must be explainable from information available to the actor.**

# Zmiany architektoniczne v0.5 — Advance Day, AI managers i long saves

## 99. Advance Day jest kontraktem UX

Podstawową jednostką postępu kariery widoczną dla gracza jest jeden dzień.

```text
ADVANCE DAY
```

Nie oznacza to globalnego dziennego ticka implementacyjnego. Runtime pozostaje event-driven i przetwarza wszystkie zdarzenia przypadające wewnątrz dnia.

Świat AI jest symulowany również wtedy, gdy organizacja gracza nie bierze udziału w danym wydarzeniu.

## 100. World continues without the player

Gracz jest jednym z uczestników świata.

Wyścigi, transfery, scouting, sponsorzy, staff market i inne procesy istnieją niezależnie od aktywności organizacji gracza.

Nie wolno implementować logiki typu „symuluj rynek dopiero po otwarciu ekranu transferów”.

## 101. AI Manager System jako osobny kontrakt

Szczegółowy design znajduje się w `AI_MANAGER_SYSTEM_v0.2.md`.

Obowiązujące zasady:

- Human i AI używają tych samych Application Commands,
- AI jest ograniczone OrganizationKnowledge,
- AI decisions są explainable,
- różnorodność wynika z composition traits + knowledge + context,
- nie istnieje jeden losowy „AI algorithm”,
- manager traits muszą posiadać jawne decision surfaces,
- manager preference i manager skill są rozdzielone.

## 102. Contextual manager trait value

Nie istnieje założenie, że dana cecha managera ma zawsze taką samą wartość.

Efektywność wynika z konfiguracji świata:

- ruleset,
- equipment,
- information environment,
- economy,
- calendar,
- organization structure,
- staff market.

Zmiana epoki może emergentnie zmienić metę zarządzania.

Nie kodujemy bonusu traitu zależnego bezpośrednio od roku kalendarzowego.

## 103. Manager balance laboratory

Headless SimRunner ma od początku umożliwiać podstawową batchową analizę managerów. Pełny multi-era Manager Balance Lab jest `DEFERRED` do czasu przejścia Race/Core Loop Playability Gate.

Raporty obejmują co najmniej:

- wyniki wg trait deciles,
- ROI transferów,
- financial survival,
- staff churn,
- dynasty frequency,
- success by ruleset,
- trait interactions.

Celem nie jest identyczna siła każdej cechy. Celem jest brak jednej cechy OP we wszystkich światach oraz cech pozbawionych istotnego wpływu.

## 104. Manager ruleset regression matrix

Po udowodnieniu core loop pełny regression lab może uruchamiać:

```text
ManagerProfiles × Rulesets × Seeds
```

aby sprawdzić, czy:

- cechy reagują logicznie na zmianę świata,
- nowa regulacja nie tworzy przypadkowej strategii dominującej,
- niektóre style stają się lepsze/słabsze z sensownego powodu.

## 105. Stable IDs are never reused

Entity ID wykorzystane w save jest spalane na zawsze dla tego typu encji.

Emerytura, archiwizacja ani compaction nie pozwala przydzielić ID nowej osobie.

Preferowany typ: monotoniczny 64-bit integer per entity category lub równoważny stabilny identyfikator.

## 106. Long save data lifecycle

Dane dzielimy koncepcyjnie na:

```text
HOT  — aktywnie symulowane
WARM — niedawna historia
COLD — skompaktowana historia
```

Historyczne encje mogą stracić transient simulation state, ale zachowują identity i referential integrity.

Szczegóły: `LONG_SAVE_AND_PERFORMANCE_v0.2.md`.

## 107. 100-Year Soak Test

100-letnia kariera jest obowiązkowym scenariuszem inżynieryjnym.

Test raportuje wydajność, wielkość bazy, integralność ID i jednocześnie zachowanie manager population.

Wstępny target projektowy:

```text
< 1 GB preferred typical 100-year save
>= 2 GB investigate
5 GB unacceptable
```

Target może zostać skorygowany po DATA_MODEL/RACE_ENGINE profilingu, ale 5 GB nie jest akceptowanym normalnym rezultatem.

## 108. Locked principles v0.4

> **The world advances one day for the player, but the engine advances events.**

> **The world continues whether the human organization participates or not.**

> **AI diversity comes from people, knowledge and context, not random behavior templates.**

> **Manager traits are context-dependent; rulesets can change what good management means.**

> **Stable IDs are never reused.**

> **A 100-year save is a first-class test case.**

---

# Cleanup contracts v0.6

## 109. Information pipeline: Truth → Signal → Knowledge → Decision

Canonical information flow:

```text
Simulation Truth / DomainEvent
↓
Publication & Observation Rules
↓
ObservationSignal
↓
Public / Organization / Personal Knowledge
↓
Interpretation / Forecast
↓
Human or AI Decision
```

AI, staff i UI nie mogą pomijać tej granicy przez bezpośrednie czytanie hidden Simulation Truth.

Przykład: ukryta kontuzja może być prawdą świata, ale rywal otrzymuje tylko publiczne symptomy lub własne obserwacje.

## 110. Knowledge record semantics i portability

Informacja, która może podróżować między organizacjami razem z człowiekiem, posiada semantykę co najmniej:

```text
Source
OwnerScope
KnownBy
AcquiredAt
Confidence
Confidentiality
Portability
Kind = Fact | Observation | Interpretation
Staleness
```

Scout zmieniający pracę zachowuje doświadczenie i osobiste wspomnienia, ale nie eksportuje całej confidential database byłego pracodawcy.

Knowledge subjects powstają **lazy**, kiedy istnieje realne źródło obserwacji/zainteresowania. Nie tworzymy `Organization × EveryPerson` rows na zapas.

Stare observations mogą przechodzić do summary/milestone form zgodnie z retention policy.

## 111. Forecast contract

> **Queries and forecasts never mutate World State and never consume gameplay RNG.**

Forecast:
- używa wyłącznie wiedzy dostępnej `AccessContext`,
- może zwracać zakres/confidence,
- nie odpala prawdziwej przyszłej symulacji na hidden state,
- nie może zmieniać wyniku przez samo otwieranie UI.

## 112. AI review cycles są scheduler work

AI organization nie wykonuje pełnego globalnego `ThinkEveryDay()`.

Decyzje AI są wzbudzane przez:
- konkretne zdarzenia,
- deadline'y,
- changes in market state,
- scheduled review cycles odpowiednich domen.

Przykłady:

```text
WeeklyRosterReview
RecruitmentNeedTriggered
ContractWindowReview
SponsorRenewalReview
PreRaceSelectionReview
```

Częstotliwość jest częścią domeny/rulesetu, nie render loop.

## 113. Explainable stochasticity

Zakazane:

```text
8% chance to make stupid decision
```

Dopuszczalne jest seedowane rozstrzygnięcie między bliskimi opcjami, jeżeli aktor posiada niepewną wiedzę lub ograniczoną uwagę.

DecisionRecord musi nadal pokazać, że opcje były oceniane podobnie i jakie uncertainty wpłynęło na wybór.

## 114. ID taxonomy

Rozróżniamy:

```text
ContentDefinitionId  // namespaced stable string, np. race.tour_de_france
WorldEntityId        // monotonic signed Int64, save-local, never reused
PersonId             // WorldEntityId osoby
RiderCareerId        // identity kariery zawodniczej
StaffCareerId        // identity kariery staff
ManagerCareerId      // identity kariery managerskiej
```

`PersonId` trwa przy zmianie roli. Career IDs nie muszą być równe PersonId.

## 115. Resolved content reproducibility

Save musi potrafić wskazać dokładny resolved content, z którym działa świat.

Minimalnie zapisuje:
- pack IDs,
- semantic versions,
- content hashes,
- resolved dependency order,
- simulation/content schema versions.

Runtime potrzebuje polityki immutable content cache lub snapshotu minimalnego zestawu definitions koniecznych do odtworzenia save'a. Autor moda nie może po cichu podmienić świata przez zmianę pliku pod tą samą wersją.

## 116. Causal-safe compaction

> **Compaction may change representation, never future causality.**

Jeżeli historyczna informacja może jeszcze uruchomić przyszły efekt (np. delayed investigation/scandal), musi zostać zachowany jej causal hook albo równoważny skompaktowany stan.

Obowiązkowy regression test:

```text
simulate X years → compact → continue Y years
vs
simulate X years → no compact → continue Y years

same build/content/state/commands
=> same gameplay-relevant future
```

## 117. Historical identity is effective-dated

Organization zachowuje stałe identity, ale historia potrzebuje kontekstu obowiązującego w dacie.

Race result/history references muszą móc odtworzyć:
- nazwę organizacji w danym sezonie,
- sponsorów,
- barwy/branding metadata,
- licencję/national identity tam, gdzie ważne.

Nie można renderować Touru 2031 nazwą sponsora, który wszedł w 2045.

## 118. Result revisions

Sporting result posiada co najmniej:
- achieved/on-the-day result,
- późniejsze administrative decisions,
- current official status.

Anulowanie wyniku po skandalu nie kasuje informacji o tym, co wydarzyło się w dniu wyścigu.

## 119. Rules transitions

Compatibility przy tworzeniu świata nie wystarcza. Rules module, który może zmienić się w trakcie kariery, definiuje transition policy:
- effective date,
- grandfathering,
- conversion,
- validation,
- repair policy.

Zmiana regulaminu nie może losowo unieważnić aktywnych kontraktów/rosterów/sprzętu bez jawnej reguły przejścia.

## 120. Race simulation parity

Nie lockujemy dwóch niezależnych silników `FullSim` i `MacroSim`.

Canonical race model jest jeden. Implementacja może później używać adaptive resolution/LOD dla wydajności, ale:

> **Watch vs Simulate cannot by itself change race physics or rules.**

Przy tym samym buildzie, seedzie, stanie i tej samej sekwencji decyzji presentation mode powinien dawać ten sam gameplay result.

Dokładna strategia performance zostanie ustalona w `RACE_ENGINE` na podstawie profilingu.

## 121. Sponsor market zamiast sztucznego luxury tax

Domyślny balans ekonomiczny ma wynikać z rynku, nie z niewidzialnej kary dla bogatych ekip.

Sponsor market zależy m.in. od:
- popularności kolarstwa w krajach/regionach,
- siły konkretnych rynków konsumenckich,
- branż zainteresowanych sponsoringiem,
- wyników lokalnych gwiazd/organizacji,
- reputacji sportu i skandali,
- media exposure,
- rulesetu/epoki,
- konkurencji między organizacjami o sponsorów.

AI organizacje adaptują strategię do dostępnego rynku sponsorskiego na tych samych zasadach co gracz.

## 122. Money model: brak automatycznej wielowiekowej inflacji nominalnej

Domyślny world economy działa w **real-value money** / stabilnym poziomie cen scenariusza.

Nie ma automatycznej zasady typu:

```text
every year salaries *= 1.03
```

prowadzącej do kontraktów liczonych w miliardach po 100+ latach.

Jeżeli konkretny historyczny/custom ruleset chce modelować inflację waluty, robi to jawny `EconomyRules` module wraz z presentation/conversion policy.

Market wage pressure nadal istnieje: zawodnicy mogą żądać więcej, gdy popyt przewyższa podaż, ale ceny nie rosną mechanicznie tylko dlatego, że przeszedł kolejny rok.

## 123. No universal balance promise for arbitrary mods

Oficjalne scenarios/rulesets przechodzą balance probes.

Custom Frankenstein world może świadomie stworzyć dominującą strategię lub trait. Silnik ma to poprawnie zasymulować i debugować, ale nie obiecuje idealnego balansu każdej możliwej kombinacji modów.

## 124. Historical development fallback

`Historical / Dynamic / Chaos` musi zdefiniować zachowanie dla `source = Generated | Custom`.

Historical mode nie może próbować odczytać nieistniejącej realnej przyszłości regena. Szczegółowa fallback policy zostanie zamknięta w Rider Development designie przed implementacją.

## 125. Documentation authority cleanup

Canonical architecture filename w repozytorium: `ARCHITECTURE.md`.

Wersjonowany export `Peloton_Manager_Technical_Architecture_v0.7.md` istnieje obok niego jako snapshot do review.

Section numbers są unikalne. Najważniejsze owner locks posiadają stabilne Decision IDs w `DECISIONS.md`.

---

## 126. Canonical Race Engine Design

Detailed race design authority for the next implementation phase:

`RACE_ENGINE_DESIGN_v0.2.md`

New accepted race invariants:
- no generic stamina-zero drop causality,
- CP/W'/Pmax + basic durability form the first physiological spike,
- position and drafting change real required power,
- drafting primarily modifies aerodynamic demand,
- gaps are dynamic physical state and can become self-reinforcing through shelter loss,
- race archetypes emerge from underlying model rather than magic terrain stats,
- RaceLive decisions use observations/interpretations, never hidden race truth,
- first headless prototype proves mountain pacing, repeated attacks, crosswind splitting, closeable gaps and "who chases?" before deeper physiology is added.

The prototype may use a 1-second fixed timestep as a reference implementation. Final production timestep/numeric representation remains open until profiling and determinism tests.



---

## 127. Race Spy diagnostic boundary

`RACE_SPY_DEBUGGING_v0.1.md` is mandatory race-development infrastructure.

Race Spy:
- passively observes structured race traces,
- never mutates World State,
- never consumes gameplay RNG,
- works headlessly,
- can compare Simulation Truth with actor-legal knowledge for developers,
- must not expose debug truth to normal queries/UI/AI,
- captures deterministic reproduction data for suspicious races,
- supports assertion checks for hidden-knowledge leaks and unexplained decisions.

Race Spy diagnostic traces are not permanent historical state and follow separate bounded retention/export rules.


---

## 128. World Spy / Decision Trace Framework

Cross-system diagnostic authority:

`WORLD_SPY_AND_DECISION_TRACING_v0.1.md`

Every major automated system must emit compatible structured `DecisionTrace` data.

Required distinction:

```text
SimulationTruthContext
ActorKnownInputs
ActorInterpretations
Goals
Constraints
Options
SelectedAction
Commands
OutcomeLinks
```

Initial domains:
- race,
- recruitment,
- negotiations/contracts,
- sponsors,
- staff/manager market,
- calendar/selection,
- training/development,
- finance,
- scouting/knowledge,
- equipment,
- integrity,
- organization strategy.

`RACE_SPY_DEBUGGING_v0.1.md` is the first domain specialization.

World Spy is passive, RNG-neutral, headless-compatible and never becomes an input to gameplay logic.


---

## 129. AI development safety contract

Mandatory workflow:
- `AI_DEVELOPMENT_RULES_v0.1.md`
- `GITHUB_WORKFLOW_v0.1.md`
- `CODEBASE_MAP.md`

The project is expected to be developed across multiple AI sessions with a non-programmer owner. Accepted decisions cannot be silently replaced; tasks remain reviewable; tests and Git history are project memory; docs describe contracts/invariants rather than every line; important automated decisions emit World Spy traces; save/content changes require versioning/migrations; completion includes verified checks and concise handoff.
