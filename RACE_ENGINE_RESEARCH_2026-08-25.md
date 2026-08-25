# Jak naprawdę działa wyścig kolarski i jak przełożyć go na grę menedżerską

Najważniejszy wniosek z całego researchu jest dość mocny: **dobrego modelu wyścigu nie powinno się budować wokół jednego paska „energii” ani jednego parametru typu FTP**. W realnym kolarstwie na wynik nakładają się trzy rzeczy: chwilowy koszt fizyczny jazdy, aktualna zdolność zawodnika do wygenerowania wymaganej mocy oraz sytuacja taktyczno-pozycyjna. Zawodnik bardzo często nie odpada dlatego, że „skończyła mu się energia” dokładnie w tym momencie. Jest już wcześniej osłabiony, a potem jedno przyspieszenie, zakręt, zwężenie, rant albo skok nachylenia wystawia mu rachunek, którego nie jest w stanie zapłacić. citeturn15search3turn17search17turn16search1

Z punktu widzenia gry najlepszym fundamentem jest więc układ:

**fizyka trasy → wymagany power → fizjologiczna zdolność zawodnika → pozycja i drafting → reakcja zawodnika → zmiana gapu → nowy wymagany power**.

To tworzy bardzo naturalne zachowania peletonu bez konieczności skryptowania, że „na 4 km do szczytu odpadają pomocnicy”.

## Model wysiłku, który warto mieć pod maską

### Critical Power jest lepszym fundamentem niż FTP

W modelu fizjologicznym rozdzieliłbym **Critical Power, W′ oraz moc maksymalną/krótkotrwałą**.

Critical Power, CP, jest parametrem wynikającym z relacji power-duration. W klasycznym dwupunktowym uproszczeniu dla wysiłków powyżej CP:

