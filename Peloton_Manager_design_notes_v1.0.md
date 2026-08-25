# Peloton Manager — design notes

**Nazwa robocza:** Peloton Manager  
**Wersja:** 1.0
**Status:** REVIEW — high-level design after architecture cleanup and Race Engine v0.1 integration

## 1. High concept

Peloton Manager to głęboki manager organizacji kolarskiej, bez wyścigów 3D i bez ręcznego sterowania zawodnikami w trakcie wyścigu.

Gracz jest managerem całego zespołu: zatrudnia ludzi, buduje strukturę organizacji, odpowiada za finanse, rekrutację, scouting, kontrakty, rozwój zawodników, sponsorów, kulturę zespołu i długoterminową strategię.

Główny hook długoterminowej wizji:

> **Wybierz moment w historii kolarstwa, zbuduj organizację i napisz tę historię od nowa.**

Docelowo gracz może rozpocząć save w różnych epokach. Od dnia rozpoczęcia kariery historia przestaje być skryptem i zaczyna żyć własnym życiem.

Pierwsza grywalna wersja nie próbuje jednak obsłużyć całej historii kolarstwa. Udowadnia core gry w jednym okresie startowym.

## 2. Główna fantazja gracza

Gracz nie jest dyrektorem sportowym wciskającym „atakuj teraz”. Gracz zatrudnia ludzi, ustala kierunek, przekazuje odpowiedzialność i później ocenia rezultaty.

To sztab prowadzi zawodników podczas wyścigu. Manager odpowiada za to, czy zatrudnił właściwego DS-a, trenera, lekarza, scoutów i ludzi od rekrutacji, czy dał im właściwe cele oraz czy organizacja działa w sposób, którego oczekuje.

Jeżeli wyścig został rozegrany źle, gracz analizuje przyczynę i reaguje organizacyjnie: zmienia ludzi, zakres obowiązków, strategię, priorytety albo strukturę zespołu.

Kluczowa zasada:

> **Delegation, not micromanagement.**

Delegacja nie może jednak oznaczać bezczynności. Gracz powinien mieć realny wpływ przed wydarzeniem, rozumieć działania sztabu w trakcie oraz móc wyciągać konsekwencje po fakcie.

## 3. Core gameplay loop

Podstawowa pętla gry:

> **informacja → ocena → priorytet → delegowanie → konsekwencja → nowa informacja**

Przykład:

1. Scout znajduje interesującego juniora.
2. Gracz decyduje, czy warto poświęcić zasoby na dokładniejsze obserwacje.
3. Scoutci dostarczają sprzeczne raporty.
4. Gracz nadaje sprawie priorytet i otwiera negocjacje.
5. Dział recruitmentu zostaje bardziej obciążony, więc inne sprawy zwalniają.
6. Zawodnik podpisuje kontrakt albo wybiera konkurencję.
7. Trener próbuje go rozwinąć.
8. Wyniki zmieniają wiedzę gracza o zawodniku.
9. Po kilku sezonach pojawia się nowy problem: rola, kontuzja, niezadowolenie, oferta rywala albo walka o przedłużenie kontraktu.

Gra powinna regularnie generować sytuacje, w których nie istnieje jedna oczywista optymalna decyzja.

## 4. Filozofia wyścigów

Nie będzie wyścigów 3D ani ręcznego sterowania zawodnikami w stylu „atakuj teraz”.

Wyścig ma być testem wcześniejszych decyzji organizacyjnych:

- jakości zawodników,
- selekcji składu,
- planu taktycznego,
- jakości i charakteru DS-a,
- przygotowania fizycznego,
- morale,
- zdrowia,
- informacji o rywalach,
- decyzji podejmowanych przez sztab w zmieniającej się sytuacji.

Gracz powinien czuć wpływ na wynik, ale nie pełną kontrolę nad nim.

## 5. Race briefing

Przed ważnym wyścigiem lub etapem gracz przygotowuje briefing dla DS-a i zespołu.

Nie powinien to być ekran kilkudziesięciu checkboxów. Briefing ma komunikować intencje, priorytety i granice decyzji.

Możliwe elementy:

- lider i hierarchia zespołu,
- role zawodników,
- główny cel sportowy,
- priorytet: etap / GC / punkty / koszulka / ekspozycja sponsora,
- główni rywale,
- poziom agresji,
- tolerancja ryzyka,
- zasady dotyczące ucieczek,
- kiedy wolno poświęcić pomocników,
- jak chronić lidera,
- jakie sytuacje uzasadniają odejście od planu.

Przykład:

> **Cel:** obrona GC  
> **Priorytet:** GC ponad zwycięstwem etapowym  
> **Lider:** A. Martin  
> **Główne zagrożenie:** J. Novak  
> **Podejście:** konserwatywne  
> **Zasada:** nie odpowiadać na wczesne ataki zawodników ze stratą powyżej 5 minut  
> **Freedom:** pomocnicy mogą wejść w ucieczkę tylko wtedy, gdy DS uzna ją za bezpieczną dla GC

DS interpretuje briefing według własnych cech, wiedzy i charakteru.

Gra powinna posiadać gotowe doctrines/presety, np.:

- Protect GC Leader,
- Sprint Stage,
- Aggressive Classics,
- Breakaway Hunting,
- Defend Jersey,
- Sponsor Exposure.

Gracz może używać presetu i zmieniać tylko najważniejsze elementy zamiast konfigurować każdy wyścig od zera.

## 6. Prezentacja wyścigu

Wyścigi mogą być prezentowane przez:

- tekstowy live ticker,
- timeline wydarzeń,
- profil trasy,
- prostą wizualizację 2D grup,
- wykresy strat,
- raporty DS-a,
- kluczowe momenty generowane przez silnik symulacji.

Przykład:

> 74 km — tworzy się ucieczka sześciu zawodników  
> 42 km — DS zwiększa tempo zespołu  
> 17 km — lider zostaje bez pomocników  
> 8 km — rywal atakuje  
> 7,4 km — DS decyduje się odpowiedzieć  
> 3 km — lider zgłasza słabe nogi  
> META — strata 41 sekund

Kluczowe zdarzenia powinny być powiązane z decyzjami, briefingiem i cechami ludzi, a nie wyglądać jak losowe komunikaty z generatora.

## 7. Race debrief

Po ważnym wyścigu gracz otrzymuje debrief pozwalający zrozumieć, co się wydarzyło.

Debrief może zawierać:

- wykonanie planu,
- najważniejsze odstępstwa od briefingu,
- decyzje DS-a,
- problemy zawodników,
- ocenę wykorzystania pomocników,
- niewykorzystane okazje,
- znane błędy,
- niepewne hipotezy sztabu,
- rekomendacje na kolejne wyścigi.

