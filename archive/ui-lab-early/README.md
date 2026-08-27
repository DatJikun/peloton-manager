# Peloton Manager — UI Prototype Lab (archiwum)

**Status:** ARCHIVED — wczesne ekrany 01–08. Aktualne biurko: `prototypes/ui/`.

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
7. `07-forever-impeccable.html` — źródła: Forever Infinite (składanki) i Impeccable slop (zakaz AI-looku).
8. `08-desk-race-next.html` — pełnoekranowe biurko z kalendarzem i skrzynką; `Advance Day` zmienia się w `Race next`, które otwiera osobne przygotowanie składu i celu bez uruchamiania wyścigu.
9. `CODEX_BRIEF.md` — brief laboratorium 08 (biurko + Race next; skrzynka nie odpala wyścigu).

Otwórz `index.html`, aby przejść do każdego etapu z jednego miejsca.

## Aktualne ustalenia robocze

- Bazową strukturą jest biurko managera w konkretnym dniu, nie KPI dashboard.
- Główny guzik: **Advance Day**; na dniu wyścigu ten sam guzik = **Race next** i wchodzi w prep (`D-034`).
- Skrzynka jest kolejką spraw i nie otwiera wyścigu.
- Nowe kafelki UI biorą się z Forever Infinite; Impeccable slop jest checklistą odrzuceń.
- A+, D+ i A×D z etapu 05 pozostają kandydatami palety; żaden nie jest finalny.
- Codex złożył etap 08 z czterech lekkich wzorców Forever Infinite, po czym usunął ich pętle demonstracyjne, neonowe kolory i inne elementy wskazane przez checklistę Impeccable.

## Zasady brancha

- Branch służy wyłącznie do prototypów UI i powiązanych notatek.
- Dane widoczne w prototypach są przykładowe i nie ustanawiają kontraktów domenowych.
- Prototypy nie mogą wprowadzać `PlayerTeam`, player-only shortcuts ani dostępu UI/AI
  do ukrytej prawdy świata.
- Wybrany ekran lub zachowanie przenosimy do głównego projektu świadomie; nie
  mergujemy całego laboratorium tylko dlatego, że prototyp wygląda dobrze.
