# Peloton Manager — research: fizjologia i kontrakty peletonu 2026

**Wersja:** 0.1  
**Status:** RESEARCH SOURCE  
**Data:** 2026-09-01  
**Cel:** zebrać publiczne (i dziennikarsko szacowane) liczby sezonu 2026: jak silni są kolarze WorldTouru i ile naprawdę zarabiają / na jakich kontraktach jeżdżą. Podstawa pod późniejszą kalibrację `content/peloton.wt-2026`, nie nowy lock.  
**Autorytet:** źródło researchu, nie kontrakt. Nie nadpisuje `DECISIONS.md`. `D-038` nadal pozwala, by paczka miała **oszacowane** liczby, byle były oznaczone.  
**Supersedes:** brak.  
**Related:** `RACE_ENGINE_RESEARCH_2026-08-25.md` (model CP/W′, nie konkretny peleton), `MANAGER_GAMES_AND_CYCLING_RESEARCH_2026-08-31.md` (ekonomia organizacji), `CAREER_WORLDTOUR_SLICE_v0.1.md`, `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md` (D-046), `content/peloton.wt-2026/README.md`

---

## 0. Honest labels

Kolarstwo zawodowe **nie publikuje** oficjalnej tabeli pensji ani plików mocowych całego peletonu. Ten dokument rozdziela trzy warstwy:

| Warstwa | Co to jest | Jak używać w grze |
|---|---|---|
| **Oficjalne UCI / CPA** | minima, średnie, mediana, gwarancja bankowa, definicja neo-pro | twarda podłoga rynku; wolno wpisać do rulesetu |
| **Dziennikarskie szacunki** | top 10 pensji, bonusy, klauzule wykupu, pojedyncze FTP/W/kg | etykieta `estimated`; nigdy nie udawać dumpa ProCyclingStats |
| **Estymacje z czasu podjazdu** | Watts2Win, Velon, Strava, analizy Velo/Cyclingnews | wejście do CP/W′ **z marginesem**; nie kopia 1:1 do JSON |

`D-038` już to mówi: fizjologia, płace i budżety w paczce 2026 **mogą** być gameplay estimates i **muszą** być tak opisane. Ten research nie zmienia locka. Pokazuje, **jak daleko** obecne estimates odbiegają od peletonu 2026.

Nie ma tu licencjonowanego pełnego składu 28 kolarzy na ekipę. Paczka ma po 4 nazwiska na klub — to cienka warstwa tożsamości, nie roster UCI.

---

## 1. Najkrótszy wniosek

W 2026 Super-GC jedzie w okolicach **6,6–7,0 W/kg przez ~30–40 minut** na kluczowych podjazdach Touru (estymacje z czasu, nie z oficjalnego power file’a). Pogačar jest osobną skrajnością: większy silnik (~430–450 W progu) w lekkim ciele (~66 kg). Vingegaard jest lżejszy (~60 kg) i wygrywa na W/kg przy dłuższym, równym wysiłku. Van der Poel i van Aert to **ciężkie silniki** (~75–78 kg, ogromne waty absolutne), nie lekkie wspinaczki.

Pensje są jeszcze bardziej rozjechane niż waty. Oficjalnie: minimum WorldTour employed veteran **€44 150**, średnia blended ~**€538k**, mediana self-employed **€350k**. Dziennikarsko: Pogačar ~**€8 mln** base, Evenepoel ~**€6,6 mln**, Vingegaard ~**€5 mln**, MVDP / van Aert / Roglič ~**€4 mln**. Domestique często **€100–400k**, neo-pro **€70–300k**.

Paczka `peloton.wt-2026` po D-046 **zaczęła** różnicować archetypy, ale nadal:

- płace są ściśnięte do **€99k–€1,08 mln** (Pogačar w grze zarabia ~7× za mało, van Aert ~16× za mało),
- masa niektórych gwiazd jest odwrócona (Evenepoel **78 kg** zamiast ~61; van Aert **60 kg** zamiast ~78),
- każdy kontrakt kończy się w dniu **10000** (placeholder, nie sezon 2026–2028).

To jest luka **contentu**, nie silnika. Silnik już chce CP/W′/Pmax/masę (`D-018`, `D-046`). Nie wolno zastąpić tego magicznym `Climbing = 84` jako przyczyną wyniku.

---

