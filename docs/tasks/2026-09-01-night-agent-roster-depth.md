# Zadanie na noc — pełne składy WorldTour 2026 (D-057)

**Dla:** osobnego Cloud Agenta (multitask, noc 2026-09-01/02).  
**Równolegle pracują:** agent główny (D-054 silnik + UI Godota), agent nocny nr 1 (D-055 CdA, D-056 rollover — Domain/Persistence/Application), agent nocny nr 3 (awatary — nowy projekt `src/Peloton.Avatars`). **Ty robisz wyłącznie treść: paczkę kolarzy.**

## Cel (gracz)
Dziś każdy klub ma **8 kolarzy** (200 w świecie). Manager ze składem 8 osób to nie manager. Po tej nocy każdy klub WorldTour ma **22 kolarzy z prawdziwego peletonu 2026** (wildcardy zostają po 8), każdy z archetypem, pasmem fizjologii i pensji, wiekiem i narodowością — a Skład, Rynek, Finanse i wyścigi dalej działają.

## Przeczytaj najpierw
1. `AGENTS.md` (D-035: kod/skrypty pisze **Composer 2.5** `model: composer-2.5`; D-045/D-053 merge; bez maili)
2. `HANDOFF.md` (stan, gate), `KNOWN_DIFFERENCE_FROM_CODE.md`
3. `content/peloton.wt-2026/README.md` — **pasma archetypów i pensji** (estimated, D-038) — to Twoja biblia liczb
4. `WT_2026_PHYSIOLOGY_AND_CONTRACTS_RESEARCH_2026-09-01.md` — źródło (nie lock)
5. `tools/calibrate_wt2026_roster.py`, `tools/generate_wt2026_real_fields.py` — istniejące skrypty; rozszerzasz je, nie robisz trzeciego od zera
6. `content/peloton.wt-2026/roster.json`, `organizations.json`, `scenario.json`; `src/Peloton.Content/JsonScenarioCatalog.cs` (co loader czyta; limit **512** kolarzy w katalogu)
7. `tests/Peloton.Application.Tests/WtRosterCalibrationTests.cs`, `RiderRatingTests.cs` (nierówności nazwane: Pogačar/Philipsen/MvdP muszą dalej przechodzić)
8. `RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md` §start listy (UCI 7 / 8 na GT) i `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md` (ratingi są widokiem fizjologii)

