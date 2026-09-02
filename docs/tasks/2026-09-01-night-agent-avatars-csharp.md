# Zadanie na noc — awatary kolarzy w C# (`src/Peloton.Avatars`, D-058)

**Dla:** osobnego Cloud Agenta (multitask, noc 2026-09-01/02).  
**Równolegle pracują:** agent główny (silnik D-054 + **UI Godota** — on podłączy portrety do kart, nie Ty), agent nocny nr 1 (D-055/D-056 — Domain/Application/Persistence), agent nocny nr 2 (roster). **Ty robisz wyłącznie nowy projekt `src/Peloton.Avatars` + jego testy + assety.**

## Cel (gracz)
Karty kolarzy mają dziś geometryczne placeholdery. Właściciel zaakceptował styl portretów **`poster`** (kontur tuszem, dwa płaskie tony, widok z przodu, bez kasku) w eksperymencie Pythona `experiments/avatar_prototype/`. Ta noc przenosi **cały deterministyczny pipeline do C#** (jeden tor kodu, testowalny headless, cache PNG), tak żeby agent UI mógł podać `RiderCareer.Id` i dostać teksturę. Godot pokazuje gotowy PNG — zero logiki w UI (decyzja z 2026-08-26 w `HANDOFF.md`).

## Przeczytaj najpierw (obowiązkowo, w tej kolejności)
1. `AGENTS.md` (D-035: kod pisze **Composer 2.5** `model: composer-2.5`; D-045/D-053; bez maili)
2. `.cursor/skills/peloton-avatars/SKILL.md` — **cały**; to zamknięte decyzje o guście właściciela, niezmienny kadr, przepisy, bramka. Nie negocjuj z nimi.
3. `experiments/avatar_prototype/README.md`, `DESIGN_SKETCH.md`, `avatarlab/*.py` (generate / rng / manifest / render / validate), `avatarlab/bake/*.py`, `scripts/selftest.py` (45 asercji — to Twoje wektory testowe)
4. `HANDOFF.md` sekcje o awatarach (2026-08-26): wyścig wykładniczy na hashach per asset z logarytmem stałoprzecinkowym („bez libm, identycznie w C#”) — **dodanie assetu nie może przetasować istniejących twarzy**
5. `CODEBASE_MAP.md` (kierunek zależności: nowy projekt zależy tylko od `Peloton.Domain` dla `WorldEntityId`; **nie** od Godota)

## Granice
- **Wolno:** nowy `src/Peloton.Avatars/**`, nowy `tests/Peloton.Avatars.Tests/**`, wpis obu projektów do `PelotonManager.sln`, `Directory.Packages.props` (jeśli potrzebna biblioteka PNG — preferuj `SixLabors.ImageSharp` albo własny minimalny koder PNG; **żadnego** System.Drawing), assety PNG pod `src/Peloton.Avatars/assets/poster/**` (skopiowane/wypieczone z eksperymentu), `experiments/avatar_prototype/**` tylko jeśli naprawiasz realny błąd i opisujesz go, ten plik, jedna linia w `HANDOFF.md` i jeden wiersz w `CODEBASE_MAP.md` na koniec.
- **Nie wolno:** `src/Peloton.Client.Godot/**` (UI podłącza agent główny), `src/Peloton.Application/**`, `Domain`, `Persistence`, `Simulation`, `content/**`, `DECISIONS.md`, kontrakty. Nie zmieniaj stylu ani kadru — to zamknięte.
- Nie commituj `playtest/*.zip`. Jedna gałąź `cursor/avatars-csharp-<suffix>`; zielony gate → merge do `main` → CI. Konfliktów z innymi agentami nie powinno być (nowe pliki + jedna linia w `.sln`).

## Co zrobić
1. **Port 1:1 generatora cech.** `TraitGenerator.FromRiderId(WorldEntityId id, int birthYear?, string nationality?)` → zestaw cech (karnacja, włosy, zarost, twarz, oczy, ...) z **identycznym** wynikiem jak Python dla tych samych wejść. Wektory testowe: wyeksportuj z Pythona (`scripts/selftest.py` + dodatkowy skrypt zrzucający 200 par `rider_id → traits JSON`) do `tests/Peloton.Avatars.Tests/vectors/*.json` i asercja równości w C#. Hash, rzadkości, wyścig wykładniczy, logarytm stałoprzecinkowy — bit w bit. Starzenie z zachowaniem tożsamości (ten sam kolarz w 22 i 36 lat to ta sama twarz) — test.
2. **Manifest i przepisy.** Wczytaj manifest pakietu assetów (tabela assetów, tagi, `excludes`, wagi, `asset_pack_version`, `asset_table_hash`) — te same reguły co `validate.py`: nieznany klucz/tag/styl = błąd. Test na literówkę `excludes_tags` (znany defekt z 2026-08-26) musi czerwienić.
3. **Kompozytor warstw.** `AvatarComposer.Render(traits, style: poster, jersey: team|tour|giro|vuelta|world|national, size: 48|96|256)` składa PNG z warstw (kolejność z `render.py`), z `head_crop` dla 48–96 px. Kolory koszulki drużyny z parametru (klub podaje agent UI później), nie z Godota.
4. **Cache.** `AvatarCache` na dysku: klucz = `{asset_pack_version}:{riderId}:{ageBucket}:{jersey}:{size}`; zmiana pakietu unieważnia; brak zapisu do SQLite (to nie World State).
5. **Assety.** Wypiecz pakiet `poster` z eksperymentu (`scripts/bake_pack.py`) i wgraj PNG do `src/Peloton.Avatars/assets/poster/`. Grafika jest **placeholderem** (napisz to w README projektu) — pipeline ma być produkcyjny, sztuka później.
6. **Weryfikacja wizualna.** Wyrenderuj planszę kontaktową 6×6 (36 kolarzy z `content/peloton.wt-2026/roster.json` po `id`) + planszę starzenia + planszę koszulek do `/opt/cursor/artifacts/avatars/*.png` i pokaż w raporcie. Porównaj obok z planszą z Pythona dla tych samych id — muszą być te same twarze.
7. **Testy architektury**: `Peloton.Avatars` nie referencuje Godota ani Application; determinizm (dwa renderowania → identyczne bajty PNG); dodanie assetu o wadze w przenosi tylko w/(W+w) puli i tylko na nowy asset (test statystyczny na 2000 id, jak w Pythonie).
8. **Gate** z `HANDOFF.md` (całość, nie tylko Twoje testy; `dotnet format` musi być czysty, analizatory `latest-recommended` + `TreatWarningsAsErrors`), merge do `main`, CI zielone.
9. **Docs:** `src/Peloton.Avatars/README.md` (API dla agenta UI: jak dostać teksturę po `RiderCareer.Id`, co jest placeholderem), `CODEBASE_MAP.md` jeden wiersz, `HANDOFF.md` jedna linia „D-058 landed: awatary w C#, UI podłącza osobno”.
10. **Raport w czacie** (po polsku): co przeniesione, ile wektorów zgodnych, plansze, jak agent UI ma to wywołać, co jest placeholderem.

## Nie robić
Zmieniać styl/kadr/decyzje z SKILL; podłączać do Godota; pisać do save; zależność od Godota/System.Drawing; nowe systemy portretów „AI”; ruszać inne projekty.

## Postęp (wypełnia agent)
- [ ] wektory z Pythona + `TraitGenerator` zgodny bit w bit
- [ ] manifest / walidacja przepisów
- [ ] kompozytor + `head_crop`
- [ ] cache
- [ ] assety `poster` w repo
- [ ] plansze w `/opt/cursor/artifacts/avatars/`
- [ ] gate + merge do `main` + CI
- [ ] README projektu, wiersz w CODEBASE_MAP, linia w HANDOFF, raport
