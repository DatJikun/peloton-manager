# Generatory awatarów — research 2026-08-28

**Status:** RESEARCH, EXPERIMENT. Nie jest kontraktem i nie jest w `DOCS.md`.
**Cel:** właściciel odrzucił obecne propozycje wyglądu. To zestawienie gotowych
generatorów z internetu, które da się spiąć z tym, czego gra już wymaga.

Otwórz `generator_gallery.html` w przeglądarce — tam są żywe próbki.
Gotowe plansze: `demo/generators/01_style_comparison.png` (style) i
`demo/generators/02_jersey_swap.png` (ta sama twarz, cztery koszulki).

W próbkach twardo wyłączone: kask, okulary, smoczek, oczy-iksy, czapki.
Fryzury ograniczone do krótkich. W grze te same dźwignie są polami w bazie,
nie filtrami podglądu.

## Czego potrzebujesz (z Twojego briefu)

| Wymaganie | Co to znaczy w grze |
|---|---|
| Algorytmicznie | z `rider_id` (albo zapisanego seedu) zawsze ta sama twarz |
| Ustalenie w bazie | słynny zawodnik może mieć ręcznie wpisane włosy / karnację / brodę; reszta peletonu losuje się z algorytmu |
| Od barków w górę | karta zawodnika, nie całe ciało, nie sama buźka |
| Bez kasku i okularów | kask/okulary wyłączone w portrecie UI |
| Koszulka TdF / Giro / Vuelta | ta sama twarz, inny kolor tułowia: żółty / różowy / czerwony |

To **nie jest nowy silnik**. Pipeline w `experiments/avatar_prototype/` już tak działa:
tożsamość z bazy → warstwy → koszulka osobno. Nie podoba Ci się **rysunek placeholder**,
nie kontrakt. Generator z internetu ma zastąpić paczkę grafik, nie logikę.

W grze **nie wołamy internetu**. Ten sam seed w DiceBear zawsze daje ten sam SVG,
biblioteka jest też w C# i działa offline. HTTP API jest tylko do oglądania próbek.

## Co pasuje (krótka lista do oceny oka)

Sześć stylów, w których tułów da się pokolorować bez ruszania twarzy.
Wszystkie: przód, barki, bez kasku, bez okularów, koszulka jako kolor.

