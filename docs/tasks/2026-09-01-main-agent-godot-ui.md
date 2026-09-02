# Zadania — agent główny: UI Godota dociągnięte do wzorca HTML (plus domknięcie D-054)

**Dla:** agenta głównego tej sesji (właściciel 2026-09-01: „chcę, żebyś ty zajął się UI”). Agent główny kończy też **D-054** (Composer już koduje na `cursor/d054-position-selection-9a2c`): przegląd → gate → merge → CI. Agent nocny ma D-055, md i release: `2026-09-01-night-agent-engine-release.md`.  
**Cel:** powłoka kariery w Godocie (`src/Peloton.Client.Godot`, scena `CareerShell.tscn`) ma wyglądać jak `peloton-manager-full-ui-poc-v3.html` — właściciel ocenił, że dziś jest „trochę średnia” w porównaniu z HTML-em, który jest **celem do osiągnięcia**.

## Przeczytaj najpierw (w tej kolejności)
1. `AGENTS.md` (role D-035: kod pisze **Composer 2.5** — `model: composer-2.5` na każdym `Task` kodującym; merge D-045; komunikacja bez maili)
2. `HANDOFF.md` → sekcja „Gdzie jest gra” i „Things the next AI must NOT do”
3. `HTML_UI_LAB.md` i sam plik `peloton-manager-full-ui-poc-v3.html` (otwórz go, przeczytaj CSS: kolory, fonty, siatka, odstępy, komponenty)
4. `CAREER_SHELL_DATES_AND_LOOK_v0.1.md` (D-052 — co już miało być zgodne z HTML)
5. `src/Peloton.Client.Godot/LookChrome.cs`, `CareerShellScreen.cs`, `CareerShellViews.cs`, `CareerLookCatalog.cs`, `CareerShell.tscn`
6. `tests/Peloton.Client.Godot.Tests/` (host testowany bez edytora Godota)
7. `.cursor/skills/peloton-avatars/SKILL.md` **tylko** jeśli ruszasz awatary na kartach (geometryczne awatary D-052 zostają, nie wprowadzaj nowego systemu portretów)

## Granice (ważne — równolegle pracuje agent nocny nad D-055)
- **Wolno edytować:** `src/Peloton.Client.Godot/**` (UI) oraz — tylko dla domknięcia D-054 — pliki z gałęzi `cursor/d054-position-selection-9a2c`, `tests/Peloton.Client.Godot.Tests/**`, ten plik (sekcja „Postęp”), jeden wiersz Godota w `CODEBASE_MAP.md`, **jedna** linia w `HANDOFF.md` → „Recent owner decisions” po wylądowaniu.
- **Nie wolno:** `src/Peloton.Simulation/**`, `src/Peloton.Application/**` (chyba że brakuje Query czysto prezentacyjnego — wtedy osobny, mały commit i wyraźna notatka), `src/Peloton.Domain/**`, `src/Peloton.Persistence/**`, `content/**`, `tests/Peloton.Application.Tests/**`, `RACE_*.md`, `DECISIONS.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`. Agent nocny ląduje tam D-055 (schema 10); wyjątek: agent główny domyka D-054 w silniku, bo Composer już to koduje.
- Przed każdym merge: `git fetch origin main` i rebase/merge swojej gałęzi na aktualny `main`; potem gate.
- Nie commituj `playtest/*.zip` (D-053). Nie otwieraj wielu PR-ów; jedna gałąź `cursor/godot-ui-html-parity-<suffix>`, po zielonym gate merge do `main`.

## Zasady produktu, których nie łamiemy
- UI to **prezentacja**: tylko Commands + Queries z `GameApplication`. Zero logiki świata, zero liczenia kasy/formy/wyników w Godocie. OVR/POT z `CareerLookCatalog` to rysunek, nie prawda.
- Ścieżka dnia wyścigu: **Advance Day / Race next → symulacja → tabela wyniku** (D-043). Watch Race to opcja, **domyślnie WYŁ**; nie robić z filmu głównego ekranu. Nie odbudowywać Career Hub (D-048).
- Sztab / sponsorzy / skauting zostają katalogiem wyglądu z toastem `Jeszcze nie w tej wersji.` — mogą wyglądać lepiej, nie mogą udawać działających.
- Polski w UI: **Bruk** (nie „kocie łby”), daty kalendarzowe (nigdy „dzień N”).
- Nie zmieniać schematu save, nie ruszać `RaceSession`, nie dotykać RNG.

