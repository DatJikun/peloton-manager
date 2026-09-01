using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Peloton.Client.Godot;

/// <summary>
/// Presentation-only copy of <c>peloton-manager-full-ui-poc-v3.html</c>.
/// Not World State, not Commands, not true ability (D-003 / D-010 / D-014).
/// </summary>
public static class CareerLookCatalog
{
    public const string ClubName = "Beskid–Vetter";
    public const string ClubCrest = "BESKID–VETTER";
    public const string ClubSub = "PROTEAM · LOOK LAB";
    public const string NotInWorld = "Jeszcze nie w tej wersji.";

    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl-PL");

    public static readonly IReadOnlyList<LookUpcomingRace> UpcomingRaces =
    [
        new(
            "mila-torino",
            "CZW 12.03",
            "Milano–Torino",
            "1.Pro",
            177,
            "Milano → Turyn (Superga)",
            "14°C · sucho · wiatr 12 km/h",
            "7/7 zgłoszonych",
            "jutro",
            [4, 4, 5, 4, 5, 6, 5, 6, 14, 6, 18, 5],
            0.87f,
            "SUPERGA ×2",
            [new LookTag("red", "Wyścig jutro"), new LookTag("", "Finał pod górę"), new LookTag("inv", "1.Pro")]),
        new(
            "msr",
            "SOB 21.03",
            "Milano–Sanremo",
            "Monument",
            289,
            "Milano → Sanremo",
            "prognoza za 10 dni",
            "wstępna lista do 18.03",
            "za 10 dni",
            [4, 4, 4, 5, 4, 4, 5, 4, 8, 6, 10, 4],
            0.88f,
            "POGGIO",
            [new LookTag("inv", "Monument"), new LookTag("", "Najdłuższy"), new LookTag("", "Cipressa + Poggio")]),
        new(
            "e3",
            "PT 27.03",
            "E3 Saxo Classic",
            "1.WT",
            204,
            "Harelbeke → Harelbeke",
            "prognoza za 2 tygodnie",
            "zgłoszenie za 9 dni",
            "za 16 dni",
            [5, 9, 4, 10, 5, 12, 5, 11, 4, 9, 5, 6],
            0.8f,
            "PATERBERG",
            [new LookTag("", "Bruki"), new LookTag("", "Hellingeny"), new LookTag("inv", "1.WT")]),
        new(
            "gw",
            "NIE 29.03",
            "Gent–Wevelgem",
            "1.WT",
            253,
            "Gent → Wevelgem",
            "prognoza za 2,5 tygodnia",
            "zgłoszenie za 11 dni",
            "za 18 dni",
            [4, 4, 5, 4, 5, 4, 13, 5, 12, 4, 5, 4],
            0.62f,
            "KEMMELBERG",
            [new LookTag("", "Wiatr"), new LookTag("", "Kemmelberg"), new LookTag("inv", "1.WT")]),
        new(
            "rvv",
            "NIE 05.04",
            "Ronde van Vlaanderen",
            "Monument",
            272,
            "Antwerpia → Oudenaarde",
            "prognoza za 3,5 tygodnia",
            "zgłoszenie za 18 dni",
            "za 25 dni",
            [6, 12, 5, 14, 6, 15, 5, 13, 8, 16, 6, 5],
            0.83f,
            "OUDE KWAREMONT",
            [new LookTag("red", "Cel sezonu"), new LookTag("", "Bruki + bergi"), new LookTag("inv", "Monument")]),
    ];

    public static readonly IReadOnlyList<LookDeskRider> DeskSquad =
    [
        new("P. Kowalczyk", "kapitan", 4, 5, 1, "zdrowy", true),
        new("M. Zieliński", "pomocnik", 3, 4, 0, "zdrowy", true),
        new("T. Barski", "pomocnik", 2, 3, -1, "stłuczone kolano — 2 dni", false),
        new("D. Rutka", "pomocnik", 3, 3, 0, "zdrowy", true),
        new("J. Malinowski", "pomocnik", 4, 4, 1, "zdrowy", true),
        new("K. Osmański", "pomocnik", 2, 4, 0, "zdrowy", true),
        new("S. Dudek", "ucieczki", 3, 5, 1, "zdrowy", true),
    ];

