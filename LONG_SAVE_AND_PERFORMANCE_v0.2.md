# Peloton Manager — Long Save & Performance Design

**Wersja:** 0.2
**Status:** REVIEW  
**Purpose:** zapewnić, że kariery 100+ lat pozostają wydajne, historycznie spójne i nie prowadzą do wielogigabajtowych save'ów ani utraty referencji.

## 1. Target

Peloton Manager ma wspierać kariery co najmniej 100-letnie jako normalny stress/soak scenario.

Cele przed premierą należy mierzyć empirycznie, ale pre-production target:

```text
100-year typical save: < 1 GB preferred
2 GB: warning / investigate retention
5 GB: unacceptable; traktować jak retention/design bug
```

Nie jest to gwarancja konkretnego rozmiaru przed finalnym Data Model.

## 2. Stable IDs są wieczne

> **Entity ID raz użyte w save nie może zostać ponownie przydzielone innemu bytowi.**

Przykład:

```text
RiderId 9213 = Jan Kowalski
retirement
next generated rider = RiderId 9214
```

Nigdy:

```text
find first unused ID → reuse 9213
```

Historyczne wyniki, kontrakty, newsy i relacje mogą nadal wskazywać stare ID.

## 3. 64-bit identifiers

Preferowany typ runtime/database dla sekwencyjnych entity IDs:

```text
signed 64-bit integer
```

Zakres jest praktycznie niewyczerpywalny dla skali gry.

Można stosować osobne monotoniczne sekwencje per entity type.

## 4. Person vs career role

Docelowy Data Model powinien rozważyć:

```text
Person
├── RiderCareer
├── StaffCareer
└── ManagerCareer
```

Były zawodnik może po latach zostać DS-em, trenerem lub managerem bez otrzymania nowej historycznej tożsamości.

`PersonId` pozostaje ten sam.

## 5. Retirement nie oznacza DELETE historii

Po emeryturze aktywny ciężki stan zawodnika jest kompaktowany.

Usuwane / archiwizowane mogą być np.:

- current fatigue,
- training queues,
- temporary morale modifiers,
- daily health state,
- active race plan,
- current objectives,
- temporary performance caches.

Zachowywane:

- identity,
- career dates,
- team history,
- important contracts,
- major results,
- selected career stats,
- important injuries/events,
- rivalries,
- records,
- legacy links.

## 6. HOT / WARM / COLD data

### HOT

Aktywnie symulowany świat:

- aktywni riders/staff,
- bieżący sezon,
- kontrakty,
- health/form,
- negotiations,
- scheduler,
- aktywna knowledge.

### WARM

Niedawna historia:

- pełne wyniki,
- key race events,
- zakończone kontrakty,
- niedawne dossier/history.

### COLD

Stara historia:

- skompaktowane career records,
- rekordy,
- zwycięzcy,
- ważne wydarzenia,
- dane potrzebne do kroniki i statystyk.

COLD data nie uczestniczy w normalnym daily simulation loop.

## 7. Historia przechowuje outcomes, nie obsolete simulation state

RaceLive może posiadać rozbudowany transient state.

Po zakończeniu wyścigu nie trzeba przechowywać na zawsze:

```text
fatigue at km 83.7 for every rider
instantaneous watts every tick
position every simulation step
all internal decision candidates
```

Zachowujemy:

- official result,
- times/gaps,
- classifications,
- important incidents,
- key tactical decisions,
- story-worthy events,
- records/context.

## 8. Event retention policy

Domain Event log może mieć różne poziomy retencji.

### Permanent structural events

- contract signed,
- race won,
- retirement,
- organization renamed,
- major injury,
- scandal,
- record broken.

### Compressible operational events

- low-level scheduler work,
- temporary notifications,
- internal simulation ticks.

Operational eventy mogą zostać po czasie zagregowane lub usunięte, jeżeli nie są potrzebne do replay/audytu.

## 9. News jako projection

Nie każdy news musi przechowywać pełny tekst przez 100 lat.

Preferowane:

```text
Structured Historical Event
↓
News Projection / localized text
```

Ważne wyjątkowe artykuły mogą zachować snapshot treści, jeśli stanowią część kroniki.

## 10. Knowledge retention

OrganizationKnowledge nie może bez końca przechowywać każdej miesięcznej estymacji każdego zawodnika.

Możliwe:

- zachowanie najnowszego aktywnego estimate,
- historyczne milestone estimates,
- agregacja starych scouting observations,
- usuwanie bezwartościowych szczegółów po emeryturze.

Historycznie można zachować fakt:

> Organization X scouting rider Y in 2036 and declined recruitment.

bez każdej starej liczby zakresu atrybutu.

## 11. Static content nie jest duplikowany bez potrzeby

Save zapisuje:

```text
ContentPack IDs
Versions
Scenario composition
World deltas/state
```

