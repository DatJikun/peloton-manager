# Zadanie na noc — silnik wyścigu (D-055), pliki md, release

**Dla:** osobnego Cloud Agenta uruchomionego przez właściciela na noc 2026-09-01/02.  
**Równolegle pracuje agent główny:** kończy **D-054** (bruk / sprint / pozycja, gałąź `cursor/d054-position-selection-9a2c`) i robi **UI Godota**. Nie dubluj tych dwóch rzeczy.

## Przeczytaj najpierw (w tej kolejności)
1. `AGENTS.md` — role D-035: **kod pisze Composer 2.5** (`model: composer-2.5` na każdym `Task` kodującym; nie `inherit`, nie `-fast`); merge D-045 / D-053; bez maili i `@mention`.
2. `HANDOFF.md` — stan, „Commands to run first”, „Things the next AI must NOT do”.
3. `DECISIONS.md` — D-053, D-054, D-055 (na końcu pliku).
4. `RACE_CDA_ROAD_TT_v0.1.md` — **Twój kontrakt** (D-055).
5. `RACE_FEEL_POSITION_AND_SELECTION_v0.1.md` — kontrakt D-054, żeby wiedzieć, co ląduje obok Ciebie.
6. `KNOWN_DIFFERENCE_FROM_CODE.md`, `CODEBASE_MAP.md`, `AI_DEVELOPMENT_RULES_v0.1.md`.

## Granice (żeby dwa agenty się nie gryzły)
- **Nie dotykaj** `src/Peloton.Client.Godot/**` ani `tests/Peloton.Client.Godot.Tests/**` — to agent główny (UI).
- **Zanim ruszysz** `src/Peloton.Simulation/Race/RaceSession.cs` i `src/Peloton.Application/WorldRaceScenarioAssembler.cs`: `git fetch origin main` i sprawdź `git log origin/main --oneline | head`, czy jest commit D-054 („D-054” / „position” / „pace-setter”). Jeśli **nie ma** — zacznij od części D-055, które go nie dotykają (pola `RiderCareer`, loader JSON z fallbackiem `cdAM2`, migracja paczek `content/**`, `RiderRatings`, persistence schema 10, testy ratingów/round-trip), i wróć do assemblera / ITT po tym, jak D-054 wyląduje (zwykle 1–2 h). Nie zaczynaj D-054 samodzielnie.
- Jedna gałąź `cursor/d055-cda-road-tt-<suffix>`; przed merge rebase na aktualny `origin/main`; po zielonym gate merge do `main` i `git push origin main`; sprawdź `gh run list --workflow gate`.
- Nie commituj `playtest/*.zip` (D-053).

## Zadania (w tej kolejności)

### 1. D-055 — CdA szosa vs TT (Composer 2.5)
Zaimplementuj **cały** `RACE_CDA_ROAD_TT_v0.1.md`: `CdARoadM2` / `CdATtM2` na `RiderCareer` i w `RaceRiderProfile`; SQLite **SchemaVersion 10** / checksum `peloton-world-checksum-v10`; klucze JSON `cdARoadM2` / `cdATtM2` z fallbackiem `cdAM2`; migracja `content/peloton.wt-2026/roster.json`, `content/peloton.skeleton/*`, `content/peloton.race-prototype/*` na dwa klucze z mnożnikami archetypów z kontraktu; ITT = starty co 60 s w odwrotnej kolejności GC (lub odwrotne `RiderId`), bez shelteru, tempo effectiveCP × 1.0, bez pace-settera; TTT = tempo czwartego najsilniejszego, czas czwartego na mecie; rating **TT** liczony z TT-aero; sondy z §5 kontraktu (ITT: zwycięzca `tt`/`super-gc`, Evenepoel top 3, Philipsen poza top 100, czasy rosnące; determinizm + spy neutral; round-trip schema 10; fallback loadera). **Wyniki szosowe z D-054 nie mogą się zmienić** — sondy Roubaix / TdF s1 / Hautacam z D-054 muszą przechodzić bez zmian.
Gate (wszystkie komendy z `HANDOFF.md`, nie fabrykować wyników): `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`, SimRunner `run` / `race` / `compare` / `day`.

### 2. D-056 — Nieskończona kariera: rollover sezonu, starzenie, neo-pro, cykl kontraktów AI (Composer 2.5) — **największy blok nocy**
Kontrakt: `CAREER_SEASON_ROLLOVER_AND_AGING_v0.1.md` (przeczytaj cały; DECISIONS D-056). Osobna gałąź `cursor/d056-season-rollover-<suffix>` **po** wylądowaniu D-055 na `main` (i D-054 — sprawdź log). Rób go **etapami, każdy etap = commit + zielony gate**, w tej kolejności, żeby coś zostało nawet, gdy noc się skończy:
1. `SeasonYear` + granica 31 XII → 1 I w `AdvanceOneDay`; kursy 2027 z `CourseCatalogGenerator.GenerateSeason(…, 2027)`; kalendarz 2027 (jeden wpis na etap); wpisy `OrganizationRaceEntry` na nowy rok; pre-season gracza otwiera się ponownie („Plan sezonu 2027”); reset formy. SimRunner `day --days 400` przechodzi przez Nowy Rok i drukuje `season=2027`. Schema **11**.
2. Starzenie fizjologii (§4.1–4.2) z `talentGate` (POT tylko jako hamulec) i deterministyczną wariancją; `BirthYear` materializowany; testy: młody rośnie, 36-latek spada, nikt nie przekracza POT.
3. Emerytury (§4.3) + neo-pro (§4.4) z paczką nazwisk `content/peloton.wt-2026/names.json`; świat się nie kurczy; cap 512.
4. Cykl kontraktów AI na rollover (§5) — fakty publiczne, deterministyczna kolejność klubów; każdy klub AI ma ≥ 8 kolarzy 2 stycznia; gracz nietknięty.
5. Podsumowanie sezonu w inboxie (po polsku) + ostrzeżenia 60/30 dni przed wygaśnięciem.
6. SimRunner `seasons --years 5` (§7) + sondy z §8; round-trip save schema 11 po rolloverze.
Godota nie ruszasz — ale uruchom `Peloton.Client.Godot.Tests`, bo powłoka musi dalej działać w 2027 (daty, kalendarz, skład). Jeśli host Godota wywala się na roku 2027 — napraw **tylko** przez Query w Application (nie w UI) i opisz to w raporcie.