    public static readonly IReadOnlyList<LookMail> DeskMail =
    [
        new("01", "Odprawa sztabu — dziś 16:00", "dziś", "Od: DS Marek Sowa", "Agenda: skład na Milano–Torino, plan na finał pod Supergą, podział zadań w peletonie. Proszę o punktualność.", true),
        new("02", "Wynik do odłożenia: GP Primavera — Rutka 12.", "wczoraj", string.Empty, string.Empty, false),
        new("03", "Media: prośba o krótki komentarz przed startem", "wczoraj", string.Empty, string.Empty, false),
    ];

    public static readonly IReadOnlyList<LookResultRow> RecentResults =
    [
        new("12.", "GP Primavera", "1.1 · niedziela 08.03", "Rutka", false),
        new("18.", "GP Primavera", "1.1 · niedziela 08.03", "Kowalczyk", false),
        new("4.", "Klasyk Śląski", "1.2 · 01.03", "Malinowski", true),
        new("2.", "Puchar Ziemi", "1.2 · 22.02", "Dudek", true),
    ];

    public static readonly IReadOnlyList<LookRankRow> Ranking =
    [
        new(5, "Veltrix Pro", 812, false),
        new(6, "Andromeda CT", 790, false),
        new(7, "Beskid–Vetter ProTeam", 655, true),
        new(8, "Fala–Karpaty", 601, false),
        new(9, "Delta Nord", 588, false),
    ];

    public static readonly IReadOnlyList<LookFinanceRow> WeekFinance =
    [
        new("Payroll (zawodnicy + sztab)", -96000),
        new("Logistyka wyścigu (Turyn)", -38500),
        new("Sponsoring — rata tygodniowa", 120000),
    ];

    public const int WeekBalance = -14500;
    public const int SeasonBudget = 412300;

    public static readonly IReadOnlyList<LookNote> StaffNotes =
    [
        new("DS Sowa · dziś 09:12", "Superga wjeżdża się dwa razy — pozycja przy drugim wjeździe przesądza o finale.", true),
        new("Mechanik · dziś 08:40", "Zapasowe koła poszły z autokarem; rowery Kowalczyka i Rutki po serwisie.", false),
        new("Doktor · wczoraj", "Barski: stłuczenie kolana bez obrzęku, decyzja o starcie jutro rano.", false),
    ];

    public static readonly IReadOnlyList<LookRider> Riders =
    [
        new(1, "Piotr", "Kowalczyk", "POL", 27, "Puncheur", 84, 88, 92, 24, 76, 88, 71, 73, 82, 86, 80, "31.12.2027", 38000, "7 500 zł / zwycięstwo", "Tomasz Bielski", 720000, 88, "Zdrowy"),
        new(2, "Jan", "Malinowski", "POL", 25, "Sprinter", 81, 84, 88, 31, 59, 74, 89, 68, 75, 79, 82, "31.12.2028", 31000, "5 000 zł / zwycięstwo", "Paweł Cygan", 610000, 84, "Zdrowy"),
        new(3, "Michał", "Zieliński", "POL", 29, "All-round", 78, 79, 84, 19, 73, 77, 70, 80, 76, 84, 85, "31.12.2026", 29000, "3 000 zł / top 5", "bez agenta", 390000, 76, "Zdrowy"),
        new(4, "Dawid", "Rutka", "POL", 24, "Klasyki", 77, 85, 86, 36, 67, 81, 78, 70, 85, 83, 78, "31.12.2027", 24000, "4 000 zł / top 3", "Tomasz Bielski", 520000, 91, "Zdrowy"),
        new(5, "Kamil", "Osmański", "POL", 22, "Bruki", 72, 87, 79, 14, 60, 74, 72, 69, 86, 79, 83, "31.12.2028", 18000, "2 500 zł / top 5", "Maja Kurek", 410000, 82, "Zdrowy"),
        new(6, "Szymon", "Dudek", "POL", 23, "Ucieczki", 74, 86, 90, 42, 72, 76, 67, 75, 71, 88, 76, "31.12.2027", 20000, "5 000 zł / zwycięstwo", "Maja Kurek", 450000, 94, "Zdrowy"),
        new(7, "Tomasz", "Barski", "POL", 30, "Góry", 70, 70, 65, 18, 82, 72, 56, 71, 57, 80, 72, "31.12.2026", 22000, "2 000 zł / top 10", "bez agenta", 190000, 69, "Kolano · 2 dni"),
        new(8, "Nicolas", "Leroy", "FRA", 26, "Pomocnik", 73, 76, 77, 21, 70, 72, 68, 74, 73, 82, 84, "31.12.2027", 21000, "1 500 zł / top 10", "Clément Picard", 280000, 80, "Zdrowy"),
        new(9, "Luca", "Ferri", "ITA", 21, "Góry", 69, 89, 81, 12, 80, 75, 58, 66, 55, 77, 87, "31.12.2028", 16000, "3 500 zł / top 5", "Marco Valli", 430000, 87, "Zdrowy"),
        new(10, "Emil", "Berg", "DEN", 28, "Czasowiec", 76, 77, 74, 26, 65, 71, 64, 86, 78, 84, 81, "31.12.2026", 27000, "4 000 zł / zwycięstwo", "Mikkel Holm", 330000, 73, "Zdrowy"),
    ];

