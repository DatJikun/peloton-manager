# Peloton Manager — Design Principles & Anti-Patterns

**Wersja:** 0.1  
**Status:** REVIEW  
**Cel:** zapisać praktyczne prawa UX/game design wynikające z doświadczeń z Pro Cycling Manager 26 i wcześniejszych projektów managerskich. Dokument służy jako filtr dla nowych feature'ów.

## 1. Główna zasada

> **Nie zabieraj trudności. Przenieś ją z interfejsu do zarządzania organizacją.**

Gracz może przegrać, bo:

- źle ocenił zawodnika,
- zaufał złemu pracownikowi,
- źle rozłożył budżet,
- spóźnił się na rynek,
- przeciążył departament,
- wybrał złą strategię.

Nie powinien przegrywać, bo nie zauważył checkboxa albo niewidocznego limitu.

## 2. No Silent Failure

Krytyczny proces nie może po cichu nie działać.

Jeżeli manager nie prowadzi poszukiwań sponsora, gra musi jasno pokazać:
- że proces jest nieaktywny,
- kto jest za niego odpowiedzialny,
- jakie są konsekwencje,
- kiedy powstanie krytyczne ryzyko.

## 3. State / Why / Forecast

Każdy ważny system powinien próbować pokazać trzy warstwy:

### STATE
Co jest teraz?

### WHY
Dlaczego?

### FORECAST
Co prawdopodobnie stanie się po decyzji?

Przykład finansów:

```text
STATE: Projected payroll 82%
WHY: 3 renewals + guaranteed sponsor commitments
FORECAST: signing this rider -> 91%
```

## 4. Cash is not budget

Finanse muszą wyraźnie odróżniać:

- cash,
- committed costs,
- guaranteed future revenue,
- projected revenue,
- payroll commitments,
- free operating budget,
- regulatory limits,
- sponsor restrictions.

Jeżeli gracz nie może czegoś kupić, UI mówi dokładnie dlaczego.

## 5. Forecast before commitment

Ważna decyzja pokazuje przewidywane konsekwencje przed zatwierdzeniem, jeżeli staff posiada wiedzę umożliwiającą prognozę.

Dotyczy m.in.:

- kontraktów,
- sponsorów,
- staffing,
- recruitment overload,
- obozów,
- kalendarza,
- R&D,
- wycofania z wyścigu.

## 6. No arbitrary preparation gates

Scouting i dossier dają wiedzę oraz przewagę.

Nie powinny być magicznym kluczem pozwalającym rozpocząć negocjacje.

Spóźnienie na rynek jest karane stanem rynku, nie disabled buttonem.

## 7. Negotiate with people, not bars

`Interest` i `Agreement` mogą istnieć wewnętrznie jako modele, ale nie powinny być właściwą rozmową.

UI pokazuje powody:

- role,
- pieniądze,
- długość,
- kalendarz,
- konkurencję,
- relacje,
- ambicje,
- projekt sportowy.

## 8. Dossier is a case file

Dossier zawiera wiedzę, historię kontaktów i rynek.

Naturalnym pierwszym krokiem może być kontakt z agentem w celu zbadania sytuacji.

Nie istnieje `Dossier 100%` jako warunek podpisania.

## 9. Agents are market actors

Agent może ujawniać, filtrować i strategicznie przedstawiać informacje.

Informacja od agenta ma źródło i nie jest automatycznie prawdą.

## 10. AI uses the same market

AI może podkupić graczowi zawodnika albo staff dlatego, że:

- go znalazło,
- oceniło,
- skontaktowało się,
- złożyło ofertę,
- wygrało konkurencję.

Nie dlatego, że odpalił się losowy event wymierzony w człowieka.

## 11. Every important automated decision has a Why

Jeżeli staff lub AI rekomenduje lidera, skład, trening albo ruch taktyczny, powinno istnieć logiczne uzasadnienie oparte na wiedzy aktora.

Debug build powinien posiadać jeszcze głębszy DecisionRecord.

## 12. Automation is accountable

Ważna automatyczna decyzja ma właściciela:

- DS,
- trener,
- Head of Recruitment,
- Head of Medical,
- department.

