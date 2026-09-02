using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;

namespace Peloton.Client.Godot;

public sealed partial class CareerShellScreen : Control
{
    internal enum View
    {
        Desk,
        Squad,
        Staff,
        Calendar,
        Sponsors,
        Finance,
        Scouting,
        Market,
        History,
        Help,
        Manager,
        RaceEvent,
    }

    private CareerShellHost? host;
    private View current = View.Desk;
    private Control? shellRoot;
    private Label? dateNumber;
    private Label? dateWord;
    private Label? dateDow;
    private Label? dateFull;
    private Control? yearPill;
    private Control? racePill;
    private HBoxContainer? pillsRow;
    private Label? employerName;
    private Control? sidebarCrestHost;
    private Button? cta;
    private Label? toast;
    private Control? content;
    private readonly Dictionary<View, Button> nav = new();
    private Window? settingsWindow;
    private WatchRaceScreen? watchScreen;
    private string? selectedEventId;
    private View raceEventBackView = View.Desk;
    private LookSort squadSort = new("last", 1);
    private long selectedRiderId;
    private bool negotiating;
    private int staffSelected = 1;
    private LookSort marketSort = new("ovr", -1);
    private long selectedMarketRiderId;
    private string marketClubFilter = string.Empty;
    private int calendarYear = 2026;
    private int calendarMonth = 1;
    private int lastCalendarDay = -1;
    private int reportSelected = 1;
    private int selectedSponsorId = 1;
    private readonly List<LookScoutMission> scoutMissions = new(CareerLookCatalog.Missions);

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
        LookChrome.EnsureFonts();

        ColorRect paper = LookChrome.Block(LookChrome.Paper);
        paper.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(paper);

        ColorRect redBand = LookChrome.Block(LookChrome.Red);
        redBand.SetAnchorsPreset(LayoutPreset.FullRect);
        redBand.OffsetTop = 680;
        redBand.OffsetBottom = -120;
        redBand.RotationDegrees = -8;
        redBand.MouseFilter = MouseFilterEnum.Ignore;
        redBand.Modulate = new Color(1, 1, 1, 0.18f);
        AddChild(redBand);

        ColorRect stripe = LookChrome.Block(LookChrome.Team);
        stripe.SetAnchorsPreset(LayoutPreset.FullRect);
        stripe.OffsetTop = 620;
        stripe.OffsetBottom = -180;
        stripe.RotationDegrees = -8;
        stripe.MouseFilter = MouseFilterEnum.Ignore;
        stripe.Modulate = new Color(1, 1, 1, 0.9f);
        AddChild(stripe);

        shellRoot = new Control();
        shellRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(shellRoot);

        HBoxContainer app = new();
        app.SetAnchorsPreset(LayoutPreset.FullRect);
        app.AddThemeConstantOverride("separation", 0);
        shellRoot.AddChild(app);

        app.AddChild(BuildSidebar());
        app.AddChild(BuildMain());

        bool editor = OS.HasFeature("editor");
        string exe = OS.GetExecutablePath();
        host = new CareerShellHost(
            ApplicationFactory.Create(WatchContentPath.FindContentRoot()),
            WatchContentPath.PlaytestSavePath("career-skeleton.peloton", editor, exe),
            WatchContentPath.PlaytestSavePath("peloton-career-prerace.peloton", editor, exe));
        Refresh();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        if (host is null)
        {
            return;
        }