    public static readonly IReadOnlyList<LookStaff> Staff =
    [
        new(1, "Marek Sowa", "Dyrektor sportowy", "Taktyka klasyków", 86, "31.12.2027", "16 500 zł/mies.", "POL", [new LookSkill("Taktyka", 91), new LookSkill("Przywództwo", 84), new LookSkill("Logistyka", 73)]),
        new(2, "Anna Wrona", "Główna trenerka", "Rozwój + forma", 91, "31.12.2028", "18 000 zł/mies.", "POL", [new LookSkill("Rozwój", 94), new LookSkill("Forma", 92), new LookSkill("Regeneracja", 82)]),
        new(3, "dr Piotr Wysocki", "Lekarz", "Prewencja urazów", 90, "31.12.2027", "17 200 zł/mies.", "POL", [new LookSkill("Diagnoza", 93), new LookSkill("Prewencja", 91), new LookSkill("Rehabilitacja", 86)]),
        new(4, "Rafał Piekarski", "Główny mechanik", "Sprzęt / bruk", 93, "31.12.2028", "14 800 zł/mies.", "POL", [new LookSkill("Serwis", 95), new LookSkill("Bruk", 94), new LookSkill("Logistyka", 82)]),
        new(5, "Lena Krawiec", "Skaut", "Europa Środkowa", 84, "31.12.2026", "12 600 zł/mies.", "POL", [new LookSkill("Ocena potencjału", 88), new LookSkill("Polska", 95), new LookSkill("Bałkany", 81)]),
        new(6, "Erik van Daal", "Skaut", "Benelux", 87, "31.12.2027", "13 400 zł/mies.", "NED", [new LookSkill("Bruki", 94), new LookSkill("Benelux", 96), new LookSkill("Młodzież", 84)]),
    ];

    public static readonly IReadOnlyList<LookTransfer> Transfers =
    [
        new(101, "Louis", "Martin", "FRA", 21, "All-round", 77, 86, 83, "do 2026", 380000, 28000, 86, "Vélo Ardennes"),
        new(102, "Andrea", "Rossi", "ITA", 24, "Sprinter", 80, 83, 75, "do 2027", 590000, 34000, 61, "Torino Corse"),
        new(103, "Milan", "de Wit", "NED", 23, "Bruki", 79, 85, 88, "do 2026", 640000, 36000, 72, "Noord Cycling"),
        new(104, "Nik", "Kovač", "SLO", 22, "Góry", 76, 90, 80, "do 2028", 760000, 39000, 48, "Triglav Pro"),
        new(105, "Matteo", "Bianchi", "ITA", 29, "Czasowiec", 82, 82, 78, "wolny agent", 0, 43000, 93, "—"),
        new(106, "Victor", "Lemaire", "BEL", 26, "Puncheur", 83, 84, 91, "do 2027", 880000, 47000, 54, "Flanders Union"),
    ];

    public static readonly IReadOnlyList<LookScoutMission> Missions =
    [
        new(1, 5, "Polska", "U23 · klasyki", 9, 21, "W toku"),
        new(2, 6, "Belgia / Holandia", "Bruki", 4, 14, "W toku"),
    ];

