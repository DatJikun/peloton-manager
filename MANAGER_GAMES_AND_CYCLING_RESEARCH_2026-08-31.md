# Peloton Manager — research: kolarstwo jako zarządzanie i co czyni gry menedżerskie dobrymi

**Wersja:** 0.1  
**Status:** RESEARCH SOURCE  
**Data:** 2026-08-31  
**Cel:** uzupełnić lukę obok `RACE_ENGINE_RESEARCH_2026-08-25.md`. Tamten dokument opisuje, jak naprawdę działa **wyścig**. Ten opisuje, jak naprawdę działa **kolarstwo jako sport zarządzania** oraz co w gatunku gier menedżerskich naprawdę trzyma gracza przy „jeszcze jeden dzień”.  
**Autorytet:** źródło researchu, nie kontrakt. Nie nadpisuje `VISION.md`, `DECISIONS.md`, `DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md` ani design notes.  
**Supersedes:** brak (wcześniej nie było gatunkowego researchu menedżerów).  
**Superseded by:** —  
**Related:** `RACE_ENGINE_RESEARCH_2026-08-25.md`, `DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md`, `VISION.md`, `Peloton_Manager_design_notes_v1.0.md`

---

## 0. Co już jest w repozytorium i czego ten dokument nie robi

Przed pisaniem tego pliku sprawdzone zostało całe drzewo `*.md`. Wynik:

| Dokument | Co pokrywa | Czego nie pokrywa |
|---|---|---|
| `RACE_ENGINE_RESEARCH_2026-08-25.md` | Fizjologia (CP/W′, durability, glikogen, ciepło), fizyka (CdA, drafting, wiatr), radio/TV/GPS DS, taktyka etapu, mapowanie na silnik wyścigu | Rynek, kalendarz, licencje, sponsorzy, transfery, co czyni **gatunek** menedżerski dobrym |
| `RACE_ENGINE_DESIGN_v0.2.md` | Kontrakt silnika z researchu wyścigu | Gatunek, ekonomia organizacji |
| `DESIGN_PRINCIPLES_AND_ANTI_PATTERNS.md` | **Prawa projektowe** Peloton Manager, wyciągnięte z PCM 26 i wcześniejszego managera właściciela | Porównanie FM / CM / OOTP / Motorsport Manager / FIFA Career; dowody z zewnątrz |
| `VISION.md` + design notes | Fantasy, locki, anti-goals („nie PCM bez 3D”), pętla informacji | Research źródłowy, liczby z peletonu, case studies gier |
| `DECISIONS.md` | Owner locki (symetria, knowledge, ManagerCareer, Advance Day, sponsor market…) | Uzasadnienie gatunkowe *dlaczego* te locki są zgodne z tym, co działa w innych grach |

**Wniosek:** research wyścigu i prawa UX już istnieją. Brakowało researchu **gatunku** i **kolarstwa poza rowerem**. Ten plik to uzupełnia.

Ten dokument **nie** jest nową wizją gry. Nie proponuje cichej zmiany locków. Tam, gdzie finding zderza się z lockiem, lock wygrywa; finding służy tylko jako argument, że lock jest trafiony, albo jako otwarte pytanie do późniejszego designu.

---

## 1. Najkrótszy wniosek

Dobre gry menedżerskie nie są arkuszami z ładniejszą czcionką. Są grami o **decyzjach z kosztem, niepełną wiedzą i konsekwencjami, które da się opowiedzieć**.

Kolarstwo jest do tego wyjątkowo dobrze ułożone — ale **inaczej niż piłka**. Piłka daje cotygodniowy mecz jedenastu pozycji. Kolarstwo daje **kalendarz szczytów**, **ósemkę, która poświęca się dla jednego**, **organizację, która umiera ze sponsorem**, i **wyścig, w którym rywale muszą współpracować, żeby kogoś złapać**.

Peloton Manager ma już locki, które to rozumieją: delegacja zamiast sterowania nogami, prawda vs wiedza, manager nie jest klubem, sponsor market zamiast ukrytego podatku, realizm nie broni nudy. Research gatunku i peletonu **potwierdza te locki**. Największe ryzyko nie polega na braku feature’ów, tylko na skopiowaniu złych wzorców z PCM (3D micromanagement, martwy rynek, kłamliwy instant result) albo z FIFA Career (mecz jako produkt, zarządzanie jako dodatek).

---

## 2. Kolarstwo jako sport zarządzania

Ta sekcja **nie** powtarza fizjologii z `RACE_ENGINE_RESEARCH_2026-08-25.md`. Chodzi o to, czym manager kolarski zarządza **między** wyścigami i **dlaczego** te decyzje są ciekawe.

### 2.1 Klub to nie klub. To pojazd sponsora

W piłce klub ma stadion, kibiców, prawa TV i herb starszy od trenera. W kolarstwie WorldTour drużyna jest zwykle **projektem komercyjnym z nazwą sponsora na koszulce**. Gdy sponsor odchodzi, projekt się składa, fuzuje albo zmienia tożsamość.

Sezon 2025→2026 to nie anegdota, tylko normalna fizyka sportu:

- Arkéa–B&B Hotels złożył się, bo nie zasypał luki rzędu €20–30 mln.
- Lotto i Intermarché-Wanty zlały się w jeden WorldTeam, bo osobno nie utrzymały tytułu; część kolarzy i sztabu wypadła, długi i brak drugiego sponsora zmusiły fuzję.
- Kilka ekip zmieniło nazwę albo title sponsora w jednym off-seasonie (m.in. Decathlon CMA CGM, XDS Astana, NSN, Picnic-PostNL).

