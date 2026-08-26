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
- wagi rzadkości, reguły kompatybilności, ograniczenia wieku, role (rider / manager),
- starzenie się z zachowaniem tożsamości,
- wykrywanie „klonów" i deterministyczne przerzucanie tylko cech drugorzędnych,
- kompozytor warstw (blend modes, tinty kolorów, ciągłe parametry twarzy),
- **profile stylu**: ten sam peleton wypiekany w kilku kierunkach artystycznych,
- cache, wersjonowanie, walidacja pakietu assetów.

**Placeholderem jest tylko grafika.** Warstwy PNG są rysowane proceduralnie w Pythonie
(`avatarlab/bake/`) jako zamiennik docelowej biblioteki assetów. Docelowy wygląd zależy od
pakietu assetów, nie od tego kodu — kod zostaje ten sam.

## Decyzje właściciela (2026-08-26)

| Pytanie | Odpowiedź | Stan w kodzie |
|---|---|---|
| Widok | front | zamknięte; pozwala lustrować oko/brew/ucho, czyli o połowę mniej assetów |
| Kask w portrecie | bez | `helmet_worn = false`, kask jest opcjonalną warstwą |
| Peleton | męski + stroje menadżerów | `role="rider"` / `role="manager"` (polo, softshell, garnitur) |
| Rozmiar w UI | karta zawodnika, max ~1/6 strony laptopa | master 512×512; `head_crop` z manifestu dla ikon 48–96 px |
| Styl | kandydat: płaski wektor, ale do porównania wszystkie | 4 profile: `flat`, `flat_outline`, `painted`, `soft` |

## Jak uruchomić

```bash
cd experiments/avatar_prototype
pip install -r requirements.txt

python3 scripts/bake_pack.py all      # wypieka 4 pakiety (out/pack_<styl>), ~2 min
python3 scripts/validate_pack.py      # walidacja każdego pakietu: 512x512, alpha, alignment
python3 scripts/selftest.py flat      # 35 asercji: determinizm, starzenie, kompatybilność, klony
python3 scripts/render_demo.py flat   # plansze do oceny + out/demo/report.txt
```

Gotowe plansze (bez uruchamiania czegokolwiek) leżą w `demo/`.

## Co oceniać na planszach

| Plansza | Pytanie do Ciebie |
|---|---|
| `demo/07_styles.png` | **Który styl?** Ci sami kolarze w czterech kierunkach artystycznych. |
| `demo/08_display_sizes.png` | Czy w rozmiarze karty (380 px) i ikony listy (48 px) portret się czyta? |
| `demo/01_contact_sheet.png` | Czy 40 kolarzy wygląda na 40 różnych ludzi, czy na jedną osobę w różnych fryzurach? |
| `demo/02_aging.png` | Czy ten sam zawodnik w wieku 19 i 44 lat to nadal ta sama osoba? |
| `demo/03_teams.png` | Czy transfer / koszulka mistrza świata zmienia tylko strój, a nie twarz? |
| `demo/09_managers.png` | Czy menadżerowie w cywilnych strojach wyglądają sensownie? |
| `demo/04_equipment.png` | Czy kask i okulary siedzą poprawnie na tej samej twarzy? |
| `demo/05_trait_variants.png` | Czy warianty pojedynczych cech (głowa, oczy, nos, usta, włosy) są dość różne? |
| `demo/06_skin_and_hair.png` | Czy ciągły odcień skóry i kolory włosów wyglądają naturalnie? |
| `demo/report.txt` | Liczby: determinizm, klony w puli 20 000, rozkłady, wydajność. |

## Jak najlepiej wykorzystać ostatni dzień ChatGPT Image

Krótko: **nie generuj assetów, wygeneruj wzorzec stylu.** Biblioteka 250 assetów w jeden
dzień nie powstanie, a assety wygenerowane bez maskowania nie trafią w ten sam kadr.
Zamiast tego zdobądź 4–6 obrazków referencyjnych, do których dopasuję rysowanie
proceduralne — przy płaskim wektorze to realna, docelowa ścieżka produkcji, nie protezа.