Gracz powinien móc zapytać o kluczowe sytuacje, np.:

> „Dlaczego drugi lider został użyty do pogoni 35 km przed metą?”

Odpowiedź zależy od DS-a. Może przyznać się do błędu, bronić decyzji, wskazać informacje dostępne w tamtym momencie albo otwarcie nie zgodzić się z managerem.

Debrief ma budować relację z personelem i pomagać odróżnić:

- zły plan,
- złą realizację,
- zły skład,
- słabą formę,
- pech,
- problem organizacyjny.

Gracz nie powinien dostawać idealnej prawdy o wszystkim. Sztab również może się mylić.

## 8. Start w różnych epokach

Docelowa wizja zakłada możliwość rozpoczęcia kariery w różnych momentach historii.

Przykładowe fantasy:

- lata 60. — próba przejęcia młodego Merckxa,
- lata 90. — budowa zespołu w zupełnie innym środowisku sportowym,
- 2000s — era Contadora, Boonena i Cancellary,
- 2010s — Froome, Sagan, Roglič,
- okolice 2018–2020 — polowanie na młodego Pogačara,
- współczesność — nowoczesny manager i wejście w proceduralną przyszłość.

Każda epoka powinna różnić się nie tylko nazwiskami, ale również strukturą zespołów, technologią, dostępem do danych, profesjonalizacją, zasadami, ekonomią i kulturą sportu.

Dlatego jest to cel długoterminowy, a nie wymaganie pierwszej wersji.

## 9. Historia jako punkt startowy, nie skrypt

Realni zawodnicy, jeżeli finalnie pozwolą na to kwestie prawne i licencyjne, mają archetyp i talent inspirowany rzeczywistością, ale ich kariery nie są z góry ustalone.

Możliwe światy:

- Roglič zostaje GOAT-em,
- Pogačar wygrywa jeszcze więcej niż w rzeczywistości,
- Pogačar doznaje wielkiej kontuzji i nigdy nie wraca na dawny poziom,
- wielki junior okazuje się flopem,
- historycznie drugoplanowy zawodnik eksploduje i zostaje legendą,
- jakaś historyczna rywalizacja nigdy nie powstaje.

Historia rzeczywista jest warunkiem początkowym symulacji. Po rozpoczęciu save'a nowa historia należy do silnika gry.

## 10. Tryby zmienności historii

### Historical

Talenty mocno trzymają się historycznego poziomu i archetypu. Wyniki mogą być inne przez transfery, kontuzje, rozwój i różne zespoły.

### Dynamic — domyślny

Historia jest silnym punktem wyjścia, ale rozwój pozostaje elastyczny. Pogačar najczęściej będzie wielkim talentem, lecz nie musi zostać dokładnie takim zawodnikiem jak w rzeczywistości.

### Chaos

Nazwiska i archetypy historyczne pozostają, ale potencjał i rozwój są znacznie bardziej losowe.

## 11. Potencjał i rozwój zawodnika

Nie powinno istnieć proste `Potential = 94`.

Rozwój ma być szczególnie elastyczny u młodych zawodników. Silnik może posiadać ukryte parametry opisujące m.in.:

- biologiczny / fizjologiczny ceiling,
- tempo dojrzewania,
- reakcję na trening,
- profesjonalizm,
- odporność psychiczną,
- regenerację,
- podatność na kontuzje,
- longevity,
- temperament.

Nie należy jednak tworzyć ogromnej liczby ukrytych zmiennych tylko dla realizmu. Każda ważna cecha powinna mieć zauważalne skutki w świecie gry.

Gracz nie musi znać prawdziwej przyczyny rozwoju lub stagnacji zawodnika, ale powinien otrzymywać informacje pozwalające budować sensowne hipotezy.

Przykłady:

> „Trener podejrzewa, że zawodnik bardzo wcześnie osiągnął fizyczną dojrzałość.”  
> „Od kilku miesięcy słabo reaguje na wysokie obciążenia.”  
> „Po kontuzji nie odzyskał wcześniejszej dynamiki.”  
> „Nasz performance staff nadal widzi przestrzeń do rozwoju.”

Ukryty model ma generować historię, nie sprawiać wrażenie arbitralnego RNG.

Rozwój nie musi być liniowy. Możliwi są early bloomers, late bloomers, nagłe skoki, stagnacja i regres.

## 12. Kontuzje i przeciążenia

Kontuzje powinny realnie zmieniać historię kariery.

Ciężka kontuzja może:

- spowolnić rozwój,
- trwale obniżyć niektóre zdolności,
- skrócić peak,
- zmienić specjalizację,
- zwiększyć ryzyko kolejnych urazów,
- wpłynąć na psychikę,
- doprowadzić do wcześniejszego końca kariery.

Gracz może też źle zarządzać obciążeniami: za dużo wyścigów, za mocny trening, brak odpoczynku, ignorowanie lekarza.

Nie ma jednak magicznego przycisku „przetrenuj zawodnika”. Skutki wynikają z całego systemu.

## 13. Fog of war

Jedną z najważniejszych opcji kariery ma być ukrywanie statystyk obcych zawodników.

### Full Attributes

Dokładne statystyki są widoczne.

### Estimated Attributes

Gracz widzi zakresy, np. `Climbing 72–78`. Lepszy scouting zawęża szacunek.

### Hidden Attributes

Brak surowych cyferek obcych zawodników. Decyzje zapadają na podstawie:

- wyników,
- raportów scoutów,
- mediów,
- reputacji,
- publicznej historii kontuzji,
- opinii trenerów,
- danych dostępnych klubowi,
- informacji od agenta.

Tryb Hidden Attributes powinien być projektowany jako pełnoprawny sposób gry, a nie utrudnienie polegające na zabraniu UI.

## 14. Scouting jako niepewna wiedza

Scout nie odkrywa prawdy. Scout ją szacuje.

Przykład:

> Bardzo mocny na długich podjazdach.  
> Słabszy technicznie na zjazdach.  
> Dobra wytrzymałość.  
> Może osiągnąć poziom lidera WorldTour.  
> Pewność oceny: niska.

Dwóch scoutów może mieć różne zdanie o tym samym zawodniku.

Scout może specjalizować się w regionach, juniorach, fizjologii, charakterze, sprintach, górach, klasykach albo analizie danych.

Raport powinien zawierać nie tylko ocenę, ale również jej podstawę i poziom pewności.

Nigdy nie musi pojawić się moment, w którym gra mówi graczowi: „teraz już znasz prawdziwy potential”.

## 15. Media i hype

Media są częścią rynku transferowego i percepcji zawodników, ale nie powinny być jednym z najcięższych systemów pierwszej wersji.

Mogą tworzyć narracje typu:

> „17-letni Jean Dupont to nowy Anquetil.”