## Granice (żeby cztery agenty się nie gryzły)
- **Wolno:** `content/peloton.wt-2026/roster.json` (+ ewentualnie `wt2026-riders-source.csv` jako źródło), `content/peloton.wt-2026/README.md`, `tools/*.py` dla rostera, `tests/Peloton.Application.Tests/WtRosterCalibrationTests.cs` (rozszerzenie), ten plik, jedna linia w `HANDOFF.md` „Recent owner decisions” na koniec.
- **Nie wolno:** `src/**` (żadnego C# poza tym testem), `content/peloton.skeleton/**`, `content/peloton.race-prototype/**`, `calendar.json`, `race-identities.json`, `organizations.json` (poza `estimatedBudgetEur` jeśli płace przerastają budżet — wtedy podnieś budżet w paśmie z README i opisz), Godot, `DECISIONS.md`, kontrakty `RACE_*`/`CAREER_*`.
- **Klucz CdA:** agent nr 1 (D-055) zmienia w rosterze `cdAM2` → `cdARoadM2` + `cdATtM2`. Zrób tak: generuj roster **skryptem z pliku źródłowego** (CSV/JSON z listą nazwisk, klub, rocznik, narodowość, archetyp, rola), żeby regeneracja była tania. Przed każdym merge `git fetch origin main`; jeśli na `main` roster ma już dwa klucze CdA, Twój skrypt ma emitować dwa klucze (mnożniki archetypów są w `RACE_CDA_ROAD_TT_v0.1.md` §2); jeśli jeszcze nie — emituj `cdAM2` (loader D-055 ma fallback). **Konflikt w `roster.json` rozwiązujesz przez ponowne wygenerowanie, nie ręczny merge.**
- Nie commituj `playtest/*.zip`. Jedna gałąź `cursor/wt2026-roster-depth-<suffix>`; zielony gate → merge do `main` → CI.

## Co zrobić
1. **Źródło nazwisk.** Composer zbiera (web search) realne składy WorldTour **2026** dla 18 klubów: 22 kolarzy na klub (kapitanowie, karta sprinter/klasyk, góral, TT, pomocnicy, 2–3 neo). Jeśli 2026 jest niepewne — bierz stan na koniec 2025 z ogłoszonymi transferami (Evenepoel w Red Bull to lock). Narodowość i rocznik prawdziwe. Wildcardy (ProTeamy/Australia w `organizations.json`) zostają po 8. Zapisz źródło do `content/peloton.wt-2026/wt2026-riders-source.csv` (kolumny: `organizationId,name,nationality,birthYear,archetype,role,squadOrder`). `role` ∈ `leader | card | support | neo`; `squadOrder` decyduje o kolejności w drużynie: **kapitan pierwszy, potem karta, potem pomocnicy** (to ważne — assembler bierze pierwszych 7/8 do startu; D-050 lider zostaje osobno).
2. **Generator.** Skrypt `tools/build_wt2026_roster.py` (Python 3, bez zewnętrznych paczek) czyta CSV + pasma z README i deterministycznie (hash z `id`) losuje fizjologię i pensję w paśmie archetypu; zachowuje **istniejące 200 wpisów bez zmian liczb** (ich `id` i wartości są w testach/sondach D-054 — nie ruszaj ich; dopisuj nowych). Nowi mają `id = rider.wt2026.<klub>.<slug-nazwiska>`; stabilne, bez kolizji. Kontrakty: `contractEndDay` zróżnicowane jak dziś (`vary_contract_end`). `potentialOvr` z pasma archetypu i wieku (neo wyżej niż OVR). Limit **≤ 512** w całym pliku (18×22 + wildcardy×8 ≈ 452).
3. **Budżety.** Suma pensji klubu ≤ `estimatedBudgetEur` klubu z `organizations.json` (kasa nie może iść w debet od 1 stycznia). Jeśli nie mieści się — pensje pomocników na dół pasma, dopiero potem budżet w górę w paśmie z README. Napisz w README, ile wynosi suma na klub.
4. **Testy** (rozszerz `WtRosterCalibrationTests`): każdy klub WT ma 22 kolarzy, wildcard 8; ≤ 512 łącznie; unikalne `id`; każdy ma archetyp i pasmo; kolejność kapitan-pierwszy zachowana dla istniejących kapitanów; pensje ≤ budżet dla każdego klubu; `CreateWorld` na `scenario.peloton.wt-2026` tworzy ~452 `RiderCareer` i `ClubRosterProjection` gracza (Alpecin) ma 22 wiersze; nierówności D-046 (Pogačar/Philipsen/MvdP) dalej przechodzą; SimRunner `day --scenario scenario.peloton.wt-2026 --days 1` działa; `compare --seed 91234` **nie zmienia** zwycięzców sprintów i Hautacam z sond D-054 (Roubaix może się zmienić — to cel punktu 4b) (start listy biorą pierwszych 7/8 w squad order, a to ci sami ludzie co dziś — sprawdź to jawnie; jeśli zwycięzca się zmienia, to znaczy, że zmieniłeś kolejność istniejących — cofnij).
4b. **Kalibracja gwiazd klasyków (przekazane z D-054, jedyny wyjątek od „200 bez zmian”).** Silnik D-054 jest już uczciwy: Roubaix wygrywa Evenepoel, bo w rosterze MvdP ma `criticalPowerW` 430 i `lowIntensityDurability` 0.90, a Evenepoel 425 / 0.94 przy 61 kg i CdA 0.25 — w takiej treści żadna fizyka nie da klasykowi płaskich 250 km. W rzeczywistości gwiazdy klasyków to ~450–460 W i wytrzymałość jako ich znak firmowy. Zrób w rosterze (pasmo `classics` w README zostaje 368–445 dla ogółu; gwiazdy nad pasmem z komentarzem w README): van der Poel CP 455, low/high durability 0.96/0.92; Van Aert CP 458, 0.95/0.91; Pedersen (jeśli jest) CP 448, 0.94/0.90; Evenepoel `lowIntensityDurability` 0.90 (realna słabość na 6 h). Domyślna wytrzymałość archetypu `classics` w generatorze: 0.86 → 0.90. Potem: odblokuj ścisłą sondę `RoubaixClassicsWinAndVanDerPoelBeatsGcRivals` w `tests/Peloton.Application.Tests/PositionAndSelectionProbeTests.cs` (usuń `Skip`) i sprawdź, czy przechodzi (`compare --seed 91234`, `case=roubaix-2025`). Jeśli nie — **nie ruszaj silnika**; zostaw `Skip` z nowym powodem i wypisz w raporcie top 10 z archetypami. Sondy sprintów i Hautacam muszą dalej przechodzić.
5. **Gate** z `HANDOFF.md` (wszystko), merge do `main`, `gh run list --workflow gate`.
6. **README paczki**: sekcja „Roster depth (D-057)”: liczby, źródła, co jest estimated. **`HANDOFF.md`**: jedna linia „`2026-09-02` — D-057 landed: 22 kolarzy na klub WT…” + w „Jeszcze nie” dopisz „wybór ósemki na konkretny wyścig to później (dziś startuje pierwszych 7/8 składu)”.
7. **Raport w czacie** (po polsku): ile kolarzy, ile klubów, przykładowy skład jednego klubu, suma pensji vs budżet, wyniki gate, czy `compare` bez zmian.

## Nie robić
Zmieniać istniejących 200 wpisów liczbowo (poza punktem 4b); ruszać silnik, assembler, schema, Godot; dodawać kluby; „kocie łby” (po polsku **bruk**); fabrykować statystyki mocy jako „prawdziwe” (to pasma estimated — D-038); przekraczać 512.

## Postęp (wypełnia agent)
- [ ] CSV źródłowy (18 × 22 + wildcardy)
- [ ] `tools/build_wt2026_roster.py`
- [ ] `roster.json` wygenerowany, ≤ 512, budżety OK
- [ ] testy rozszerzone i zielone
- [ ] gate + merge do `main` + CI
- [ ] README paczki, linia w HANDOFF, raport