\[
t \approx \frac{W'}{P-CP}
\]

gdzie:

- \(P\) to generowana moc,
- \(CP\) to Critical Power,
- \(W'\) to skończona ilość pracy, którą można wykonać ponad CP.

CP ma bardzo konkretne znaczenie fizjologiczne. Powyżej niego organizm znajduje się w domenie severe: zużycie tlenu i inne odpowiedzi metaboliczne nie osiągają stabilnego steady state, a przy wystarczająco długim wysiłku VO₂ zmierza w stronę VO₂max i pojawia się nietolerancja wysiłku. Poniżej CP możliwe jest przynajmniej względnie stabilne funkcjonowanie przez znacznie dłuższy czas. citeturn15search3turn15search18turn15search21

Dla przykładowego zawodnika:

- CP = 350 W
- W′ = 20 kJ

bardzo uproszczony model przewiduje:

| Moc | Nadwyżka ponad CP | Teoretyczny czas |
|---|---:|---:|
| 370 W | 20 W | ~16:40 |
| 400 W | 50 W | ~6:40 |
| 450 W | 100 W | ~3:20 |
| 550 W | 200 W | ~1:40 |

To nie jest gotowa tabela czasu do odpadnięcia. W realnym zawodniku power-duration nie jest idealną hiperbolą, istnieje moc maksymalna, wpływa wcześniejsze zmęczenie, pacing, temperatura, glikogen i mnóstwo innych czynników. Ale jako warstwa symulacyjna CP/W′ jest znacznie sensowniejsza od zasady „zawodnik ma 100 punktów staminy”. citeturn15search0turn15search12

**W′ nie należy jednak traktować dosłownie jak akumulatora anaerobowego.** Badania pokazują, że to użyteczny parametr opisujący skończoną tolerancję pracy powyżej CP, ale nie jest prostym magazynem jednego paliwa metabolicznego. Co więcej, maksymalna moc dostępna zawodnikowi może spadać wraz z wcześniejszym wykorzystaniem W′ jeszcze przed jego teoretycznym całkowitym wyczerpaniem. citeturn15search18turn15search0

FTP natomiast jest bardzo użytecznym terminem treningowym, ale znacznie słabszym fundamentem symulacji. Najpopularniejszy wariant, 95% mocy z 20-minutowego testu, nie jest wymienny ani z CP, ani z dokładnie określonym fizjologicznym progiem u każdego zawodnika. Badania na trenowanych kolarzach pokazują duży rozrzut indywidualny, a osobna praca stwierdziła, że FTP nie powinno być utożsamiane z granicą maximal metabolic steady state. citeturn22search0turn22search1turn22search10

**Do gry:** FTP może pozostać liczbą pokazywaną graczowi, bo jest intuicyjna. Pod spodem trzymałbym CP, W′ i krzywą power-duration.

### Co się dzieje po ataku

Załóżmy, że zawodnik jedzie:

300 W → 650 W przez 20 s → 450 W przez 40 s → 320 W.

Przy CP = 350 W pierwsze dwie fazy nad progiem mocno wykorzystują W′. Kiedy zawodnik zjeżdża do 320 W, w uproszczonym modelu zaczyna odzyskiwać zdolność do kolejnego wysiłku powyżej CP. Odzyskiwanie nie jest jednak natychmiastowe ani liniowe. Jest szybsze, gdy moc regeneracyjna znajduje się wyraźniej poniżej CP, i różni się między zawodnikami. Badania pokazują też, że prosty pojedynczy wykładniczy model W′bal nie zawsze dobrze opisuje rzeczywiste zachowanie. citeturn15search18turn15search0

Co bardzo ważne, **jeżeli po ataku zawodnik nadal jedzie powyżej CP, właściwie nie ma prawdziwej fazy regeneracji W′**. To tłumaczy potężną skuteczność „atak po ataku”. Pierwszy ruch nie musi nikogo urwać. Może jedynie wymusić 40 sekund na 500 W, po których grupa wciąż jedzie 410 W. Wtedy drugi atak jest wykonywany przeciwko zawodnikom, którzy praktycznie nie odzyskali rezerwy. citeturn15search18

Dlatego dla gry użyłbym czegoś w tym rodzaju:

\[
W'_{bal}(t+\Delta t)=W'_{bal}(t)-\max(0,P-CP)\Delta t+Recovery
\]

przy czym `Recovery`:

- występuje głównie przy \(P<CP\),
- rośnie wraz ze spadkiem mocy poniżej CP, ale ma ograniczenie,
- ma indywidualną stałą czasową,
- pogarsza się przy dużym zmęczeniu całodniowym.

To ostatnie jest szczególnie istotne. Nie należy pozwalać zawodnikowi po pięciu godzinach bardzo ciężkiego wyścigu regenerować W′ dokładnie tak samo jak po świeżym, dwudziestominutowym treningu.

### „Durability”, czyli dlaczego świeże waty kłamią

W ostatnich latach bardzo mocno rozwija się w nauce o kolarstwie pojęcie **durability**, odporności możliwości wysiłkowych na wcześniejszą pracę.

To fundamentalna rzecz dla gry.

Dwóch zawodników może świeżych mieć:

- 5 min: 440 W,
- 20 min: 380 W,
- CP: 350 W.

Po 2500 kJ wyścigu jeden nadal zrobi 420 W przez 5 minut, a drugi tylko 385 W.

Badania na profesjonalnych kolarzach pokazują, że późniejsza krzywa power-duration przesuwa się w dół wraz z wcześniejszą pracą, a sama ilość kJ nie wystarcza do opisania efektu. **Intensywność wcześniejszej pracy ma znaczenie.** W badaniu profesjonalistów wcześniejsze interwały na 105–110% CP bardziej pogarszały późniejszą krótką moc niż większa ilość pracy wykonana poniżej 70% CP. citeturn17search17

To znaczy, że:

> 2000 kJ w spokojnym peletonie ≠ 2000 kJ podczas ciągłych ataków, rantów i gonitwy.

I właśnie dlatego dwa etapy o identycznej długości i podobnym średnim power mogą pozostawić zupełnie inne zmęczenie.

W praktyce w grze miałbym co najmniej dwa liczniki wcześniejszego obciążenia:

\[
Work_{low}
\]

oraz

\[
Work_{high}
\]

gdzie praca w okolicy i powyżej CP ma większy wpływ na późniejszą dostępność sprintu, W′ i krótkiego power niż spokojne kilodżule. Badania nad durability właśnie w tę stronę wskazują. citeturn17search0turn17search6turn17search17

### Glikogen i fueling nie są drugim W′

Glikogen działa w znacznie dłuższej skali czasowej.

W czasie wielogodzinnego wyścigu maleją zapasy węglowodanów w mięśniach i wątrobie. Przy wysokich intensywnościach zależność od węglowodanów rośnie, dlatego niedostateczna dostępność CHO szczególnie mocno ogranicza możliwość wykonywania intensywnych wysiłków pod koniec etapu. citeturn18search0turn17search14

Klasyczne zalecenia dla długich zawodów mówią o 30–60 g węglowodanów na godzinę, a przy wysiłkach >2,5 h do około 90 g/h, zwłaszcza przy użyciu mieszaniny różnych transportowalnych węglowodanów. citeturn18search0

Współczesne zawodowe kolarstwo poszło jednak dalej i zawodnicy często eksperymentują z poborem przekraczającym 90 g/h. Literatura z lat 2025–2026 opisuje strategie ≥100 g/h, ale jednocześnie podkreśla, że dowód na automatyczną korzyść wydolnościową z każdej kolejnej porcji powyżej klasycznych ~90 g/h nie jest jeszcze jednoznaczny. Tolerancja przewodu pokarmowego i zdolność utleniania egzogennego węglowodanu bardzo różnią się między zawodnikami. citeturn17search5turn1news27turn1news28

To aż prosi się o statystykę:

**gut tolerance / carbohydrate absorption**

Zawodnik może próbować jeść 120 g/h, ale nie powinno to oznaczać „+120 jednostek energii”. Zbyt agresywny fueling powinien zwiększać ryzyko problemów żołądkowych, szczególnie przy wysokiej intensywności i upale.

Jednocześnie jedzenie na rowerze nie jest magicznym ładowaniem mięśniowego glikogenu podczas wyścigu. Egzogenny węglowodan pomaga utrzymać dostępność paliwa i glikemię, ale efekt na oszczędzanie mięśniowego glikogenu nie jest zawsze prosty ani identyczny. citeturn17search5turn17search7

### Temperatura i odwodnienie powinny być dwoma osobnymi stanami

Upał i odwodnienie są powiązane, ale nie są tym samym.

Wysoka temperatura zwiększa stres termoregulacyjny i sercowo-naczyniowy oraz może zwiększać wykorzystanie węglowodanów. Odwodnienie dodatkowo zmniejsza objętość płynów dostępnych dla układu krążenia. Ich połączenie szczególnie utrudnia wielogodzinny wysiłek. citeturn17search1turn17search4turn17search10

Nie robiłbym więc zasady:

> „-2% masy przez wodę = -X% mocy”.

Wpływ jest zależny od temperatury, tempa, aklimatyzacji, dostępności płynów i indywidualnego pocenia. Badania kontrolowane pokazują jednak, że odwodnienie może pogarszać performance rowerowy nawet po oddzieleniu części efektu od samego pragnienia. citeturn17search10turn17search27

Lepsze zmienne to:

\[
FluidDeficit
\]

\[
ThermalLoad
\]

i dopiero interakcja między nimi wpływa na tętno, percepcję wysiłku, wykorzystanie CHO, zdolność do wysokiej mocy oraz regenerację.

## Fizyka jazdy i prawdziwa skala przewag

Podstawowy model wymaganej mocy jest bardzo dobrze znany i został wielokrotnie zwalidowany eksperymentalnie. W uproszczeniu moc zawodnika idzie na pokonanie:

\[
P=P_{aero}+P_{rolling}+P_{gravity}+P_{acceleration}
\]

pomijając na chwilę straty napędu. citeturn16search0

Dla jazdy bez wiatru:

\[
P_{aero}\approx\frac12 \rho CdA v^3
\]

\[
P_{rolling}\approx C_{rr}mgv
\]

\[
P_{gravity}\approx mg\sin(\theta)v
\]

\[
P_{acceleration}\approx m_{effective}av
\]

To daje kilka bardzo ważnych konsekwencji dla symulacji. citeturn16search0

### Prędkość i CdA eksplodują na płaskim

Weźmy przykładowo:

- zawodnik 65 kg,
- rower i sprzęt 8 kg,
- system 73 kg,
- CdA = 0,30 m²,
- Crr = 0,004,
- gęstość powietrza 1,20 kg/m³,
- bez wiatru.

Moje obliczenia z powyższego modelu dają:

| Warunki | Aero | Toczenie | Grawitacja | Razem |
|---|---:|---:|---:|---:|
| 40 km/h, płasko | ~247 W | ~32 W | 0 | **~279 W** |
| 50 km/h, płasko | ~482 W | ~40 W | 0 | **~522 W** |
| 20 km/h, 8% | ~31 W | ~16 W | ~317 W | **~364 W** |

I nagle widać, dlaczego „aero bike” na Alpe d'Huez i kilogram masy na sprinterskim finiszu nie mają tej samej wartości.

Na płaskim aero rośnie w przybliżeniu z sześcianem prędkości. Między 40 a 50 km/h prędkość rośnie o 25%, ale wymagany power aerodynamiczny prawie się podwaja.

### Co daje jeden kilogram

Na przykładowym podjeździe 8% przy 20 km/h dodatkowy kilogram systemu kosztuje około:

\[
1\cdot9,81\cdot\sin(\arctan0,08)\cdot5,56
\approx8,7 W
\]

Tymczasem na płaskim przy 40 km/h dodatkowy kilogram zwiększa sam opór toczenia tylko o około 0,44 W, pomijając przyspieszanie.

Czyli:

**strome i szybkie podjeżdżanie → masa jest potężna**

**jazda ze stałą prędkością po płaskim → masa ma zaskakująco małe znaczenie**

Masa wraca do gry na płaskim podczas wielokrotnych przyspieszeń, bo trzeba rozpędzać zawodnika, rower i elementy rotujące. Klasyczne modele power cycling uwzględniają właśnie opór aerodynamiczny, toczenie, grawitację i zmianę energii kinetycznej. citeturn16search0

### Co daje CdA

Zmniejszenie CdA z 0,30 do 0,29 m² w tym samym przykładzie oszczędza:

- około **8,2 W przy 40 km/h**,
- około **16,1 W przy 50 km/h**.

To jest różnica tylko 0,01 m².

Dlatego w TT pozycja, strój, kask, rower i koła mogą być niezwykle istotne. I dlatego equipment w grze najlepiej nie powinien dawać abstrakcyjnego „+3 aero”, lecz modyfikować **CdA zawodnik + rower**, czasem kosztem komfortu, mocy w pozycji albo prowadzenia.

### Rolling resistance

Zmiana Crr z 0,004 na 0,003 przy 73 kg i 40 km/h daje w tym modelu około:

\[
8 W
\]

oszczędności.

Na bardzo złej nawierzchni znaczenie opon, ciśnienia i deformacji systemu jest jeszcze większe. Dlatego bruków nie traktowałbym jedynie jako „podjazdu o 0% z karą do handlingu”. Nawierzchnia powinna wpływać na efektywny opór toczenia, kontrolę roweru, ryzyko awarii i zmęczenie od wibracji. Pomiary z zawodów takich jak Paris-Roubaix potwierdzają bardzo wysoką ekspozycję zawodników na drgania mechaniczne. citeturn5search9

### Wiatr czołowy jest brutalny

Przy 40 km/h i 10 km/h bezpośredniego wiatru czołowego prędkość zawodnika względem powietrza wynosi 50 km/h.

W powyższym przykładzie aerodynamiczny power rośnie mniej więcej:

**247 W → 386 W**.

To około **139 W więcej**, by utrzymać dokładnie tę samą prędkość względem drogi.

Właśnie dlatego wartości typu „średnia prędkość etapu” same w sobie niewiele znaczą bez wiatru.

Przy wietrze bocznym trzeba już używać wektora prędkości powietrza oraz kąta yaw. CdA i siły boczne mogą zmieniać się z yaw, a najbardziej istotny taktycznie staje się kierunek, z którego można się schować. Badania aerodynamiczne grup jadących przy bocznym wietrze pokazują, dlaczego naturalną optymalną formacją jest echelon, czyli wachlarz. citeturn16search3turn16search7turn16search23

### Drafting jest jedną z największych „statystyk” w całym sporcie

To chyba najważniejsza rzecz fizyczna do dobrego odwzorowania.

Badania nad pojedynczymi kolarzami jadącymi jeden za drugim znajdują redukcje oporu aerodynamicznego tylnego zawodnika rzędu dziesiątek procent, zależnie od dystansu i ustawienia. citeturn3search0turn3search1

Jeszcze bardziej ekstremalny wynik przyniosły symulacje CFD i testy tunelowe 121-osobowego peletonu: dla zawodników głęboko schowanych w środkowo-tylnej części aerodynamiczny drag spadał w modelu nawet do **5–10% drag samotnego zawodnika**. To nie oznacza 90–95% mniejszej całkowitej mocy, bo wciąż zostają rolling resistance, przyspieszanie i inne straty, a model przedstawiał bardzo gęsty, idealizowany układ peletonu. Pokazuje jednak, jak gigantyczną różnicą jest pozycja. citeturn16search1turn16search9

Na naszym przykładzie 40 km/h:

solo:

\[
247+32=279W
\]

gdyby lokalna osłona zmniejszała aero tylko o 40%:

\[
0,6\cdot247+32\approx180W
\]

Różnica to prawie **100 W**.

I to prowadzi bezpośrednio do mechanizmu odpadania.

Ważne: drafting nie znika całkowicie na podjazdach. Badanie CFD zwalidowane tunelem aerodynamicznym wyliczyło na nachyleniu 7,5% i prędkości 21,6 km/h ponad **7% oszczędności całkowitej wymaganej mocy** dla jadącego na kole zawodnika. Przy 28,8 km/h korzyść przekraczała 16%. citeturn16search2turn16search18

Czyli nawet górski lider powinien chcieć siedzieć za pomocnikiem, o ile grupa wciąż jedzie szybko.

## Co faktycznie powoduje „odpadnięcie” zawodnika

Moim zdaniem tutaj leży potencjalnie najlepsza mechanika całej gry.

**Odpadnięcie powinno być skutkiem interakcji stanu zawodnika z konkretnym zdarzeniem, a nie momentem, w którym pasek staminy osiąga zero.**

Wyobraźmy sobie zawodnika siedzącego w grupie na płaskim.

Jedzie:

- 200–250 W dzięki draftingowi,
- jego CP wynosi 340 W,
- więc wygląda zupełnie dobrze.

Ale od trzech godzin ma:

- coraz mniejszy glikogen,
- 60% W′,
- trochę odwodnienia,
- wysokie wcześniejsze obciążenie,
- obniżoną późną 1–5-minutową moc.

Nagle przychodzi:

zakręt → rozciągnięcie peletonu → 700 W przez 12 s → 500 W przez 30 s → 400 W przez minutę.

Pierwsze 10 sekund zużywa W′. Po zakręcie nie ma czasu na regenerację, bo tempo pozostaje powyżej CP. Drugi skok zabiera resztę.

Wtedy robi się metr przerwy.

I ten metr jest krytyczny.

Zawodnik przestaje korzystać z pełnego draftu. Wymagana moc rośnie. Żeby wrócić na koło, musi nie tylko jechać tak szybko jak grupa, lecz **jechać szybciej niż grupa**, aby zamknąć lukę. Musi więc wykonać kolejny wysiłek ponad CP właśnie wtedy, gdy W′ jest niskie.

Powstaje dodatnie sprzężenie:

\[
gap \uparrow
\Rightarrow drafting \downarrow
\Rightarrow required\ power \uparrow
\Rightarrow gap \uparrow
\]

To może zamienić 1,5 metra w 30 sekund straty w zadziwiająco krótkim czasie.

### Czy zatem odpada przez koszt energetyczny czy przez brak odpowiedzi na przyspieszenie?

**Najczęściej oba procesy są potrzebne, ale pełnią inne role.**

Długotrwały koszt wyścigu przygotowuje zawodnika do porażki: obniża jego późniejszą krzywą power-duration, zmniejsza dostępność W′, pogarsza dostępność węglowodanów i zwiększa stres cieplny. Badania durability pokazują wyraźnie, że poprzednia praca, zwłaszcza intensywna, pogarsza późniejszą zdolność do generowania wysokiej mocy. citeturn17search17turn17search0

**Samo faktyczne odpadnięcie bardzo często następuje przy konkretnym „teście”: kolejnym przyspieszeniu, stromszym fragmencie, wyjściu z zakrętu, rozpoczęciu rantu albo zmianie tempa.**

Innymi słowy:

> fatigue obniża sufit, a wydarzenie wyścigowe sprawdza, czy zawodnik jeszcze do niego dosięga.

To jest znacznie lepsze od mechaniki, w której zawodnik traci po 0,1 staminy na sekundę, a przy 0 nagle puszcza koło.

### Na podjeździe mechanizm jest trochę inny

Na stromym podjeździe głównym kosztem jest grawitacja. Jeśli grupa jedzie 6 W/kg, a zawodnik po wcześniejszej pracy jest obecnie zdolny do 5,7 W/kg przez wymagany czas, zaczyna systematycznie tracić.

Utrata draftu nadal boli, szczególnie przy zawodowych prędkościach, ale sprzężenie zwrotne jest słabsze niż przy 50 km/h na płaskim. citeturn16search2

Dlatego górskie odpadnięcie często wygląda jak:

480 W → 475 → 465 → 450...

zawodnik nie ma dramatycznej eksplozji, po prostu nie może utrzymać tempa.

Na płaskim może wyglądać odwrotnie:

220 W → 750 W → 500 W → luka → **bang**, grupa odjeżdża.

### „Positioning” powinien być parametrem wydolnościowym

Brzmi dziwnie, ale w praktyce pozycja może oszczędzić więcej energii niż kilka procent różnicy w FTP.

Dobry zawodnik:

- zaczyna podjazd w pierwszych 20 miejscach,
- przed zakrętem pozwala sobie lekko przesunąć się do tyłu,
- nie musi hamować tak mocno,
- trafia na właściwe koło,
- unika bycia w końcu „gumki” peletonu,
- w rencie zajmuje stronę osłoniętą.

Słaby positioning oznacza więcej przyspieszeń, więcej pracy na wietrze i większe prawdopodobieństwo znalezienia się za zawodnikiem, który sam puszcza koło.

W grze nie traktowałbym więc positioning jako bonusu do sprintu. To powinien być parametr wpływający na **rozkład wymaganej mocy w ciągu całego wyścigu**.

## Pięć kompletnie różnych rodzajów wysiłku

### Pięciominutowy podjazd

Tutaj bardzo mocno liczą się:

- W′,
- wysoka 3–8-minutowa moc,
- VO₂max i kinetyka VO₂,
- W/kg,
- świeżość po wcześniejszym wyścigu,
- zdolność do przyspieszania nad CP.

Zawodnik może pozwolić sobie na znaczące wykorzystanie W′, ponieważ meta albo szczyt znajdują się blisko. CP nadal jest ważne, ale różnica między:

360 W CP + 30 kJ W′

a

370 W CP + 15 kJ W′

może sprawić, że pierwszy zawodnik będzie lepszy na bardzo krótkim podjeździe, mimo niższego CP.

Takie wysiłki są właśnie obszarem, w którym relacja CP/W′ opisuje zachowanie znacznie lepiej niż samo FTP. citeturn15search3turn15search18

Takticznie pierwszy atak może być wykonany bardzo wysoko ponad CP, bo koszt nie musi zostać spłacony przez kolejne 40 minut.

### Czterdziestominutowy podjazd

Tutaj W′ nadal ma znaczenie dla ataków, ale główną walutą stają się:

**CP + W/kg + durability + pacing.**

Jeżeli lider odpali 700 W przez 30 sekund, nic nie szkodzi pod warunkiem, że potem może wrócić w okolice mocy możliwej do utrzymania. Jeżeli po ataku grupa nadal jedzie wyraźnie ponad jego CP, nie ma kiedy odbudować rezerwy. citeturn15search18

W późnej fazie górskiego etapu ogromne znaczenie ma również to, **co zawodnik zrobił wcześniej**. Fresh 20-minute power i late-race 20-minute power to dwa różne parametry sportowe. Badania zawodowych kolarzy coraz wyraźniej traktują durability jako oddzielną cechę sukcesu. citeturn17search0turn17search17

To oznacza, że dobry „diesel climber” w grze nie musi mieć ogromnego W′. Może mieć za to wysokie CP/kg i bardzo mały spadek możliwości po 3000 kJ.

### Bruki

Bruki mieszają praktycznie wszystkie systemy.

Zawodnik potrzebuje:

- wysokiej mocy absolutnej,
- bardzo dobrej durability,
- kolejnych wysiłków ponad CP,
- dobrego positioningu,
- prowadzenia roweru,
- odporności na wibracje,
- właściwych opon i ciśnienia,
- siły do przyspieszania po zakrętach i zwężeniach,
- szczęścia mechanicznego.

Peleton na sektorach jest rozciągnięty. Różnice draftu między miejscami są duże, a wejście na sektor na pozycji 50 zamiast 10 może oznaczać dziesiątki dodatkowych przyspieszeń.

Badania mierzące Paris-Roubaix pokazują znaczącą ekspozycję na drgania mechaniczne, więc sensowne jest modelowanie bruku jako czegoś więcej niż „+Crr”. citeturn5search9

W praktyce utworzyłbym współczynnik:

\[
SurfaceCost =
Rolling +
Vibration +
Handling +
AccelerationVariance
\]

Zawodnik klasykowy może mieć tę samą czystą moc co góral, ale tracić znacznie mniej efektywnego power przez kontrolę roweru i zmęczenie powierzchnią.

### Płaski sprint

Tutaj trzysekundowy peak power jest znacznie mniej użyteczny, niż mogłoby się wydawać.

Sprinter dochodzi do ostatnich 200 m po:

- kilku godzinach jazdy,
- kolejnych walkach o pozycję,
- przyspieszaniu na rondach,
- potencjalnym rencie,
- pracy lead-outu,
- 30–60 sekundach bardzo wysokiej intensywności przed właściwym sprintem.

Badania repeated-sprint pokazują, że wcześniejsze sprinty i intensywna praca obniżają późniejszą zdolność do wygenerowania mocy, szczególnie komponent anaerobowy. W jednym badaniu seria 30-sekundowych sprintów z czterominutową regeneracją dawała kolejne spadki średniej mocy około 815 → 780 → 744 W. citeturn4search25

W badaniach nad zawodowym kolarstwem bardzo istotna okazuje się właśnie zdolność do uzyskania dużej mocy **po wcześniejszej pracy**, a nie rekordowy sprint wykonany na świeżo. citeturn17search17

Do tego przy 65 km/h aerodynamika jest olbrzymia.

Dlatego najlepszy sprinter w grze powinien być kombinacją:

\[
SprintResult \sim
LateSprintPower
\times Position
\times Draft
\times Timing
\times Aero
\]

a nie:

\[
SprintResult = SprintStat
\]

### Time trial

TT jest najbardziej „czystym” zastosowaniem fizyki.

Najważniejsze są:

- power-duration,
- CdA,
- Crr,
- masa na podjazdach,
- pacing,
- wiatr,
- zdolność utrzymania aerodynamicznej pozycji.

Na płaskim zawodnik o 420 W i kiepskim CdA może przegrywać z zawodnikiem o 390 W, który ma znacznie lepszą pozycję.

Jednocześnie optymalny pacing nie musi być stałym wattage. Na podjazdach ekonomicznie opłaca się wydawać więcej mocy, kiedy dodatkowy wat bardziej zwiększa prędkość lub zmniejsza czas, a na bardzo szybkich zjazdach kolejne waty dają mały zwrot, bo koszt aero jest ogromny.

W′ daje dodatkowo możliwość chwilowego przekraczania CP na krótkich podjazdach, po nawrotach czy przy końcowym wysiłku. Modele łączące CP/W′ z TT są właśnie używane do opisu takich strategii. citeturn15search24

W aktualnych przepisach UCI komunikacja radiowa jest dopuszczona również w próbach czasowych w odpowiednich kategoriach wyścigów, więc DS może przekazywać pacing i informacje o trasie. citeturn10view1

## Etapówka i tajemnica zawodnika, który jednego dnia lata, a następnego jest pusty

Tutaj pojedynczy parametr „recovery” będzie zdecydowanie zbyt ubogi.

### Glikogen nie wraca natychmiast po kolacji

Po ciężkim wysiłku synteza glikogenu zaczyna się szybko. Przy krótkim czasie do następnego wysiłku standardową strategią jest wysokie spożycie węglowodanów, około 1,0–1,2 g/kg/h w pierwszych godzinach. citeturn18search1turn18search9

Ale nawet ekstremalnie agresywne odżywienie nie gwarantuje kompletnego resetu.

W badaniu opublikowanym w 2025 r. spożycie około 10 g/kg masy ciała w ciągu 12 godzin szybko odbudowało glikogen wątrobowy, lecz **nie doprowadziło do pełnej odbudowy glikogenu mięśniowego w ciągu 12 godzin**. citeturn18search2turn18search13

To jest idealna naukowa podstawa dla etapówki.

Zawodnik może następnego dnia wystartować:

- z normalną glikemią,
- dobrze najedzony,
- subiektywnie okej,

ale z mięśniami nadal nie do końca odtworzonymi po poprzednim etapie.

### Nie wszystkie zmęczenia mają tę samą szybkość regeneracji

Praktycznie podzieliłbym stan zawodnika na kilka warstw:

| Stan | Skala |
|---|---|
| W′bal | sekundy–minuty |
| temperatura / odwodnienie | minuty–godziny |
| dostępność glikogenu | godziny–kolejny dzień |
| acute fatigue / durability loss | godziny–dni |
| uszkodzenia mięśni / wibracje / upadki | wolniej |
| sen / choroba / stres | wielodniowo |

Nie sugeruję, że te czasy są sztywnymi biologicznymi zegarami. Chodzi o architekturę symulatora. Źródła dotyczące W′ pokazują regenerację w skali minut, badania glikogenu pokazują niedokończoną odbudowę nawet po 12 godzinach, a literatura durability pokazuje skutki wcześniejszego dużego obciążenia dla późniejszego performance. citeturn15search3turn18search2turn17search17

### Dlaczego „świetny wczoraj” nie znaczy „świetny dzisiaj”

Możliwy scenariusz:

**Dzień A**

Zawodnik jest w ucieczce. Robi 3500 kJ, sporo pracy nad CP, atakuje pięć razy, jedzie ostatnie 30 minut prawie na limicie. Wygrywa.

Wieczorem je idealnie.

**Dzień B**

Jego W′ jest oczywiście dawno „odbudowane”, bo od mety minęło kilkanaście godzin.

Ale:

- glikogen mięśniowy nie musi być w pełni odbudowany,
- durability została mocno naruszona,
- poprzednie wysiłki wysokiej intensywności szczególnie pogorszyły krótką moc,
- może mieć stres cieplny albo resztkowe odwodnienie,
- sen mógł być słabszy,
- mikrourazy mogą podnosić koszt wysiłku.

Badania profesjonalnych kolarzy wskazują, że sama liczba wcześniejszych kJ nie wystarcza do przewidywania późniejszej zdolności wysiłkowej. Intensywność ich wykonania jest istotna. citeturn17search17

Badania snu w otoczeniu Tour de France pokazują dodatkowo, że realna regeneracja podczas Grand Touru odbywa się w środowisku bardzo dalekim od laboratoryjnego: wielodniowe ściganie, stres, ból mięśni, harmonogram i jakość snu wzajemnie na siebie wpływają. citeturn17search2

Dlatego **forma dnia powinna być raczej rezultatem stanu organizmu niż losowym ±5% rzutem kością**.

Losowość nadal może istnieć, ale na bazie:

- jakości snu,
- infekcji,
- GI,
- stresu,
- indywidualnego recovery,
- warunków pogodowych.

### Paradoks „łatwego” etapu

Etap 200 km może być regeneracyjny dla lidera, jeżeli przez większość dnia jedzie głęboko w peletonie przy bardzo małym koszcie aerodynamicznym.

Etap 130 km może go zniszczyć, jeśli trwa ciągła walka o ucieczkę, są ranty, krótkie podjazdy i dziesiątki wysiłków ponad CP.

To kolejny powód, dla którego obciążenie nie może wynikać z:

\[
Fatigue = distance \times difficulty
\]

Znacznie lepsze jest rzeczywiste zintegrowanie historii mocy zawodnika, szczególnie czasu i pracy w poszczególnych domenach intensywności. citeturn17search17

## Co naprawdę wie dyrektor sportowy podczas etapu

Tutaj ważna uwaga dla realizmu gry: **samochód zespołu nie jest centrum dowodzenia Formuły 1 z perfekcyjną telemetrią wszystkich zawodników.**

### Radio Tour

UCI wymaga od organizatora systemu informacyjnego Radio-Tour. Pojazdy w wyścigu są wyposażone w odbiornik i otrzymują komunikaty race control, między innymi informacje dotyczące sytuacji wyścigowej i oficjalne komunikaty. citeturn10view0

To jest ważny kanał DS.

Nie musi widzieć na ekranie każdego ruchu. Część informacji dostaje głosem.

### Team radio

W aktualnych przepisach UCI najwyższe klasy zawodowego kolarstwa mają możliwość korzystania z zabezpieczonej komunikacji radiowej. Regulamin dopuszcza wymianę między zawodnikami i dyrektorem sportowym oraz między zawodnikami tej samej drużyny w objętych przepisem wyścigach. citeturn10view1

Przez radio zawodnik może powiedzieć rzeczy, których żaden sensor nie pokaże dobrze:

„nogi są złe”

„jestem okej”

„nie mam bidonu”

„mam kapcia”

„lider rywala wygląda źle”

„nie dam rady już pracować”

„jadę za daleko z tyłu”

„przerzutka nie działa”

„jestem pusty”

To jest bardzo cenna mechanika dla gry, bo raport zawodnika może być niedokładny. Jeden zawodnik świetnie ocenia własny stan, drugi przesadza, trzeci zawsze mówi, że jest dobrze, dopóki nie eksploduje.

### Obraz telewizyjny

DS może oglądać transmisję w samochodzie, ale pojawia się opóźnienie i problemy z łącznością. W sezonie 2026 zespoły zaczęły szerzej wykorzystywać internet satelitarny typu Starlink w samochodach, właśnie dlatego, że klasyczne 4G/5G na trasie bywa niestabilne. Relacje z samochodów opisują układ, w którym jeden DS prowadzi, drugi obserwuje transmisję i informacje o trasie, równocześnie słuchając team radio i Radio Tour. citeturn7news36

To daje ciekawy model informacji:

**zawodnik widzi lokalnie najlepiej, DS widzi globalnie najlepiej, ale obaj mają luki.**

Zawodnik nie wie, co dzieje się 40 sekund za nim.

DS nie czuje, jak ciężko jedzie jego lider.

### GPS i gaps

Nie zakładałbym, że DS ma perfekcyjny, aktualizowany co sekundę GPS każdego zawodnika jak w grze strategicznej.

UCI nadal rozwija i standaryzuje technologie śledzenia w celach bezpieczeństwa i zarządzania wyścigiem, więc kompleksowego obowiązkowego systemu typu F1 nie należy traktować jako uniwersalnego standardu zawodowego peletonu w 2026 r. citeturn7news39

W praktyce ważne są:

- oficjalne gaps,
- informacje Radio Tour,
- obraz TV,
- team radio,
- informacje od innych członków sztabu,
- obserwacja z samochodu,
- komputer zawodnika.

Istnieją również systemy takie jak Velon Live Rider Data, zdolne przekazywać dane zawodników do transmisji i platform cyfrowych, ale to **nie jest to samo co uniwersalny kokpit DS z ciągłym power, HR, W′ i GPS wszystkich ośmiu zawodników**. citeturn7search26

To ostatnie traktuję jako ważny wniosek projektowy wynikający z zestawienia źródeł: w grze dałbym DS dużo informacji, ale **z opóźnieniem, niepewnością i różną jakością**.

### Co widzi sam zawodnik

Na head unicie może mieć między innymi:

- power,
- prędkość,
- tętno,
- dystans,
- profil/nawigację,
- czas,
- kadencję.

Ale to nie znaczy, że patrzy na ekran podczas każdego ataku.

W peletonie przy 60 km/h pierwszym problemem jest nie wjechać komuś w tylne koło.

Dlatego automatyczna decyzja zawodnika powinna łączyć dane z komputera z percepcją: feeling, oddech, nogi, pozycja i zachowanie rywali.

## Jak naprawdę wygląda taktyka DS

Najciekawsze jest to, że większość decyzji nie ma odpowiedzi „zawsze rób X”.

DS zarządza **kosztem energii oraz tym, kto jest zmuszony tę energię wydawać**.

Sam Bewley, były zawodnik i później główny DS Israel-Premier Tech, opisywał właśnie wydatkowanie energii jako podstawę wielu decyzji. Podał przykład etapu, na którym plan zakłada ucieczkę, lecz po około 50 km scenariusz nie działa. W takiej sytuacji może nakazać zawodnikom przestać trwonić siły i zacząć oszczędzać energię z myślą o następnym dniu. citeturn19search9

To jest właściwie definicja gameplayu menedżerskiego.

### „Lider został zaatakowany, co robimy?”

Najgorsza możliwa mechanika:

> rywal atakuje → automatycznie chase.

Realny DS powinien zadać sobie:

1. Kto zaatakował?
2. Czy jest groźny dla GC?
3. Jak daleko do mety?
4. Czy teren mu sprzyja?
5. Czy mamy człowieka z przodu?
6. Czy inne drużyny mają większy interes w gonieniu?
7. Czy lider wygląda dobrze?
8. Jak drogie będzie zamknięcie ataku?
9. Czy to może być przynęta przed drugim atakiem?

Świetny przykład dała Visma/Jumbo w Tour de France 2022. Roglič i Vingegaard mogli naprzemiennie atakować Pogačara, wymuszając jego reakcje. Strategia nie polegała na tym, że każdy pojedynczy atak miał natychmiast zdobyć minutę. Chodziło również o zmuszanie przeciwnika do kolejnych kosztownych odpowiedzi i wykorzystanie przewagi posiadania więcej niż jednego zagrożenia. citeturn14search7turn12search17

To pięknie współgra z W′.

Atak zawodnika A:

\[
Opponent\ W' \downarrow
\]

A jednocześnie zawodnik B nie musi reagować z taką samą intensywnością.

Po kilku takich ruchach B atakuje zawodnika, który nadal ma wysokie CP, ale znacznie mniej dostępnego „punchu”.

### „Mamy dwóch liderów, jeden jest z przodu”

Nie ściągałbym go automatycznie.

Zawodnik z przodu może:

- być realnym zagrożeniem GC,
- zmusić rywala do pracy,
- pozwolić pierwszemu liderowi siedzieć na kole,
- później czekać jako satellite rider,
- sam zostać nowym liderem, jeśli sytuacja się odwróci.

To jest właśnie potęga dwóch kart.

Przed Tourem 2022 Jumbo-Visma otwarcie mówiło, że wspólnie z zawodnikami przygotowuje plan przeciw Pogačarowi i rozpracowuje różne scenariusze. citeturn19search1

Dla gry powinno istnieć coś takiego jak:

\[
TacticalThreat
\]

Nawet zawodnik, który nie atakuje, ma wartość, jeżeli przeciwnik **nie może pozwolić mu odjechać**.

### „Pomocnik odpada”

To zależy, gdzie.

Jeśli lider jedzie w grupie GC, a jego pomocnik po wykonaniu pracy puszcza koło, lider zwykle nie ma powodu na niego czekać.

DS może powiedzieć pomocnikowi:

„koniec pracy, jedź ekonomicznie do mety”.

To ma znaczenie w etapówce. Zużywanie dodatkowych 300 kJ tylko po to, żeby ukończyć etap dwie minuty wcześniej bez korzyści sportowej, może być gorszą decyzją niż oszczędzenie energii na jutro. Myślenie Bewleya o „wydatku dzisiaj kontra potrzeby jutra” dokładnie wspiera taki model. citeturn19search9

Ale sytuacja odwraca się, jeśli:

- lider ma defekt,
- lider upadł,
- pomocnik jest potrzebny do dowiezienia go do grupy,
- etap jest płaski i samotny lider będzie fatalnie wystawiony na wiatr.

Wtedy DS może poświęcić pomocnika albo nawet kilku.

### „Ucieczka ma za dużo”

Najpierw należy zdefiniować „za dużo”.

5 minut może być absolutnie niegroźne.

2 minuty mogą być alarmem.

Liczy się nie gap, lecz:

\[
Threat =
Gap
\times RiderQuality
\times GCPosition
\times TerrainSuitability
\times RemainingDistance
\]

W praktyce DS patrzy między innymi na **virtual GC**.

Jeżeli zawodnik 4 minuty za liderem klasyfikacji dostanie 5 minut przewagi, zaczyna potencjalnie przejmować wyścig.

Jeżeli zawodnik jest 1:40 h z tyłu, nawet 12 minut może być dla ekipy lidera kompletnie obojętne.

Dlatego łatwy do implementacji system:

```text
for rider in break:
    virtual_gc = rider.gc_gap - break_gap
```

i dopiero to powinno wpływać na reakcję ekip GC.

### „Kto goni?”

Tu pojawia się prawdziwa teoria gier.

Załóżmy, że z przodu jest Van der Poel.

Za nim:

- Red Bull ma czterech zawodników,
- Lidl ma Pedersena,
- Soudal ma dwóch ludzi,
- Visma ma dobrego finishera.

Każda drużyna myśli:

> „Jeśli zacznę ciągnąć, zapłacę energią, a inni pojadą za darmo”.

I właśnie dlatego czasami bardzo silna grupa **nie łapie jednego zawodnika**, mimo że fizycznie mogłaby.

W E3 Saxo Classic 2026 Van der Poel utrzymał stosunkowo niewielką przewagę, a pościg cierpiał na dokładnie ten problem: największe ekipy miały sprzeczne interesy, próbowały ataków albo czekały, aż odpowiedzialność przejmie ktoś inny. citeturn20search1

To powinno istnieć w AI.

Każda drużyna ma:

\[
BenefitOfChase
\]

\[
CostOfChase
\]

oraz

\[
ExpectedOtherTeamsContribution
\]

Jeśli wszyscy uznają:

\[
Cost > IndividualBenefit
\]

nikt nie zaczyna.

Ucieczka wygrywa.

To o wiele ciekawsze niż skrypt:

> „przy 30 km do mety peleton zaczyna odejmować 10 sekund/km”.

### Drużyny sprinterów mają inny rachunek

Jeśli etap jest niemal idealny pod sprint, sprinter team ma znacznie większy interes w kontrolowaniu ucieczki.

W Tour de France 2026 Soudal-QuickStep i Alpecin na etapach sprinterskich potrafiły od początku utrzymywać ucieczkę na bardzo niewielkim dystansie czasowym właśnie po to, by zachować kontrolę nad scenariuszem finiszu. citeturn20search2turn20news34

W grze niech więc decyzja „pull” ma cenę.

Pomocnik jadący 300 W przez godzinę:

- zmniejsza przewagę ucieczki,
- ale traci durability i glikogen,
- może później nie być dostępny w lead-oucie,
- będzie bardziej zmęczony jutro.

Nagle odpowiedź „kto goni?” staje się decyzją, a nie automatem.

### Ranty

Rant zaczyna się zanim grupa się rozerwie.

DS wie:

- kierunek trasy,
- prognozę wiatru,
- które odcinki są otwarte,
- gdzie pojawi się boczny wiatr,
- gdzie droga jest szeroka lub wąska.

Przed wjazdem na potencjalny odcinek każe całej drużynie iść do przodu.

To kosztuje energię **przed** właściwym rantem.

Potem ekipa przyspiesza i ustawia się wachlarzem. Liczba chronionych miejsc jest ograniczona przez szerokość drogi i kąt pozornego wiatru. Zawodnicy poza echelonem są „w rynnie” i otrzymują znacznie mniejszy shelter. Badania aerodynamiczne potwierdzają potężne różnice między osłoniętym miejscem w echelonie a pozycją poza nim. citeturn16search3turn16search7

To daje genialny naturalny algorytm:

```text
available_sheltered_slots =
    f(road_width, wind_yaw, group_speed)

if rider_position > sheltered_slots:
    aero_shelter sharply decreases
```

I nagle rantu **nie trzeba skryptować**.

Wystarczy:

silny crosswind + wąska droga + wysoka prędkość + agresywna ekipa z przodu.

Fizyka zrobi resztę.

Drużyny analizują takie sytuacje wcześniej. Przykładowo Visma przed jednym z wietrznych etapów Touru szczegółowo oceniała nie samą siłę wiatru, ale jego kierunek względem drogi i na tej podstawie szacowała prawdopodobieństwo skutecznego rozbicia peletonu. citeturn14search0

### Radio też może przestać działać

To drobiazg, który mógłby świetnie działać w grze.

Podczas Tour de France 2026 zdarzały się problemy z radiami zawodników; gdy radio Michaela Matthewsa zawiodło, informacje trzeba było przekazywać w bardziej improwizowany sposób. citeturn11news35

Nie robiłbym z tego częstego RNG, bo byłoby irytujące.

Ale łączność może mieć:

- chwilowe zakłócenia,
- opóźnienie,
- zawodnik może nie usłyszeć polecenia,
- DS może nie wiedzieć, że zawodnik jest w problemie.

Wtedy gracz zarządza informacją, a nie kamerą boga.

## Briefing kontra decyzje podejmowane na żywo

Nie znalazłem wiarygodnego badania, które pozwalałoby powiedzieć coś w rodzaju:

> „73% decyzji DS zapada przed etapem”.

Takiego procentu nie należy wymyślać.

Natomiast z wypowiedzi dyrektorów i przykładów zespołów wyłania się bardzo czytelny system: **strategia i scenariusze powstają wcześniej, a dokładne wykonanie jest adaptowane na żywo.**

Przed etapem można ustalić:

- główny cel,
- hierarchię liderów,
- kto ma iść w ucieczkę,
- kto jej nie może odpuścić,
- sektory rantów,
- kluczowe podjazdy,
- miejsca pozycjonowania,
- kto pracuje w pościgu,
- plan lead-outu,
- plan żywieniowy,
- Plan B/C.

Ale briefing nie może z góry wiedzieć:

- kto znajdzie się w ucieczce,
- jak mocno będzie jechał peleton,
- kto się przewróci,
- czy lider będzie miał nogi,
- czy zmieni się wiatr,
- która drużyna będzie chciała pracować,
- kto zużyje pomocników wcześniej,
- czy rywal zaatakuje 30 km przed planowanym miejscem.

Przypadek Bena Healy'ego na 6. etapie Tour de France 2025 świetnie pokazuje całe spektrum planowania. Healy zaznaczył ten etap jako cel już po publikacji trasy. EF przygotowało ogólny plan, a w poranek etapu sztab przejechał fragment trasy i doprecyzował miejsce potencjalnego ruchu. Healy ostatecznie zaatakował około 42 km przed metą i wygrał solo. citeturn20search14turn20search0

Z drugiej strony Bewley opisuje dokładnie odwrotną sytuację: zakładany przed startem plan z ucieczką może zostać porzucony po 50 km, jeśli rzeczywistość wyścigu pokazuje, że jego koszt jest zbyt duży. citeturn19search9

Dlatego briefing w grze powinien tworzyć **conditional plan**, nie skrypt.

Na przykład:

```text
PRIMARY:
Protect Leader A

BREAK POLICY:
Send Rider F if break contains <= 12 riders
Do not chase harmless break

GC THREAT:
Chase if virtual GC threatens top 3 by < 60 s

CROSSWIND KM 82-104:
Move entire team to front at km 75
Attack only if effective crosswind > threshold

FINAL CLIMB:
Rider B pulls until exhausted
Rider C stays with leader
Leader may attack if W' > 45% at 4 km to go
```

A potem silnik wyścigu może powiedzieć:

na 73 km lider ma defekt.

Plan właśnie wyleciał przez okno.

I wtedy zaczyna się właściwa praca DS.

## Jak zbudowałbym z tego silnik twojej gry

Najbardziej realistyczny system nie wymaga symulowania każdej reakcji biochemicznej. Potrzebuje kilku dobrze dobranych warstw.

### Profil zawodnika

Podstawowe parametry fizjologiczne:

```text
CP [W]
W' [J]
Pmax
power-duration curve
durability_low
durability_high
recovery_W'
glycogen_capacity
carb_tolerance
heat_tolerance
sweat_rate
day_to_day_recovery
```

Parametry fizyczne:

```text
body_mass
CdA_road
CdA_TT
Crr/equipment
```

Parametry wyścigowe:

```text
positioning
handling
cobbles
descending
tactical_awareness
risk_tolerance
communication
self_assessment
```

Nie dawałbym osobnych magicznych wartości „climbing = 84”, „hills = 81”, „flat = 76” jako głównych determinantów.

Climbing powinno w dużej części **wynikać** z:

CP/kg + W′/kg + durability + masa + positioning + obecny fatigue.

Sprint z:

late Pmax + aktualne W′ + CdA + positioning + drafting.

TT z:

CP + CdA + pacing + durability + course.

Dzięki temu archetyp zawodnika wyłoni się z modelu, zamiast być narzucony etykietą.

### Stan dynamiczny zawodnika

W każdej chwili:

```text
W'_balance
glycogen
fluid_deficit
thermal_load
acute_fatigue
durability_loss
position
draft_quality
gap
current_power
recent_high_intensity_work
```

Między etapami:

```text
muscle_glycogen_recovery
cumulative_fatigue
sleep_quality
muscle_damage
illness_risk
injury
morale/stress
```

### Fizyczny power wymagany w danej sekundzie

Dla każdego zawodnika wyliczasz:

\[
P_{req}
=
P_{aero}(CdA,wind,shelter)
+
P_{rolling}(Crr,mass,surface)
+
P_{gravity}(mass,gradient)
+
P_{acceleration}(mass,a)
\]

na bazie dobrze zwalidowanego modelu fizycznego jazdy. citeturn16search0

Kluczowe jest:

\[
CdA_{effective}=CdA\times DraftMultiplier
\]

a `DraftMultiplier` wynika z pozycji w grupie, odległości, liczby osłaniających zawodników, wiatru i szerokości drogi.

Nie zmniejszaj przez drafting całego power. Zmniejszaj **część aerodynamiczną**.

To drobna różnica w kodzie, gigantyczna różnica w zachowaniu symulacji.

### Potem sprawdzasz, czy zawodnika na to stać

Jego aktualna zdolność nie powinna być stała:

\[
PowerCurve_{current}
=
PowerCurve_{fresh}
\times
DurabilityFactor
\times
GlycogenFactor
\times
HeatFactor
\times
HydrationFactor
\]

ale poszczególne mnożniki powinny działać różnie na różne czasy trwania.

Przykładowo wysoka wcześniejsza intensywność może mocniej uszkadzać późniejsze:

- 15 s,
- 1 min,
- 3 min,

niż CP, co odpowiada wynikom badań zawodowych kolarzy. citeturn17search17

Czyli nie:

```text
fatigue = 0.9
all stats *= 0.9
```

tylko raczej:

```text
P15s *= 0.84
P1m  *= 0.87
P5m  *= 0.91
CP   *= 0.97
```

dla konkretnego rodzaju zmęczenia.

Inny zawodnik będzie miał inny profil decay.

To właśnie tworzy „diesla”, „puncheura, który gaśnie”, czy klasykowca, który po sześciu godzinach nadal ma sprint.

### Mechanizm odpadania

Tu zrobiłbym dynamiczny gap.

Jeżeli zawodnik potrzebuje 500 W, ale obecnie jest w stanie dać maksymalnie 450 W:

nie powinien natychmiast dostać `dropped = true`.

Zaczyna po prostu jechać odrobinę wolniej.

\[
\Delta v < 0
\]

Gap rośnie:

\[
gap_{distance}(t+\Delta t)
=
gap_{distance}(t)+(v_{group}-v_{rider})\Delta t
\]

Gdy odległość wzrasta:

\[
DraftMultiplier \uparrow
\]

czyli ochrona aerodynamiczna maleje.

Więc:

\[
P_{req}\uparrow
\]

I może pojawić się lawina.

To samo wyjaśni:

- splits na rantach,
- „gumkę” z końca peletonu,
- urwanie po zakręcie,
- zawodnika wracającego po kilkunastu sekundach,
- zawodnika, który wisi 10 m za grupą i w końcu wraca,
- pomocnika celowo zwalniającego po zakończeniu pracy.

Nie trzeba żadnego osobnego skryptu „drop rider”.

### Model grupy powinien być równie ważny jak model zawodnika

Dwa identyczne watowo peletony nie powinny zachowywać się identycznie.

Potrzebujesz:

```text
road_width
group_density
group_length
wind_angle
corner_frequency
surface
pace_variability
front_team_intent
rider_position
```

Gęsta, spokojna grupa na szerokiej autostradzie może być energetycznie bardzo tania.

Ten sam peleton:

- na wąskiej drodze,
- przy bocznym wietrze,
- z drużyną atakującą z przodu,

staje się maszyną produkującą zmęczenie.

### AI DS powinno maksymalizować wynik, a nie minimalizować gap

Każda akcja powinna mieć koszt i wartość.

Przykład:

```text
Action: chase break

Expected benefit:
+35% chance bunch sprint
+12% chance stage win
prevents virtual GC threat

Expected cost:
Domestique A: +420 kJ
Domestique B: +280 kJ
lower leadout availability
higher tomorrow fatigue
```

DS wybiera chase dopiero wtedy, kiedy korzyść uzna za wartą ceny.

To automatycznie tworzy sytuację:

„Nikt nie chce gonić”.

A to jest jedno z najbardziej charakterystycznych zachowań prawdziwego peletonu, co dobrze ilustruje choćby E3 Saxo Classic 2026. citeturn20search1

### Pre-race tactics jako policy tree

Najlepszym systemem briefingu byłoby coś między instrukcją a drzewem warunków:

```text
IF dangerous GC rider attacks:
    IF satellite rider ahead:
        leader follows wheels
        do not chase with team
    ELSE IF rival team has incentive:
        wait 20 s
    ELSE:
        domestique_chase

IF crosswind:
    IF road exposure > threshold
       AND team_position_good:
        initiate_echelon

IF sprinter dropped:
    IF climb_remaining < X
       AND chase_cost acceptable:
        send 2 riders back
    ELSE:
        switch objective to breakaway rider
```

To znacznie wierniej odwzorowuje rzeczywistość niż „aggression slider 1–100”.

Przykłady Jumbo-Visma, EF oraz wypowiedzi zawodowych DS pokazują dokładnie takie podejście: wcześniej przygotowane role i scenariusze, następnie decyzja zależna od realnego przebiegu wyścigu. citeturn19search1turn20search14turn19search9

### Najważniejsza zasada projektowa

Gdybym miał sprowadzić cały research do jednego równania dla gry, byłoby to:

\[
\boxed{
\text{Czy zawodnik utrzyma grupę?}
=
f(
P_{required},
P_{available},
W'_{bal},
durability,
position,
draft,
gap
)
}
\]

a nie:

\[
\boxed{
\text{Czy zawodnik utrzyma grupę?}
=
stamina
}
\]

Fizyka mówi, ile watów wymaga sytuacja. CP/W′ mówi, jak długo zawodnik może tolerować wysoką intensywność. Durability, glikogen, temperatura i odwodnienie zmieniają to, co jest jeszcze dostępne po kilku godzinach. Pozycja decyduje, czy wymagane jest 220 czy 350 W. Przyspieszenie sprawdza chwilową rezerwę. Luka odbiera drafting, przez co sama zaczyna powiększać koszt. citeturn16search0turn15search3turn17search17turn16search1

W efekcie dostajesz coś bardzo bliskiego temu, co naprawdę widać w wyścigu: zawodnik może przez dwie godziny wyglądać znakomicie, nie odpowiedzieć na jeden pozornie niewielki ruch i zniknąć z grupy. Ktoś inny może mieć gorsze świeże waty, ale po pięciu godzinach nadal dysponować prawie pełnym pięciominutowym power. Sprinter może być fizycznie mocniejszy, ale przegrać przez utratę koła. Lider może świadomie nie odpowiadać na atak, bo jego rywal płaci za pogoń. Pomocnik może odpaść bez żadnego „bonka”, po prostu dlatego, że skończył robotę i nie ma ekonomicznego powodu wydawać kolejnych kilodżuli.

I wtedy wyścig zaczyna zachowywać się jak wyścig kolarski, a nie jak kilka statystyk jadących po profilu etapu.