### 3. Pliki md po wylądowaniu (Composer pisze, Ty sprawdzasz, że nic nie kłamie)
- `KNOWN_DIFFERENCE_FROM_CODE.md`: sekcje „D-055 landed” i „D-056 landed” (co z kontraktu weszło, co nie; ostateczne stałe starzenia) (schema 10, model startów ITT, reguła TTT; odroczone: tuck na zjazdach, optymalizator tempa TT, sprzęt); linie „Prototype stores one CdA” → landed.
- `HANDOFF.md`: SchemaVersion **11** (10 jeśli D-056 nie wylądował); „Current milestone” / „Gdzie jest gra” (CdA szosa/deska działa; ITT jedzie się solo; kariera przechodzi w 2027: starzenie, emerytury, neo-pro, kluby AI odnawiają kontrakty); **Next task = ręczny playtest właściciela nowej paczki Windows, bez nowego systemu**; jedna linia w „Recent owner decisions”.
- `CODEBASE_MAP.md`: wiersz Persistence → schema 10; wiersz Race engine → ścieżka ITT/TTT.
- `DOCS.md`: dodaj do tabeli `RACE_FEEL_POSITION_AND_SELECTION_v0.1.md` (DRAFT, D-054) i `RACE_CDA_ROAD_TT_v0.1.md` (DRAFT, D-055) i `CAREER_SEASON_ROLLOVER_AND_AGING_v0.1.md` (DRAFT, D-056); w „Already in code” dopisz D-054/D-055; „Next coding task” → brak (playtest).
- `README.md`: akapit „Known difference” → dwa CdA, pace-setter; paczka Windows jest w **GitHub Releases**, nie w repo.
- Nie ruszaj `DECISIONS.md` (D-055 już wpisane) poza literówkami. Nie zmieniaj locków. §49 zostaje `NOT VERIFIED`.

### 4. Release nocny (tylko gdy wszystko jest na `main` i CI zielone)
Warunek: na `origin/main` są D-054, D-055 **i** commit UI agenta głównego (szukaj „Godot UI” / „HTML parity” w logu; jeśli do rana go nie ma — zrób release bez czekania i zapisz to w raporcie).
1. `git tag playtest-2026-09-02 && git push origin playtest-2026-09-02` → workflow `playtest-windows` buduje zip i publikuje Release; sprawdź `gh run list --workflow playtest-windows` i `gh release view playtest-2026-09-02`.
2. Gdy Release istnieje: usuń `playtest/PelotonManager-playtest-windows.zip` z drzewa (`git rm`), zaktualizuj `playtest/CZYTAJ_MNIE.txt` o to, co nowego (bruk, sprint wieloekipowy, góry, CdA TT, UI), commit na `main`, push. **Bez** `git filter-branch` / przepisywania historii.

### 5. Raport końcowy w czacie (po polsku)
Co wylądowało (D-055, D-056 — które etapy, md, release), wynik `compare --seed 91234` przed/po (linie `case=`), totale testów, link do Release, co zostało otwarte, czy trafiłeś na konflikt z D-054/UI i jak go rozwiązałeś. Bez maila, bez `@mention`, bez nowych otwartych PR-ów.

## Nie robić
§49 zamykać; Watch Race rozbudowywać; Career Hub; sponsorzy; zwalnianie managera; skauting; zmiana stałych D-054 „żeby ITT lepiej wyszło”; merge z padającymi testami; `new Random()`; przepisywanie historii Gita.

## Postęp (wypełnia agent nocny)
- [ ] D-055 kod + sondy
- [ ] D-056 etap 1 (rollover, kursy i kalendarz 2027, schema 11)
- [ ] D-056 etap 2 (starzenie)
- [ ] D-056 etap 3 (emerytury + neo-pro)
- [ ] D-056 etap 4 (kontrakty AI)
- [ ] D-056 etap 5 (inbox)
- [ ] D-056 etap 6 (`seasons --years 5`, sondy)
- [ ] gate lokalny zielony
- [ ] merge do `main`, CI zielone
- [ ] md zaktualizowane
- [ ] tag `playtest-2026-09-02`, Release istnieje
- [ ] zip usunięty z drzewa, `CZYTAJ_MNIE.txt` odświeżony
- [ ] raport w czacie
