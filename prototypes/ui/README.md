# Peloton Manager — UI Prototype Lab

Ten katalog jest eksperymentalnym laboratorium interfejsu. Prototypy nie są
kanoniczną specyfikacją gry ani kodem przeznaczonym do produkcji.

## Zawartość

1. `01-initial-directions.html` — pierwsze cztery interpretacje dashboardu.
2. `02-anti-ai-reference-board.html` — tablica prawdziwych referencji i analiza
   elementów nadających interfejsom generyczny „AI look”.
3. `03-layout-structures.html` — Manager Home, Decision Workbench i Season Board.
4. `04-radical-skins.html` — Sports Broadcast, Classic PC Manager, Team Roadbook
   i Service Course Workstation na strukturze Manager Home.
5. `05-beauty-pass.html` — Cinematic Broadcast, Carbon Atelier i Grand Tour Studio.
6. `06-empty-dashboard.html` — pełnoekranowy, statyczny Manager Home (Opus 5; bez JS).

Otwórz `index.html`, aby przejść do każdego etapu z jednego miejsca.

## Aktualne ustalenia robocze

- Bazową strukturą dashboardu jest `Manager Home`.
- Kalendarz pojawia się po użyciu `Advance Day`, a nie jako stały panel HQ.
- A+, D+ i A×D pozostają kandydatami wizualnymi; żaden nie jest jeszcze finalny.

## Zasady brancha

- Branch służy wyłącznie do prototypów UI i powiązanych notatek.
- Dane widoczne w prototypach są przykładowe i nie ustanawiają kontraktów domenowych.
- Prototypy nie mogą wprowadzać `PlayerTeam`, player-only shortcuts ani dostępu UI/AI
  do ukrytej prawdy świata.
- Wybrany ekran lub zachowanie przenosimy do głównego projektu świadomie; nie
  mergujemy całego laboratorium tylko dlatego, że prototyp wygląda dobrze.