        if (host.State == GameState.Management && watchScreen is not null)
        {
            HideWatch();
            Refresh();
        }
    }

    private ColorRect BuildSidebar()
    {
        ColorRect side = LookChrome.Block(LookChrome.Team);
        side.CustomMinimumSize = new Vector2(236, 0);
        side.SizeFlagsVertical = SizeFlags.ExpandFill;

        VBoxContainer column = new();
        column.SetAnchorsPreset(LayoutPreset.FullRect);
        column.OffsetLeft = 14;
        column.OffsetTop = 18;
        column.OffsetRight = -14;
        column.OffsetBottom = -14;
        column.AddThemeConstantOverride("separation", 2);
        side.AddChild(column);

        sidebarCrestHost = LookChrome.Crest("PELOTON", "PELOTON");
        column.AddChild(sidebarCrestHost);

        ColorRect hair = LookChrome.Block(new Color(0, 0, 0, 0.28f));
        hair.CustomMinimumSize = new Vector2(0, 2);
        column.AddChild(hair);

        VBoxContainer nav = new();
        nav.AddThemeConstantOverride("separation", 2);
        MarginContainer navPad = new();
        navPad.AddThemeConstantOverride("margin_top", 14);
        navPad.AddChild(nav);
        column.AddChild(navPad);

        AddNav(nav, View.Desk, "home", "Biurko", 0);
        AddNav(nav, View.Squad, "person", "Skład", 0);
        AddNav(nav, View.Staff, "id-card", "Sztab", 0);
        AddNav(nav, View.Calendar, "calendar", "Kalendarz", 0);

        nav.AddChild(LookChrome.NavSection("ZARZĄDZANIE"));
        AddNav(nav, View.Sponsors, "tag", "Sponsorzy", 0);
        AddNav(nav, View.Finance, "wallet", "Finanse", 0);
        AddNav(nav, View.Scouting, "magnifier", "Skauting", 0);
        AddNav(nav, View.Market, "arrows-swap", "Rynek transferowy", 0);
        AddNav(nav, View.History, "clock", "Historia zespołu", 0);
        AddNav(nav, View.Help, "question", "Pomoc", 0);

        Control spacer = new();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddChild(spacer);

        Button settings = LookChrome.NavItem("sliders", "Ustawienia", 0, false, OpenSettings);
        column.AddChild(settings);

        Button manager = LookChrome.ManagerFoot("MN", "M. Nowak", "profil managera · kariera", () => Show(View.Manager));
        column.AddChild(manager);

        return side;
    }

    private void AddNav(VBoxContainer column, View view, string icon, string label, int badge)
    {
        Button button = LookChrome.NavItem(icon, label, badge, false, () => Show(view));
        nav[view] = button;
        column.AddChild(button);
    }

    private MarginContainer BuildMain()
    {
        VBoxContainer main = new();
        main.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        main.SizeFlagsVertical = SizeFlags.ExpandFill;
        main.AddThemeConstantOverride("separation", 16);
        MarginContainer pad = new();
        pad.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        pad.SizeFlagsVertical = SizeFlags.ExpandFill;
        pad.AddThemeConstantOverride("margin_left", 24);
        pad.AddThemeConstantOverride("margin_top", 18);
        pad.AddThemeConstantOverride("margin_right", 24);
        pad.AddThemeConstantOverride("margin_bottom", 24);
        pad.AddChild(main);

        main.AddChild(BuildTopBar());

        toast = LookChrome.Body(string.Empty, 13, LookChrome.Gray, bold: true);
        toast.Visible = false;
        main.AddChild(toast);

        ScrollContainer scroll = new();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        content = new VBoxContainer();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        ((VBoxContainer)content).AddThemeConstantOverride("separation", 14);
        scroll.AddChild(content);
        main.AddChild(scroll);

        return pad;
    }

    private HBoxContainer BuildTopBar()
    {
        HBoxContainer top = new();
        top.AddThemeConstantOverride("separation", 16);

        HBoxContainer date = new();
        date.RotationDegrees = -1.2f;
        date.AddThemeConstantOverride("separation", 6);
        dateNumber = LookChrome.Display("1 STY", 26, LookChrome.TeamOn);
        dateNumber.HorizontalAlignment = HorizontalAlignment.Center;
        dateNumber.VerticalAlignment = VerticalAlignment.Center;
        ColorRect d1 = LookChrome.Block(LookChrome.Team);
        d1.CustomMinimumSize = new Vector2(92, 42);
        d1.AddChild(dateNumber);
        dateNumber.SetAnchorsPreset(LayoutPreset.FullRect);
        dateWord = LookChrome.Display("PN", 26, LookChrome.Paper);
        dateWord.HorizontalAlignment = HorizontalAlignment.Center;
        dateWord.VerticalAlignment = VerticalAlignment.Center;
        ColorRect d2 = LookChrome.Block(LookChrome.Black);
        d2.CustomMinimumSize = new Vector2(64, 42);
        d2.AddChild(dateWord);
        dateWord.SetAnchorsPreset(LayoutPreset.FullRect);
        date.AddChild(d1);
        date.AddChild(d2);
        VBoxContainer meta = new();
        meta.AddThemeConstantOverride("separation", 2);
        dateDow = LookChrome.Body("Nowa gra", 14, LookChrome.Black, bold: true);
        dateFull = LookChrome.Body(string.Empty, 12, LookChrome.Gray);
        meta.AddChild(dateDow);
        meta.AddChild(dateFull);
        date.AddChild(meta);
        top.AddChild(date);

        HBoxContainer pills = new();
        pills.AddThemeConstantOverride("separation", 8);
        pillsRow = pills;
        yearPill = LookChrome.Pill("ROK", "2026");
        racePill = LookChrome.Pill("WYŚCIG", null);
        pills.AddChild(yearPill);
        pills.AddChild(racePill);
        top.AddChild(pills);

        Control spacer = new();
        spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        top.AddChild(spacer);

        ColorRect employer = LookChrome.Block(LookChrome.Black);
        employer.CustomMinimumSize = new Vector2(280, 52);
        employer.RotationDegrees = -1;
        VBoxContainer empCol = new();
        empCol.SetAnchorsPreset(LayoutPreset.FullRect);
        empCol.OffsetLeft = 16;
        empCol.OffsetTop = 8;
        empCol.OffsetRight = -16;
        empCol.OffsetBottom = -8;
        Label empLabel = LookChrome.Meta("ZESPÓŁ", 11, LookChrome.Paper);
        empLabel.Modulate = new Color(1, 1, 1, 0.6f);
        employerName = LookChrome.Display("—", 22, LookChrome.Team);
        empCol.AddChild(empLabel);
        empCol.AddChild(employerName);
        employer.AddChild(empCol);
        top.AddChild(employer);

        cta = LookChrome.Primary("ADVANCE DAY", OnPrimary);
        top.AddChild(cta);
        return top;
    }

    private void Show(View view)
    {
        current = view;
        Refresh();
    }

    private void OnPrimary()
    {
        if (host is null)
        {
            return;
        }

        if (host.State == GameState.PreSeasonPlanningFlow)
        {
            Apply(host.ConfirmPreSeasonPlan());
            return;
        }

        if (host.State == GameState.Management)
        {
            Apply(host.FollowPrimary());
            return;
        }

        if (host.State == GameState.RacePreparationFlow)
        {
            Apply(host.RunRace());
            return;
        }

        Apply(host.ContinueOutcome());
    }

    private void Apply(CommandResult result)
    {
        if (!result.Succeeded)
        {
            ShowToast(Reason(result.ReasonCode));
        }

        if (host is { State: GameState.RaceLive, Watch: not null })
        {
            ShowWatch();
        }

        Refresh();
    }

    private void ShowWatch()
    {
        if (host?.Watch is null || shellRoot is null)
        {
            return;
        }

        watchScreen?.QueueFree();
        watchScreen = new WatchRaceScreen
        {
            ExternalHost = host.Watch,
        };
        watchScreen.ReturnedToManagement += OnWatchClosed;
        watchScreen.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(watchScreen);
        shellRoot.Visible = false;
    }

    private void HideWatch()
    {
        if (watchScreen is not null)
        {
            watchScreen.ReturnedToManagement -= OnWatchClosed;
            watchScreen.QueueFree();
            watchScreen = null;
        }

        if (shellRoot is not null)
        {
            shellRoot.Visible = true;
        }
    }

    private void OnWatchClosed()
    {
        HideWatch();
        current = View.Desk;
        Refresh();
    }

    private void OpenSettings()
    {
        settingsWindow?.QueueFree();
        settingsWindow = new Window
        {
            Title = "Ustawienia",
            Size = new Vector2I(480, 380),
            Unresizable = true,
            Exclusive = true,
        };
        settingsWindow.CloseRequested += () => settingsWindow?.Hide();

        ColorRect back = LookChrome.Block(LookChrome.Paper);
        back.SetAnchorsPreset(LayoutPreset.FullRect);
        settingsWindow.AddChild(back);

        ColorRect stripe = LookChrome.Block(LookChrome.Team);
        stripe.SetAnchorsPreset(LayoutPreset.FullRect);
        stripe.OffsetTop = 220;
        stripe.OffsetBottom = -60;
        stripe.RotationDegrees = -8;
        stripe.MouseFilter = MouseFilterEnum.Ignore;
        stripe.Modulate = new Color(1, 1, 1, 0.9f);
        settingsWindow.AddChild(stripe);

        VBoxContainer box = new();
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        box.OffsetLeft = 18;
        box.OffsetTop = 18;
        box.OffsetRight = -18;
        box.OffsetBottom = -18;
        box.AddThemeConstantOverride("separation", 12);
        settingsWindow.AddChild(box);

        box.AddChild(LookChrome.Display("USTAWIENIA", 18, LookChrome.Black));
        box.AddChild(LookChrome.Body("Zapis i wczytanie idą przez Application Commands. To nie jest demo z HTML.", 13, LookChrome.Gray));
        box.AddChild(LookChrome.Solid("Zapisz karierę", OnSave, LookChrome.Paper, LookChrome.Black, compact: true));
        box.AddChild(LookChrome.Solid("Wczytaj karierę", OnLoad, LookChrome.Paper, LookChrome.Black, compact: true));

        bool filmOn = host?.Settings.WatchFilmEnabled == true;
        box.AddChild(LookChrome.Body(
            filmOn
                ? "Film Watch jest włączony. Race next otworzy oglądanie etapu. Wynik zostaje ten sam."
                : "Film Watch jest wyłączony. Race next pokazuje wynik i tabelę.",
            12,
            LookChrome.Gray,
            bold: true));
        box.AddChild(LookChrome.Solid(
            filmOn ? "FILM: WŁ" : "FILM: WYŁ",
            () =>
            {
                if (host is null)
                {
                    return;
                }

                host.SetWatchFilmEnabled(!host.Settings.WatchFilmEnabled);
                settingsWindow?.Hide();
                ShowToast(host.Settings.WatchFilmEnabled
                    ? "Film włączony. Następny wyścig otworzy oglądanie."
                    : "Film wyłączony. Następny wyścig pokaże wynik.");
                Refresh();
            },
            filmOn ? LookChrome.Team : LookChrome.Paper,
            filmOn ? LookChrome.TeamOn : LookChrome.Black,
            compact: true));
        box.AddChild(LookChrome.Primary("Zamknij", () => settingsWindow?.Hide()));
        AddChild(settingsWindow);
        settingsWindow.PopupCentered();
    }

    private void OnSave()
    {
        if (host is null)
        {
            return;
        }

        CommandResult result = host.Save();
        ShowToast(result.Succeeded ? "Zapisano karierę." : Reason(result.ReasonCode));
        settingsWindow?.Hide();
    }

    private void OnLoad()
    {
        if (host is null)
        {
            return;
        }

        CommandResult result = host.Load();
        ShowToast(result.Succeeded ? "Wczytano karierę." : Reason(result.ReasonCode));
        settingsWindow?.Hide();
        Refresh();
    }

    private void Refresh()
    {
        if (host is null || content is null)
        {
            return;
        }

        CareerDayProjection? day = host.Day;
        int dayNumber = day?.DayNumber ?? 0;
        if (dateNumber is not null)
        {
            dateNumber.Text = host.State == GameState.MainMenu
                ? "—"
                : CareerCalendarDates.FormatSlab(dayNumber);
        }

        if (dateWord is not null)
        {
            dateWord.Text = host.State == GameState.MainMenu
                ? "—"
                : CareerCalendarDates.FormatWeekdayShort(dayNumber);
        }

        if (dateDow is not null && dateFull is not null)
        {
            string flow = host.State switch
            {
                GameState.MainMenu => "Nowa gra",
                GameState.PreSeasonPlanningFlow => "Plan sezonu",
                _ => string.Empty,
            };
            if (host.State == GameState.MainMenu || host.State == GameState.PreSeasonPlanningFlow)
            {
                dateDow.Text = flow;
                dateFull.Text = string.Empty;
            }
            else if (day is null)
            {
                dateDow.Text = host.IsWorldTourWorld ? "WorldTour" : "Szkielet świata";
                dateFull.Text = string.Empty;
            }
            else
            {
                dateDow.Text = CareerCalendarDates.FormatWeekdayLong(dayNumber);
                int week = ISOWeek.GetWeekOfYear(CareerCalendarDates.ToDate(dayNumber).ToDateTime(TimeOnly.MinValue));
                dateFull.Text = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{CareerCalendarDates.FormatLong(dayNumber)} · tydzień {week}");
            }
        }

        RebuildPills(day, dayNumber);

        if (sidebarCrestHost is not null)
        {
            string club = host.State == GameState.MainMenu
                ? "PELOTON"
                : (host.EmployerName ?? "PELOTON").ToUpperInvariant();
            string sub = host.State switch
            {
                GameState.MainMenu => "PELOTON",
                _ when host.IsWorldTourWorld => "WORLDTOUR · 2026",
                _ when host.State is not GameState.MainMenu => "SZKIELET · 2026",
                _ => "PELOTON",
            };
            LookChrome.UpdateCrest(sidebarCrestHost, club, sub);
        }

        if (employerName is not null)
        {
            employerName.Text = (host.EmployerName ?? "—").ToUpperInvariant();
        }

        if (cta is not null)
        {
            cta.Text = PrimaryCaption(host, day);
            cta.Disabled = host.State == GameState.RaceLive;
        }

        if (host.State == GameState.RaceLive && host.Watch is not null)
        {
            if (watchScreen is null)
            {
                ShowWatch();
            }

            return;
        }

        HideWatch();
        EnsureSelectedEvent();
        if (day is not null && day.DayNumber != lastCalendarDay)
        {
            lastCalendarDay = day.DayNumber;
            (calendarYear, calendarMonth) = CareerCalendarDates.MonthFromDayNumber(day.DayNumber);
        }

        foreach ((View view, Button button) in nav)
        {
            (string icon, string label) = NavMeta(view);
            int badge = view == View.Desk ? host.Inbox.Count : 0;
            LookChrome.SetNavItem(button, icon, label, badge, view == current);
        }

        RebuildContent();
        SyncContentHeight();
    }

    private void RebuildContent()
    {
        foreach (Node child in content!.GetChildren())
        {
            child.QueueFree();
        }

        if (host!.State == GameState.MainMenu)
        {
            BuildNewGame();
            return;
        }

        if (host.State == GameState.PreSeasonPlanningFlow)
        {
            BuildSeasonPlan();
            return;
        }

        if (host.State == GameState.RaceResultsFlow && host.Result is not null)
        {
            BuildRaceResults();
            return;
        }

        switch (current)
        {
            case View.Desk:
                BuildDesk();
                break;
            case View.Squad:
                BuildSquad();
                break;
            case View.Staff:
                BuildStaff();
                break;
            case View.Calendar:
                BuildCalendar();
                break;
            case View.Sponsors:
                BuildSponsors();
                break;
            case View.Finance:
                BuildFinance();
                break;
            case View.Scouting:
                BuildScouting();
                break;
            case View.Market:
                BuildMarket();
                break;
            case View.RaceEvent:
                BuildRaceEvent();
                break;
            case View.History:
                BuildHistory();
                break;
            case View.Manager:
                BuildManager();
                break;
            default:
                BuildHelp();
                break;
        }
    }

    private static VBoxContainer Panel(
        string title,
        Control body,
        string? linkText = null,
        Action? onLink = null,
        Control? rightAccessory = null,
        bool expandVertical = false)
    {
        VBoxContainer stack = new();
        stack.AddThemeConstantOverride("separation", 0);
        stack.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (expandVertical)
        {
            stack.SizeFlagsVertical = SizeFlags.ExpandFill;
        }

        PanelContainer card = LookChrome.Card();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (expandVertical)
        {
            card.SizeFlagsVertical = SizeFlags.ExpandFill;
        }

        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 0);
        inner.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (expandVertical)
        {
            inner.SizeFlagsVertical = SizeFlags.ExpandFill;
        }

        inner.AddChild(LookChrome.SectionBar(title, linkText, onLink, rightAccessory));
        inner.AddChild(Pad(body, expandVertical));
        card.AddChild(inner);
        stack.AddChild(card);
        return stack;
    }

    private void SyncContentHeight()
    {
        if (content?.GetParent() is not ScrollContainer scroll)
        {
            return;
        }

        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.CustomMinimumSize = new Vector2(0, Mathf.Max(scroll.Size.Y, 0));
    }

    private void RebuildPills(CareerDayProjection? day, int dayNumber)
    {
        if (pillsRow is null)
        {
            return;
        }

        foreach (Node child in pillsRow.GetChildren())
        {
            child.QueueFree();
        }

        int year = host!.State == GameState.MainMenu
            ? 2026
            : CareerCalendarDates.ToDate(dayNumber).Year;
        (string yearPrefix, string yearAccent) = LookFormat.YearPillParts(year);
        yearPill = LookChrome.Pill(yearPrefix, yearAccent);
        pillsRow.AddChild(yearPill);

        string racePrefix;
        string? raceAccent = null;
        if (day is null)
        {
            racePrefix = "WYŚCIG";
        }
        else if (day.RaceDueToday)
        {
            racePrefix = "WYŚCIG DZIŚ";
        }
        else if (day.DaysUntilNextRace > 0)
        {
            (racePrefix, raceAccent) = LookFormat.RaceCountdownPill(day.DaysUntilNextRace);
        }
        else
        {
            racePrefix = "BRAK WYŚCIGU";
        }

        racePill = LookChrome.Pill(racePrefix, raceAccent);
        pillsRow.AddChild(racePill);
    }

    private static MarginContainer Pad(Control child, bool expandVertical = false)
    {
        MarginContainer pad = new();
        pad.AddThemeConstantOverride("margin_left", 14);
        pad.AddThemeConstantOverride("margin_top", 12);
        pad.AddThemeConstantOverride("margin_right", 14);
        pad.AddThemeConstantOverride("margin_bottom", 12);
        if (expandVertical)
        {
            pad.SizeFlagsVertical = SizeFlags.ExpandFill;
            child.SizeFlagsVertical = SizeFlags.ExpandFill;
        }

        pad.AddChild(child);
        return pad;
    }

    private void ShowToast(string message)
    {
        if (toast is null)
        {
            return;
        }

        toast.Text = message;
        toast.Visible = !string.IsNullOrWhiteSpace(message);
    }

    private static string Reason(string code)
    {
        return code switch
        {
            "RACE_DAY_PENDING" => "Najpierw jedź wyścig. Advance Day jest zablokowane.",
            "PREP_STRATEGY_INCOMPLETE" => "Ustaw lidera i support.",
            "SAVE_FORBIDDEN_IN_RACE_LIVE" => "Zapis w trakcie etapu jest zablokowany.",
            "LOAD_FORBIDDEN_IN_RACE_LIVE" => "Wczytanie w trakcie etapu jest zablokowane.",
            "INBOX_SOURCE_CANNOT_BE_DISMISSED" => "Terminu wyścigu nie da się schować.",
            "GAME_STATE_INVALID" => "Ta akcja nie jest teraz dostępna.",
            "CONTRACT_OFFER_REJECTED" => "Oferta odrzucona.",
            "CONTRACT_OFFER_INVALID" => "Oferta niepoprawna.",
            "CONTRACT_OFFER_INCOMPLETE" => "Dokończ ofertę.",
            "RIDER_NOT_FOUND" => "Nie znaleziono zawodnika.",
            "EMPLOYER_REQUIRED" => "Brak przypisanego klubu.",
            _ => code,
        };
    }

    private static (string Icon, string Label) NavMeta(View view)
    {
        return view switch
        {
            View.Desk => ("home", "Biurko"),
            View.Squad => ("person", "Skład"),
            View.Staff => ("id-card", "Sztab"),
            View.Calendar => ("calendar", "Kalendarz"),
            View.Sponsors => ("tag", "Sponsorzy"),
            View.Finance => ("wallet", "Finanse"),
            View.Scouting => ("magnifier", "Skauting"),
            View.Market => ("arrows-swap", "Rynek transferowy"),
            View.History => ("clock", "Historia zespołu"),
            View.Help => ("question", "Pomoc"),
            _ => ("home", "Biurko"),
        };
    }

    private static string PrimaryCaption(CareerShellHost host, CareerDayProjection? day)
    {
        return host.State switch
        {
            GameState.PreSeasonPlanningFlow => "ZATWIERDŹ SEZON",
            GameState.RacePreparationFlow => host.Settings.WatchFilmEnabled ? "OGLĄDAJ ETAP" : "JEDŹ WYŚCIG",
            GameState.RaceResultsFlow => "DALEJ",
            GameState.RaceDebriefFlow => "ZAMKNIJ",
            _ => (day?.PrimaryLabel ?? HubPrimaryActionLabels.AdvanceDay).ToUpperInvariant(),
        };
    }
}