Źródła: [Cyclingnews o fuzji Lotto–Intermarché](https://www.cyclingnews.com/news/officially-submitted-mooted-merger-further-solidifies-as-lotto-confirms-joint-uci-registration-application-with-intermarche-wanty/), [Inner Ring — Merger of Unequals](https://inrng.com/2025/10/lotto-intermarche-merger/), [Rydecruz 2026 sponsor shake-up](https://rydecruz.com/blogs/pedal-press/2026-worldtour-sponsor-shake-up-every-team-name-bike-and-wheel-change-explained).

To jest dokładnie to, co design notes już nazywają „klub to nie sponsor”. Dla gry oznacza:

- sukces sportowy nie gwarantuje istnienia organizacji,
- nazwa, kolory i budżet mogą zniknąć bez „bankructwa w stylu FM”,
- manager może przeżyć klub (lock `D-004`),
- rynek sponsorów **jest** ekonomią, nie tłem (`D-011`).

### 2.2 Pieniądze płyną prawie wyłącznie od sponsorów

UCI pokazało na seminarium WorldTour w Genewie liczby, które *La Gazzetta dello Sport* i potem prasa kolarska rozpisały na 2026:

- łączny budżet męskiego WorldTouru (w próbie UCI 20 ekip, nie tylko 18 licencji): **€663 mln**,
- średnia ekipa: **ok. €33,1 mln** (w 2023 było ok. €26 mln),
- mediana niżej, super-ekipy (UAE, Visma) w okolicach **€50 mln**, kolejna półka ~€45 mln,
- **ok. 87% przychodu** ze sponsorów,
- zespoły **nie dostają** zbiorczych praw TV ani collective merchandisingu,
- mediana pensji self-employed kolarza ok. **€350k**; średnia self-employed ~€654k, employed ~€384k; Pogačar w osobnej skali (~€8 mln od zespołu, więcej z umów osobistych).

Źródła: [Domestique, 3 stycznia 2026](https://www.domestiquecycling.com/en/news/bigger-budgets-bigger-gaps-worldtour-spending-hits-eur663-million/), [Cyclingnews o budżetach 2026](https://www.cyclingnews.com/pro-cycling/teams-riders/2026-mens-worldtour-budgets-total-eur663-million-as-median-male-rider-salaries-touch-eur350-000/), [Cyclingnews — 87% od sponsorów](https://www.cyclingnews.com/pro-cycling/where-do-cyclings-super-teams-spend-their-millions/).

Jonathan Vaughters (EF) ujął to wprost: peleton próbuje pływać w cenach wielkiej ligi na **jednym filarze przychodu**. Super-ekipa, która „wyda ile trzeba, żeby wygrać wszystko”, pcha inflację płac przez resztę rynku.

**Dla gry:** ukryty luxury tax albo procentowa inflacja przez 100 lat jest złym naśladownictwem piłki. Naturalny presja to: sponsor wchodzi / wychodzi, Tour sprzedaje ekspozycję, brak udziału w prawach TV, pogoń płacowa za gwiazdami, fuzja albo śmierć projektu. To już jest `D-011` / `D-012`. Research tylko dodaje liczby i mechanizm.

Prize money w kolarstwie **nie** utrzymuje WorldTeamu. Wygrana etapu Touru jest historią i argumentem sprzedażowym, nie modelem biznesowym. Design principle „sporting success should have traceable financial consequences” nadal obowiązuje — ale konsekwencja idzie przez **widoczność i odnowienie sponsorów**, nie przez przelew z ASO pokrywający payroll.

### 2.3 Trzy ligi, licencja na trzy lata, dzika karta organizatora

Współczesny szkielet (męski, cykl 2026–2028):

- **18 UCI WorldTeams** z licencją na trzy lata. Licencja to nie tylko punkty: etyka, finanse, administracja, organizacja, sport. Gdy chętnych jest więcej niż miejsc, rozstrzyga kryterium sportowe — suma punktów UCI z trzech sezonów.
- **ProTeams** (druga dywizja): mogą dostać automatyczne zaproszenia do WorldTouru, jeśli są w czołówce rankingu drugiej ligi z poprzedniego sezonu; poza tym żyją z dzikich kart.
- **Continental** (trzecia): lokalny kalendarz, często development parent teamu.
- Ranking drużynowy: punkty **20 najlepszych kolarzy** kontraktowych, reset na start sezonu, publikacja co tydzień.
- Grand Tour od 2025: **23 ekipy**. WorldTeams + obowiązkowe zaproszenia z rankingu + wildcards organizatora. Od 2026 wildcard Grand Touru tylko dla ekip z **top 30** poprzedniego sezonu.
- WorldTour: **8 kolarzy** na starcie Grand Touru, **7** na pozostałych WT.

Źródła: [UCI — licencje 2026–2028](https://www.uci.org/pressrelease/allocation-of-the-14-uci-womens-worldtour-licences-and-18-uci-worldtour/6QRootu0WfaZVcXXJVQerE), [UCI — 23 ekipy w GT](https://www.uci.org/pressrelease/the-number-of-teams-participating-in-the-mens-grand-tours-increased-to-23-by/1jF0eFAMAXIr98KOHYIe62), [Cyclingnews — jak liczą się punkty](https://www.cyclingnews.com/features/how-does-the-uci-worldtour-points-system-work/), [Beyond the Peloton 2025 review](https://beyondthepeloton.substack.com/p/2025-season-in-review-part-1-deciphering), regulamin UCI part 2 (start lists, top-20 riders, wildcards).

**Co to robi z gameplayem** (i czego piłka nie ma w tej postaci):

1. **Trzyletni cykl** to campaign inside a campaign. Sezon „udany” może być fatalny dla licencji, jeśli dwa poprzednie były słabe.
2. **Top 20 kolarzy** karze wąski roster gwiazd i nagradza głębokość. Kontuzja lidera nie zeruje drużyny, ale dziura w dwudziestce boli.
3. **Wildcard** to polityka, nie tylko sport. Organizator (ASO, RCS, Unipublic) ma własny interes: lokalna ekipa, sponsor wyścigu, widowisko. Top 30 obcina „darmowe” karty outsiderom.
4. **Obowiązkowy kalendarz WorldTouru** oznacza, że nie można „grać tylko Touru”. Trzeba obsadzić klasyki, tygodniówki i trzy GT skończonym składem ~30 osób.

To jest naturalny conflict of goals: punkty na licencję vs. GC w Maju vs. Flandria vs. ekspozycja sponsora w kraju X. Design notes i zasady kalendarza (`Calendar provenance`, `Calendar audit`) już na to czekają.

### 2.4 Kalendarz jest taktyką sezonu

W piłce liga ustawia Ci soboty. W kolarstwie **Ty** ustawiasz, kto i kiedy ma być w szczycie.

Sport ma trzy nakładające się sezony w jednym roku:

- **luty–kwiecień:** klasyki i monumenty (bruk, ardeny), krótki, brutalny szczyt,
- **maj–wrzesień:** Giro, Tour, Vuelta + tygodniówki przygotowawcze,
- **wrzesień–październik:** mistrzostwa, klasyki jesienne, zamykanie punktów.

Kolarz nie jest w szczycie od stycznia do października. Klasykowiec buduje formę na Flandrię/Roubaix; lider GT na trzy tygodnie w lipcu. Ten sam człowiek rzadko wygrywa oba na tym samym poziomie — a jeśli wygrywa (van der Poel, Pogačar), jest wyjątkiem, nie template’em.

Agent Dries Smets mówi wprost, że klasyki wiosenne są **witryną transferową**: w kilka tygodni wartość kolarza na końcówce kontraktu może się podwoić, albo runąć przez kontuzję. Źródło: [dnlbenson — Classics and the market](https://dnlbenson.substack.com/p/cycling-transfers-why-and-how-the).

**Dla gry:** kalendarz nie jest checklistą „wpisz 200 wyścigów”. Jest decyzją o **szczytach, rolach i opportunity cost**. PCM 26 próbował to zrobić nowym plannerem i dostał pochwały za zamiar oraz krytykę za UI, który gubi pracę gracza („Recalculate” kasuje ręczny plan) i absurdy AI (cel bez wyścigów przygotowawczych). Źródło: [Lev3lup recenzja PCM 26](https://lev3lup.be/blog/review-pro-cycling-manager-26/), [Velora o plannerze](https://veloracycling.com/tech/pro-cycling-manager-26-review-race-planner-2026).

Peloton ma już zasady: obóz musi mieć cel, recon koliduje z wyścigiem, audit przed zatwierdzeniem planu. Research kolarski mówi, że **to jest core loop organizacji**, nie dodatek.

### 2.5 Ósemka, nie jedenastka. Hierarchia, nie pozycje

WorldTeam licencjonuje zwykle **do 30 kolarzy** (pas regulaminowy bywa podawany jako ok. 23–30; ekipy topowe domykają 30). Na Tour wjeżdża **ośmiu**. Siódemka / ósemka nie ma „bramkarza, stopera, „9””. Ma:

- lidera GC albo kandydata na etap,
- górskich / brukowych pomocników,
- woziwodę, który wozi bidony i oddaje koło,
- sprintera + lead-out **albo** klasykowca — rzadko wszystko naraz,
- kolarza, którego jedyną robotą jest być **zagrożeniem**, żeby rywal nie mógł go puścić.

To jest ekonomia poświęcenia. Sześciu ludzi pracuje, żeby siódmy miał waty na 4 km do szczytu. Research wyścigu już to modeluje przez energię i gap. Research **zarządzania** dodaje: obietnica roli (`Leader` jako kontrakt, nie suwak), dwa programy w jednej ekipie (klasyki + GT) wymagają głębokości budżetu, a zły skład na Tour jest błędem **kwietnia**, nie 1 lipca.

PCM 26 wprowadził hierarchy / role i atrybuty Stage Race Focus vs Classics Focus. Recenzenci chwalą to jako jedną z niewielu mechanik, które naprawdę zmieniają planowanie. Źródło: [Cyanide devblog / Steam PCM 26](https://steampulse.org/game/3936530/news), [TheBigBois recenzja](https://thebigbois.com/strategy/pro-cycling-manager-26-review/). Wniosek dla Peloton: role mają wynikać z fizjologii i kalendarza (lock `D-018`: archetypy emergentne), a UI może streszczać je etykietą. Nie odwrotnie.

### 2.6 Rynek transferowy nie jest oknem FIFA

Nie ma jednego Mercato. Negocjacje toczą się w sezonie. Historyczna data 1 sierpnia nadal organizuje **ogłoszenia**, ale rozmowy, przedłużenia i ruchy neo-pro dzieją się wcześniej; Tour jest szczytem rozmów. Kolarz bez kontraktu na przyszły rok używa klasyków jako audycji. Mid-season move istnieje, ale dotyczy głównie wolnych agentów i neo-pro, nie „okna zimowego”.

Źródła: [Cyclingnews transfer hub 2027](https://www.cyclingnews.com/pro-cycling/transfers/cycling-transfers-all-the-latest-news-and-announcements-for-the-2027-season/), [dnlbenson Transfer Index](https://dnlbenson.substack.com/p/cycling-transfer-index-20262027).

PCM od lat dostaje tę samą recenzję rynku: AI jest leniwe, klauzule cienkie, youth promotion mechaniczna, dossier-punkty zamiast ludzi. Galaxus o PCM 25: kolarze „klikają się” do małych ekip, brak agentów i powodów zmiany. Źródło: [Galaxus PCM 25](https://www.galaxus.at/en/page/tactics-timing-triumph-how-i-helped-marc-hirschi-to-win-the-tour-de-france-stage-38353). Lev3lup o PCM 26: rynek wciąż nijaki.

Peloton już zablokował antidotum: dossier jako sprawa, agent jako aktor, brak hard gate’u, AI na tym samym rynku, knowledge per organizacja. Research gatunku (FM, OOTP, CM 01/02) mówi, że **to jest miejsce, gdzie menedżer żyje latami**. Research PCM mówi, że Cyanide tego miejsca nie wygrało.

### 2.7 Development team to nie akademia FIFA

WorldTeam / ProTeam może mieć zarejestrowany **UCI Continental development team** tej samej narodowości. Od 2020 kolarze Conti mogą wjeżdżać do parent teamu na wyścigi .Pro (do 2) i .1 (do 4); w drugą stronę pro może zjechać poprowadzić młodzież. Punkty zdobyte „na dzień” zostają w Conti. Super junior dostaje kontrakt i pensję, ale kalendarz U23 zamiast od razu Touru.

Źródło: [Inner Ring — On Development Teams](https://inrng.com/2022/01/uci-continental-development-teams/).

To jest ciekawsze niż „youth intake 12 lipca”. Jest też pułapka: bogate ekipy zamykają silos talentu. Dla Peloton: akademia jako **osobna organizacja z własnym kalendarzem i wiedzą**, nie suwak „youth facilities 20”. Nie trzeba tego budować w pierwszym vertical slice.

### 2.8 Polowanie na punkty UCI jest prawdziwe i niebezpieczne jako meta

System punktów ma sprawić, że licencja WorldTouru zależy od wyników, nie tylko od budżetu. W praktyce peleton uczy się **optymalizować punkty**: wysyłać puntów na .1, zbierać miejsca 8–15, traktować niektóre wyścigi jako farmę, nie jako historię.

Dla gry to legalny conflict (licencja vs. Tour), ale zły **jedyny** cel. Jeśli AI i gracz wygrają save’a zbieraniem trzecich miejsc w marcu, a Tour będzie ozdobą, produkt będzie PCM-em od strony biurokracji. Design notes mają anti-cheese i „fun beats feature count”. Research mówi: punkty muszą istnieć jako **presja licencyjna**, nie jako high-score.

### 2.9 Co z kolarstwa już jest w researchu wyścigu

Nie powtarzać tu: CP/W′, durability, drafting, radio, briefing vs live, atak-po-ataku, kto goni. To `RACE_ENGINE_RESEARCH_2026-08-25.md`. Jedyne, co warto złączyć:

- **Manager** ustawia skład, szczyt, briefing i ludzi.
- **DS** wydaje energię w dniu wyścigu na niepełnej informacji.
- Gracz, który w PCM klika „jedz / atakuj / schowaj się w kole” przez 180 km, gra w inną grę. VISION to odrzuca. Research gatunku (niżej) potwierdza, że odrzucenie jest słuszne, **o ile** dzień wyścigu nadal ma rzadkie, drogie decyzje.

---

## 3. Co czyni gry menedżerskie dobrymi

Poniższe prawa pochodzą z gier, które utrzymały ludzi przez dekady, oraz z GDC / esejów projektantów. To nie jest lista feature’ów do skopiowania.

### 3.1 Gra to seria ciekawych decyzji

Sid Meier (GDC 2012, *Interesting Decisions*): gra to seria ciekawych decyzji. Decyzja jest ciekawa, gdy:

- ma **trade-off** (coś zyskujesz, coś tracisz),
- jest **sytuacyjna** (ta sama opcja nie jest zawsze najlepsza),
- gracz ma **inwestycję** w wybór (to jego plan, nie tutorial),
- jest **więcej niż jedna rozsądna odpowiedź**, ale nie dwadzieścia checkboxów,
- wynik da się **oszacować, ale nie mieć pewności**.

Źródła: [Gamasutra / Game Developer, GDC 2012](https://www.gamedeveloper.com/design/gdc-2012-sid-meier-on-how-to-see-games-as-sets-of-interesting-decisions), [GDC Vault](https://www.gdcvault.com/play/1015756/Interesting).

To jest słowo w słowo test z `DESIGN_PRINCIPLES` §31 i VISION „Meaningful Decisions”. Research nie dodaje nowego locka. Dodaje ostrzeżenie Meiera o **decision fatigue**: każda decyzja, też głupia, zużywa wolę. Manager, który pyta o 40 checkboxów przed etapem, nie jest „głęboki”. Jest męczący. PCM i wcześniejszy projekt właściciela wpadły w tę dziurę od strony wyścigu.

### 3.2 „Jeszcze jedna tura” / „Continue”

Meier o Civilization: nagradzaj za dojście tutaj, ale myśl o tym, **co będzie za rogiem**. Cliffhanger trzyma lepiej niż ekran „sezon zakończony, statystyki”.

W menedżerach sportowych ten przycisk nazywa się Continue / Advance Day / Next. Działa, gdy po kliknięciu:

- świat się ruszył bez Ciebie,
- wróciła nowa informacja (oferta, skaut, kontuzja, wynik rywala),
- została otwarta decyzja, nie zamknięta lista tasków.

`D-006` (Advance Day + living world) jest więc nie tylko modelem czasu. Jest pętlą uzależnienia gatunku. FIFA Career psuje ją, bo między meczami świat jest martwy. FM trzyma ją, bo skrzynka i rynek nigdy nie milczą.

### 3.3 Symulacja ma być story-rich, nie maksymalnie wierna

Tynan Sylvester (*The Simulation Dream*, 2013): sen o pełnej ekologii świata pada, gdy gracze **nie zauważają** systemu. Ultima Online wycięło wirtualną ekologię, bo nikt jej nie zobaczył. Wartość gry żyje w **modelu w głowie gracza**, nie w kodzie. Komputer robi logistykę; gracz dokłada znaczenie (apophenia). System ma być **skondensowany narracyjnie**, nie wierną kopią.

Źródło: [tynansylvester.com/2013/06/the-simulation-dream](https://tynansylvester.com/2013/06/the-simulation-dream/).

To **nie** kłóci się z matematycznym silnikiem wyścigu. Kłóci się z dodawaniem fizjologii, której nikt nie umie odczytać. Dlatego Peloton ma już: knowledge-bounded decyzje, debrief, Race Spy dla dewelopera, „results are evidence”. Sylvester ostrzega przed drugim BioShock ecology: głęboki glikogen, którego UI nie umie opowiedzieć, jest wart tyle co wilki w UO.

Synteza dla tego projektu:

```text
Silnik może być twardy (CP/W′, gap, shelter).
Gracz dostaje obserwacje, historię etapu i ludzi.
Spy widzi prawdę.
Jeżeli system nie wchodzi do modelu w głowie gracza — nie istnieje jako gameplay.
```

To jest też argument przeciw „PCM bez 3D”: 3D nie jest historią. Historią jest „pomocnik oddał się na 4 km, DS nie złapał, lider został sam, sponsor krzyczy o Flandrię”.

### 3.4 Dźwignia musi ciągnąć przeciwną dźwignię

Overbaked Studio (2026): w złym management simie podnosisz liczbę, inna liczba też idzie w górę. W dobrym **każdy wybór kosztuje coś, co też chciałeś zatrzymać**. Two Point Hospital: nowy gabinet zjada kasę, powierzchnię i uwagę staffu naraz.

Źródło: [What Makes a Good Management Sim Fun?](https://overbaked.studio/blog/what-makes-a-management-sim-fun/).

W kolarstwie naturalne dźwignie:

- szczyt na Tour **albo** na klasyki,
- wildcard na Giro **albo** świeżość na lipiec,
- pensja gwiazdy **albo** głębokość dwudziestki punktowej,
- recon **albo** wyścig przygotowawczy,
- agresywny briefing **albo** ochrona lidera.

Jeżeli signing zawsze pomaga i nigdy nie psuje chemii / payroll / ról, to nie jest manager.

### 3.5 Niepewność jest rozgrywką, nie utrudnieniem UI

CM 01/02 stał się legendą nie przez grafikę (nie miał jej) i nie przez liczbę suwaków. *The Athletic* (2021): mecz w pięć minut, sezon w sześć godzin, kariera w miesiąc; **brak obrazu meczu nie osłabiał gry — umysł malował sam**. Mgła atrybutów i skauci, którzy się mylą, robiły z rynku polowanie. Tsigalko jest pamiętany, bo był **odkryciem**, nie cyfrą z sofifa.

Źródła: [The Athletic — CM 01/02 twenty years on](https://www.nytimes.com/athletic/2876413/2021/10/12/championship-manager-01-02-revisiting-an-old-friend-two-decades-on/), [Football Whispers — Tsigalko](https://footballwhispers.com/blog/a-tribute-to-maxim-tsigalko-my-favourite-football-manager-signing/).

OOTP mówi to samo językiem systemu: skaut nie dostaje true ratings. Dostaje **widok** z błędem; younger / international = większy błąd; scout „tools” vs „ability” to **wybór kogo zatrudnić**, nie jeden najlepszy skaut. Development ma szansę, challenge level, flop high-potential. Źródło: [OOTP developer guide to scouting](https://forums.ootpdevelopments.com/showthread.php?t=363170), [OOTP 22 manual — player development](https://manuals.ootpdevelopments.com/index.php?man=ootp22&page=player_development).

Football Manager zbudował wokół tego produkt: sieć skautów, hidden attributes, „regens”. Miles Jacobson jednocześnie traktuje pomyłkę bazy (gracz w grze ≠ rzeczywistość) jako **porażkę researchu**, nie jako feature — i jednocześnie wie, że te pomyłki **żyją w save’ach ludzi latami**. Źródło: [Telegraph — Jacobson](https://www.telegraph.co.uk/gaming/features/miles-jacobson-interview-like-think-managing-watford-wouldnt/).

Peloton idzie dalej niż FM w locku `D-010`: AI też nie czyta true ability. To jest zgodne z OOTP/FM **dla człowieka** i ostrzejsze wobec bota. Research mówi: niepewność działa, gdy ma **źródło** (skaut, agent, wynik, plotka), **pewnność** i **koszt zdobycia**. Nie działa, gdy UI kłamie albo ukrywa checkbox.

### 3.6 Świat musi iść bez gracza

FM trzyma, bo Tottenham kupuje Twojego targetu, gdy grasz mecz. OOTP trzyma, bo 29 innych GM-ów licytuje. PCM słabnie, gdy AI rynku jest „leniwe”. FIFA Career słabnie, gdy ligę wygrywający Pep dostaje dymisję w tym samym sezonie, a transfer requesty sypią się z ławki.

Living world to nie „losowe eventy przeciw graczowi”. To **symetria** (`D-002`). Research gatunku: jeżeli tylko Human Organization jest prawdziwa, po trzech sezonach widać tekturowe tło.

### 3.7 Ludzie, nie kolumny

Overbaked: liczby mówią, co się dzieje; postacie sprawiają, że Cię to obchodzi. RimWorld zamienia kryzys zasobów w pretensje konkretnego kolonisty. VISION Peloton: „czy obchodzi mnie, co stanie się z tymi ludźmi?”.

W menedżerach sportowych ta magia siada na:

- obietnicach roli,
- flopie cudownego dziecka,
- DS-ie, który broni złej decyzji,
- sponsorze, który niszczy projekt,
- rywalu, którego znasz z debriefu, nie z overallu.

F1 Manager 2024 jest chwalony za strategię weekendu i krytykowany za **brak relacji kierowców / team orders z osobowością**. Źródło: [Game8 recenzja F1 Manager 2024](https://game8.co/reviews/f1-manager-2024/f1-manager-2024-review). PCM: twarze wymienne, rynek bez powodów. Wniosek: avatar i dossier nie są kosmetyką. Są nośnikiem apophenii.

### 3.8 Fast sim musi kłamać mniej niż live, nie bardziej

Historyczny grzech PCM: Instant Result produkował sprintera na górze i minuty, których 3D by nie dało. PCM 26 „Detailed Simulation” to w praktyce **ten sam silnik bez renderu** — i recenzenci piszą, że to największy skok od lat, bo wyniki przestają być losowym roll. Źródła: [TheBigBois PCM 26](https://thebigbois.com/strategy/pro-cycling-manager-26-review/), [Lev3lup](https://lev3lup.be/blog/review-pro-cycling-manager-26/).

CM 01/02 pokazał odwrotną lekcję: **tekst wystarczy**, jeśli silnik jest uczciwy i tempo sezonu jest ludzkie.

Peloton ma już: jeden silnik dla Watch i Simulate, Key Race Story, D-033 (płynny zegar, nie teleport). Research: nie budować drugiego, gorszego resolvera „żeby było szybciej”. Szybkość ma być **pominięciem filmu**, nie pominięciem przyczyny.

Kalendarz kolarski ma ~200+ dni wyścigowych w WorldTourze. Nikt nie obejrzy wszystkich 1:1. Gęstość decyzji musi być **rzadka i droga**; reszta to sim + historia. PCM 2023: etapy po 30 minut klików, potem lawina administracji — recenzent tracił chęć. Źródło: [Holygamerz PCM 2023](https://www.holygamerz.com/en/pro-cycling-manager-2023-the-manager-review-for-true-fans).

### 3.9 Głębia ukryta, nie zrzucona na głowę

Jacobson: SI projektuje na **kohorty** (nowi → 1000+ godzin) i wymaga, by karta feature’u mówiła, **dla kogo** jest. Źródło: [wywiad FM26](https://fm.zweierkette.de/en/interview-with-miles-jacobson-about-fm26/).

Jacobson też: najczęściej proszony feature, którego **nie zrobią**, to start jako youth-team manager — bo to byłoby **nudne** (brak transferów, mediów, taktyki; trening, którego i tak mało kto używa). Źródło: [FourFourTwo — How we make Football Manager](https://www.fourfourtwo.com/features/miles-jacobson-how-we-make-football-manager-future-and-where-you-come-it).

Lekcja: nie każdy realistyczny etat jest grą. Peloton ma managera organizacji, nie „trenera juniorów z checkboxem treningu”. Szczegół treningu, który nikt nie chce klikać, jest anti-patternem SI potwierdzonym dekadą danych.

Overbaked: ucz jeden system na raz. PCM i FM straszą pierwszym ekranem. VISION Peloton: gra ma być zrozumiała bez wiedzy o kolarstwie; terminologia później. To nie jest „casualizacja”. To jest kolejność nauczania.

### 3.10 Realizm nie usprawiedliwia nudy — nawet u twórców FM

Jacobson odrzuca feature, który brzmi dobrze na papierze, jeśli playtest pokazuje, że nikt w to nie gra. Właściciel Peloton ma tę samą lekcję z Ping-Pong Managera. Research gatunku ustawia to jako **normę branży**, nie kaprys.

Meier: AI nie powinno być „zbyt dobre”; pierwsze minuty muszą dać nagrodę, przegrana przychodzi później. Źródło: podsumowanie memoir / GDC (*The Psychology of Game Design*).

---

## 4. Case studies

### 4.1 Championship Manager 01/02 — mgła, tempo, wyobraźnia

**Co działa:** ukryte atrybuty, skauci z błędem, tekstowy mecz, sezon w ludzkim czasie, emergentne legendy (Tsigalko, wonderkids), prosty UI.

**Czego nie kopiować:** twardy cheat meta (znane imiona z forów), brak wyjaśnialności silnika, kosmetyczne atrybuty ≠ atrybuty meczowe (community to odkryło latami).

**Most do Peloton:** Watch nie musi być 3D. Musi być uczciwy. Odkrycie juniora ma być historią. Tempo sezonu jest częścią fun gate’u.

### 4.2 Football Manager — proces, nie gol

FM wciąż ustawia standard, bo modeluje **procesy wokół meczu**: zmęczenie → kontuzje → rotacja → morale → wynik → zaufanie zarządu. Nie ma jednej dźwigni do exploita w nieskończoność. Baza skautów jest produktem **i** kotwicą w rzeczywistości.

**Słabości, których Peloton nie chce:** coroczny cykl „nowa baza + stary silnik”; UI, który karze nowicjusza gęstością; trening jako feature, którego nikt nie otwiera; kohorta 1000h, która krzyczy przy każdej zmianie UI (FM26).

**Most:** interlocking systems, scouting as gameplay, Continue. Nie: licencja na 500k prawdziwych kolarzy jako warunek istnienia gry. Peloton jest o **alternatywnej historii**, nie o corocznej bazie Cyanide/SI.

### 4.3 Out of the Park Baseball — skaut jako wybór, rozwój jako szansa

OOTP jest najbliżej Peloton w filozofii hidden ratings. Świadomie: różne typy skautów, żaden nie jest ściśle lepszy; rozwój może zaskoczyć w obie strony; gracz może **wyłączyć** scouting, jeśli nie chce tej gry.

**Most:** locki fog-of-war w design notes (Full / Estimated / Hidden / None) są w rodzinie OOTP. Challenge-level development (zbyt łatwo na A-team = stagnacja) jest kandydatem na późniejszy design treningu, nie na prototype.

### 4.4 Pro Cycling Manager — najbliższy kuzyn i lista rzeczy, których nie powtarzać

**Co PCM robi dobrze (szczególnie 26):** jeden świat kolarski, kalendarz, role, świadomość, że Instant Result kłamał, Detailed Sim = ten sam engine, sprint/lead-out i drafting jako gameplay.

**Co psuje fun, według recenzji 2023–26:**

- 3D, które trzeba klikać albo oglądać za długo, a wygląda źle,
- micromanagement energii / jedzenia / pozycji jako substytut decyzji DS,
- planner, który jest głęboki i jednocześnie gubi pracę gracza,
- martwy transfer market i mechaniczne dossier-punkty,
- AI, które goni niegroźną ucieczkę cały dzień,
- lawina administracji między etapami,
- brak agentów, powodów, osobowości,
- grafika i twarze, które nie noszą historii.

VISION: „nie PCM-em bez 3D”. Research: **bez 3D to zaleta**, jeżeli silnik i rynek są lepsze. PCM bez 3D, ale z tym samym martwym rynkiem i checkboxami, byłby nadal nudny. To jest lekcja Ping-Pong Managera przeniesiona na kolarstwo.

### 4.5 Motorsport Manager (2016) vs F1 Manager

Motorsport Manager: weekend wyścigowy ma **rzadkie, drogie** decyzje (opony, deszcz, safety car, ego kierowców, cele sponsora). Off-track (części, budżet, morale) wiąże się z torem. Słabość: długi wyścig, którego nie można zostawić, staje się nużący.

F1 Manager 2024: lepszy sim, Create-a-Team, Mentality Hub — i powtarzalny weekend (praktyka → setup → powtórz), popsutę kontrakty na starcie, brak dynamiki między kierowcami. Frontier musiał dodać **simulate race**, bo długość weekendu zabijała karierę.

**Most:** Watch Race Peloton jest bliżej MM/F1 niż FM (wydarzenie trwa długo). D-033 i pauza na DecisionRequest są odpowiedzią. Nie wolno wpaść w F1-ową pętlę „ustaw, czekaj, ustaw, czekaj” bez nowej informacji. Research wyścigu już wymaga, by popup nie zastępował decyzji.

### 4.6 FIFA / FC Career — jak zabić managera

Career Mode od lat jest oskarżany o: nielogiczne dymisje, spam transfer requestów, budżet bez rozmowy z zarządem, scouting-generyczny, brak życia świata, mecz jako właściwy produkt. EA w notatkach FC 27 obiecuje wieloetapowe transfery i mniej teatrzyku 3D przy negocjacjach — czyli **ucieczkę od własnego anti-patternu**.

**Most:** nie budować Peloton jako „ładny wyścig z menu kariery dookoła”. Wyścig jest testem decyzji. Kariera jest grą.

### 4.7 Civilization i RimWorld — kuzyni nie-sportowi

Civ: one more turn, interesting decisions, AI, które nie ma Cię upokorzyć w minucie pierwszej.

RimWorld: story generator, nie „wygrana”; eventy z ludzkimi stawkami (życie, relacje), nie logistyka dla logistyki.

Peloton chce emergent history **bez** AI storyteller, który podkłada dramat pod save. Locki mówią: historia wychodzi ze zderzenia systemów. Sylvester i tak jest użyteczny: **pokaż** zderzenie w debriefie, inaczej zostanie w krzemie.

---

## 5. Antywzorce gatunku (checklista)

Skrót rzeczy, które regularnie zabijają managerów. W nawiasie lock / zasada Peloton, jeśli już istnieje.

1. **Data entry bez trade-offu** — podnosisz suwak, liczba rośnie. (Meaningful Decisions)
2. **Trudność z UI** — checkbox, ukryty limit, nieczytelny budżet. (`DESIGN_PRINCIPLES` §1, §27)
3. **Cichy fail** — proces nie działa i nikt nie mówi. (No Silent Failure; PCM Recalculate)
4. **Drugi, gorszy silnik na fast sim.** (PCM Instant Result; Peloton: jeden engine)
5. **Micromanagement live jako substytut zarządzania.** (VISION anti-goal; PCM 3D)
6. **Martwe AI rynku.** (PCM transfery; `D-002`)
7. **God-eye bota.** (`D-010`)
8. **PlayerTeam jako osobny gatunek.** (`D-004`, `D-005`)
9. **Feature, bo „tak jest w sporcie”**, mimo że playtest jest nudny. (Jacobson youth manager; Ping-Pong lesson)
10. **Symulacja, której gracz nie umie odczytać.** (Sylvester; Spy vs UI)
11. **Lawina administracji** między wydarzeniami. (PCM 2023; attention management w design notes)
12. **Wynik = atrybut.** (Results are evidence)
13. **Jedna meta na każdą epokę.** (`D-016` lab; no universal manager meta)
14. **Ukryty podatek od sukcesu** zamiast rynku. (`D-011`)
15. **Match/race as the product, career as a folder.** (FIFA Career)

---

## 6. Mapowanie na locki Peloton Manager

Research **potwierdza** istniejące decyzje. Nie otwiera ich do renegocjacji.

| Finding z researchu | Lock / dokument | Status |
|---|---|---|
| Manager przeżywa sponsor-vehiculum | `D-004`, `D-009` | potwierdzone przez fuzje i zgony ekip 2025–26 |
| Ekonomia = rynek sponsorów, nie bilety i luxury tax | `D-011`, `D-012`, VISION living sponsor market | liczby UCI/Gazzetta: ~87% od sponsorów, brak TV share |
| AI i human na tym samym rynku | `D-002` | PCM ginie, gdy AI jest leniwe; FM/OOTP żyją, gdy świat kupuje Twojego targetu |
| Prawda ≠ wiedza | `D-003`, `D-010`, `D-020` | CM/FM/OOTP; Jacobson; Sylvester Player Model |
| Advance Day + świat bez gracza | `D-006` | Continue/one-more-turn gatunku |
| Wyścig bez stamina-bara i bez skryptu | `D-017`–`D-021` | osobny research wyścigu; PCM Detailed Sim jako dowód, że kłamliwy resolver zabija zaufanie |
| Watch to film z nadzorem, nie 1:1 i nie teleport | `D-033` | MM/F1/PCM: długi event musi być przyspieszalny i uczciwy |
| Delegacja, nie klikanie nóg | VISION, design notes §2 | PCM micromanagement i F1 practice loop jako negatyw |
| Fun > feature count | `DESIGN_PRINCIPLES` §30, HANDOFF Ping-Pong | Jacobson: youth manager byłby nudny |
| Dossier / agent / brak hard gate | design notes §16–18, §41–42 | PCM dossier-punkty są cytowanym antywzorcem |
| Kalendarz z provenance i audytem | `DESIGN_PRINCIPLES` §24–25 | prawdziwy peleton: szczyty, WT mandatory, wildcard |
| Role jako obietnice | `DESIGN_PRINCIPLES` §20 | ósemka GT, hierarchy PCM 26 jako słaba ale trafiona próba |
| Wyjaśnialny fast sim | `DESIGN_PRINCIPLES` §22 | PCM Instant vs Detailed; CM tekst |

---

## 7. Czego świadomie nie kopiować

Nawet gdy „tak robi lider gatunku”:

- **Coroczna baza licencyjna jako produkt.** Peloton sprzedaje historię i symulację, nie update składu UAE z lutego.
- **3D race control.** Nawet gdy PCM na tym stoi. VISION zamknięte.
- **Youth-team start / trening-minigra.** Jacobson: mało kto tego używa.
- **AI storyteller** podkładający dramaty. Peloton chce emergence ze zderzenia; Sylvester jest o czytelności, nie o reżyserze.
- **UCI points high-score** jako główny zwycięski warunek.
- **Okno transferowe FIFA** w świecie, który tak nie działa.
- **Stadion, bilety, merch jako główny cash.** W kolarstwie tego filaru prawie nie ma.
- **Hidden luxury tax** „bo FM ma FFP”. FFP jest jawnym rulesetem; `D-011` pozwala na jawny moduł, nie na cichy balans.
- **Kohorta 1000h jako jedyny odbiorca.** Jacobson sam projektuje 20% na każdą kohortę; VISION wymaga wejścia bez eksperckiej wiedzy o kolarstwie.

---

## 8. Otwarte pytania (nie decyzje)

To nie są propozycje locków. To miejsca, w których późniejszy design (kalendarz, rekrutacja, ekonomia) będzie musiał wybrać, **mając ten research pod ręką**.

1. **Jak głośno punkty UCI wchodzą do UI?** Jako licencyjny forecast („po tym sezonie jesteśmy 16. w cyklu”) czy jako tabela, którą da się zoptymalizować w marcu?
2. **Ile wyścigów w sezonie zasługuje na DecisionRequest?** Kalendarz jest długi. Gęstość z prototypu nie skaluje się 1:1 na 180 dni.
3. **Czy development team jest organizacją-córką od dnia 1, czy dopiero po vertical slice?** Research mówi, że to prawdziwa dźwignia talentu. Scope mówi: nie teraz.
4. **Jak modelować wildcard organizatora** bez God-eye i bez skryptu „utrudnij graczowi”? Organizator ma cele (lokalność, widowisko, sponsor wyścigu).
5. **Szczyt formy:** PCM dodał Tour Focus / Classics Focus jako atrybuty. Peloton chce archetypów z modelu (`D-018`). Pytanie implementacyjne: czy szczyt to stan kalendarza + zmęczenie, czy cecha kolarza?
6. **Super-team vs. cap.** Peleton realny odrzucił budget cap na 2026. Peloton może mieć cap jako **opcjonalny rules module**, spójny z `D-011` (jawny, nie ukryty).
7. **Kobiety / mieszane światy.** Women’s WT rośnie szybciej procentowo (~€80 mln / 14 ekip, 2026). To content/rules module, nie rdzeń Milestone 0.

Żadne z tych pytań nie blokuje obecnego Watch Race / §49.

---

## 9. Co ten research oznacza praktycznie dla kolejnych designów

Kolejność z `DOCS.md` zostaje. Research tylko podpowiada **oś decyzji**, gdy te dokumenty powstaną:

1. **Kalendarz / Recruitment / Economy** — tu jest największy zwrot z tego pliku. Kolarstwo-jako-zarządzanie żyje między wyścigami.
2. **Race debrief i Key Race Story** — apophenia potrzebuje czytelnych sygnałów, nie więcej fizjologii.
3. **AI rynku** — PCM umarło na leniwym rynku; nie wolno odłożyć symetrii AI „na później” w rekrutacji.
4. **Nie dodawać warstw treningu / R&D / equipment**, dopóki kalendarz i rynek nie dają ciekawych decyzji. Jacobson i PCM equipment-as-backdrop są tu zgodne.

Nie zaczynać z tego dokumentu implementacji. Najpierw owner playtest §49. Potem system design, potem kod.

---

## 10. Źródła

### Kolarstwo (zarządzanie, nie fizjologia)

- UCI, allocation of WorldTour licences 2026–2028: <https://www.uci.org/pressrelease/allocation-of-the-14-uci-womens-worldtour-licences-and-18-uci-worldtour/6QRootu0WfaZVcXXJVQerE>
- UCI, 23 teams in men’s Grand Tours: <https://www.uci.org/pressrelease/the-number-of-teams-participating-in-the-mens-grand-tours-increased-to-23-by/1jF0eFAMAXIr98KOHYIe62>
- UCI regulations / memoranda (start lists 8/7, top-20 team ranking, wildcards top 30): UCI Part 2 Road Races
- Cyclingnews, UCI points system: <https://www.cyclingnews.com/features/how-does-the-uci-worldtour-points-system-work/>
- Beyond the Peloton, 2025 promotion/relegation: <https://beyondthepeloton.substack.com/p/2025-season-in-review-part-1-deciphering>
- Domestique, WorldTour budgets €663m / avg €33.1m (Gazzetta / UCI seminar): <https://www.domestiquecycling.com/en/news/bigger-budgets-bigger-gaps-worldtour-spending-hits-eur663-million/>
- Cyclingnews, salaries and 87% sponsor revenue: <https://www.cyclingnews.com/pro-cycling/teams-riders/2026-mens-worldtour-budgets-total-eur663-million-as-median-male-rider-salaries-touch-eur350-000/>, <https://www.cyclingnews.com/pro-cycling/where-do-cyclings-super-teams-spend-their-millions/>
- Inner Ring, development teams: <https://inrng.com/2022/01/uci-continental-development-teams/>
- Inner Ring, Lotto–Intermarché merger: <https://inrng.com/2025/10/lotto-intermarche-merger/>
- Cyclingnews, Lotto–Intermarché registration: <https://www.cyclingnews.com/news/officially-submitted-mooted-merger-further-solidifies-as-lotto-confirms-joint-uci-registration-application-with-intermarche-wanty/>
- Rydecruz, 2026 sponsor shake-up: <https://rydecruz.com/blogs/pedal-press/2026-worldtour-sponsor-shake-up-every-team-name-bike-and-wheel-change-explained>
- Cyclingnews, 2027 transfer market: <https://www.cyclingnews.com/pro-cycling/transfers/cycling-transfers-all-the-latest-news-and-announcements-for-the-2027-season/>
- dnlbenson, Classics as shop window: <https://dnlbenson.substack.com/p/cycling-transfers-why-and-how-the>
- inrng / team press: WorldTeam roster toward 30 (e.g. Red Bull–BORA–hansgrohe 2026)

Fizjologia i DS: nie dublowane; kanoniczne w `RACE_ENGINE_RESEARCH_2026-08-25.md`.

### Gatunek i projektanci

- Sid Meier, GDC 2012 *Interesting Decisions*: <https://www.gamedeveloper.com/design/gdc-2012-sid-meier-on-how-to-see-games-as-sets-of-interesting-decisions>
- Tynan Sylvester, *The Simulation Dream* (2013): <https://tynansylvester.com/2013/06/the-simulation-dream/>
- Overbaked Studio, *What Makes a Good Management Sim Fun?* (2026): <https://overbaked.studio/blog/what-makes-a-management-sim-fun/>
- Miles Jacobson, FourFourTwo (youth manager / training unused): <https://www.fourfourtwo.com/features/miles-jacobson-how-we-make-football-manager-future-and-where-you-come-it>
- Miles Jacobson, FM26 cohorts: <https://fm.zweierkette.de/en/interview-with-miles-jacobson-about-fm26/>
- Miles Jacobson, Telegraph (scouting misses as stories): <https://www.telegraph.co.uk/gaming/features/miles-jacobson-interview-like-think-managing-watford-wouldnt/>
- The Athletic, CM 01/02: <https://www.nytimes.com/athletic/2876413/2021/10/12/championship-manager-01-02-revisiting-an-old-friend-two-decades-on/>
- OOTP scouting design (developer): <https://forums.ootpdevelopments.com/showthread.php?t=363170>
- OOTP player development manual: <https://manuals.ootpdevelopments.com/index.php?man=ootp22&page=player_development>

### Recenzje gier (PCM, F1, MM, Career)

- Lev3lup, PCM 26: <https://lev3lup.be/blog/review-pro-cycling-manager-26/>
- TheBigBois, PCM 26 Detailed Simulation: <https://thebigbois.com/strategy/pro-cycling-manager-26-review/>
- Velora, PCM 26 planner: <https://veloracycling.com/tech/pro-cycling-manager-26-review-race-planner-2026>
- Galaxus, PCM 25 dossier points / market: <https://www.galaxus.at/en/page/tactics-timing-triumph-how-i-helped-marc-hirschi-to-win-the-tour-de-france-stage-38353>
- Holygamerz, PCM 2023 race length / admin avalanche: <https://www.holygamerz.com/en/pro-cycling-manager-2023-the-manager-review-for-true-fans>
- Gamecritics, PCM 2024 3D / narrative: <https://gamecritics.com/david-bakker/pro-cycling-manager-2024-review/>
- Eurogamer, Motorsport Manager 2016: <https://www.eurogamer.net/motorsport-manager-review>
- Game8 / Traxion / IGN, F1 Manager 2024
- EA forums / Pitch Notes, FC Career Mode complaints and FC 27 transfer overhaul

---

## 11. Jedno zdanie na koniec

Kolarstwo daje Peloton Managerowi lepszy sport menedżerski niż piłka — pod warunkiem, że gra będzie o **szczytach, rolach, sponsorze i niepełnej wiedzy**, a wyścig będzie uczciwym testem tych decyzji, nie trzydziestoma minutami klikania w 3D i nie instant result, który kłamie.
