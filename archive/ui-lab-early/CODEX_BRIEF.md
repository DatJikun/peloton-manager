# Codex — zadanie UI (lab 08)

Czytaj to całe zanim cokolwiek narysujesz. To laboratorium HTML, nie Godot i nie C#.

## Co zbudować

Jeden nowy statyczny prototyp:

`prototypes/ui/08-desk-race-next.html`

Pełnoekranowe **biurko managera w konkretnym dniu**, dwa stany przełączane bez backendu (przycisk / dwa foldery w pliku):

1. **Dzień roboczy** — główny guzik nazywa się **Advance Day**.
2. **Dzień wyścigu** — ten sam guzik zmienia nazwę na **Race next** i otwiera **menu przygotowania do wyścigu** (osobny widok w tym samym pliku albo jasny panel). Nie startuje od razu RaceLive.

Dopisz pozycję 08 w `index.html` i `README.md`.

## Co jest na biurku (i tylko to)

- data / pracodawca (red) / że świat idzie dalej
- **kalendarz z wpisami** (nie pusty ozdobny tydzień): np. dzień 12 Skeleton race scheduled → due
- **skrzynka** jako lista spraw: „A race is due today.” oraz po wyścigu wynik, który można odłożyć
- jeden główny guzik postępu (Advance Day / Race next)

Skrzynka **nie** otwiera wyścigu. Klik w mail nie wchodzi w prep. Tylko **Race next**.

## Czego nie robić

- nie kopiuj Career Hub / AI dashboardu (odrzucony PR #4 i prototyp 06 jako „pusty dashboard KPI”)
- nie rób hero metrics, czterech dużych liczb, rekomendacji sztabu, workload %
- nie `PlayerTeam`, nie ukryta prawda fizjologii, nie mid-race UI
- nie Godot, nie zmiana `src/`
- nie zamykaj §49, nie implementuj D-032
- nie przemianowuj Advance Day na Advance Week — lock to **Advance Day**

DNA: Football Manager + Motorsport Manager + PCM (`02-anti-ai-reference-board.html`). Kolory możesz wziąć z beauty pass 05 (petrol / yellow broadcast albo carbon atelier), nie z fioletowego AI.

## SOURCE 01 — Forever (skąd brać kawałki)

- canvas: https://forever-components.vercel.app/infinite/
- agent: https://raw.githubusercontent.com/isas1/forever-ai-components/main/agents.json
- indeks: https://raw.githubusercontent.com/isas1/forever-ai-components/main/infinite/components.index.json
- fetch jednego kafelka: `https://raw.githubusercontent.com/isas1/forever-ai-components/main/infinite/{file}`

Zrób tak:

1. Pobierz indeks. Filtruj w kodzie / lokalnie. Nie wklejaj całego JSON do chatu.
2. Wybierz **mały** zestaw (ok. 3–8) tanich komponentów pod: przycisk, listę/inbox, kalendarz albo pasek daty, ewentualnie nawigację. `perfTier: cheap`, raczej `css`, `labels` accessible / mobile-ready.
3. Wklej i **dostosuj** do palety Peloton (zamień hexy i fonty). Zostaw reduced-motion.
4. Na górze `08-desk-race-next.html` w komentarzu HTML wypisz: które `file` wziąłeś i po co.

Nie ściągaj artystycznych particle / cosmic / kinetic hero. To nie landing page.

## SOURCE 02 — Impeccable (czego nie rysować)

https://impeccable.style/slop/

Po złożeniu ekranu **sam** odhacz. Jeśli coś strzela — popraw zanim oddasz.

Szczególnie zero:

- purple/cyan gradientów, glass, neon glow, radial halo
- side-tab card (gruba krawędź z jednego boku)
- karty w kartach, identyczny grid ikona-w-zaokrąglonym-kwadracie nad nagłówkiem
- Inter / Geist / Space Grotesk / Instrument Serif jako jedyna twarz
- gradient text, pulsing status, bounce/elastic, kicker/eyebrow nad wielkim H1
- hero-metric (duża liczba + trzy staty)
- auto-marquee, fake terminal cursor

Font: para z charakterem, nie jeden sans na wszystko. Hierarchy wyraźna. Tekst czytelny, kontrast OK.

Opcjonalnie, jeśli masz npx: `npx impeccable detect` na pliku 08.

## Definition of done

- `08-desk-race-next.html` otwiera się lokalnie (index.html linkuje)
- widać dzień roboczy i dzień wyścigu z **Race next** → prep (skład, cel jednym zdaniem, Start race jako drugi krok, nie zrobiony)
- skrzynka widoczna i **nie** jest launcherem
- komentarz z listą Forever `file`
- krótka notka w `README.md` co Codex dodał
- żadnego `src/`, żadnego gameplay C#

Właściciel nie jest programistą. Ekran ma dać się kliknąć i zrozumieć bez dokumentacji.