| # | Styl | Gdzie oglądać | Koszulka lidera | Licencja | Charakter |
|---|---|---|---|---|---|
| 1 | **Toon Head** | [DiceBear](https://www.dicebear.com/styles/toon-head/) · [Figma](https://www.figma.com/community/file/1589627891082866389) | `clothesColor` na `tShirt` / `shirt` | CC BY 4.0 (Johan Melin) | gruba kreska, blisko plakatu UI |
| 2 | **Micah / Nice Avatar** | [DiceBear Micah](https://www.dicebear.com/styles/micah/) · [playground](https://nice-avatar.vercel.app/) · [Figma](https://www.figma.com/community/file/829741575478342595) | `shirtColor` na `crew` | CC BY 4.0 (Micah Lanier) | czysty, geometryczny, „ikona w dashboardzie” |
| 3 | **Avataaars** | [avataaars.com](https://avataaars.com/) · [DiceBear](https://www.dicebear.com/styles/avataaars/) | `clothesColor` na `shirtCrewNeck` | darmowe komercyjnie (Pablo Stanley) | klasyczny kreskówkowy półportret; trochę „startup” |
| 4 | **Open Peeps** | [openpeeps.com](https://www.openpeeps.com/) · [opeeps.fun](https://opeeps.fun/) · [DiceBear](https://www.dicebear.com/styles/open-peeps/) | `clothingColor` | CC0 (Pablo Stanley) | szkic ołówkiem; najłatwiejsza licencja |
| 5 | **Personas** | [personas.draftbit.com](https://personas.draftbit.com/) · [DiceBear](https://www.dicebear.com/styles/personas/) | `clothingColor` | CC BY 4.0 (Draftbit) | charakterystyczne, płaskie, czytelne w małym rozmiarze |
| 6 | **Pixel Art** | [DiceBear](https://www.dicebear.com/styles/pixel-art/) | `clothingColor` | CC0 | 8-bit, od razu „gra”; słabiej na dużej karcie |

Hub do wszystkich stylów DiceBear (61 sztuk, seed + suwaki):
[dicebear.com](https://www.dicebear.com/) i [playground](https://www.dicebear.com/playground).
Jest też biblioteka C# i [przewodnik Godot](https://www.dicebear.com/guides/use-the-library-with-godot/).

## Jak to mapuje się na bazę

```text
Rider
  rider_id                         # zawsze
  appearance_seed                  # zwykle = rider_id; ten sam seed → ta sama twarz
  appearance_override JSON         # OPCJA: włosy, karnacja, broda, oczy…
                                   # puste pola dociąga algorytm
  team_kit { primary, secondary }  # koszulka drużyny
  gc_leader  team | tour | giro | vuelta
                                   # zmienia TYLKO kolor tułowia
```

Przykład: Pogacar w bazie ma wpisane krótkie ciemne włosy i konkretną karnację.
Reszta WorldTour-u dostaje twarz z seedu. Gdy prowadzi Tour, overlay `tour`
przemalowuje koszulkę na `#FFD400`; Giro `#E66FA2`; Vuelta `#D11F1F`. Czaszka
się nie rusza.

To jest dokładnie podział `identity` / `shape` / `mutable` / `equipment` z
obecnego prototypu. Generator dostarcza **słownik cech** (które włosy, który nos)
zamiast naszych placeholderowych PNG.

## Czego gotowiec NIE da

Żaden publiczny generator nie rysuje prawdziwej kolarskiej koszulki (zamek,
kołnierz, rękawy, logo ekipy). Dają **kolor t-shirtu**. Żółty / różowy / czerwony
lidera na tym działa. Wzory WorldTour (UAE, Visma, Quick-Step) wymagają **naszej**
warstwy koszulki — i tę warstwę prototyp już ma.

Dlatego najlepsza ścieżka, jeśli żaden styl nie jest „kolarski”:

```text
twarz  = wybrany generator (włosy, oczy, nos, karnacja)
tułów  = nasza koszulka (drużyna + lider GC)
```

Style **tylko-twarz** (dobry kandydat na górę tego hybrydy):

| Styl | Link | Uwaga |
|---|---|---|
| Adventurer | [DiceBear](https://www.dicebear.com/styles/adventurer/) | Lisa Wischofsky, CC BY; dużo fryzur, zero tułowia |
| Lorelei | [DiceBear](https://www.dicebear.com/styles/lorelei/) | ta sama artystka, **CC0** |
| Notionists | [DiceBear](https://www.dicebear.com/styles/notionists/) | kreska jak w Notion, CC0; ubranie jest, ale słabo się barwi pod lidera |
| Dylan | [DiceBear](https://www.dicebear.com/styles/dylan/) | duża głowa, zero koszulki |

## Świadomie odrzucone

Nie spełniają briefu albo już je odrzuciłeś jako „za realistyczne jak na awatar”.

| Generator | Dlaczego nie |
|---|---|
| [This Person Does Not Exist](https://thispersondoesnotexist.com/) / StyleGAN | zdjęcie; nie da się przemalować koszulki; wygląda jak wklejony portret |
| [Ready Player Me](https://readyplayer.me/) | 3D, sieć, kask/okulary w pakiecie; za ciężkie na ikonę UI |
| Football Manager FaceGen | plastikowe twarze 3D; społeczność i tak je zmienia na fotki |
| Pro Cycling Manager | **fotki w bazie**, nie algorytm; `gene_sz_photo` → plik TGA |
| [Boring Avatars](https://boringavatars.com/), Identicon, Rings | geometryczne plamy, nie ludzie |
| [Multiavatar](https://www.multiavatar.com/) | mieszanka abstrakcji; koszulki lidera nie da się osobno ustawić |
| [ToonMe](https://toonme.com/) / AI cartoonizer | potrzebuje zdjęcia, niedeterministyczne, sieć w runtime |
| [Humaaans](https://www.humaaans.com/), pełne Open Peeps bodies | całe ciało / pozy, nie kadr „od barków” na kartę |
| Roboty Bottts, emoji, kciuki | nie kolarze |

## Inne place, które warto znać

- [DiceBear Figma plugin](https://www.dicebear.com/guides/create-an-avatar-style-with-figma/) — własny styl kolarski na tym samym silniku (seed, opcje, C#).
- [Humation](https://github.com/endo-yusuke/humation) — własna paczka części SVG, seed → awatar, MIT; gdybyśmy rysowali kolarzy od zera.
- [Bean Heads / Big Heads](https://bigheads.io/) — przesadzone kreskówki, React; słabo pod koszulkę lidera.
- [UI Avatars](https://ui-avatars.com/), Gravatar — inicjały / upload, nie twarze.

## Rekomendacja (do Twojej decyzji wzrokowej, nie do kodu)

1. Otwórz `generator_gallery.html`.
2. Wskaż **jeden** z sześciu stylów z koszulką, albo **hybrydę** (Adventurer/Lorelei na twarz + nasza koszulka).
3. Dopiero potem podmieniamy paczkę assetów w istniejącym pipeline. Kod generacji cech zostaje.

Nic z tego nie wchodzi do `DOCS.md` ani do `PelotonManager.sln`, dopóki nie wskażesz kierunku.
