# Zadanie na noc — UI Godota dociągnięte do wzorca HTML

**Dla:** osobnego Cloud Agenta uruchomionego przez właściciela na noc 2026-09-01/02.  
**Cel:** powłoka kariery w Godocie (`src/Peloton.Client.Godot`, scena `CareerShell.tscn`) ma wyglądać jak `peloton-manager-full-ui-poc-v3.html` — właściciel ocenił, że dziś jest „trochę średnia” w porównaniu z HTML-em, który jest **celem do osiągnięcia**.

## Przeczytaj najpierw (w tej kolejności)
1. `AGENTS.md` (role D-035: kod pisze **Composer 2.5** — `model: composer-2.5` na każdym `Task` kodującym; merge D-045; komunikacja bez maili)
2. `HANDOFF.md` → sekcja „Gdzie jest gra” i „Things the next AI must NOT do”
3. `HTML_UI_LAB.md` i sam plik `peloton-manager-full-ui-poc-v3.html` (otwórz go, przeczytaj CSS: kolory, fonty, siatka, odstępy, komponenty)
4. `CAREER_SHELL_DATES_AND_LOOK_v0.1.md` (D-052 — co już miało być zgodne z HTML)
5. `src/Peloton.Client.Godot/LookChrome.cs`, `CareerShellScreen.cs`, `CareerShellViews.cs`, `CareerLookCatalog.cs`, `CareerShell.tscn`
6. `tests/Peloton.Client.Godot.Tests/` (host testowany bez edytora Godota)
7. `.cursor/skills/peloton-avatars/SKILL.md` **tylko** jeśli ruszasz awatary na kartach (geometryczne awatary D-052 zostają, nie wprowadzaj nowego systemu portretów)

## Granice (ważne — równolegle pracuje agent główny)
- **Wolno edytować:** `src/Peloton.Client.Godot/**`, `tests/Peloton.Client.Godot.Tests/**`, ten plik (sekcja „Postęp”), jeden wiersz Godota w `CODEBASE_MAP.md`, **jedna** linia w `HANDOFF.md` → „Recent owner decisions” po wylądowaniu.
- **Nie wolno:** `src/Peloton.Simulation/**`, `src/Peloton.Application/**` (chyba że brakuje Query czysto prezentacyjnego — wtedy osobny, mały commit i wyraźna notatka), `src/Peloton.Domain/**`, `src/Peloton.Persistence/**`, `content/**`, `tests/Peloton.Application.Tests/**`, `RACE_*.md`, `DECISIONS.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`. Agent główny ląduje tam D-054/D-055 (silnik wyścigu, schema 10).
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

## Postęp (wypełnia agent nocny)
- [ ] gap list (tabela poniżej)
- [ ] LookChrome / theme
- [ ] Biurko
- [ ] Skład
- [ ] Wynik
- [ ] Kalendarz
- [ ] Finanse
- [ ] Rynek
- [ ] Plan sezonu / Nowa gra
- [ ] Inbox / Ustawienia
- [ ] zrzuty ekranów (lub uczciwa notatka, że render na VM niemożliwy)
- [ ] gate + merge do `main` + CI zielone
- [ ] `CODEBASE_MAP.md` wiersz Godota, `HANDOFF.md` jedna linia

| Ekran | HTML v3 | Godot dziś | Zmiana |
|---|---|---|---|
| | | | |