Scout gracza może odpowiedzieć:

> „Nie podzielam zachwytu. Wyniki są dobre, ale konkurencja była słaba.”

Gracz musi zdecydować, komu ufać.

Media mogą wpływać na:

- reputację,
- cenę kontraktu,
- presję,
- morale,
- zainteresowanie innych zespołów,
- oczekiwania sponsora.

## 16. Dossier

Dossier jest **teczką rekrutacyjną**, nie paskiem `Interest 0–100`.

Może zawierać:

- historię obserwacji,
- raporty scoutów,
- wyniki,
- przewidywaną charakterystykę,
- ocenę charakteru,
- szacowane wymagania kontraktowe,
- relacje z agentem,
- zainteresowanie transferem,
- oczekiwaną rolę,
- informacje o konkurencji,
- preferencje geograficzne,
- szacowany rozwój,
- znane kontuzje,
- uwagi trenerów.

Im dłużej obserwujemy zawodnika, tym więcej wiemy, ale nigdy nie musimy dostać stuprocentowej pewności.

## 17. Brak hard gate'u negocjacji

Jeżeli świetny 19-latek eksploduje formą w lipcu, gracz nadal może do niego zadzwonić.

Kara za spóźnienie jest naturalna:

- inne zespoły negocjują od miesięcy,
- zawodnik lepiej zna ich projekty,
- agent może żądać większej pensji,
- mamy mniej informacji,
- możemy nie znać prawdziwych oczekiwań,
- zawodnik może być blisko podpisu gdzie indziej.

Nie powinno być blokady typu:

`Interest 90/100 — negotiations unavailable`

Gra pozwala podejmować ryzykowne i nieefektywne decyzje, ale jasno komunikuje ich konsekwencje.

## 18. Dynamiczne zainteresowanie zawodnika

Zawodnik ocenia ofertę na podstawie wielu czynników:

- prestiż i wyniki zespołu,
- pensja,
- długość kontraktu,
- rola,
- szanse na wielkie wyścigi,
- trenerzy i DS,
- obecni zawodnicy,
- narodowość/lokalizacja,
- historia klubu,
- sponsorzy,
- konkurencja w składzie,
- relacje z agentem,
- sytuacja w obecnym zespole.

Duże pieniądze nie gwarantują podpisu.

Zainteresowanie nie powinno być jedną magiczną liczbą. Gracz powinien widzieć konkretne powody przyciągające lub odpychające zawodnika, z odpowiednim poziomem niepewności.

## 19. Recruitment Department

Klub posiada rzeczywisty dział rekrutacji zamiast abstrakcyjnych punktów dossier.

Możliwe role:

- Head of Recruitment,
- scouts,
- contract manager,
- agent liaison,
- recruitment analyst,
- później elementy legal/finance.

Dział ma ograniczoną przepustowość wynikającą z ludzi, jakości procesów i infrastruktury.

## 20. Miękka przepustowość zamiast limitu slotów

Nie istnieje sztywny limit typu `6/6 negotiations`.

Gracz może rozpocząć więcej procesów, niż dział jest w stanie komfortowo obsłużyć. Gra nie blokuje działania, ale zwiększający się workload wywołuje naturalne konsekwencje.

Przykładowy panel:

> **Recruitment workload: 78%**  
> Status: HIGH  
> 6 active negotiations  
> 3 contract renewals  
> 4 priority scouting cases

Przy przeciążeniu mogą wystąpić:

- wolniejsze odpowiedzi,
- opóźnione raporty,
- gorsze przygotowanie negocjacji,
- niższa jakość informacji,
- mniej czasu dla agentów,
- niedopilnowane procesy niskiego priorytetu,
- większa szansa, że konkurencja wyprzedzi klub.

Nie powinno to działać jako ukryty procent kary dla wszystkiego.

## 21. Priorytety, prognozy i ostrzeżenia

Gracz ustala priorytety aktywnych procesów, np.:

1. przedłużenie lidera — critical,
2. generacyjny junior — critical,
3. zatrudnienie nowego DS-a — high,
4. transfer pomocnika — normal,
5. obserwacja kilku juniorów — low.

Jeżeli dział zostaje przeciążony, najpierw cierpią procesy o niższym priorytecie.

Przed otwarciem nowej sprawy UI pokazuje przewidywany wpływ:

> **Projected workload: 94%**  
> Expected consequences: slower responses, delayed scouting reports

Przy przekroczeniu możliwości działu:

> **Projected workload: 121%**  
> **CRITICAL OVERLOAD**  
> Several lower-priority processes are likely to be delayed.

Head of Recruitment może również ostrzec gracza:

> „Jeżeli otworzymy kolejne rozmowy, sugeruję zawiesić negocjacje z Martínezem albo obniżyć ich priorytet.”

Gracz może zignorować ostrzeżenie.

Dobra porażka ma wynikać z decyzji, a nie z informacji ukrytej przed graczem.

## 22. Staff w tym samym systemie rekrutacji

Zatrudnianie najważniejszych członków sztabu może wykorzystywać tę samą przepustowość działu recruitmentu co zawodnicy.

Nie wymaga to sztucznego slotu. Proces po prostu zajmuje czas i uwagę ludzi.

Przykład przeciążonego działu:

- przedłużenie dwóch zawodników,
- negocjacje z juniorem,
- poszukiwanie nowego DS-a,
- rozmowy z trenerem,
- scouting kilku innych kandydatów.

Nieudany proces również kosztuje realny czas organizacji.

## 23. Staff ma znaczenie, ale nie każdy jest osobną minigrą

Kluczowi pracownicy nie są tylko numerami typu `Coach 17`.

### DS

Możliwe cechy:

- agresywność,
- konserwatyzm,
- góry,
- sprint,
- klasyki,
- GC management,
- zarządzanie konfliktem,
- praca z młodzieżą,
- skłonność do ryzyka,
- skłonność do odchodzenia od briefingu.

### Head of Recruitment

- talent identification,
- market knowledge,
- agent relationships,
- negotiation efficiency,
- closing ability,
- regional knowledge,
- organizacja pracy.

### Trener

- specjalizacje,
- zarządzanie obciążeniami,
- rozwój juniorów,
- relacje interpersonalne,
- adaptacja do nowych metod.

### Lekarz / Head of Medical

- diagnoza,
- rehabilitacja,
- profilaktyka,
- poziom ostrożności,
- reputacja,
- etyka zawodowa.

Najważniejsi ludzie mogą mieć osobowość, historię, relacje i własne preferencje.

Nie każdy dietetyk, fizjolog, analityk czy mechanik musi być równie głęboko symulowaną postacią. Część organizacji może istnieć jako departamenty posiadające jakość, budżet, kulturę i przepustowość.