    public static readonly IReadOnlyList<LookScoutReport> Reports =
    [
        new(
            1,
            "Polska · U23 · 18 dni",
            "02.03.2026",
            [
                new LookProspect("Mateusz Król", "POL", 19, "Puncheur", 82, "88–94", "Mocny 2–4 min, dobry finisz z małej grupy."),
                new LookProspect("Kacper Róg", "POL", 20, "All-round", 76, "82–89", "Równy profil; do sprawdzenia dłuższe podjazdy."),
            ]),
        new(
            2,
            "Słowenia · góry · 24 dni",
            "18.02.2026",
            [new LookProspect("Luka Žagar", "SLO", 19, "Góry", 91, "90–96", "Bardzo wysoki pułap na długich podjazdach.")]),
    ];

    public static readonly IReadOnlyList<LookCalendarMonth> Months =
    [
        new(2026, 2, "Luty 2026"),
        new(2026, 3, "Marzec 2026"),
        new(2026, 4, "Kwiecień 2026"),
    ];

    public static readonly IReadOnlyList<LookCalendarRace> CalendarRaces =
    [
        new("puchar", 2026, 2, 22, "Puchar Ziemi", "1.2", 168, "Kraków → Myślenice", "Falująca trasa z krótkimi podjazdami w drugiej połowie. Selekcja powinna nastąpić przed finałowymi 20 km.", [6, 1, 4], [new LookPoint(15, 65), new LookPoint(45, 54), new LookPoint(75, 58), new LookPoint(105, 37), new LookPoint(137, 44), new LookPoint(168, 29), new LookPoint(202, 48), new LookPoint(235, 25), new LookPoint(270, 36)], "Dobczyce"),
        new("primavera", 2026, 3, 8, "GP Primavera", "1.1", 184, "Savona → Finale Ligure", "Szybki klasyk z serią krótkich podjazdów i technicznym zjazdem przed metą.", [4, 1, 2], [new LookPoint(15, 62), new LookPoint(45, 57), new LookPoint(75, 40), new LookPoint(105, 53), new LookPoint(137, 32), new LookPoint(168, 45), new LookPoint(202, 29), new LookPoint(235, 52), new LookPoint(270, 34)], "Capo Finale"),
        new("mila-torino", 2026, 3, 12, "Milano–Torino", "1.Pro", 177, "Milano → Torino", "Długi, płaski dojazd do Turynu i dwa wejścia na Supergę. Pozycjonowanie przed drugim podjazdem jest kluczowe.", [1, 4, 6], [new LookPoint(12, 62), new LookPoint(42, 61), new LookPoint(72, 56), new LookPoint(103, 57), new LookPoint(133, 50), new LookPoint(164, 53), new LookPoint(194, 46), new LookPoint(225, 52), new LookPoint(250, 24), new LookPoint(273, 42)], "Superga ×2"),
        new("msr", 2026, 3, 21, "Milano–Sanremo", "Monument", 289, "Milano → Sanremo", "Najdłuższy dzień w kalendarzu. Cipressa i Poggio tworzą finał dla zawodników odpornych na dystans i krótkie eksplozje.", [1, 2, 8], [new LookPoint(12, 58), new LookPoint(43, 57), new LookPoint(73, 59), new LookPoint(104, 55), new LookPoint(135, 56), new LookPoint(165, 50), new LookPoint(196, 54), new LookPoint(224, 40), new LookPoint(246, 49), new LookPoint(263, 28), new LookPoint(279, 42)], "Poggio"),
        new("e3", 2026, 3, 27, "E3 Saxo Classic", "1.WT", 204, "Harelbeke → Harelbeke", "Bruk, wiatr i seria hellingenów. Wyścig premiuje technikę, odporność na powtarzane wysiłki i pozycję w peletonie.", [4, 5, 1], [new LookPoint(12, 54), new LookPoint(42, 34), new LookPoint(70, 52), new LookPoint(98, 26), new LookPoint(126, 48), new LookPoint(154, 23), new LookPoint(182, 47), new LookPoint(211, 28), new LookPoint(240, 50), new LookPoint(276, 37)], "Paterberg"),
        new("gw", 2026, 3, 29, "Gent–Wevelgem", "1.WT", 253, "Ypres → Wevelgem", "Wiatr i Kemmelberg. Jeśli nie rozerwie peletonu, finał może premiować szybkich zawodników z klasykowym zapleczem.", [2, 4, 5], [new LookPoint(12, 57), new LookPoint(45, 55), new LookPoint(78, 48), new LookPoint(108, 51), new LookPoint(140, 24), new LookPoint(170, 49), new LookPoint(204, 29), new LookPoint(236, 53), new LookPoint(276, 50)], "Kemmelberg"),
        new("rvv", 2026, 4, 5, "Ronde van Vlaanderen", "Monument", 272, "Antwerpia → Oudenaarde", "Najważniejszy brukowy cel wiosny. Kwaremont, Paterberg i ciągłe walki o pozycję wymagają kompletnego klasykowca.", [4, 5, 1], [new LookPoint(12, 55), new LookPoint(42, 29), new LookPoint(72, 52), new LookPoint(103, 25), new LookPoint(132, 48), new LookPoint(161, 21), new LookPoint(191, 46), new LookPoint(221, 27), new LookPoint(250, 18), new LookPoint(279, 42)], "Oude Kwaremont"),
        new("roubaix", 2026, 4, 12, "Paris–Roubaix", "Monument", 259, "Compiègne → Roubaix", "Brukowy test wytrzymałości i prowadzenia roweru. Sprzęt, pozycjonowanie i odporność na awarie mają ogromne znaczenie.", [5, 4, 8], [new LookPoint(12, 53), new LookPoint(42, 48), new LookPoint(73, 31), new LookPoint(103, 51), new LookPoint(133, 26), new LookPoint(164, 43), new LookPoint(194, 20), new LookPoint(224, 46), new LookPoint(251, 24), new LookPoint(279, 40)], "Carrefour de l'Arbre"),
        new("amstel", 2026, 4, 19, "Amstel Gold Race", "1.WT", 254, "Maastricht → Berg en Terblijt", "Ciąg krótkich podjazdów i częste zmiany rytmu. Dobre miejsce dla puncheurów i mocnych klasykowców.", [1, 4, 6], [new LookPoint(12, 55), new LookPoint(43, 36), new LookPoint(74, 51), new LookPoint(106, 31), new LookPoint(138, 47), new LookPoint(170, 27), new LookPoint(202, 44), new LookPoint(235, 30), new LookPoint(279, 39)], "Cauberg"),
    ];