## Co zrobić (krok po kroku)
1. **Lista luk (gap list).** Composer (subagent typu ogólnego, `model: composer-2.5`) porównuje ekran po ekranie HTML vs Godot i zapisuje w tym pliku tabelę: ekran | co jest w HTML | co jest w Godocie | co zmienić. Ekrany: Nowa gra (wybór klubu), Plan sezonu, Biurko, Kalendarz, Skład (karta kolarza, sortowanie), Rynek, Finanse, Inbox, Przygotowanie wyścigu, Wynik/tabela, Ustawienia. Uwzględnić: paletę, typografię (rozmiary, wagi, wersaliki), szynę boczną i herb, nagłówki sekcji, karty/kafle, tabele (wyrównanie liczb, zebra, nagłówki), przyciski (główny/drugorzędny, stany hover/disabled), odstępy i siatkę, puste stany, toasty, responsywność okna (min. 1280×720 i 1920×1080).
2. **Wspólny chrome najpierw.** Poprawki w `LookChrome.cs` (theme, kolory, fonty, spacing, style tabel i przycisków) — jedna zmiana naprawia wiele ekranów. Potem ekrany w kolejności: Biurko → Skład → Wynik → Kalendarz → Finanse → Rynek → Plan sezonu → Nowa gra → Inbox → Ustawienia.
3. **Weryfikacja wizualna.** Spróbuj ściągnąć Godota 4.4.1 .NET na VM (`tools/pack-windows-playtest.sh` pokazuje oczekiwaną ścieżkę `/tmp/godot-setup/…`; URL: `https://github.com/godotengine/godot/releases/download/4.4.1-stable/Godot_v4.4.1-stable_mono_linux_x86_64.zip`). Jeśli jest wyświetlacz albo `xvfb-run`, uruchom projekt i zrób zrzuty każdego ekranu do `/opt/cursor/artifacts/godot-ui/` (PNG), obok zrzutu tego samego ekranu z HTML (Playwright/Chromium headless). Zrzuty załącz w raporcie końcowym. Jeśli renderowanie nie jest możliwe na VM — napisz to wprost i pokaż przynajmniej `dotnet build` projektu Godota + zielone `Peloton.Client.Godot.Tests`.
4. **Gate** (z `HANDOFF.md`, wszystkie komendy — nie tylko testy Godota): `dotnet format --verify-no-changes`, `dotnet build PelotonManager.sln`, `dotnet test PelotonManager.sln`, komendy SimRunnera. Zielone → merge do `main` → `git push origin main` → sprawdź `gh run list --workflow gate`.
5. **Notatki:** wypełnij sekcję „Postęp” niżej; jeden wiersz Godota w `CODEBASE_MAP.md`; jedna linia w `HANDOFF.md` „Recent owner decisions”: `2026-09-02 — Godot UI dociągnięte do HTML v3 (agent nocny): …`.
6. **Raport w czacie** dla właściciela po polsku: co zmienione ekran po ekranie, zrzuty przed/po, co zostało, czy gate i CI zielone. Bez maila, bez `@mention`.

## Kryteria „zrobione”
- Właściciel po otwarciu Godota widzi ten sam język wizualny co w HTML v3: ta sama paleta, ta sama hierarchia typograficzna, ta sama szyna i herb, te same proporcje kart i tabel.
- Żadna zmiana nie przesunęła logiki do UI; testy hosta przechodzą; gate i CI zielone; zmiana jest na `main`.
- Watch nadal WYŁ domyślnie; katalog wyglądu nadal oznaczony jako niedziałający.