Gracz może ocenić człowieka, nie anonimowy algorytm.

## 13. Route labels are summaries, not truth

`Flat / Hills / Mountain` jest skrótem.

System powinien potrafić opisać trasę bardziej znacząco:

- decisive climbing,
- sprint likelihood,
- final profile,
- technical difficulty,
- exposure,
- fatigue profile.

## 14. Training camps need a purpose

Obóz nie istnieje dlatego, że „manager kolarski powinien mieć obozy”.

Musi odpowiadać na konkretny cel:

- altitude preparation,
- heat adaptation,
- recovery,
- cohesion,
- recon,
- TT preparation.

Koszt i opportunity cost muszą być czytelne.

## 15. Conflict is good, opaque value is bad

Recon kolidujący z wyścigiem może być dobrą decyzją strategiczną.

Problemem jest sytuacja, w której gracz nie zna skali potencjalnego zysku ani kosztu.

## 16. Development is a process

Gra nie może pokazywać wyłącznie nowych cyfr po sezonie.

Staff interpretuje:

- tempo rozwoju,
- reakcję na trening,
- stagnację,
- możliwe przyczyny,
- poziom pewności.

## 17. Results are evidence, not truth

Zawodnik może:

- rosnąć bez poprawy wyników,
- stagnować mimo sezonu życia,
- wyglądać gorzej przez zdrowie i rolę,
- wyglądać lepiej dzięki sprzyjającemu kontekstowi.

W `None` jest to centralna część gameplayu.

## 18. Knowledge belongs to organizations

Każdy zespół posiada własną wiedzę.

Nie istnieje globalny scouting truth dla wszystkich uczestników rynku.

## 19. Data is not Potential

Waty, tętno, testy i wyniki są evidence.

Nie powinny automatycznie odsłaniać `Potential` albo pełnej przyszłej klasy zawodnika.

## 20. Contract roles are promises

`Leader` nie jest wyłącznie suwakiem satysfakcji przy podpisaniu.

Zawodnik pamięta:

- obiecaną rolę,
- leadership opportunities,
- Grand Tour promises,
- rzeczywiste wykorzystanie.

Niedotrzymanie obietnicy wpływa na relację i przyszłe decyzje.

## 21. Sporting success should have traceable financial consequences

Prize money i sukces sportowy nie muszą natychmiast zwiększać wage budget 1:1.

Ale każdy przepływ musi być czytelny:

```text
Prize money
- rider/staff bonuses
- taxes/fees if modeled
= organization share
```

## 22. Fast sim owes an explanation

Ten sam race engine działa dla live i instant simulation.

Po fast sim gracz może zobaczyć Key Race Story. W szkielecie (`D-036`) są to oficjalny zwycięzca, miejsca czwórki i czy cel StageWin wyszedł — bez ukrytej fizjologii. Docelowo:

- kto pracował,
- gdzie powstał problem,
- jakie decyzje podjął DS,
- dlaczego lider stracił.

Fast sim is the default race-day presentation; Watch film is optional.

## 23. Staff must own race decisions

Jeżeli wyścig został źle rozegrany, powinno być możliwe powiązanie decyzji z konkretnym DS-em i briefingiem.

Staff nie jest tylko pasywnym bonusem procentowym.

## 24. Calendar provenance

Przy wydarzeniu można sprawdzić:

> `Why is this on our calendar?`

Przykładowa odpowiedź:

- mandatory,
- sponsor priority,
- pre-season plan,
- wildcard accepted,
- preparation race,
- manually added.

## 25. Calendar audit before confirmation

Przed finalizacją planu sezonu system wychwytuje:

- przeciążenie zawodników,
- brak preparation races,
- konflikt celów,
- sponsor objective bez pokrycia,
- wydarzenia niskiego priorytetu bez wyraźnego powodu.

## 26. Sponsor goals sound like business goals

Nie:

`Visibility 10`

Raczej:

> „Zależy nam na ekspozycji w Polsce.”

Silnik może pod spodem mierzyć naturalne wskaźniki ekspozycji.

## 27. Difficulty comes from decisions, not obscurity

Beginner/Advanced/Expert mogą zmieniać ilość interpretacji i ostrzeżeń.

