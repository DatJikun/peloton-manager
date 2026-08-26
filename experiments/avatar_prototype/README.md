# Avatar prototype — EKSPERYMENT (placeholder art)

**Status:** EXPERIMENT. To nie jest kontrakt projektowy i nie jest wpisane do `DOCS.md`.
Celem jest **wizualna ocena podejścia**, nie zamknięcie systemu.

## Co to jest

Deterministyczny, warstwowy system awatarów w stylu „NFT trait compositing", ale dla
kolarzy: wygląd zawodnika jest **wyliczany z jego wiersza w bazie** (`rider_id`, wiek,
region, dyscyplina, drużyna), a portret **składany w kodzie gry z gotowych warstw PNG**.
Żadnego wywołania AI w trakcie gry.

W tym eksperymencie **prawdziwy jest cały pipeline**:

- deterministyczna generacja cech z `rider_id` (te same dane → ten sam portret, zawsze),
- rozdział cech na `identity` (stałe na całą karierę) / `mutable` (wiek, włosy) / `equipment` (drużyna),
- wagi rzadkości, reguły kompatybilności, ograniczenia wieku,
- starzenie się z zachowaniem tożsamości,
- wykrywanie „klonów" i deterministyczne przerzucanie tylko cech drugorzędnych,
- kompozytor warstw (blend modes, tinty kolorów, ciągłe parametry twarzy),
- cache, wersjonowanie, walidacja pakietu assetów.

**Placeholderem jest tylko grafika.** Warstwy PNG są rysowane proceduralnie w Pythonie
(`avatarlab/bake/`) jako zamiennik docelowej biblioteki assetów (AI albo ręcznie malowane).
Docelowy wygląd zależy od pakietu assetów, nie od tego kodu — kod zostaje ten sam.

## Jak uruchomić

```bash
cd experiments/avatar_prototype
pip install -r requirements.txt

python3 scripts/bake_pack.py       # wypieka pakiet placeholder do out/pack (~25 s, 223 PNG)
python3 scripts/validate_pack.py   # walidacja: 512x512, alpha, alignment, reguły manifestu
python3 scripts/selftest.py        # 31 asercji: determinizm, starzenie, kompatybilność, klony
python3 scripts/render_demo.py     # plansze do oceny + out/demo/report.txt
```

Gotowe plansze (bez uruchamiania czegokolwiek) leżą w `demo/`.

## Co oceniać na planszach

| Plansza | Pytanie do Ciebie |
|---|---|
| `demo/01_contact_sheet.png` | Czy 40 kolarzy wygląda na 40 różnych ludzi, czy na jedną osobę w różnych fryzurach? |
| `demo/02_aging.png` | Czy ten sam zawodnik w wieku 19 i 44 lat to nadal ta sama osoba, i czy starzenie jest widoczne? |
| `demo/03_teams.png` | Czy transfer / koszulka mistrza świata zmienia tylko strój, a nie twarz? |
| `demo/04_equipment.png` | Czy kask i okulary siedzą poprawnie na tej samej twarzy? |
| `demo/05_trait_variants.png` | Czy warianty pojedynczych cech (głowa, oczy, nos, usta, włosy) są dość różne? |
| `demo/06_skin_and_hair.png` | Czy ciągły odcień skóry i kolory włosów wyglądają naturalnie? |
| `demo/report.txt` | Liczby: determinizm, klony w puli 20 000, rozkłady, wydajność. |

## Co jest świadomie NIE zrobione

- Brak portu do C#/Godot — prototyp jest w Pythonie, bo w nim najszybciej się ocenia wygląd.
  Cała logika jest napisana tak, żeby dała się przenieść 1:1 (patrz `DESIGN_SKETCH.md`).
- Brak integracji z `DATA_MODEL_v0.1.md`, save'em i UI.
- Brak kobiet w peletonie, brak pakietu „historycznego" (lata 90.), brak animacji.
- Docelowy styl artystyczny nie jest wybrany — placeholder to celowo neutralna,
  półrealistyczna wektorówka.

## Pliki

```text
avatarlab/rng.py         deterministyczne strumienie z domenami (blake2b + splitmix64)
avatarlab/generate.py    cechy: identity / shape / mutable / equipment, wagi, klony
avatarlab/manifest.py    kontrakt danych pakietu assetów (JSON)
avatarlab/render.py      kompozytor warstw, tinty, cache
avatarlab/validate.py    walidator pakietu (rozmiar, alpha, regiony, reguły)
avatarlab/bake/          PLACEHOLDER: proceduralne rysowanie warstw (zamiennik AI)
scripts/                 bake / validate / selftest / render_demo
demo/                    plansze do oceny + report.txt
out/                     wyniki lokalne (gitignore)
```

Szczegóły techniczne i plan docelowy: `DESIGN_SKETCH.md`.