## 2. Fizjologia sezonu 2026

Ta sekcja **nie** powtarza modelu CP/W′ z `RACE_ENGINE_RESEARCH_2026-08-25.md`. Tamten dokument mówi, **jak** modelować wysiłek. Tutaj: **jakie liczby** pokazuje peleton 2026.

### 2.1 FTP, CP i to, czego nie mamy

Dla gry kanoniczne jest **Critical Power**, nie FTP (`RACE_ENGINE_RESEARCH`, `D-022`). Dziennikarstwo prawie zawsze mówi FTP / „próg” / W/kg na podjeździe.

Grube przełożenie, wystarczające do kalibracji contentu:

- FTP (60 min, treningowe) ≈ CP albo trochę poniżej, zależnie od zawodnika.
- 20–40 min na podjeździe GT przy 6,5–7,0 W/kg to domena **powyżej CP** albo tuż przy nim u najlepszych — więc sam CP nie może być 7,0 W/kg u każdego lidera, bo wtedy nikt nie spala W′.
- W/kg z czasu podjazdu to **estymacja** (masa, CdA, wiatr, drafting, VAM). Watts2Win i podobne serwisy nie są power meterem UCI.

Źródła mocy, które są czymś więcej niż plotką:

- rzadkie przecieki / Strava (Pogačar, luty 2026: strefy sugerujące próg rzędu **~450 W** w szczycie; Beyond the Peloton),
- Evenepoel publicznie ~**425 W** FTP (Velo, przed Tour 2026),
- van der Poel: 90 min @ **446 W** na E3 (Velo; przy 75–78 kg to klasyk, nie GC),
- czasy Tour 2026 (Alpe d’Huez itd.) → estymacje ~6,9–7,0 W/kg u Pogačara na 35 min.

### 2.2 Masy (publiczne zestawienia)

Wagi wahają się w sezonie (klasyki ciężej, GT lżej). Domestique 2026 podaje m.in.:

| Kolarz | wzrost | masa (zestawienie) |
|---|---:|---:|
| Tadej Pogačar | 1,76 m | 66 kg |
| Jonas Vingegaard | 1,75 m | 60 kg |
| Remco Evenepoel | 1,71 m | 61 kg |
| Primož Roglič | 1,77 m | 65 kg |
| João Almeida | 1,78 m | 63 kg |
| Mathieu van der Poel | 1,84 m | 75 kg |
| Wout van Aert | 1,87 m | 78 kg |
| Jasper Philipsen | 1,76 m | 69 kg |
| Filippo Ganna | 1,93 m | 82 kg |
| Isaac del Toro | 1,80 m | 64 kg |
| Paul Seixas | 1,84 m | 61 kg |
| Tom Pidcock | 1,70 m | 58 kg |