    public static readonly IReadOnlyList<LookSponsor> Sponsors =
    [
        new(1, "Vetter Industries", "Tytularny", "4,8 mln zł / rok", "2028", 84, ["Top 10 Milano–Torino", "Top 15 Europe Tour", "Minimum 2 podia do 31 maja"]),
        new(2, "Beskid Bank", "Główny partner", "1,5 mln zł / rok", "2027", 76, ["Starty w Polsce", "1 zwycięstwo w wyścigu 1.1"]),
        new(3, "Nordwerk Bikes", "Dostawca sprzętu", "900 tys. zł + sprzęt", "2028", 91, ["Ekspozycja w monumentach", "Test prototypu kół na bruku"]),
    ];

    public static readonly IReadOnlyList<LookLedgerRow> Ledger =
    [
        new("11.03", "Rata sponsorska", "Sponsorzy", 120000),
        new("10.03", "Payroll", "Zespół", -96000),
        new("09.03", "Transport · Turyn", "Logistyka", -38500),
        new("08.03", "Premia GP Primavera", "Nagrody", 24000),
        new("06.03", "Hotel · Mediolan", "Logistyka", -18400),
        new("01.03", "Serwis sprzętu", "Sprzęt", -12000),
    ];

    public static readonly IReadOnlyList<LookExpenseSlice> Expenses =
    [
        new("Zawodnicy", "2,85 mln zł", 42, "2050c8"),
        new("Sztab", "1,15 mln zł", 17, "0c0c0d"),
        new("Logistyka", "1,56 mln zł", 23, "d11f1f"),
        new("Sprzęt", "0,75 mln zł", 11, "7f7b72"),
        new("Pozostałe", "0,48 mln zł", 7, "bdb5a5"),
    ];

    public static readonly IReadOnlyList<LookHistoryEvent> Chronicle =
    [
        new("08.03.2026", "GP Primavera · Rutka 12.", "Pierwsze punkty w marcowym bloku włoskich klasyków."),
        new("01.03.2026", "Klasyk Śląski · Malinowski 4.", "Najlepszy wynik zespołu na poziomie 1.2."),
        new("22.02.2026", "Puchar Ziemi · Dudek 2.", "Pierwsze podium Beskid–Vetter."),
        new("01.01.2026", "Powstanie Beskid–Vetter ProTeam", "Licencja ProTeam i pierwsza kadra licząca 18 zawodników."),
    ];

