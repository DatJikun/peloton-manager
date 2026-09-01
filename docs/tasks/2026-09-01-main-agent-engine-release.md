# Zadania — agent główny (sesja 2026-09-01, noc)

**Właściciel:** agent główny tej sesji (silnik wyścigu, dokumenty, release).  
**Zasady:** D-035 (kod pisze Composer 2.5), D-045 / D-053 (zielony gate → merge do `main` w tej samej sesji, brak stosu PR-ów), D-043 / D-048 (Watch off, Career Hub nie wraca), §49 `NOT VERIFIED`.  
**Granica z agentem nocnym:** ten agent **nie dotyka** `src/Peloton.Client.Godot/` ani `tests/Peloton.Client.Godot.Tests/` — to ma agent nocny (`2026-09-01-night-agent-godot-ui.md`).

## Zrobione
- [x] Przegląd repo, gate lokalny zielony (249 testów), lista poprawek uzgodniona z właścicielem.
- [x] D-053: `.github/workflows/gate.yml` (CI = gate z `HANDOFF.md`), `.github/workflows/playtest-windows.yml` (Release z tagu `playtest-*`), `global.json` (SDK 8 — CI używało .NET 10 i padało na analizatorach).
- [x] Zamknięte 12 przeterminowanych PR-ów (#13–#16, #20, #21, #24–#26, #31, #34, #36); gałęzie awatarów zostały na `origin`.
- [x] Poprawki md: `CODEBASE_MAP` (schema 9), `HANDOFF` (D-052 landed, reguły zipa i PR-ów), `DECISIONS` D-053/D-054/D-055, `GITHUB_WORKFLOW`, `playtest/README`.
- [x] Kontrakty: `RACE_FEEL_POSITION_AND_SELECTION_v0.1.md` (D-054), `RACE_CDA_ROAD_TT_v0.1.md` (D-055).
- [x] Release `playtest-2026-09-01` zbudowany przez CI (stan przed D-054).

## W toku
- [ ] **D-054** (Composer, gałąź `cursor/d054-position-selection-9a2c`): pozycja z `Positioning` + drift, pace-setter na bruku/podjazdach/finale, koszt bruku (shelter + surge), sondy Roubaix / TdF s1 / TDU s6 / Hautacam, goldeny, `PhysicsContractVersion` 2. Po raporcie: przegląd → gate → merge → CI zielone.

## Do zrobienia (kolejność)
- [ ] **D-055** CdA szosa/TT (Composer, kontrakt gotowy): `CdARoadM2` / `CdATtM2`, schema 10, ITT 60 s starty bez shelteru, TTT, rating TT z TT-aero, sondy ITT, fallback `cdAM2` w loaderze. Wyniki szosowe z D-054 nie mogą się zmienić.
- [ ] **Pliki md po wylądowaniu** (Composer pisze, agent główny sprawdza): `HANDOFF.md` (milestone, „Gdzie jest gra”, Next task = ręczny playtest właściciela), `KNOWN_DIFFERENCE_FROM_CODE.md` (D-054, D-055, ostateczne stałe), `CODEBASE_MAP.md`, `DOCS.md` (dodać oba kontrakty do indeksu), `README.md` (paczka w Releases).
- [ ] **Release nocny:** gdy D-054 + D-055 + UI nocnego agenta są na `main` i CI zielone → tag `playtest-2026-09-02` → sprawdzić workflow → usunąć `playtest/*.zip` z drzewa (historia zostaje, bez rewrite).
- [ ] **Raport końcowy w czacie:** co wylądowało, `compare` przed/po (Roubaix, sprint, Hautacam), link do Release, co otwarte. Bez maili, bez `@mention`.

## Nie robić
§49 zamykać, Watch Race rozbudowywać, Career Hub, starzenie, sponsorzy, skauting, przepisywanie historii Gita, merge z padającymi testami.