## Postęp (wypełnia agent główny)
- [x] D-054: przegląd raportu Composera, gate, merge do `main`, CI zielone (`900b280`)
- [x] gap list (tabela poniżej)
- [x] LookChrome / theme (skala HTML, stretch canvas_items, Pill, SectionBar, Table, NavItem, Crest, ManagerFoot)
- [x] Biurko
- [x] Skład
- [x] Wynik (+ naprawa SimulateRace na WT: dzisiejszy wyścig zamiast id prototypu)
- [x] Kalendarz
- [x] Finanse (bez zmyślonej księgi/donuta — brak Query)
- [x] Rynek
- [ ] Plan sezonu / Nowa gra (dziedziczą chrome; osobnego przebiegu pikseli nie było — zostaje na później, jeśli właściciel powie)
- [ ] Inbox / Ustawienia (Inbox na Biurku zrobiony; Ustawienia = FILM WŁ/WYŁ, bez przebiegu HTML)
- [x] zrzuty ekranów: `/opt/cursor/artifacts/godot-ui/after-*.png` (Godot 4.4.1 .NET na VM, Xvfb)
- [x] Przekazanie sesji: `docs/tasks/2026-09-02-session-handoff-main-agent.md`
- [x] gate + merge do `main` + CI zielone (`85b4257`; `gh run` `33642782814` success)
- [x] `CODEBASE_MAP.md` wiersz Godota, `HANDOFF.md` jedna linia (Recent owner decisions, 2026-09-02)

Zrzuty źródłowe (2026-09-02, Godot 4.4.1 .NET na VM, fullscreen 1920×1200; HTML w Chrome headless 1600×900): `/opt/cursor/artifacts/godot-ui/godot-0*.png` vs `html-*.png`. Narzędzia: `/tmp/gshot.sh out.png` (zrzut ekranu Godota), `xdotool mousemove X Y click 1` (klik), sesja tmux `godot-shell`.