    public static readonly IReadOnlyList<LookKv> Records =
    [
        new("Pierwsze podium", "S. Dudek · 22.02.2026"),
        new("Najlepszy wynik", "2. miejsce"),
        new("Najwięcej punktów", "D. Rutka · 118"),
        new("Najwyższy ranking", "7. Europe Tour"),
    ];

    public static readonly IReadOnlyList<LookArchiveRow> Archive =
    [
        new("08.03", "GP Primavera", "D. Rutka", "12.", "18"),
        new("01.03", "Klasyk Śląski", "J. Malinowski", "4.", "42"),
        new("22.02", "Puchar Ziemi", "S. Dudek", "2.", "65"),
    ];

    public static readonly LookManager Manager = new(
        "Mikołaj Nowak",
        "MN",
        "POL · manager Beskid–Vetter ProTeam",
        68,
        1,
        2,
        [
            new LookKv("Klub", "Beskid–Vetter"),
            new LookKv("Stanowisko", "Manager"),
            new LookKv("Umowa do", "31.12.2028"),
            new LookKv("Pensja", "24 000 zł / mies."),
            new LookKv("Zaufanie zarządu", "81 / 100"),
        ],
        [
            new LookHistoryEvent("2026", "Beskid–Vetter ProTeam", "Manager · pierwszy kontrakt zawodowy."),
            new LookHistoryEvent("2025", "KS Beskid U23", "Dyrektor sportowy · 9 zwycięstw."),
        ],
        [
            new LookKv("Pierwsze podium ProTeam", "22.02.2026"),
            new LookKv("Najwyższa reputacja", "68"),
            new LookKv("Wygrane kariery", "9"),
        ]);

    public static readonly IReadOnlyList<LookHelpCard> Help =
    [
        new("Tabele", "Kliknięcie nagłówka sortuje dane. Ponowne kliknięcie odwraca kolejność."),
        new("Profile", "Kliknięcie wiersza otwiera kartę zawodnika, pracownika albo kolarza z rynku."),
        new("Kalendarz", "Strzałki zmieniają miesiąc. Kliknięcie wyścigu otwiera jego kartę."),
        new("Skauting", "Nowe nazwiska pojawiają się dopiero w raportach z zakończonych misji."),
    ];

    public static readonly IReadOnlyList<string> ScoutRegions =
        ["Polska", "Benelux", "Włochy", "Bałkany", "Francja"];

    public static readonly IReadOnlyList<string> ScoutFoci =
        ["U23 · dowolny", "Klasyki", "Góry", "Sprint", "Czasówka"];

    public static readonly IReadOnlyList<int> ScoutDurations = [14, 21, 30];

    public static string Zloty(int amount)
    {
        string number = Math.Abs(amount).ToString("N0", Pl)
            .Replace('\u00A0', ' ')
            .Replace('\u202F', ' ');
        return number + " zł";
    }

    public static string SignedZloty(int amount)
    {
        return (amount >= 0 ? "+" : "−") + Zloty(Math.Abs(amount));
    }

    public static string Euro(long amount)
    {
        string number = Math.Abs(amount).ToString("N0", Pl)
            .Replace('\u00A0', ' ')
            .Replace('\u202F', ' ');
        return number + " €";
    }

    public static string SignedEuro(long amount)
    {
        return (amount >= 0 ? "+" : "−") + Euro(Math.Abs(amount));
    }

    public static string Initials(string name)
    {
        string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => part.Length > 0 && char.IsUpper(part[0]))
            .ToArray();
        if (parts.Length == 0)
        {
            return "?";
        }