Celem jest sprawić, żeby zatrudnienie lub zwolnienie ważnego człowieka miało znaczenie, bez zamieniania gry w zarządzanie setką pracowników jeden po drugim.

## 24. Ewolucja organizacji przez epoki

Struktura zespołu w 1965 i 2025 nie może być identyczna.

W starszych epokach klub posiada prostszy sztab: manager, DS, mechanicy, soigneurs, lekarz, bardzo prosty scouting.

Z czasem pojawiają się:

- wyspecjalizowani trenerzy,
- fizjolodzy,
- dietetycy,
- analitycy danych,
- performance directors,
- specjaliści aero,
- laboratoria,
- pomiary mocy,
- nowoczesny scouting.

Profesjonalizacja kolarstwa jest częścią historii świata.

Rozwój organizacji przez epoki powinien zmieniać możliwości gracza, a nie tylko listę stanowisk.

## 25. Klub to nie sponsor

Sponsor nie jest tożsamością organizacji.

Klub może zmieniać nazwę, sponsora i barwy, ale jego historia pozostaje ciągła.

Gra powinna przechowywać:

- wszystkie historyczne nazwy,
- sponsorów,
- logotypy/barwy,
- managerów,
- zwycięstwa,
- rekordy,
- największe transfery,
- wychowanków,
- legendy organizacji,
- kryzysy i skandale,
- charakterystyczne epoki klubu.

## 26. Sponsorzy

Sponsorzy posiadają własne:

- cele,
- rynki,
- budżety,
- długość umów,
- tolerancję ryzyka,
- oczekiwania sportowe,
- oczekiwania dotyczące kalendarza,
- oczekiwania dotyczące reputacji i wizerunku.

Sponsor może preferować określone kraje, wyścigi, narodowości albo typ ekspozycji.

Zmiana sponsora wpływa na finansowanie, nazwę i priorytety zespołu, ale nie wymazuje historii organizacji.

## 27. Finanse jako źródło decyzji

Finanse nie powinny być wyłącznie ekranem budżetu.

Pieniądze mają zmuszać gracza do wyboru między konkurującymi priorytetami.

Przykłady:

- zatrzymać lidera czy rozbudować scouting,
- zatrudnić lepszego DS-a czy drugiego trenera,
- kupić drogiego zawodnika czy inwestować w rozwój juniorów,
- zaakceptować wymagającego sponsora za większy budżet,
- sprzedać przyszłą gwiazdę, żeby uratować płynność klubu,
- ograniczyć kalendarz albo strukturę organizacji po utracie sponsora.

Presja finansowa powinna generować historie i kompromisy, a nie tylko karę za przekroczenie liczby.

## 28. Doping, integralność i kultura organizacji

Jeżeli gra obejmuje historyczne i alternatywne kolarstwo, doping może istnieć jako część świata oraz jako opcjonalny system decyzji organizacyjnych.

Nie powinien być przedstawiany jako sklep z mechanicznymi buffami typu:

`EPO +12% endurance`

System powinien być abstrakcyjny i skupiony na konsekwencjach, presji i relacjach między ludźmi, a nie na odtwarzaniu realnych metod stosowania lub ukrywania dopingu.

Możliwe elementy systemu:

- kultura etyczna organizacji,
- podejście managera,
- podejście lekarzy i trenerów,
- indywidualna gotowość zawodników do ryzyka,
- presja wyniku,
- reputacja pracowników,
- kontrola antydopingowa zależna od epoki,
- możliwość przecieków,
- whistleblowerzy,
- śledztwa,
- ryzyko utraty sponsora,
- konsekwencje sportowe i historyczne.

Na poziomie designu gracz nie powinien mieć prostego przycisku pozwalającego „zdopingować nieświadomego zawodnika”. Jeżeli organizacja prowadzi nielegalny program, istotne jest kto o nim wie, kto bierze w nim udział, kto go toleruje i kto może odmówić.

Różni zawodnicy mogą reagować inaczej:

- zdecydowanie odmówić,
- odejść z zespołu,
- zgłosić sprawę,
- zaakceptować ryzyko,
- samemu naciskać na bardziej agresywne podejście.

Różni pracownicy również mogą posiadać własne granice.

Gracz może prowadzić całkowicie czystą organizację, tolerować podejrzane zachowania albo świadomie budować nielegalny system. Gra nie powinna moralizować przez arbitralne popupy, ale powinna symulować konsekwencje.

## 29. Skandale i konsekwencje po latach

Nie wszystkie konsekwencje muszą pojawić się natychmiast.

Przykładowa historia:

1. Zespół dominuje przez kilka sezonów.
2. Po zmianie pracownika pojawiają się przecieki.
3. Rozpoczyna się śledztwo.
4. Sponsor wycofuje się albo stawia ultimatum.
5. Dawne zwycięstwa otrzymują nowy kontekst lub zostają podważone zgodnie z zasadami danej epoki.
6. Reputacja byłych zawodników i pracowników ulega zmianie.
7. Klub przez kolejne lata próbuje odbudować wiarygodność.

Dzięki temu system dopingu jest powiązany z HISTORY i CONSEQUENCES, zamiast być jednorazowym bonusem do wyników.

Różne epoki mogą różnić się kulturą sportu, możliwościami kontroli, reputacyjnym kosztem oraz sposobem reagowania organizacji i mediów.

## 30. Kronika świata

Świat ma pamiętać swoją alternatywną historię.

W roku 2040 tabela zwycięzców Touru może wyglądać zupełnie inaczej niż w rzeczywistości.

Profile emerytowanych zawodników nadal istnieją i zawierają pełną historię kariery, zespoły, sukcesy, rywali, kontuzje i najważniejsze wydarzenia.

Gra może automatycznie identyfikować epoki klubu, np.:

- `The Merckx Era`,
- `The Wilderness Years`,
- `The Roglič Renaissance`.

Kronika nie powinna być wyłącznie tabelą wyników. Silnik historii powinien próbować rozpoznawać kontekst wydarzeń:

- pierwszy Tour po wielu latach porażek,
- powrót po wielkiej kontuzji,
- były zawodnik pokonujący dawny klub,
- dominację konkretnego trenera lub DS-a,
- wieloletnią rywalizację,
- rozpad dynastii,
- skandal zmieniający ocenę wcześniejszej epoki.

## 31. Emergent storytelling

To jeden z najważniejszych celów projektu.

Gra ma generować historie, których twórca nie napisał ręcznie:

- scout odradza zawodnika, który potem wygrywa sześć Tourów,
- tani junior staje się największą legendą klubu,
- cudowne dziecko okazuje się flopem,
- zawodnik odbudowuje karierę po ciężkiej kontuzji,
- zły trener niszczy generacyjne okno kariery,
- świetny DS regularnie łamie briefing, ale wygrywa,
- największy rywal przez dekadę blokuje sukcesy organizacji,
- klub dominuje przez lata, po czym rozpada się po utracie sponsora,
- były pracownik wywołuje skandal, który zmienia ocenę całej epoki zespołu.

Idealnie gracze mają dyskutować o swoich save'ach tak, jak kibice dyskutują o prawdziwej historii sportu.

Najlepsze historie powinny powstawać ze zderzenia kilku systemów, nie z ręcznie napisanych questów.

## 32. Anty-cheese

Gra powinna ograniczać gracza przez naturalne zasoby:

- pieniądze,
- czas,
- pracowników,
- reputację,
- informacje,
- workload i capacity działów,
- konkurencję,
- zainteresowanie zawodników,
- kalendarz,
- relacje,
- ryzyko.

Nie przez arbitralne mechaniki i ukryte pułapki UI.

Dobra porażka:

> „Źle zarządziłem organizacją.”

Zła porażka:

> „Nie kliknąłem właściwego checkboxa dwa miesiące temu.”

Gra może pozwalać graczowi robić głupie rzeczy. Musi jednak jasno pokazać, że są głupie albo ryzykowne, jeżeli jego pracownicy posiadają wiedzę pozwalającą to ocenić.

## 33. UI jako zasada projektowa

> **Gracz nie powinien walczyć z interfejsem. Powinien walczyć z problemami prowadzenia drużyny.**

Krytyczne informacje muszą być komunikowane jasno:

- wygasające kontrakty,
- sponsorzy,
- workload działów,
- ryzyko przeciążenia procesów,
- ryzyko kontuzji,
- konkurencyjne oferty,
- kryzysy finansowe,
- konflikty personalne,
- ważne odstępstwa od briefingu.

UI powinno pokazywać zarówno obecny stan, jak i przewidywany skutek ważnej decyzji.

Przykład:

> `Open Formal Negotiations`  
> Current workload: 91%  
> Projected workload: 108%  
> Expected impact: two low-priority scouting reports may be delayed.

Gracz może nadal kliknąć „potwierdź”.

## 34. Zarządzanie uwagą gracza

Przy dużej liczbie zawodników, pracowników i procesów gra nie może wymagać ręcznego sprawdzania każdego ekranu.

System powinien sam wynosić na powierzchnię sprawy wymagające decyzji.

Dobre powiadomienie mówi:

- co się wydarzyło,
- dlaczego to ważne,
- jaki jest deadline,
- kto rekomenduje działanie,
- jakie są możliwe konsekwencje braku reakcji.

Gracz powinien móc delegować rutynę i zachować uwagę dla decyzji strategicznych.

## 35. Komercyjny hook

Najmocniejsze pozycjonowanie:

> **Start with a tiny cycling team. Scout future legends. Build a dynasty. Rewrite cycling history.**

Alternatywa:

> **Build a team. Shape an era.**

Gra nie powinna być sprzedawana jako „PCM bez 3D”, ale jako manager organizacji połączony z symulatorem alternatywnej historii kolarstwa.

Najmocniejsze marketingowo historie powinny brzmieć jak opowieści z save'a, a nie lista funkcji.

## 36. Największe ryzyko: scope

Pełna historia kolarstwa, tysiące zawodników, wiele epok, zmieniające się zasady, sprzęt, wyścigi i struktury organizacyjne to gigantyczny zakres.

Największym zagrożeniem projektu jest zbudowanie świetnego symulatora bez skończonej grywalnej pętli.

Priorytetem jest udowodnienie:

- że codzienna praca managera jest interesująca,
- że delegacja nadal daje poczucie sprawczości,
- że uncertainty tworzy decyzje zamiast frustracji,
- że po kilku sezonach gracza naprawdę obchodzą zawodnicy i historia świata.

Pełna wieloepokowość jest celem rozwoju, nie warunkiem pierwszej wersji.

## 37. Vertical slice / MVP

Pierwszy prototyp powinien udowodnić jedną rzecz:

> **Czy samo zarządzanie organizacją i obserwowanie powstającej historii jest uzależniające?**

Minimalny zakres:

- jeden okres startowy,
- kilka poziomów zespołów,
- kilkaset zawodników,
- kilkadziesiąt wyścigów,
- kluczowy staff,
- uproszczone departamenty,
- scouting,
- dossier,
- negocjacje,
- workload recruitmentu,
- dynamiczny rozwój,
- kontuzje,
- briefing wyścigowy,
- prosty race engine,
- debrief,
- podstawowe finanse i sponsorzy,
- historia kariery zawodników,
- historia klubów,
- możliwość przesymulowania co najmniej 10 sezonów.

Pożądane później, ale niekoniecznie w pierwszym vertical slice:

- rozbudowane media,
- wiele epok,
- bardzo głęboka ewolucja struktur organizacji,
- pełny system dopingu i historycznych skandali,
- rozbudowane systemy prawne i regulacyjne,
- bardzo szeroka baza historyczna.

Bez 3D, bez ręcznego sterowania podczas wyścigu i bez próby odwzorowania całego stulecia w pierwszej wersji.

Najważniejsze testy:

> **Czy po kilku sezonach naprawdę obchodzi mnie, co stanie się z tymi zawodnikami?**

> **Czy kiedy przegrywam, rozumiem dlaczego i chcę coś zmienić?**

> **Czy mam ochotę rozegrać jeszcze kilka tygodni, żeby zobaczyć konsekwencje moich decyzji?**

Jeżeli tak, core działa.

## 38. Licencje i realna historia

Przed komercyjną premierą trzeba sprawdzić kwestie prawne dotyczące prawdziwych nazwisk, wizerunku, nazw ekip, sponsorów, logotypów i znaków towarowych wyścigów.

Nie należy zakładać, że można bez licencji sprzedawać grę z pełną bazą Merckx–Armstrong–Pogačar.

Potencjalna droga:

- oficjalna baza fikcyjna lub historycznie inspirowana,
- rozbudowany edytor,
- mod support,
- Steam Workshop,
- architektura umożliwiająca społeczności tworzenie historycznych baz.

Core gry musi działać również bez prawdziwych nazwisk.

Jeżeli anonimowy 19-letni zawodnik może zostać legendą, o której gracz pamięta po dwudziestu sezonach, system działa. Realne nazwiska wtedy wzmacniają fantasy, ale go nie tworzą.

## 39. Cztery główne filary

### HISTORY

Świat zachowuje trwałą, queryable pamięć historycznie znaczących outcomes, tożsamości, rekordów, sukcesów, porażek i skandali. Nie oznacza to przechowywania każdego transient ticka lub każdej starej obserwacji.

### UNCERTAINTY