Nie budujemy trudności na:

- ukrytych checkboxach,
- nieopisanych limitach,
- nieczytelnych budżetach,
- magicznych paskach.

## 28. No silent AI omniscience

AI nie zna prawdziwych ukrytych atrybutów tylko dlatego, że jest komputerem.

Jeżeli używa uproszczenia, powinno ono zachowywać charakter niepewnej wiedzy i nie tworzyć oczywistego cheatowania.

## 29. Same rules are multiplayer preparation

Mechanika zaprojektowana symetrycznie dla Human/AI jest automatycznie bliżej hotseat i przyszłego online multiplayer.

Nie implementujemy networkingu w MVP, ale nie kodujemy świata pod dokładnie jednego człowieka.

## 30. Fun beats feature count

System, który istnieje, ale nie daje ciekawej decyzji ani interesującej obserwacji, nie jest sukcesem.

Doświadczenie z wcześniejszym managerem pokazało, że gra może mieć dużo poprawnych mechanik i nadal być nudna, jeżeli kluczowe momenty są pasywne.

## 31. Acceptance questions dla każdego systemu

Przed implementacją lub akceptacją feature'a:

```text
What does the player decide?
Why are at least two choices reasonable?
What information is uncertain?
What is the opportunity cost?
Who is responsible?
Can the player understand WHY?
Can the player see a forecast?
Can AI use the same system?
Does it still work with hidden attributes?
Does it create a story after several seasons?
```

Jeżeli większość odpowiedzi jest pusta, feature prawdopodobnie nie jest gotowy do produkcji.

## 32. The world does not wait for the player

Wyścigi, transfery i decyzje AI nie mogą zależeć od tego, czy gracz otworzył dany ekran albo bierze udział w wydarzeniu.

## 33. One day in UX, events in runtime

`Advance Day` jest prostym modelem mentalnym dla gracza.

Nie wolno przez to zamienić silnika w jeden kosztowny globalny daily loop aktualizujący każdą historyczną encję.

## 34. AI diversity is not random noise

Nie tworzymy pozornej różnorodności przez losowe decyzje.

Różnica zachowania ma wynikać z:

- traits,
- skills,
- knowledge,
- staff,
- organization identity,
- world context.

## 35. No universal manager meta by design

Nie projektujemy cechy lub archetypu jako zawsze najlepszego.

Jeżeli jedna kombinacja wygrywa niezależnie od epoki i rulesetu, headless balance lab ma to ujawnić.

## 36. Historical identity outlives active simulation state

Emerytowany zawodnik może stracić ciężki aktywny stan, ale jego ID nie wraca do puli i historia nie traci referencji.

---

## Career / organization anti-patterns

### Anti-pattern: PlayerTeam is a special species
Bad: separate `PlayerTeam` and `AITeam` market logic. Good: `Organization + ManagerCareer employment + DecisionAuthority`. The person managing the organization and the source of input are separate concepts.

### Anti-pattern: manager steals former employer data
A manager changing jobs must not inherit confidential scouting, medical, performance or negotiation data. Knowledge always has an owner.

### Anti-pattern: magical team-selection transition
The UI may simplify career transitions, but changing employer should be explainable by a vacancy/approach/application, interest, expectations, contract and employment change.



## 30. No artificial long-save economy balancing

Bad:

```text
if teamTooRich:
    applyHiddenLuxuryTax()
```

albo mechaniczna procentowa inflacja przez 120 lat tylko po to, aby pochłaniać gotówkę.

Good:
- dynamiczny sponsor market,
- naturalny popyt/podaż płac,
- role i ambicje zawodników,
- roster/ruleset constraints,
- workload,
- malejące marginalne korzyści organizacyjne,
- realne przepisy finansowe tylko wtedy, gdy aktywny ruleset je definiuje.

Domyślne pieniądze pozostają czytelne w real terms przez bardzo długie save'y.

## 31. AI uncertainty is a feature, not a handicap

AI nie dostaje true attributes rywali, żeby „grało lepiej”.

Bogatszy klub może mieć lepszy scouting i dane, ale nadal działa na evidence i interpretations. Różne organizacje mogą logicznie wycenić tego samego zawodnika inaczej.