        return string.Concat(parts.Take(2).Select(part => part[0]));
    }

    public static string Trend(int trend)
    {
        return trend > 0 ? "rosnąca" : trend < 0 ? "spadkowa" : "stabilna";
    }

    public static string Stars(int rate)
    {
        rate = Math.Clamp(rate, 0, 5);
        return new string('★', rate) + new string('☆', 5 - rate);
    }

    public static LookSort Toggle(LookSort sort, string key, int freshDir = 1)
    {
        return sort.Key == key ? sort with { Dir = -sort.Dir } : new LookSort(key, freshDir);
    }

    public static IReadOnlyList<LookRider> SortedRiders(LookSort sort)
    {
        return Riders.OrderBy(rider => RiderKey(rider, sort.Key), Comparer<object>.Create(CompareKeys))
            .ThenBy(rider => rider.Last, StringComparer.Create(Pl, true))
            .ReverseIf(sort.Dir < 0)
            .ToArray();
    }

    public static IReadOnlyList<LookTransfer> SortedTransfers(LookSort sort)
    {
        return Transfers.OrderBy(row => TransferKey(row, sort.Key), Comparer<object>.Create(CompareKeys))
            .ThenBy(row => row.Last, StringComparer.Create(Pl, true))
            .ReverseIf(sort.Dir < 0)
            .ToArray();
    }

    public static IReadOnlyList<LookDeskRider> SortedDeskSquad(LookSort sort)
    {
        return DeskSquad.OrderBy(row => DeskKey(row, sort.Key), Comparer<object>.Create(CompareKeys))
            .ReverseIf(sort.Dir < 0)
            .ToArray();
    }

    public static LookRider? Rider(int id)
    {
        return Riders.FirstOrDefault(rider => rider.Id == id);
    }

    public static LookStaff? StaffMember(int id)
    {
        return Staff.FirstOrDefault(person => person.Id == id);
    }

    public static LookTransfer? Transfer(int id)
    {
        return Transfers.FirstOrDefault(row => row.Id == id);
    }

    public static LookUpcomingRace? Upcoming(string id)
    {
        return UpcomingRaces.FirstOrDefault(race => race.Id == id);
    }

    public static LookCalendarRace? CalendarRace(string id)
    {
        return CalendarRaces.FirstOrDefault(race => race.Id == id);
    }

    public static LookSponsor? Sponsor(int id)
    {
        return Sponsors.FirstOrDefault(row => row.Id == id);
    }

    public static LookScoutReport? Report(int id)
    {
        return Reports.FirstOrDefault(row => row.Id == id);
    }

    public static IReadOnlyList<LookStaff> Scouts()
    {
        return Staff.Where(person => person.Job == "Skaut").ToArray();
    }

    public static int FitScore(LookRider rider)
    {
        int terrain = rider.Role is "Bruki" ? rider.Cobbles : rider.Hill;
        return (int)Math.Round((rider.Form + rider.Stamina + terrain) / 3.0);
    }

    public static IReadOnlyList<LookCalendarCell> Cells(LookCalendarMonth month)
    {
        DateTime first = new(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        int lead = ((int)first.DayOfWeek + 6) % 7;
        DateTime cursor = first.AddDays(-lead);
        LookCalendarCell[] cells = new LookCalendarCell[42];
        for (int i = 0; i < cells.Length; i++)
        {
            DateTime day = cursor.AddDays(i);
            bool outside = day.Month != month.Month;
            bool today = day.Year == 2026 && day.Month == 3 && day.Day == 11;
            LookCalendarRace? race = CalendarRaces.FirstOrDefault(entry =>
                entry.Year == day.Year && entry.Month == day.Month && entry.Day == day.Day);
            cells[i] = new LookCalendarCell(day.Day, day.Month, day.Year, outside, today, race);
        }

        return cells;
    }

    private static object RiderKey(LookRider rider, string key)
    {
        return key switch
        {
            "first" => rider.First,
            "last" => rider.Last,
            "role" => rider.Role,
            "age" => rider.Age,
            "rate" => rider.Rate,
            "pot" => rider.Pot,
            "form" => rider.Form,
            "fatigue" => rider.Fatigue,
            "contractEnd" => rider.ContractEnd,
            _ => rider.Last,
        };
    }

    private static object TransferKey(LookTransfer row, string key)
    {
        return key switch
        {
            "first" => row.First,
            "last" => row.Last,
            "age" => row.Age,
            "role" => row.Role,
            "rate" => row.Rate,
            "pot" => row.Pot,
            "form" => row.Form,
            "salary" => row.Salary,
            "interest" => row.Interest,
            _ => row.Rate,
        };
    }

    private static object DeskKey(LookDeskRider row, string key)
    {
        return key switch
        {
            "name" => row.Name,
            "role" => row.Role,
            "rate" => row.Rate,
            "trend" => row.Trend,
            "statusK" => row.Healthy ? 1 : 0,
            _ => row.Rate,
        };
    }

    private static int CompareKeys(object? left, object? right)
    {
        if (left is string ls && right is string rs)
        {
            return Pl.CompareInfo.Compare(ls, rs, CompareOptions.IgnoreCase);
        }

        return Comparer<IComparable>.Default.Compare((IComparable)left!, (IComparable)right!);
    }
}