Gracz nie zna całej prawdy. Talent, scouting, rozwój, zdrowie, decyzje ludzi i przyszłość są niepewne.

### DELEGATION

Gracz zatrudnia ludzi, ustala priorytety i buduje organizację zamiast mikrozarządzać każdy wyścig i każdy proces.

### CONSEQUENCES

Decyzje mają długoterminowe skutki dla zawodników, pracowników, finansów, reputacji i historii całego świata.

## 40. Jednozdaniowa definicja

**Peloton Manager to głęboki manager organizacji kolarskiej, w którym budujesz zespół przez dekady, delegujesz odpowiedzialność ludziom, podejmujesz decyzje przy niepełnej wiedzy i obserwujesz, jak zawodnicy, pracownicy, rywale oraz twoje własne wybory tworzą zupełnie nową historię sportu.**

## 41. Dossier jako sprawa rekrutacyjna, nie pasek postępu

Dossier nie jest walutą, punktami ani bramką odblokowującą negocjacje.

Jest zbiorem wiedzy organizacji o konkretnym zawodniku i rynku wokół niego.

Sprawa może zacząć się od:

- obserwacji scouta,
- wyników,
- rekomendacji trenera,
- informacji od agenta,
- zainteresowania managera,
- sygnału medialnego,
- zawodnika oferowanego klubowi,
- informacji o kończącym się kontrakcie.

Gracz może spróbować skontaktować się z agentem również późno. Brak wcześniejszego dossier nie blokuje rozmowy. Powoduje naturalne koszty: słabszą wiedzę, mniejszą relację, mniej czasu, wyższą cenę i ryzyko, że konkurencja jest już daleko w procesie.

## 42. Pierwszy kontakt z agentem

Jedną z podstawowych akcji rynku transferowego jest kontakt z agentem.

Celem nie musi od razu być formalna oferta. Gracz może próbować ustalić:

- czy zawodnik w ogóle rozważa zmianę,
- jakiej roli oczekuje,
- jakie ma priorytety sportowe,
- jakie są przybliżone wymagania finansowe,
- czy inne zespoły wykazują zainteresowanie,
- czy istnieją zaawansowane rozmowy z konkurencją,
- czy zawodnik chce poczekać z decyzją,
- co może zwiększyć zainteresowanie projektem.

Agent nie jest obiektywnym źródłem prawdy. Może:

- odmówić ujawnienia części informacji,
- blefować zainteresowaniem konkurencji,
- podbijać oczekiwania,
- próbować przyspieszyć decyzję,
- wykorzystywać relacje z klubem.

Informacje od agenta powinny mieć źródło i odpowiedni poziom wiarygodności.

## 43. AI i gracz grają na tych samych zasadach

To jest podstawowa zasada świata.

AI nie powinno posiadać osobnych magicznych akcji takich jak:

`StealPlayerFromHumanTeam()`

AI zespoły korzystają z tych samych podstawowych możliwości co człowiek:

- scoutują,
- kontaktują agentów,
- budują wiedzę,
- prowadzą dossier,
- negocjują,
- składają oferty,
- przedłużają kontrakty,
- zatrudniają i podkupują staff,
- tracą pracowników,
- walczą o sponsorów,
- wybierają partnerów sprzętowych,
- przeciążają departamenty,
- popełniają błędy oceny,
- przepłacają,
- rezygnują,
- reagują na sytuację finansową.

AI może próbować przejąć zawodnika lub pracownika gracza dlatego, że jego organizacja uznała go za atrakcyjny cel i rozpoczęła normalny proces rynkowy, nie dlatego, że gra chce sztucznie utrudnić save.

## 44. Wiedza należy do organizacji

Prawda o zawodniku należy do silnika symulacji.

Wiedza o zawodniku należy do konkretnej organizacji.

Nie istnieje globalne:

`Rider.Scouted = true`

Zamiast tego każda organizacja posiada własny stan wiedzy, np.:

- raporty,
- obserwacje,
- szacunki,
- historię kontaktów,
- znane oczekiwania,
- znane problemy zdrowotne,
- znane zainteresowanie rynku,
- poziom pewności,
- źródło informacji.

Team A może uważać zawodnika za przyszłego lidera Grand Touru. Team B może uważać go za solidnego pomocnika. Oba zespoły mogą posiadać sensowne przesłanki i oba mogą się mylić.

## 45. Prawda, obserwacja i interpretacja

Gra rozdziela trzy poziomy:

### Simulation Truth

Rzeczywisty stan wykorzystywany przez silnik.

### Evidence

To, co można zaobserwować:

- wyniki,
- czasy,
- dane treningowe,
- waty,
- tętno,
- zachowanie w wyścigach,
- testy,
- zdrowie,
- reakcję na obciążenia.

### Interpretation

Wniosek człowieka lub organizacji:

> „Prawdopodobnie się poprawił.”

> „Wyniki nie pokazują postępu.”

> „Podejrzewam, że osiągnął już sufit.”

Interpretacja może być błędna.

## 46. Wyniki są dowodem zdolności, nie zdolnością samą w sobie

To jedna z głównych zasad systemu zawodników.

Dobry rok nie musi oznaczać rozwoju.

Słaby rok nie musi oznaczać regresu.

Na wyniki wpływają m.in.:

- forma,
- zdrowie,
- kalendarz,
- rola,
- taktyka,
- jakość drużyny,
- konkurencja,
- profil tras,
- kraksy,
- pech,
- pogoda,
- decyzje DS-a.

Zawodnik może realnie stać się mocniejszy, a mimo to uzyskać podobne lub gorsze wyniki niż rok wcześniej.

Może również pozostać na podobnym poziomie, a dzięki sprzyjającym okolicznościom zrobić sezon życia.

## 47. Tryb None jako pełnoprawna gra w ocenę ludzi

W `Attribute Visibility = None` UI nie powinno próbować zastępować atrybutów znakami zapytania.

Obcy zawodnik nie posiada dla gracza ekranu typu:

`Climbing: ???`

Gracz ocenia go na podstawie evidence i interpretation.

Przykład:

Zawodnik poprawił się wewnętrznie przez dwa sezony, ale:

- ścigał się jako pomocnik,
- miał infekcję przed najważniejszym startem,
- dwa razy upadł,
- nie dostał własnej szansy.

Manager może dojść do błędnego wniosku, że zawodnik stagnuje.

Trener może powiedzieć:

> „Wyniki tego nie pokazują, ale na treningach wygląda wyraźnie mocniej niż rok temu.”

Scout może być bardziej sceptyczny.

To tworzy decyzję zamiast prostego odczytania cyfry.

## 48. Dane fizjologiczne są informacją, nie odpowiedzią

W nowoczesnej epoce własny zespół może posiadać dane takie jak:

- moc,
- historyczne testy,
- reakcję na trening,
- obciążenie,
- regenerację,
- wybrane dane zdrowotne.

Nie oznacza to poznania `Potential` ani pełnej prawdy o zawodniku.

Dostępność danych zależy od epoki, infrastruktury, staffu i relacji z zawodnikiem.

W starszych epokach ta sama decyzja może być oparta znacznie bardziej na obserwacji ludzi i wynikach.

## 49. Automatyzacja posiada właściciela i uzasadnienie

Ważna automatyczna decyzja powinna być przypisana do człowieka lub departamentu.

Nie:

`Auto-selection chose Pedro.`

Raczej:

> `DS Marc Dubois recommends Pedro as leader.`

I możliwość sprawdzenia:

- dlaczego,
- na podstawie jakich informacji,
- jaki priorytet zastosował,
- jaki jest poziom pewności.

Jeżeli decyzja była zła, gracz może ocenić konkretną osobę i jej proces decyzyjny.

## 50. Przyszły hotseat i multiplayer

Hotseat i online multiplayer nie są częścią MVP.

Architektura i design nie powinny jednak zakładać, że w świecie istnieje dokładnie jedna organizacja sterowana przez człowieka.

Potencjalnie kilka organizacji może posiadać ludzkich managerów.

Wiedza, dossier, negocjacje i prywatne informacje muszą być przypisane do organizacji, dzięki czemu Team A nie widzi automatycznie informacji Team B.

Hotseat jest naturalnym przyszłym rozszerzeniem świata symulowanego według wspólnych zasad.

Online multiplayer jest znacznie późniejszym problemem technicznym, zwłaszcza podczas live race, i nie powinien zwiększać scope'u MVP.

## 51. Symetria świata jako test designu

Dla każdej ważnej mechaniki należy zadać pytanie:

> **Co się dzieje, jeżeli dokładnie tę samą akcję wykonuje organizacja AI przeciwko graczowi albo przeciwko innemu AI?**

Jeżeli odpowiedź wymaga osobnego sztucznego systemu, mechanika może być źle zaprojektowana.

Wyjątki są dopuszczalne ze względów wydajnościowych lub UX, ale muszą zachowywać ten sam sens ekonomiczny i strategiczny.

## 52. Difficulty comes from decisions, not obscurity

Gra nie powinna budować trudności przez ukrywanie zasad interfejsu, nieczytelne budżety, przypadkowe checkboxy i niewidoczne hard gate'y.

Dobre pytanie managerskie:

> „Czy wydajemy 200k na lidera, czy inwestujemy w organizację?”

Złe pytanie managerskie:

> „Czy zauważyłem checkbox, pasek i limit, którego gra wcześniej dobrze nie wyjaśniła?”

Trudność powinna wynikać z konkurujących priorytetów, niepełnej wiedzy i konsekwencji.

## 53. Advance Day i żyjący świat

Podstawowym przyciskiem postępu jest `ADVANCE DAY`.

Jeden klik przesuwa świat o jeden dzień, przetwarzając w tle wszystkie wydarzenia tego dnia.

Gracz nie jest centrum symulacji:

- wyścigi bez jego zespołu nadal się odbywają,
- AI skautuje,
- AI negocjuje,
- staff zmienia pracodawców,
- sponsorzy podejmują decyzje,
- zawodnicy rozwijają się i tracą formę,
- rynek reaguje na wyniki.

Advance może zatrzymać się wewnątrz dnia, jeśli wydarzenie wymaga decyzji człowieka.

## 54. Managerowie AI jako prawdziwi uczestnicy rynku

AI nie posiada jednego „algorytmu utrudniającego grę”.

Managerowie różnią się:

- cechami,
- umiejętnościami,
- wiedzą,
- staffem,
- pamięcią doświadczeń,
- celami organizacji,
- rulesetem i epoką, w której działają.

Dlatego liczba kombinacji stylów ma być bardzo duża bez pisania dziesięciu sztywnych archetypów.

## 55. Styl managera wynika z cech

Przykładowe osie:

- risk tolerance,
- youth trust,
- leader loyalty,
- form sensitivity,
- reputation bias,
- data reliance,
- staff trust,
- sponsor priority,
- long-term planning,
- transfer aggression,
- financial discipline,
- innovation openness.

Nie każda z tych cech musi wejść do MVP.

Każda wdrożona cecha musi zmieniać konkretne decyzje.

## 56. Manager skill ≠ manager preference

Agresywny manager nie jest automatycznie lepszy ani gorszy.

Preference mówi, **co lubi robić**.

Skill mówi, **jak dobrze interpretuje sytuację i wykonuje swój plan**.

Dzięki temu dwóch managerów o podobnym stylu może osiągać zupełnie inne wyniki.

## 57. Epoka może zmieniać wartość cech managera

Cechy nie mają stałego bonusu niezależnego od świata.

Zmieniające się:

- zasady,
- sprzęt,
- dostęp do danych,
- medycyna,
- struktura organizacji,
- ekonomia,
- kalendarz

mogą sprawić, że inne podejście stanie się bardziej skuteczne.

To ma wynikać z systemów, nie z tabeli `year → trait bonus`.

## 58. Managerowie mogą się adaptować

Manager może zmieniać konkretne heurystyki na podstawie doświadczeń, zachowując główną osobowość.

Przykład:

manager nadal jest ostrożny, ale po kilku sezonach może zacząć mocniej ufać młodzieży, jeżeli jego wcześniejsze decyzje przyniosły dobre rezultaty.

Adaptacja ma być powolna i wyjaśnialna.

## 59. Debugowalne AI

W development buildzie można podejrzeć organizację AI i odpowiedzieć:

- dlaczego podpisała zawodnika,
- dlaczego wybrała lidera,
- dlaczego zmieniła trenera,
- dlaczego pojechała dany wyścig,
- jakie informacje posiadała.

Celem jest naprawianie przyczyny nielogicznego zachowania zamiast dodawania bandaży typu `if six sprinters then stop buying sprinters`.

## 60. 100-letnie symulacje jako laboratorium balansu

Długie symulacje służą nie tylko wydajności.

Mierzymy również:

- które cechy managerów korelują z sukcesem,
- które są zbyt mocne,
- które nie wpływają na wynik,
- które kombinacje tworzą dominującą metę,
- jak zmienia się ich wartość w różnych rulesetach i epokach.

Nie oczekujemy identycznego win rate każdej cechy.

Oczekujemy, że świat nie posiada jednej uniwersalnej kombinacji zawsze wygrywającej.

## 61. Historia managerów

Managerowie i DS-y mogą tworzyć własne historyczne epoki.

Przykłady:

- manager dominujący przez dekadę,
- specjalista świetny w jednej epoce i słabszy po reformach,
- były zawodnik zostający genialnym DS-em,
- pracownik podkupiony przez rywala, który później buduje konkurencyjną dynastię.

Historia organizacji ma pamiętać również ludzi, którzy ją budowali.

## 62. Długie save'y i tożsamość historyczna

ID ludzi i innych trwałych bytów nie są ponownie używane.

Emerytowany zawodnik może zostać skompaktowany z aktywnego stanu do archiwum, ale jego historyczna tożsamość i referencje pozostają.

100-letni save jest podstawowym przypadkiem testowym, nie egzotycznym edge case'em.

---

## Manager career and changing organizations

The human player represents a manager career, not a permanent team identity.

> **Human player identity belongs to the manager career, never to an organization.**

A career may include remaining with one organization, accepting another job, applying for a vacancy, being dismissed, unemployment and returning to management.

When the human manager changes organization, the previous organization remains alive and receives AI control, while the new organization receives human control. No gameplay subsystem changes rules because the controller changed.

### Organization knowledge vs personal knowledge

Private scouting data, medical data, internal performance tests, staff reports and negotiation intelligence belong to the organization. They do not magically travel with the manager.

Required separation:

```text
OrganizationKnowledge
PersonalKnowledge / Relationships
```

Personal memory may retain broad familiarity, relationships, prior collaboration and qualitative opinions. It does not retain the former employer's confidential measurements, medical records, exact internal estimates or private offers.

### Manager job market

The same world should support renewals, approaches, applications, dismissals, unemployment, contract negotiations and organization expectations. AI managers participate in the same labor market. Manager reputation should be multidimensional rather than a single overall score.



---

## Sponsor market, real-value money i długie save'y

Długoterminowego balansu finansowego nie bronimy globalnym `LuxuryTax` ani automatycznym pompowaniem wszystkich cen z każdym rokiem.

Domyślna gospodarka operuje na stabilnym real-value money poziomu scenariusza. To pozwala, aby kontrakt elitarnego zawodnika w 2150 nadal był czytelny, zamiast kosztować miliardy wyłącznie przez procent składany.

Rynek sponsorów jest dynamiczny i kontekstowy.

Może zmieniać się przez:
- popularność kolarstwa w kraju/regionie,
- sukces lokalnych zawodników,
- liczbę i siłę lokalnych organizacji,
- reputację sportu,
- skandale,
- media exposure,
- branże zainteresowane sponsoringiem,
- regulacje i strukturę danej epoki.

W efekcie w różnych dekadach inne kraje i typy organizacji mogą mieć łatwiejszy lub trudniejszy dostęp do kapitału.

To nie jest catch-up mechanic przeciwko bogatym. To część symulacji świata.

AI zna rynek tylko przez informacje dostępne organizacji i podejmuje te same decyzje sponsorskie co gracz.

## AI recruitment diversity through uncertainty

AI nie otrzymuje `true attributes` ani `true potential` konkurencyjnych zawodników.

Ocena opiera się na tych samych klasach evidence co u gracza:
- wyniki i kontekst wyników,
- scouting,
- staff opinions,
- agent information,
- public reputation,
- dane wewnętrzne własnych zawodników,
- telemetrykę dostępną w danej epoce,
- relacje,
- uncertainty.

Bogatsza organizacja może kupić lepszą informację, ale nie prawdę.

Dzięki temu dwa silne zespoły mogą logicznie ocenić tego samego zawodnika inaczej. Jeden przepłaci za sezon życia, drugi zignoruje przyszłego mistrza, trzeci zaufa scoutowi mimo słabych wyników.

Ta niepewność jest naturalnym źródłem różnorodności rynku i długich save'ów.

## RaceLive scope

`RaceLive` obejmuje pojedynczy etap / dzień wyścigowy. Etapówka wraca pomiędzy etapami do normalnej kariery, gdzie można zapisać grę, analizować wyniki i wykonać działania dozwolone przez kalendarz.

---

## Race Engine direction v0.1

Source design: `RACE_ENGINE_DESIGN_v0.1.md`.

Locked high-level behavior:

> A rider does not drop because a generic stamina bar reaches zero. The race situation requires power; the rider has a changing capability; failure to match speed creates a gap; loss of shelter can make the problem worse.

First race prototype focuses on:
- CP,
- W',
- Pmax,
- basic durability,
- body/system mass,
- CdA,
- Crr,
- wind/gradient,
- groups,
- positioning,
- drafting,
- dynamic gaps,
- basic tactical intent.

Glycogen, fueling, thermal load, hydration, detailed cobbles, sleep and illness are deliberately deferred until the small race engine proves engaging.

Race archetypes should emerge from the underlying model. "Climber", "puncheur", "diesel", "sprinter" and similar labels are descriptions of behavior/performance rather than primary scripted result generators.

Race briefing is a conditional strategic policy. Human/AI/DS decisions operate only on information available to the actor. Internal values such as true W' balance are simulation truth, not direct manager inputs.

Key prototype scenarios:
- sustained mountain pacing,
- repeated attacks,
- crosswind split,
- rider losing and potentially closing a small gap,
- teams deciding who pays for a chase,
- identical physics with different briefings producing different tactical behavior.



---

## Race Spy / race explainability

Race development requires a developer-only Race Spy.

It can answer:
- why a rider dropped,
- why an attack failed,
- why a team chased or waited,
- what the DS actually knew,
- what the DS believed,
- which options were considered,
- which briefing rule was active,
- why the player was or was not consulted,
- whether AI illegally used hidden truth.

Race Spy may see Simulation Truth for debugging, but normal RaceLive and AI never receive that omniscience.

Structured trace is preferred over prose-first logging so suspicious races can be reproduced and analyzed in headless tests.


---

## World Spy / explainability everywhere

Race Spy generalizes into `World Spy`.

Important automated decisions across the game should be diagnosable through the same structure:

```text
actor
trigger
knowledge
interpretation
goals
constraints
options
selected action
reasons
commands
outcome
```

Initial applications:
- race tactics,
- rider recruitment,
- contract offers,
- staff hiring,
- manager hiring/firing,
- sponsor selection,
- calendar decisions,
- rider selection,
- training recommendations,
- finance,
- equipment/R&D,
- scouting interpretation,
- organization strategy.

This is developer infrastructure first.

Player-facing `Why?` uses the same structured reasoning where useful but filters everything through legal `AccessContext`.


---

## AI-assisted development workflow

Because the owner is not expected to review implementation code line-by-line, the project uses small scoped tasks, clean Git history, no silent design drift, regression tests, World Spy diagnostics, contract-focused documentation, concise owner-facing completion reports and a CODEBASE_MAP. Full contract: `AI_DEVELOPMENT_RULES_v0.1.md`.