Nie powinien bez potrzeby kopiować całej statycznej bazy JSON do każdego save'a.

## 12. Lazy loading archiwów

Nie trzeba trzymać w RAM wszystkich emerytowanych ludzi ze 100 lat.

Aktywny świat jest załadowany.

Stary profil można pobrać z SQLite na żądanie.

## 13. Advance Day i performance

UX używa jednego dnia.

Runtime jest event-driven.

Nie wykonujemy:

```text
for every historical person:
    update_every_day()
```

Emerytowany człowiek bez aktywnej roli nie jest daily simulation entity.

## 14. Indexing

SQLite schema musi posiadać indeksy pod kluczowe historyczne query, m.in.:

- PersonId/RiderId,
- OrganizationId,
- RaceEditionId,
- date,
- contract active range,
- result winner/participant,
- world event type/date.

Indeksy projektujemy na podstawie realnych query i profilingu.

## 15. 100-Year Soak Test

Obowiązkowy headless test:

```text
simulate 100 years
```

Raportuje:

- save/database size,
- load time,
- save time,
- Advance Day latency,
- peak RAM,
- active entity count,
- archived entity count,
- race result rows,
- domain event rows,
- knowledge rows,
- DB growth/year and growth by decade,
- rows created/compacted per year,
- scheduler queue size p50/p95,
- Advance Day p50/p95/p99 by decade,
- largest tables,
- knowledge rows per active organization,
- compaction ratio,
- deterministic world checksum,
- p95 historical query latency,
- query latency,
- invalid references,
- duplicate/reused IDs,
- database integrity.

## 16. Long-save manager analytics

Ten sam 100-Year Soak Test powinien raportować również:

- manager population,
- manager career length,
- manager trait distributions,
- success by trait quantile,
- success by era/ruleset,
- staff mobility,
- organization survival,
- strategy diversity.

Performance testing i balance testing mogą używać tego samego świata.

## 17. Purge/compaction musi zachować referential integrity

Przed usunięciem ciężkich danych system musi upewnić się, że:

- trwałe historyczne referencje nadal działają,
- profile archiwalne można wyświetlić,
- rekordy nie tracą właściciela,
- organizacyjna kronika nadal wskazuje prawidłowe osoby.

## 18. Save compaction jest jawna i testowalna

Compaction nie może być rozsianymi delete'ami po systemach.

Preferowany dedykowany pipeline:

```text
ArchiveCandidateSelection
↓
CreateHistoricalSnapshot
↓
ValidateReferences
↓
RemoveTransientState
↓
CompactOperationalHistory
↓
IntegrityCheck
```

## 19. No ID recycling invariant test

Automatyczny test:

```text
Generate 100,000 riders
retire/archive many
Generate 100,000 more
assert all IDs unique for lifetime of save
```

## 20. Najważniejsze zasady

> **Stable IDs are never reused.**

> **Historical identity is permanent; active simulation state is not.**

> **Long-term history stores outcomes and meaning, not obsolete simulation state.**

> **A 100-year career is a required engineering test case, not an exotic edge case.**


## 21. Causal-safe compaction

> **Compaction może zmienić reprezentację danych, ale nie może zmienić przyszłej symulacji.**

Dane posiadające możliwy przyszły causal effect nie mogą zostać usunięte bez zachowania równoważnego causal hook.

Dotyczy m.in.:
- delayed investigations,
- whistleblowers,
- unresolved contract/legal obligations,
- przyszłych sponsor consequences,
- historycznych promises/relationships, jeżeli nadal mogą mieć efekt.

Regression test:

```text
same world at year X
branch A: compact
branch B: no compact
same future commands
=> same gameplay-relevant future
```

## 22. Knowledge lifecycle is lazy

Nie tworzymy rekordu wiedzy `every organization × every person`.

Knowledge subject powstaje dopiero, gdy istnieje źródło:
- scouting,
- public result/reputation,
- direct interaction,
- staff knowledge,
- agent contact,
- wewnętrzne dane.

Stare observations mogą zostać:
- zagregowane,
- oznaczone jako stale,
- zredukowane do milestone summary,
- usunięte, jeśli nie posiadają causal/history value.

## 23. Content reproducibility

Save nie musi duplikować całej statycznej bazy, ale musi znać dokładny resolved content przez version + cryptographic/content hash + dependency resolution identity.

Długowieczny save nie może zależeć wyłącznie od tego, czy autor moda nadal hostuje starą wersję pliku.

## 24. ID layers

`ContentDefinitionId` i `WorldEntityId` są różnymi rzeczami.

```text
ContentDefinitionId = namespaced stable string
WorldEntityId = monotonic Int64, never reused
```

`PersonId` trwa przez zmianę kariery/roli, natomiast `RiderCareerId`, `StaffCareerId` i `ManagerCareerId` są osobnymi identity records.
