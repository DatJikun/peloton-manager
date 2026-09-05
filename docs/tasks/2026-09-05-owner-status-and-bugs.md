# Raport stanu gry — 2026-09-05

**Dla:** właściciela (Dat) i następnej sesji.  
**Stan:** `origin/main` @ `be52e6b` (D-059 + query slice).  
**Paczka Windows:** [playtest-2026-09-05](https://github.com/DatJikun/peloton-manager/releases/tag/playtest-2026-09-05) — zip z nowym UI, wiekiem/narodowością, sparkline i czasami. To jest ta, którą warto kliknąć.

Ten plik opisuje **co jest w grze**, **czego nie ma** i **jakie błędy backendu** znalazła sesja audytu. Nie zamyka fun gate’u §49.

---

## 1. Co możesz zagrać dziś

To nie jest jeszcze pełny menedżer kolarstwa. Jest **cienka kariera WorldTour**, w której da się przejść sezon i zobaczyć wyniki.

**Nowa gra (Godot):**
1. Wybierasz klub WT (18 ekip).
2. Planujesz sezon: które imprezy jedziesz i kto jest liderem na imprezę.
3. Siadasz przy biurku: skład, kalendarz, kasa, skrzynka, rynek.

**Dzień:**
- Zwykły dzień → **Advance Day** (świat żyje: forma, kasa, kontrakty).
- Dzień wyścigu Twojej ekipy → **Race next** → przygotowanie → **symulacja** → **tabela wyniku**.
- Oglądanie wyścigu (FILM) jest w grze, **domyślnie wyłączone**. Nie jest sposobem gry.

**Świat 2026:**
- 18 ekip WT + zaproszeni, **22 kolarzy na klub WT** (452 na start świata).
- Kalendarz: jeden dzień = jeden etap (Tour = 21 kliknięć, nie jeden wpis).
- Statystyki 1–99 (góry, pagórki, płaskie, TT, sprint, **bruk**, OVR, POT) liczone z fizjologii — Pogačar jest góralem, Philipsen sprinterem.
- Po etapie: miejsce, czas/strata, koszulki GC / punkty / góry / młodzież / drużynowa (tabele, nie decyzje DS w trakcie).
- 1 stycznia 2027+ świat się toczy dalej: starzenie, emerytury, neo-pro, kontrakty AI, nowy kalendarz, znowu plan sezonu.

**Prawda vs rysunek na ekranie:**

| Ekran | Czyta świat | Rysunek (nie zapisuje się) |
|---|---|---|
| Biurko, Skład, Rynek, Finanse, Kalendarz, Wynik | tak (kasa, pensje, wiek, narodowość, sparkline, czasy) | — |
| Sztab / Sponsorzy / Skauting | nie | katalog wyglądu + toast „Jeszcze nie w tej wersji.” |
| Karta kolarza: OVR/POT, kontrakt | tak | rola/forma/wartość — Query ich jeszcze nie oddaje, UI nie zmyśla puncheura |

---

## 2. Co działa pod spodem (backend)

To jest kanon. UI tylko woła Commands/Queries.

- **Dziewięć stanów gry**, save SQLite **schema 11**, deterministyczny seed.
- **PrototypeRaceEngine** liczy oficjalne wyniki (nie stub). Płaski finisz z peletonu; góry selekcjonują; ITT solo co 60 s; TTT czas 4. kolarza; dwa CdA (szosa / deska).
- **Start WT** ma kształt UCI: TDU 140, monument 175, Grand Tour 176, inny WT 154. Na starcie jedzie **pierwszych 7/8 składu** (kolejność kapitana), nie ręczny wybór ósemki.
- **Kontrakty:** pensja, data końca, cienka oferta (D-044). AI kluby odnawiają/łapią wolnych przy Nowym Roku; Twojego klubu nie ruszają.
- **Kasa:** sponsor tytułowy vs płace, może iść w minus, bez ukrytego podatku.
- **Query na biurko (2026-09-05):** narodowość, wiek = rok sezonu − rok urodzenia, sparkline 12 pkt z najbogatszego etapu imprezy, czas i strata z `RiderStageTime`.

Sonda feel seed `91234` (nie jest to „prawda historii”): Philipsen 1. na najpłaskszym Flacie, Pogačar 8. na największej górze. **Roubaix nadal wygrywa Evenepoel** — to treść rosteru, nie dźwignia silnika. Ścisła sonda jest `Skip`.

---

## 3. Czego jeszcze nie ma (to nie są bugi)

Świadomie odłożone. Nie kodować w tej rundzie, dopóki właściciel nie wskaże.

1. **Wybór ósemki na konkretny wyścig** — dziś pierwszych 7/8 z kolejności składu.
2. **D-058 awatary C#** — eksperyment Python żyje, pipeline w `Peloton.Avatars` nie wylądował.
3. **Skauting, rynek sponsorów, AI managerowie** — ekrany są atrapą.
4. **Watch Race jako gra** — film opcjonalny, wyłączony (D-043 / D-048 / D-059). Starych PR-ów radia nie mergujemy.
5. **God-mode wiedzy** — dossier/skauting z modelu danych nie istnieje.
6. **Incydenty, wiatr, pociąg sprinterski, zmiana lidera GC w trakcie etapu (D-032).**
7. **Fun gate §49** — tylko ręczny playtest właściciela.
8. **Wymiana Godota na WebView2** — D-059: Godot zostaje na playtest. Tauri odrzucone. WebView2 dopiero jeśli biurko w Godocie zaboli po zagraniu.

Query, których UI jeszcze nie ma (nie zmyślać): etykieta roli (puncheur), forma, `IdentityLine` w jednym stringu. Cieńsza wersja (wiek/narodowość) jest na `main`.

---

## 4. Błędy znalezione w tej sesji

### B-1. Koszulka młodzieżowa zamarznięta na rok 2026 (backend)

`ClassificationQueries.Build` ma domyślny `seasonYear: 2026`. `GameApplication.Classifications` **nie podaje** `World.SeasonYear`.

Po 1 stycznia 2027 kolarz z 2002 r. nadal liczyłby się jako U25 (wiek 24 zamiast 25). W 2026 testy przechodzą, więc nikt tego nie widział.

**Naprawa tej sesji:** brać rok z świata; test 2026 vs 2027.

### B-2. Strata powyżej godziny w tabeli wyniku (prezentacja)

Application umie formatować godziny (`FormatClock(3661)` → `1:01:01`). Godot dla miejsc 2+ liczy `minuty = sekundy / 60`, więc +90 minut wygląda jak `+90'00"` zamiast `+1h 30'00"`. Zwycięzca ma godziny, reszta nie.

**Naprawa tej sesji:** jeden formatter w Application (cyklistyczny: `m.cz.` / `+12'33"` / `+1h 12'33"`), Godot go woła, nie liczy sam.

### Świadomie nie ruszamy

- Label wyniku w CLI to `OriginDefinitionId` (`rider.wt2026.…`); Godot i tak bierze nazwisko. Zmiana złamałaby złote testy prototypu.
- Roubaix / Evenepoel — treść, nie silnik.
- HANDOFF sprzed tej sesji mówił o `playtest-2026-09-02` jako „następny tag” — tag `playtest-2026-09-05` już jest; HANDOFF dociągamy tu.

Gate CI na `be52e6b` (D-059) był w toku w chwili audytu. Merge tej poprawki tylko na zielonym gate.

---

## 5. Kolejność dalej (żeby się nie gryźć)

Dat gra zipa. Agenci nie zaczynają nowego systemu równolegle z playtestem.

| Kto | Następny mały krok | Nie ruszać |
|---|---|---|
| **Właściciel** | Playtest Windows `playtest-2026-09-05` | — |
| **Cursor (docs / review)** | ten raport, HANDOFF | Godot chrome, Watch, Tauri |
| **Gemini / Composer (kod)** | B-1 + B-2, testy, gate | D-058, ósemka, skauting, silnik Roubaix |
| **Peloton AI** | ewentualne dociągnięcie Godota po playteście (odstępy, czytelność) | te same pliki Application co Gemini |

Po playteście, **jedna** rzecz na raz (D-035):

1. Feedback właściciela z zipa (czy biurko da się czytać).
2. Jeśli tak: query roli/formy **albo** wybór 7/8 na wyścig — nie oba w jednym drzewie.
3. D-058 awatary osobno.
4. WebView2 tylko jeśli Godot boli (D-059). Nie Tauri.

---

## 6. Locki, których ta sesja nie łamie

D-043 Watch wyłączony. D-045 merge gdy zielone. D-048 bez Career Hub. D-053 zip tylko przez tag. D-059 Godot na playtest, Tauri nie. §49 otwarte.
