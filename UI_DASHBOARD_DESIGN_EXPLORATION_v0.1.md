# Peloton Manager — eksploracja designu dashboardu

**Wersja:** 0.1

**Status:** EXPLORATION — dokument niekanoniczny

**Zakres:** dashboard trybu managerskiego i jego oprawa wizualna

Ten plik zapisuje przebieg eksploracji oraz komentarze właściciela projektu. Nie zastępuje `UI_SITEMAP_v0.1.md` i nie ustanawia finalnego design systemu.

## Sposób pracy

- Najpierw ustalaliśmy architekturę informacji w kierunku pełnego managera sportowego, inspirowanego m.in. *Football Manager*, *Motorsport Manager* i *Pro Cycling Manager*.
- Te same treści i funkcje były prezentowane w różnych układach, aby porównywać design, a nie zakres gry.
- Powstawały warianty bez wyspecjalizowanego skilla oraz warianty prowadzone przez różne skille projektowe.
- Po odrzuceniu pierwszych wersji jako zbyt „AI-owych” punktem odniesienia stały się prawdziwe gry managerskie zamiast typowych dashboardów SaaS.
- Testowaliśmy nierówne proporcje paneli, mocniejszą hierarchię, bardziej charakterystyczną typografię i motywy wynikające z kolarstwa.
- Każda iteracja była oceniana na działającym mockupie, a nie tylko na opisie.

## Testowane kierunki i komentarze właściciela

| Etap | Style / warianty | Co sprawdzaliśmy | Komentarz właściciela |
|---|---|---|---|
| Pierwsze dashboardy | Classic Manager, Race Radio, Director's Ledger, Modern Operations | Różne interpretacje dashboardu w ramach wariantu A | Kierunek ogólny trafny, ale wszystkie wersje wyglądały zbyt „AI-owo” i odrzucająco. |
| Warianty struktury | Manager Home, Decision Workbench, Season Board | Różne priorytety informacji i rytmy ekranu | Najlepszy był layout 1 — Manager Home. Kalendarz z wariantu 3 powinien pojawiać się dopiero po kliknięciu `Advance Day`. |
| Radykalne skórki layoutu 1 | A — Sports Broadcast, B — Classic PC Manager, C — Team Roadbook, D — Service Course Workstation | Jak daleko można zmienić charakter ekranu bez zmiany jego funkcji | Najlepsze były A i D, ale nadal zbyt kanciaste, mało piękne i trochę nudne. |
| Beauty pass | A+ — Cinematic Broadcast, D+ — Carbon Atelier, A×D — Grand Tour Studio | Miększa geometria, lepsza kompozycja, obraz, światło i bardziej dopracowany charakter | Wszystkie trzy są dobre w swoim własnym stylu. Nie został jeszcze wybrany jeden finalny kierunek. |

## Użyte skille i ich wpływ

| Skill | Zastosowanie | Wniosek |
|---|---|---|
| `superpowers:brainstorming` | Iteracyjne zawężanie kierunku i porównywanie działających wariantów | Pomógł oddzielić decyzje o strukturze od decyzji o stylu. |
| `frontend-design` | Budowanie wyraźnych kierunków artystycznych i elementów charakterystycznych | Dał większe zróżnicowanie, ale sam nie gwarantował uniknięcia „AI look”. |
| `hallmark` | Próba ograniczenia typowych oznak generycznego interfejsu AI | Użyteczny jako filtr, lecz powierzchowna zmiana stylu nadal dawała sztuczny efekt. |
| `product-ui-design` | Oparcie projektu na realnych produktach, ograniczenie kart KPI, gradientów, poświat i generycznych akcentów | Najbardziej pomógł odejść od dashboardu SaaS i zachować charakter prawdziwej gry managerskiej. |
| `ui-ux-pro-max` | Szersze warianty typografii, kolorów, układu i kontrola zasad UX | Przydatny jako baza i kontrola jakości; wymaga pilnowania, aby wynik nie wracał do estetyki aplikacji SaaS. |
| `skill-installer` | Wyszukanie i dodanie zewnętrznych skilli projektowych | Umożliwił porównanie podejścia bez skilla z kilkoma różnymi metodami projektowymi. |

## Aktualny kierunek roboczy

- Architektura informacji: wariant A, czyli pełny manager sportowy z rozbudowaną nawigacją.
- Bazowy układ dashboardu: layout 1 — `Manager Home`.
- Kalendarz nie zajmuje stale miejsca na dashboardzie; otwiera się jako podgląd po użyciu `Advance Day`.
- A+, D+ i A×D pozostają równorzędnymi kandydatami wizualnymi.
- Finalne kolory, fonty, kształty paneli i gęstość informacji nie są jeszcze zaakceptowaną decyzją.

## Następny krok

Na podstawie ustalonej struktury należy przygotować `UI_SITEMAP_v0.1.md`. Sitemap powinien opisywać ekrany, nawigację i zachowanie `Advance Day` bez przedwczesnego zamykania wyboru finalnej skórki wizualnej.