Źródło: [Domestique — height and weight](https://www.domestiquecycling.com/en/height-and-weight-of-pro-cyclists/).

Dla silnika masa jest **statystyką wydolnościową**, nie kosmetyką. 5 kg przy tym samym CP to inny zawodnik na 8% (Cyclingnews o Pogačar vs Vingegaard: przy 7 W/kg różnica absolutna ~35 W, jeśli jeden ma 65 kg, a drugi 60).

### 2.3 Pogačar, Vingegaard, Evenepoel — trzy silniki GC

**Pogačar (UAE, ~66 kg)**

- Szacowany FTP / próg: **420–450 W** (Velo 420–440; Beyond the Peloton ~450 ze stref Strava; Watts2Win FTP **439 W**).
- To **~6,4–6,8 W/kg** na godzinę i **więcej** na 20–35 min.
- Tour 2026, estymacje Watts2Win / Velo:
  - Alpe d’Huez, etap 19, 24.07.2026: **35:27**, ~**6,9–7,0 W/kg**, rekord podjazdu.
  - Plateau de Solaison, etap 15: **32:19**, ~**6,86 W/kg**.
  - Tourmalet, etap 6: **36:06**, ~**6,65 W/kg**.
  - krótkie mury (Montjuïc / Pla del Mir): **~8 W/kg** przez 3–4 min.
- Atak „odpięcia” to 600–700 W przez ~1 min (Cyclingnews, szacunek) — to Pmax/W′, nie CP.
- Durability: powtarzalność ataków po kilku godzinach jest jego przewagą; sam „świeży FTP” nie tłumaczy Touru (Velo o Evenepoelu: podobny FTP ≠ podobny punch 3–5 min).

**Vingegaard (Visma, ~60 kg)**

- Szacowany FTP ~**410 W** → **~6,8 W/kg** przy 60 kg (Velo).
- Plotki 2026 o kilku dodatkowych kilogramach, żeby zamknąć lukę absolutnych watów do Pogačara — niepotwierdzone, ale ważne dla gry: **celowa zmiana masy** to decyzja, nie błąd danych.
- Historycznie wygrywa długie, równe 30–40 min; przegrywa, gdy Pogačar szarpie.
- W naszym modelu: nieco niższy CP absolutny, niższa masa, wysokie `highIntensityDurability` / W′bal recovery — nie kopia Pogačara z −6 W.

**Evenepoel (Red Bull–Bora, ~61 kg)**

- Publiczny FTP ~**425 W** @ ~64 kg w styczniu → **~6,6 W/kg**; na Tour lżejszy, W/kg rośnie.
- Solaison 2026: ~**6,77 W/kg** / 32:19 (Watts2Win) — GC-caliber, nie tylko TT.
- Słabość, którą sam nazywa: 3–5 min Pogačara.
- W paczce jest zakodowany jak ciężki lider (78 kg, 5,08 W/kg). To psuje D-046 bardziej niż jakikolwiek suwak OVR.

### 2.4 Klasyki: van der Poel i van Aert

To nie są „liderzy GC z inną etykietą”.

- **Van der Poel ~75 kg** (klasyki nawet ~80). 90 min @ 446 W ≈ 5,7 W/kg przy 78 kg; FTP bywa szacowany blisko **500 W** absolutnych (Velo). Ogromny Pmax i positioning na bruku.
- **Van Aert ~78 kg**. Ten sam archetyp: waty na płaskim, lead-out, klasyki, nie 6,8 W/kg na Alpie.

W paczce van Aert ma **60 kg** i 6,45 W/kg CP — wygląda jak wspinacz. Van der Poel ma 70 kg i 405 W — za lekki i za słaby absolutnie jak na Roubaix.

### 2.5 Sprinterzy i TT

- Pro road sprint: literatura ~**17,4 ± 1,7 W/kg** peak (to 1–5 s, nie 20 s). Philipsen w paczce ma Pmax **1800 W** @ 75 kg = **24 W/kg** — powyżej realistycznego piku. Kooij 1710 @ 76 kg podobnie.
- Ganna ~**82 kg**, ogromny silnik TT/flat. Paczka: 390 W @ 78 kg jako `support-2` — za słaby i zła rola.

### 2.6 Reszta WorldTouru nie jeździ 7 W/kg

Badanie metabolomiczne 21 kolarzy WT (przytaczane w omówieniach power-profile): prawie wszyscy utrzymali **5 W/kg** w protokole, wielu **5,5**, trzech **6 W/kg przez 10 min**. GC Touru to ogon rozkładu, nie średnia.

Grube pasma do kalibracji contentu (świeży zawodnik, lab, nie 6. godzina Touru):

| Pasmo | Kto | CP (W/kg, orientacyjnie) | Masa typowa | Pmax |
|---|---|---|---|---|
| Super-GC | Pogačar, Vingegaard w szczycie | 6,4–6,8 (godzina); 6,7–7,1 na 20–35 min | 60–66 kg | wysoki punch u Pogačara |
| GC / stage hunter | Almeida, Roglič, Rodríguez, del Toro | 6,0–6,5 | 60–67 kg | niższy punch niż TP |
| Klasyki / rouleur | MVDP, van Aert, Pedersen | 5,4–5,9 przy 70–78 kg; wysokie waty absolutne | 70–78 kg | bardzo wysoki |
| Sprinter WT | Philipsen, Milan, Kooij, Merlier | 4,8–5,4 CP; pik 16–20 W/kg | 69–80 kg | najwyższy Pmax |
| Solidny pomocnik WT | większość dwudziestki | 5,2–5,8 | 63–75 kg | średni |
| Neo-pro / depth | dolna półka 30-osobowego składu | 4,8–5,5 | szeroko | niski–średni |

Durability (przesunięcie krzywej po kJ) zostaje w modelu z researchu wyścigu. W 2026 publiczny wniosek jest ten sam: **powtarzalność po pracy** oddziela Pogačara od ludzi z podobnym świeżym FTP.

### 2.7 Tour 2026 jako publiczny ranking (nie power file)

Klasyfikacja generalna Touru 2026 jest jawnym evidence, nie dumpem watów. Cyclingnews, Paryż:

| Miejsce | Kolarz | Ekipa 2026 | Strata |
|---|---|---|---|
| 1 | Tadej Pogačar | UAE Team Emirates-XRG | 73:56:26 |
| 2 | Remco Evenepoel | Red Bull–Bora–Hansgrohe | +6:26 |
| 3 | Isaac del Toro | UAE | +9:42 |
| 4 | Paul Seixas | Decathlon CMA CGM | +11:56 |
| 5 | Lenny Martinez | Bahrain Victorious | +13:02 |
| 6 | Mattias Skjelmose | Lidl–Trek | +14:59 |
| 7 | Juan Ayuso | Lidl–Trek | +17:48 |
| 8 | Richard Carapaz | EF Education–EasyPost | +20:00 |
| 9 | Tom Pidcock | Pinarello–Q36.5 (ProTeam) | +29:28 |
| 10 | Jordan Jegat | TotalEnergies | +33:21 |

Źródło: [Cyclingnews, TdF 2026 GC](https://www.cyclingnews.com/pro-cycling/racing/tour-de-france-gc-standings-2026/).

Dla gry: Vingegaard **nie** jest automatycznie „drugim Pogačarem” w każdym sezonie. Evenepoel po transferze jest GC #2 Touru. Seixas (19 lat) i del Toro są już w paśmie GT, nie w slocie `support-1`. Pidcock jeździ **ProTeamem** z dziką kartą / rankingiem, nie WorldTeamem INEOS — `D-038` (niższe ligi jako architektura) jest tu prawdziwe, nie teoretyczne. Ayuso w 2026 jest Lidl–Trek, nie UAE.

### 2.8 Jak to mapować na pola silnika (gdy przyjdzie pass contentu)

Nie implementować w tym PR. Gdy właściciel każe kalibrować paczkę:

1. Ustaw **masę** z publicznych tabel (±2 kg, sezonowość później).
2. Ustaw **CP** tak, by `CP / mass` wpadał w pasmo archetypu, a nie w jedną liczbę „lider = 410 W”.
3. **W′** wyższe u punchy (Pogačar, klasyki) niż u diesel-GC.
4. **Pmax** z piku 5–15 s, nie 1800 W u każdego sprintera.
5. **CdA** niższe u TT (Ganna, Evenepoel na desce), nie 0,29 u wszystkich.
6. Nie wyprowadzać płacy z CP w runtime — `CAREER_WORLDTOUR_SLICE` już tego zabrania. Płaca jest osobnym faktem rynku.

---

## 3. Kontrakty i płace 2026

### 3.1 Oficjalna podłoga (UCI + Joint Agreement CPA/AIGCP)

Minima męskiego WorldTouru na 2026 są **zamrożone na poziomie 2025** (CPA wybrało reformę zasad kontraktu zamiast kolejnych +5%).

| Status | Employed (brutto) | Self-employed (brutto) |
|---|---:|---:|
| Veteran WT | €44 150 | €72 404 |
| Neo-pro WT | €35 721 | €58 582 |

Neo-pro: pierwsze **dwa** sezony na WorldTeam/ProTeam **i** wiek ≤ 25 (mężczyźni). Od trzeciego sezonu musi dostać minimum weterana.

Self-employed jest wyższe na papierze, bo kolarz sam płaci ZUS/ubezpieczenie. UCI 2026: **57%** peletonu WT to employed (średnia **€384k**, mediana **€216k**); self-employed średnia **€654k**, mediana **€350k**. Blended średnia ~**€538k**.

Źródła: [Velora — minimum salary](https://veloracycling.com/features/uci-worldtour-minimum-salary), [CPA Joint Agreement](https://www.cpacycling.com/en/joint-agreement.asp), [Escape Collective / UCI figures](https://escapecollective.com/money-talks-who-are-the-worldtours-top-earners/), [Domestique top 10 + UCI averages](https://www.domestiquecycling.com/en/top-10-highest-paid-professional-cyclists-in-2025/).

ProTeam (2. liga) employed veteran 2025/26: **€35 392**. Women’s WT employed veteran: **€38 000** (też freeze 2026 poza Women’s ProTeam).

WorldTeam musi złożyć **gwarancję bankową** — typowo **25% masy płac** — na wypadek upadku sponsora (ok. 3 miesiące pensji). To jest mechanika ekonomii, nie flavor text.

Wypowiedzenie / brak przedłużenia: Joint Agreement — zawiadomienie **przed 30 września**. CPA chce przesunąć start/koniec kontraktów na **1 października** (zgodność z końcem sezonu szosowego). Dziś ogłoszenia wciąż gęstnieją koło 1 sierpnia, a rozmowy toczą się przez Tour.

### 3.2 Dziennikarskie top 10 (base 2026, bez bonusów i sponsorów osobistych)

Szacunki agentów / insiderów (Escape, TOUR, Domestique, Brújula). Rozrzut między redakcjami jest realny; kolejność jest stabilna.

| # | Kolarz | Ekipa 2026 | Base ~€ / rok |
|---|---|---|---:|
| 1 | Tadej Pogačar | UAE Team Emirates-XRG | 8,0–8,4 mln |
| 2 | Remco Evenepoel | Red Bull–Bora–Hansgrohe | 6,6 mln (niektóre źródła 6–8) |
| 3 | Jonas Vingegaard | Visma \| Lease a Bike | 5 mln (4,5–5,5) |
| 4 | Mathieu van der Poel | Alpecin–Premier Tech | 4 mln |
| 5 | Wout van Aert | Visma \| Lease a Bike | 4 mln |
| 6 | Primož Roglič | Red Bull–Bora–Hansgrohe | 3,5–4 mln |
| 7 | Tom Pidcock | Pinarello–Q36.5 (ProTeam) | 2,7 mln |
| 8 | Adam Yates | UAE | 2,7 mln |
| 9 | Egan Bernal | INEOS Grenadiers | 2,5 mln |
| 10 | Carlos Rodríguez | INEOS / Netcompany–INEOS | 2,0–2,5 mln |

Pogačar: kontrakt z UAE do **2030** (przedłużenie po 2024; *Gazzetta* — podwyżka z ~7 do ~8 mln base, suma restante ~€48 mln). Bonusy cytowane przez Domestique: **€1 mln** za Tour, **€500k** Giro/Vuelta, **€250k** MŚ. Sponsorzy osobisti ~**€2 mln**/rok. W 2026 krążą doniesienia o dalszym przedłużeniu (2031/2032) i „special clauses” (kalendarz, bonusy, logistyka) — **nieoficjalne**, dopóki nie ma komunikatu UAE.

Evenepoel: transfer do Red Bulla na 2026 przed końcem umowy z Soudal; cytowany buyout **≥ €2,5 mln**. Wieloletnia umowa bywa opisywana jako ~€20 mln łącznie.

Visma: model „gwiazdy trochę mniej, pomocnicy trochę więcej” (TOUR/Bike) — to jest **tożsamość organizacji**, nie globalny tax.

Źródła: [Escape Collective rich list, 2.07.2026](https://escapecollective.com/money-talks-who-are-the-worldtours-top-earners/), [TOUR Magazin 2026](https://www.tour-magazin.de/en/professional-cycling/latest-news/pogacar-on-the-1-this-is-how-much-the-stars-of-cycling-will-earn-in-2026/), [Domestique top 10](https://www.domestiquecycling.com/en/top-10-highest-paid-professional-cyclists-in-2025/), [UAE — Pogacar to 2030](https://www.uaeteamemirates.com/tadej-pogacar-uae-team-emirates-agree-long-term-contract-extension-2030/).

### 3.3 Pasma rynku (nie top 10)

Zestawienie z CyclingUpToDate + Daniel Benson (pasy 2025, w 2026 inflacja gwiazd, nie podłogi):

| Pasmo | Typowa pensja base / sezon |
|---|---|
| Super-gwiazda | €4–8 mln |
| Lider WT / lieutenant GT | €1–2,7 mln |
| Upper mid / kapitan / klasykowiec | €450–700k (część do €1,5 mln) |
| Solidny pomocnik | €200–500k |
| Core domestique | €100–400k; część < €150k |
| Neo-pro | €70–300k; rzadko do €500k; podłoga UCI €35,7k |
| Minimum WT veteran employed | €44 150 |

Większość peletonu WT: **€250–400k**, nie miliony. Średnia €538k jest podciągana przez ogon Pogačara.

Prize money etapu Touru **nie** utrzymuje gwiazdy. To bonus i historia; payroll idzie ze sponsora (już w researchu menedżerów: ~87% przychodu ekipy).

### 3.4 Anatomia kontraktu, której cienki `RiderContract` jeszcze nie ma

Dziś w kodzie: klub, `AnnualWage`, start, `EndDate`, cienka lojalność (`D-039`, `D-044`). Brak minigry agenta — lock.

Research 2026 mówi, co jest **prawdziwe** i co można dodać później jako pola, nie jako planszówkę:

| Element | Real 2026 | Na teraz |
|---|---|---|
| Base annual | tak | `AnnualWage` |
| Bonus za GT / MŚ / monument | tak u gwiazd | later; nie minigra |
| Image / personal sponsors | osobny strumień (Pogačar ~€2 mln) | overkill (`D-039`) |
| Buyout / release | Evenepoel ≥ €2,5 mln; plotki o absurdalnych klauzulach Pogačara | later; nie transfer fee w D-044 |
| Długość | neo-pro często 2 lata; gwiazdy 4–6 | `EndDate`; **nie** dzień 10000 u wszystkich |
| Employed vs self-employed | 57 / 43 | later ruleset |
| Notice 30 wrz / sezon od 1 paź | CPA | later kalendarz rynku |
| Gwarancja 25% masy płac | UCI financial obligations | later ekonomia |

`D-044` zostaje: oferta = pensja + data końca; accept z pensji i `Loyalty01`. Research nie każe budować aukcji agenta.

### 3.5 Transfery 2026, które zmieniają „kto ile kosztuje”

Lista 18 WorldTeamów 2026 w paczce (`organizations.json`) jest zgodna z cyklem licencji UCI 2026–2028 (w tym Lotto–Intermarché, NSN, Uno-X Mobility, UAE … XRG). Cienka czwórka nazwisk **nie** jest.

Publiczne ruchy, które research 2026 musi zapisać (UCI / Cyclingnews; nie kalibrujemy JSON w tym PR):

| Kolarz | Paczka | Sezon 2026 |
|---|---|---|
| Remco Evenepoel | Soudal Quick-Step, `leader` | **Red Bull–Bora–Hansgrohe** (buyout ≥ €2,5 mln) |
| Juan Ayuso | **brak** w 72 nazwiskach | **Lidl–Trek** (do 2030; 7. Touru 2026) |
| Tom Pidcock | INEOS, `support-1`, €378k | **Pinarello–Q36.5 (ProTeam)**, ~€2,7 mln; 9. Touru |
| Olav Kooij | Visma, `card` | **Decathlon CMA CGM** (etap 5 Touru 2026) |
| Biniam Girmay | NSN | NSN — tu paczka trafia |
| Søren Wærenskjold | Alpecin | w Tourze 2026 startuje w **Uno-X Mobility** |

To nie jest błąd silnika. To jest dług cienkiej warstwy tożsamości: slot `leader`/`support` × budżet klubu, nie osoba. Pełny 28-osobowy roster UCI nadal nie jest celem `D-038`.

---

## 4. Luka względem `content/peloton.wt-2026`

Paczka po D-046 **nie jest już** jednym copy-paste 410 W. Nadal jest **role template** (leader / support-1 / support-2 / card) × `budgetBand`, nie kalibracja osoby.

Odczyt z `roster.json` na `main` (2026-09-01):

| Kolarz | Paczka CP / kg / W/kg / € | Research (rząd wielkości) | Problem |
|---|---|---|---|
| Pogačar | 432 W, 65 kg, 6,65, **€1,08 mln** | 430–450 W, 66 kg, ~€8 mln | fizjologia w paśmie; **płaca ×7 za niska** |
| Vingegaard | 426 W, 65 kg, 6,55, €1,08 mln | ~410 W, **60 kg**, ~€5 mln | za ciężki; W/kg zaniżone |
| Evenepoel | Soudal, 396 W, **78 kg**, **5,08**, €800k | Bora, ~425 W, **61 kg**, ~€6,6 mln | zły klub + archetyp + płaca |
| van der Poel | 405 W, 70 kg, 5,79, €800k | FTP ~wysokie 400–500 W, **75 kg**, ~€4 mln | za lekki/słaby absolutnie; płaca |
| van Aert | 387 W, **60 kg**, 6,45, **€243k** | **78 kg**, ~€4 mln | odwrócona masa; płaca ×16 |
| Roglič | 424 W, 67 kg, 6,33, €1,08 mln | ~65 kg, ~€4 mln | fizjologia bliżej; płaca niska |
| Philipsen | Pmax **1800**, 75 kg, €350k | pik ~17 W/kg nie 24; pensja raczej mid-high | Pmax za wysoki |
| Ganna | 390 W, 78 kg, €243k | ~82 kg, TT-monster | za słaby, zła rola w czwórce |
| Seixas | 376 W, **73 kg**, €280k | **61 kg**; 4. Touru 2026 | masa; za słaby jak na GC top 5 |
| Pidcock | INEOS, €378k | Q36.5 ProTeam, ~€2,7 mln | zły klub (nawet zła dywizja) |
| Kooij | Visma, Pmax 1710 | Decathlon 2026 | zły klub; Pmax za wysoki jak Philipsen |
| Ayuso | **brak** | Lidl–Trek, 7. Touru | cienka czwórka go nie ma |
| cały pack | wage €99k–€1,08 mln; `contractEndDay=10000` | min €36k, gwiazdy €8 mln, daty 2026–2030 | ściśnięta ekonomia; placeholder dat |

`README` paczki nadal uczciwie pisze: physiology/wages = estimated bands. To jest zgodne z `D-038`. D-046 („stop copy-paste lab numbers”) **nie jest domknięty**, dopóki Evenepoel waży 78 kg, a van Aert 60 kg.

Budżety w `organizations.json` (UAE €50 mln, Visma €32 mln, Picnic €12 mln) są w tej samej skali co Gazzetta/UCI 2026 (średnia ekipa ~€33 mln, super-ekipy ~€45–50 mln). **Payroll gwiazd** w rosterze tej skali nie wypełnia.

---

## 5. Co z tego wynika dla gry (bez cichej zmiany locków)

1. **Silnik ma rację.** CP/W′/Pmax/masa/durability są właściwym miejscem prawdy (`D-018`, `D-046`). Research 2026 dostarcza **pasma**, nie nowej statystyki „Climbing”.
2. **Content ma dług.** Kolejny pass `peloton.wt-2026` powinien kalibrować **osobę**, nie slot `leader`. To zadanie contentowe, nie nowy system.
3. **Płace gwiazd są historią rynku.** Ściśnięcie do €1,08 mln ukrywa, dlaczego UAE „wyda ile trzeba”. Nie trzeba od razu wpisywać €8 mln — ale rząd wielkości (miliony vs setki tysięcy vs minimum UCI) musi być widoczny, inaczej sponsor market (`D-011`) nie ma zębów.
4. **Daty kontraktów** powinny być sezonami (2026, 2027, 2028…), nie dniem 10000. Cienki `EndDate` już na to pozwala.
5. **Niepewność zostaje.** D-010 / D-042: gracz w `None` nie dostaje tych watów. Research jest dla autorów contentu i dla All/Guessed, nie dla God-eye AI.
6. **Nie budować** w tym kroku: aukcji agentów, image rights minigry, employed/self-employed jako UI. Locki `D-039` / `D-044` stoją.

---

## 6. Otwarte pytania (nie decyzje)

1. Czy kalibracja 72 kolarzy idzie jako osobny content task, czy czekamy na pełniejsze składy (nie 4 na klub)?
2. Czy płace w JSON mają iść w euro „prawie prawdziwych” (Pogačar 8 mln), czy w **stabilnej jednostce gry** z jawnym przelicznikiem (`D-012`)?
3. Czy bonus GT to pole kontraktu, czy event po wyniku (knowledge-bounded)?
4. Czy masa sezonowa (klasyki vs GT) to osobny stan, czy stała lab?
5. Women’s WT: minima i gwiazdy (~€150–500k, Vollering plotki ~€1 mln) — poza obecną paczką.

Żadne z tych pytań nie blokuje Simulate + Results (`D-043`).

---

## 7. Źródła

### Oficjalne / semi-oficjalne

- CPA–AIGCP Joint Agreement (minima 2024–2025, notice 30 Sep, pension 12%): <https://www.cpacycling.com/en/joint-agreement.asp>
- Velora, WorldTour minimum 2026 freeze: <https://veloracycling.com/features/uci-worldtour-minimum-salary>
- Velora, UCI confirms freeze: <https://veloracycling.com/news/uci-freezes-worldtour-minimum-salaries-2026>
- UAE, Pogačar extension to 2030: <https://www.uaeteamemirates.com/tadej-pogacar-uae-team-emirates-agree-long-term-contract-extension-2030/>
- UCI, 2026 WorldTour reshuffle (Evenepoel, Ayuso, Girmay, Lotto–Intermarché, NSN, Uno-X): <https://www.uci.org/article/uci-worldtour-evenepoel-girmay-et-al-reshuffle-the-elite/5W0saF9ZpxlGIuaowkQtZl>
- Cyclingnews, 18 WT teams 2026: <https://www.cyclingnews.com/pro-cycling/teams-riders/worldtour-teams-2026-a-comprehensive-guide-to-the-18-top-tier-squads-in-the-mens-peloton/>
- Cyclingnews, Tour de France 2026 final GC: <https://www.cyclingnews.com/pro-cycling/racing/tour-de-france-gc-standings-2026/>

### Pensje (szacunki)

- Escape Collective, 2026 rich list + UCI employed/self-employed averages (2 Jul 2026): <https://escapecollective.com/money-talks-who-are-the-worldtours-top-earners/>
- Domestique, top 10 2026 + bonusy Pogačara + UCI averages: <https://www.domestiquecycling.com/en/top-10-highest-paid-professional-cyclists-in-2025/>
- TOUR Magazin, ranking 2026: <https://www.tour-magazin.de/en/professional-cycling/latest-news/pogacar-on-the-1-this-is-how-much-the-stars-of-cycling-will-earn-in-2026/>
- CyclingUpToDate, pasma rynku: <https://cyclinguptodate.com/faq/how-much-do-professional-cyclists-make-in-2026-cyclist-salary-guide-top-earners-and-minimums>
- Daniel Benson, transfer price bands: <https://dnlbenson.substack.com/p/how-much-do-riders-cost-on-the-transfer>
- Gazzetta via Domestique, team budgets 2026: <https://www.domestiquecycling.com/en/news/bigger-budgets-bigger-gaps-worldtour-spending-hits-eur663-million/>

### Fizjologia / W/kg (estymacje)

- Watts2Win, Tour 2026 climbs: <https://watts2win.eu/en/race/2/2026/watts>
- Watts2Win, Pogačar profile (FTP 439 W, 66 kg): <https://watts2win.eu/en/cyclist/2043/performance>
- Velo, Alpe d’Huez 2026 record ~6,9 W/kg: <https://velo.outsideonline.com/road/road-racing/tour-de-france/power-analysis-pogacar-record-alpe-dhuez/>
- Velo, Evenepoel FTP ~425 W: <https://velo.outsideonline.com/road/road-racing/evenepoel-power-data/>
- Velo, FTP vs world’s best (Vingegaard, MVDP): <https://velo.outsideonline.com/road/road-training/ftp-best-cyclists-world/>
- Beyond the Peloton, Pogačar Strava ~450 W (13 Feb 2026): <https://beyondthepeloton.substack.com/p/tadej-pogacars-surprise-strava-transparency>
- Velo, „7 W/kg era” Tour 2026: <https://velo.outsideonline.com/road/road-racing/tour-de-france/tour-de-france-start-7w-kg-era/>
- Domestique, height/weight: <https://www.domestiquecycling.com/en/height-and-weight-of-pro-cyclists/>

Model wysiłku (CP/W′, durability, nie liczby 2026): `RACE_ENGINE_RESEARCH_2026-08-25.md`.

---

## 8. Jedno zdanie na koniec

Sezon 2026 pokazuje dwa rozkłady z ciężkim ogonem: **waty** (większość WT ~5–6 W/kg, Pogačar ~7 na kluczowych 35 minutach) i **pieniądze** (minimum €44k, mediana setki tysięcy, cztery gwiazdy w milionach). Paczka gry złapała rząd wielkości silnika u Pogačara, ale wciąż myli ciała klasyków z wspinaczami i ukrywa, że jeden kolarz może kosztować tyle co pół pomocniczej ekipy.