internal static class LookLinq
{
    public static IEnumerable<T> ReverseIf<T>(this IEnumerable<T> source, bool reverse)
    {
        return reverse ? source.Reverse() : source;
    }
}

public readonly record struct LookSort(string Key, int Dir);

public readonly record struct LookTag(string Kind, string Text);

public readonly record struct LookSkill(string Name, int Value);

public readonly record struct LookPoint(int X, int Y);

public readonly record struct LookKv(string Label, string Value);

public sealed record LookUpcomingRace(
    string Id,
    string Date,
    string Name,
    string Category,
    int DistanceKm,
    string Route,
    string Weather,
    string Squad,
    string When,
    int[] Heights,
    float KeyX,
    string KeyLabel,
    LookTag[] Tags);

public sealed record LookDeskRider(
    string Name,
    string Role,
    int Rate,
    int Pot,
    int Trend,
    string Status,
    bool Healthy);

public sealed record LookMail(
    string Index,
    string Subject,
    string When,
    string From,
    string Body,
    bool Urgent);

public sealed record LookResultRow(string Place, string Race, string Meta, string Rider, bool Highlight);

public sealed record LookRankRow(int Place, string Team, int Points, bool Mine);

public sealed record LookFinanceRow(string Label, int Amount);

public sealed record LookNote(string Who, string Text, bool Urgent);

public sealed record LookRider(
    int Id,
    string First,
    string Last,
    string Nat,
    int Age,
    string Role,
    int Rate,
    int Pot,
    int Form,
    int Fatigue,
    int Mountain,
    int Hill,
    int Sprint,
    int Tt,
    int Cobbles,
    int Stamina,
    int Recovery,
    string ContractEnd,
    int Salary,
    string Bonus,
    string Agent,
    int Value,
    int Morale,
    string Status)
{
    public string FullName => First + " " + Last;
}

public sealed record LookStaff(
    int Id,
    string Name,
    string Job,
    string Spec,
    int Rating,
    string Contract,
    string Cost,
    string Nat,
    LookSkill[] Skills);

public sealed record LookTransfer(
    int Id,
    string First,
    string Last,
    string Nat,
    int Age,
    string Role,
    int Rate,
    int Pot,
    int Form,
    string Contract,
    int Value,
    int Salary,
    int Interest,
    string Team)
{
    public string FullName => First + " " + Last;
}

public sealed record LookScoutMission(
    int Id,
    int ScoutId,
    string Region,
    string Focus,
    int DaysLeft,
    int Total,
    string Status);

public sealed record LookProspect(
    string Name,
    string Nat,
    int Age,
    string Type,
    int Known,
    string Pot,
    string Note);

public sealed record LookScoutReport(
    int Id,
    string Mission,
    string Date,
    LookProspect[] Prospects);

public sealed record LookCalendarMonth(int Year, int Month, string Label);

public sealed record LookCalendarRace(
    string Id,
    int Year,
    int Month,
    int Day,
    string Name,
    string Category,
    int DistanceKm,
    string Route,
    string Desc,
    int[] FitIds,
    LookPoint[] Map,
    string Climb);

public sealed record LookCalendarCell(
    int Day,
    int Month,
    int Year,
    bool OutsideMonth,
    bool IsToday,
    LookCalendarRace? Race);

public sealed record LookSponsor(
    int Id,
    string Name,
    string Tier,
    string Value,
    string Until,
    int Mood,
    string[] Goals);

public sealed record LookLedgerRow(string Date, string Operation, string Category, int Amount);

public sealed record LookExpenseSlice(string Label, string Amount, int Percent, string ColorHex);

public sealed record LookHistoryEvent(string Time, string Title, string Body);

public sealed record LookArchiveRow(string Date, string Race, string Rider, string Result, string Points);

public sealed record LookHelpCard(string Title, string Body);

public sealed record LookManager(
    string Name,
    string Initials,
    string Meta,
    int Reputation,
    int Seasons,
    int Podiums,
    LookKv[] Contract,
    LookHistoryEvent[] Career,
    LookKv[] Achievements);