| Ekran | HTML v3 | Godot dziś | Zmiana |
|---|---|---|---|
| Szyna boczna | Herb (kwadrat z ukośnym podziałem) + nazwa klubu Anton + „PROTEAM · 2026” meta; pozycje nawigacji **z ikonami** (20 px stroke), wyrównane do lewej, aktywna = czarne tło; „ZARZĄDZANIE” jako meta 9 px, wersaliki, rozstrzelone; na dole „Ustawienia” z ikoną + **karta managera** (inicjały w ramce, „M. Nowak”, „profil managera · kariera”) | Sam tekst klubu, brak herbu; pozycje wyśrodkowane bez ikon; „Ustawienia”/„Karta managera” jako gołe napisy | `LookChrome.NavItem(icon, text, badge, active)`, `LookIcons` rysowane w `_Draw` (11 ikon), `Crest(club)`, `ManagerFoot(name)`; wyrównanie do lewej, padding 10/12 |
| Nagłówek dnia | „11 MAR” Anton na niebieskim + „ŚR” Anton na czarnym; obok „Środa / 11 marca 2026 · tydzień 11”; pigułki „ROK 2026”, „WYŚCIG ZA 1 DZIEŃ” — 2 px ramka, bold 13 px, **wersaliki, rozstrzelone**, liczba na niebiesko | Pigułki małe, normalna wielkość liter, bez rozstrzelenia; data dnia jako zwykły tekst | `LookChrome.Pill(text, accentPart)` z FontVariation `SpacingGlyph`; większe fonty (Anton 26, meta 10) |
| Pasek sekcji (niebieski) | Meta 10 px bold, wersaliki, rozstrzelone + **link po prawej** („PEŁNY KALENDARZ ›”) | Anton mały; link jako osobny pełnoszerokościowy przycisk wewnątrz karty | `SectionBar(title, linkText?, onLink?)`; usunąć przyciski „pełny kalendarz ›” z treści kart |
| Tabele (Skład, Rynek, ostatnie wyniki) | Płaska tabela: nagłówek szary meta 10 px wersaliki z strzałką sortowania (niebieska), wiersze ~34 px z linią włoskową, liczby bold w kolumnie kluczowej, wyrównanie liczb do prawej/środka, wybrany wiersz **czarny** z jasnym tekstem, chipy statusu | Każdy wiersz to obramowana karta z awatarem, ~80 px; nagłówki kolumn jako obramowane przyciski — ciężko i mało wierszy na ekranie | `LookChrome.Table(columns, rows, selectedIndex, onSort, onSelect)` na `GridContainer`; usunąć awatary z listy (zostają na karcie zawodnika); mini „micro” podpis (kraj) pod nazwiskiem |
| Biurko | Lista wyścigów: **czarny chip daty** („CZW 12.03”) + nazwa bold + meta wersaliki („1.PRO · 177 KM · JUTRO”) + mini-profil trasy; panel Wyścig: tytuł Anton 30 px + kategoria niebieska, rząd meta (DATA / DYSTANS / TRASA), wykres profilu, wiersze POGODA / SKŁAD, chipy; Inbox: numerowane wiersze, pilne w czerwonej ramce, data po prawej | Data jako zwykły tekst, brak chipa i profilu; panel Wyścig: tytuł + chip „scheduled” + pełnoszerokościowy przycisk; Inbox „Brak spraw” | `DateChip(date)`, meta pod nazwą, `LookCharts` mini-profil (już jest w Watch — użyć); panel Wyścig: rząd meta + przycisk „OTWÓRZ WYŚCIG ›” jako pigułka, nie belka; puste Inbox jako pusty stan w stylu HTML |
| Skład – karta zawodnika | Awatar 110×130, nazwisko Anton 30 px, meta „POL · 27 LAT · PUNCHEUR”, tabela OVR/POT, Forma, Wartość; **dwie kolumny pasków** (Góry/Sprint/Bruk/Regeneracja | Pagórki/TT/Wytrzymałość/Forma); ramka KONTRAKT z 4 polami meta; przyciski NEGOCJUJ (niebieski) / ZWOLNIJ (czerwony) | Awatar OK, nazwisko OK, statystyki jako lista kv w jednej kolumnie, brak pasków | `Stat` w dwóch kolumnach; ramka kontraktu w `Frame(Paper)`; przyciski jako `Solid` z wersalikami |
| Finanse | 3 panele: BUDŻET (kwota Anton 34 px + „WOLNE ŚRODKI · 2026” + linie szczegółów), WYDATKI (donut + legenda), KSIĘGA OPERACJI (tabela, kwoty zielone/czerwone, wyrównane do prawej) | Jeden panel z listą kv; 80 % ekranu puste | Układ 2 kolumny: BUDŻET (kasa Anton + kv) i KASA TYGODNIA (sponsor/dzień, płace/dzień, bilans dnia — te dane już są); niżej tabela ostatnich operacji jeśli Query je ma; kwoty +zielone/−czerwone; donut tylko jeśli są kategorie — nie zmyślać |
| Kalendarz | Nagłówki dni **czarne** z białym meta; wpisy wyścigów jako wypełnione chipy (niebieski 1.WT, czarny Monument) z dwoma liniami; dziś = niebieska ramka; dni sąsiednich miesięcy przygaszone; panel Wyścig z profilem i „NAJLEPIEJ PASUJĄCY ZAWODNICY” | Bardzo blisko: nagłówki dni jasne, wpis czarny bez kategorii, dziś jako szare tło | Nagłówki dni czarne; chip wyścigu z kategorią (kolor po klasie); dziś niebieska ramka 3 px |
| Rynek | Tabela płaska (jak Skład) + karta „ZAWODNIK” z ramką „SYTUACJA TRANSFEROWA” (4 pola meta) + przyciski | Karty-wiersze z awatarami, filtr klubu jako szeroki OptionButton | Ta sama `Table`; filtr jako pigułka-dropdown po prawej w pasku sekcji |
| Tło | Kilka geometrycznych pasm (niebieskie, czerwone, żółte, przygaszone) | Jedno niebieskie pasmo pod kątem | Zostawić jedno pasmo, ale w dwóch tonach (niebieski + przygaszony czerwony pas), nie ruszać więcej |
| Typografia | Body 14 px, meta 10 px bold wersaliki `letter-spacing .12em`, Anton dla liczb i tytułów | Body ~12 px, brak wersalików/rozstrzelenia | Skala w `LookChrome`: `Body 14`, `Meta 10`, `Title 30`, `Number 26`; `FontVariation.SpacingGlyph` dla meta |