**Prompt 1 — wzorzec stylu (wygeneruj 3–4 warianty):**

```text
Flat vector portrait illustration of a male professional road cyclist. Front-facing,
symmetrical, head and shoulders, neutral expression, plain white background, no text,
no logos. Style: flat vector, exactly three flat tone steps per colour, no gradients,
no texture, no photorealism, thin darker line only where two shapes meet, soft rounded
shapes, muted realistic skin tone, sports-manager UI portrait. Framing: head centred,
eyes on the horizontal line at 40% of image height, top of head near the top edge,
chin at 68% of image height, shoulders cropped by the bottom edge. Square 1024x1024.
```

Warianty do porównania: „with a thick black outline", „with only two tone steps",
„with soft painted shading instead of flat tones".

**Prompt 2 — test spójności (jeden obrazek, sześć twarzy):**

```text
Contact sheet, 2 rows x 3 columns, six different male professional road cyclists in the
exact same flat vector style, same framing, same lighting, same head size in every cell.
Vary age, face shape, skin tone and hairstyle. Plain white background, no text.
```

**Prompt 3 — decydujący test (czy AI utrzyma tożsamość):**

```text
The same man, same face, same framing, same style as the previous image, but with a
different hairstyle. Change only the hair. Do not change the face, eyes, nose, jaw,
skin tone or lighting.
```

Jeśli prompt 3 zmienia twarz — a najprawdopodobniej zmieni — to potwierdza, że biblioteka
assetów z AI wymaga inpaintingu z maską, a bez tego zostajemy przy rysowaniu
proceduralnym (co przy płaskim wektorze i tak daje idealne wyrównanie warstw).
Wrzuć wyniki gdziekolwiek w repo albo do rozmowy, a ja dopasuję do nich pakiet.

## Co jest świadomie NIE zrobione

- Brak portu do C#/Godot — prototyp jest w Pythonie, bo w nim najszybciej się ocenia
  wygląd. Logika jest napisana tak, żeby przenieść ją 1:1 (patrz `DESIGN_SKETCH.md`).
- Brak integracji z `DATA_MODEL_v0.1.md`, save'em i UI.
- Brak kobiet w peletonie i pakietów historycznych (zgodnie z Twoją decyzją).
- Nie proponuję układu karty zawodnika — to należy do `UI_SITEMAP_v0.1.md`.
  `08_display_sizes.png` pokazuje wyłącznie rozmiary, nie layout.

## Znane słabości pakietu placeholder

Zarost i wąsy nadal czytają się jak plama, a nie jak włosy. Kołnierze koszulek przy
niektórych kolorach drużyn wyglądają jak obręcz. Kształty koszulek są najsłabszym
elementem. To wady **grafiki zastępczej**, nie pipeline'u — znikają razem z wymianą pakietu.

## Pliki

```text
avatarlab/rng.py         deterministyczne strumienie z domenami (blake2b + splitmix64)
avatarlab/generate.py    cechy: identity / shape / mutable / equipment, wagi, role, klony
avatarlab/manifest.py    kontrakt danych pakietu assetów (JSON)
avatarlab/render.py      kompozytor warstw, tinty, head_crop, cache
avatarlab/validate.py    walidator pakietu (rozmiar, alpha, regiony, reguły)
avatarlab/bake/draw.py   master reference + profile stylu + primitywy rysowania
avatarlab/bake/pack.py   PLACEHOLDER: przepisy na wszystkie kategorie assetów
scripts/                 bake / validate / selftest / render_demo
demo/                    plansze do oceny + report.txt
out/                     wyniki lokalne (gitignore)
```

Szczegóły techniczne, rekomendacja renderera i plan docelowy: `DESIGN_SKETCH.md`.
