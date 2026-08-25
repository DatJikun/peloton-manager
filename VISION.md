# Peloton Manager — VISION

**Status:** REVIEW  
**Rola:** north star projektu. Czytać przed bardziej szczegółową dokumentacją.

## Jedno zdanie

> **Peloton Manager to głęboki, ale czytelny manager organizacji kolarskiej, w którym matematyczna symulacja, niepewna wiedza i decyzje ludzi tworzą wieloletnią alternatywną historię sportu.**

## Fantazja gracza

Gracz buduje organizację istniejącą przez dekady. Nie steruje bezpośrednio nogami zawodników. Zatrudnia ludzi, ustala priorytety, wybiera ryzyko, interpretuje niepełną informację i reaguje na konsekwencje.

Najlepszy save powinien dać się opowiedzieć jak prawdziwa historia sportowa: scout odradził przyszłego mistrza, cudowne dziecko zostało flopem, anonimowy junior wyrósł na legendę, sponsor zniszczył lub uratował klub, genialny DS zbudował dynastię, a skandal po latach zmienił ocenę całej epoki.

## Priorytety

### 1. Simulation Integrity
Świat liczy rezultat z własnego stanu. Nie skryptujemy zwycięzców. Ten sam stan + seed + decyzje powinny prowadzić do tego samego wyniku.

### 2. Meaningful Decisions
Realizm nie wystarcza. System jest wartościowy tylko wtedy, gdy generuje ciekawą decyzję, ciekawą niepewność albo interesujące obserwowanie konsekwencji. Jeżeli istnieje jedna oczywista poprawna odpowiedź, system wymaga przebudowy.

### 3. Emergent History
Najlepsze historie powstają ze zderzenia systemów. Gra pamięta kariery, kontrakty, rywalizacje, wyniki, kontuzje, transfery, sponsorów, skandale i epoki organizacji.

### 4. Uncertainty
Gracz nie zna całej prawdy. Scouting, rozwój, zdrowie, forma, intencje ludzi i przyszłość są niepewne. Niepewność ma prowadzić do decyzji, nie frustracji.

### 5. Delegation, not Direct Control
Gracz zarządza ludźmi i procesami. Delegacja nie oznacza bezczynności. W ważnych chwilach może zmienić priorytet, zaakceptować ryzyko, zaufać DS-owi albo odrzucić jego rekomendację.

### 6. Clarity & Accessibility
Gra ma być zrozumiała również bez wiedzy o kolarstwie. Najpierw pokazuje, co jest ważne, dlaczego i z jakimi konsekwencjami. Terminologii można nauczyć później.

### 7. Modularity
Jedna epoka nie jest zaszyta w kodzie. Świat składa się z niezależnych modułów riders, teams, calendar, competition rules, transfers, equipment, medicine, anti-doping, economy, organization i sponsor market.


### 8. Symmetric World

Gracz nie jest specjalnym przypadkiem silnika.

AI organizacje scoutują, negocjują, zatrudniają, tracą ludzi i popełniają błędy przez te same podstawowe mechaniki co organizacja człowieka.

Konkurencja ma wynikać z rynku i celów organizacji, nie ze skryptu "utrudnij graczowi życie".

### 9. Truth vs Knowledge

> **Truth belongs to the simulation. Knowledge belongs to organizations.**

Silnik zna rzeczywisty stan świata.

Organizacje znają wyłącznie to, co mogły zaobserwować, zmierzyć, usłyszeć albo oszacować.

Dwie drużyny mogą rozsądnie oceniać tego samego zawodnika inaczej.

### 10. Results Are Evidence

> **Results are evidence of ability, not ability itself.**

Świetny sezon nie jest automatycznie dowodem rozwoju. Słaby sezon nie jest automatycznie dowodem regresu.

Forma, zdrowie, kalendarz, rola, taktyka, konkurencja i pech oddzielają rzeczywistą zdolność od obserwowanego wyniku.



### 11. Living World

Świat nie czeka na gracza.

Każdy dzień przetwarza wyścigi, transfery, rozwój i decyzje innych organizacji również wtedy, gdy człowiek nie bierze w nich udziału.

> **The human organization is one actor in the world, not the center of the simulation.**

### 12. People Create Strategy

Różnorodność AI ma wynikać z ludzi, wiedzy, organizacji i kontekstu.

Nie chcemy losowych botów ani kilku zamkniętych stylów.

Zmiana rulesetu lub epoki może zmienić to, które cechy managera są wartościowe, ponieważ zmienia się sam świat podejmowania decyzji.


## Anti-goals

Peloton Manager nie ma być:
- PCM-em bez 3D,
- grą o ręcznym sterowaniu każdym zawodnikiem,
- arkuszem, w którym najwyższa cyferka zawsze wygrywa,
- symulatorem administracyjnych checkboxów,
- grą wymagającą wiedzy eksperta przed pierwszym sezonem,
- systemem broniącym nudy argumentem „tak jest realistycznie”,
- monolitem zależnym od jednego roku lub jednej bazy.

## Race gameplay

Ważny wyścig ma być wydarzeniem. Gracz powinien chcieć go oglądać, bo briefing ma znaczenie, DS interpretuje sytuację, część problemów wymaga decyzji, informacje są niepełne, a każda decyzja ma koszt alternatywny. Częstszy popup nie jest substytutem ciekawej decyzji.

## Talent i organizacja

Wyjątkowi zawodnicy mogą być naprawdę wyjątkowi. Organizacja pomaga wykorzystać talent, ale staff, infrastruktura i R&D mają często diminishing returns. Najbogatszy klub nie powinien produkować generacyjnego talentu samym budżetem.

## Zasada pracy

Testy automatyczne odpowiadają: „czy to działa?”. Headless simulation: „czy świat zachowuje się sensownie?”. Właściciel projektu: „czy to jest ciekawe?”. Żadna warstwa nie zastępuje pozostałych.

## Najważniejszy test

> **Czy po kilku sezonach naprawdę obchodzi mnie, co stanie się z tymi ludźmi i czy chcę nacisnąć Advance jeszcze raz?**

## Manager career identity

The player is not permanently bound to one team. A long career can span multiple organizations while former employers continue to exist and compete in the same world. Changing employer never grants magical access to the former organization's confidential data.



### 10. Living sponsor market

Kapitał w kolarstwie nie jest stałym globalnym kranem. Dostępność sponsorów zależy od kraju, epoki, popularności sportu, reputacji, mediów i sukcesów lokalnych aktorów.

Nie balansujemy długich save'ów ukrytym luxury tax ani automatyczną inflacją, która po 100 latach zamienia normalne kontrakty w miliardowe liczby.

### 11. Uncertainty creates market diversity

AI nie zna ukrytych zdolności rywali. Tak jak gracz interpretuje wyniki, scouting, dane, opinie ludzi i informacje agentów. Dwie dobre organizacje mogą racjonalnie dojść do innych wniosków.


### Race causality

Important races are driven by physical demand, current rider capability, position, shelter, team intent and imperfect information.

The engine should produce splits and failures from those interacting systems rather than from a universal stamina bar or scripted race events.


### Race explainability

Complex race behavior must be diagnosable. Developer tooling can inspect truth, knowledge and decision reasoning without changing the simulation. Normal gameplay remains knowledge-bounded.


### Explainable world

Important automated decisions should leave enough structured reasoning to be audited during development. Complex emergence is desirable; unexplained black-box behavior is not.


### Development must remain auditable

The project must survive many AI coding sessions. Clean Git history, tests, structured diagnostics and concise contract documentation are part of the architecture.
