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
    }

    private CareerShellHost? host;
    private View current = View.Desk;
    private Control? shellRoot;
    private Label? dateNumber;
    private Label? dateWord;
    private Label? dateMeta;
    private Label? racePill;
    private Label? employerName;
    private Button? cta;
    private Label? toast;
    private Control? content;
    private readonly Dictionary<View, Button> nav = new();
    private Window? settingsWindow;
    private WatchRaceScreen? watchScreen;
    private string lookRaceId = "mila-torino";
    private LookSort deskSquadSort = new("rate", -1);
    private LookSort squadSort = new("last", 1);
    private int selectedRiderId = 1;
    private bool negotiating;
    private int staffSelected = 1;
    private LookSort marketSort = new("rate", -1);
    private int marketSelected = 101;
    private bool watchingTransfer;
    private int monthIndex = 1;
    private string lookCalRaceId = "mila-torino";
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
        CommandResult opened = host.OpenSkeleton();
        if (!opened.Succeeded)
        {
            ShowToast($"Nie udało się otworzyć świata ({opened.ReasonCode}).");
        }

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
        column.AddThemeConstantOverride("separation", 4);
        side.AddChild(column);

        VBoxContainer crest = new();
        crest.AddThemeConstantOverride("separation", 4);
        Label club = LookChrome.Display(CareerLookCatalog.ClubCrest, 13, LookChrome.TeamOn);
        Label sub = LookChrome.Body(CareerLookCatalog.ClubSub, 10, LookChrome.TeamOn, bold: true);
        sub.Modulate = new Color(1, 1, 1, 0.7f);
        crest.AddChild(club);
        crest.AddChild(sub);
        column.AddChild(crest);

        ColorRect hair = LookChrome.Block(new Color(0, 0, 0, 0.28f));
        hair.CustomMinimumSize = new Vector2(0, 2);
        column.AddChild(hair);

        AddNav(column, View.Desk, "Biurko");
        AddNav(column, View.Squad, "Skład");
        AddNav(column, View.Staff, "Sztab");
        AddNav(column, View.Calendar, "Kalendarz");

        Label manage = LookChrome.Body("ZARZĄDZANIE", 9, LookChrome.TeamOn, bold: true);
        manage.Modulate = new Color(1, 1, 1, 0.6f);
        column.AddChild(manage);
        AddNav(column, View.Sponsors, "Sponsorzy");
        AddNav(column, View.Finance, "Finanse");
        AddNav(column, View.Scouting, "Skauting");
        AddNav(column, View.Market, "Rynek transferowy");
        AddNav(column, View.History, "Historia zespołu");
        AddNav(column, View.Help, "Pomoc");

        Control spacer = new();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddChild(spacer);

        Button settings = LookChrome.Ghost("Ustawienia", OpenSettings);
        column.AddChild(settings);

        Button manager = LookChrome.Ghost("Karta managera", () => Show(View.Manager));
        column.AddChild(manager);

        return side;
    }

    private void AddNav(VBoxContainer column, View view, string label)
    {
        Button button = LookChrome.Ghost(label, () => Show(view));
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
        dateNumber = LookChrome.Display("DZIEŃ", 26, LookChrome.TeamOn);
        dateNumber.AddThemeStyleboxOverride("normal", LookChrome.Fill(LookChrome.Team, 10, 6));
        ColorRect d1 = LookChrome.Block(LookChrome.Team);
        d1.AddChild(dateNumber);
        dateNumber.SetAnchorsPreset(LayoutPreset.FullRect);
        d1.CustomMinimumSize = new Vector2(92, 42);
        dateWord = LookChrome.Display("0", 26, LookChrome.Paper);
        ColorRect d2 = LookChrome.Block(LookChrome.Black);
        d2.CustomMinimumSize = new Vector2(64, 42);
        dateWord.SetAnchorsPreset(LayoutPreset.FullRect);
        dateWord.HorizontalAlignment = HorizontalAlignment.Center;
        dateWord.VerticalAlignment = VerticalAlignment.Center;
        dateNumber.HorizontalAlignment = HorizontalAlignment.Center;
        dateNumber.VerticalAlignment = VerticalAlignment.Center;
        d2.AddChild(dateWord);
        date.AddChild(d1);
        date.AddChild(d2);
        VBoxContainer meta = new();
        dateMeta = LookChrome.Body("Szkielet świata", 12, LookChrome.Black, bold: true);
        meta.AddChild(dateMeta);
        date.AddChild(meta);
        top.AddChild(date);

        racePill = LookChrome.Body("WYŚCIG", 13, LookChrome.Black, bold: true);
        racePill.AddThemeStyleboxOverride("normal", LookChrome.Fill(LookChrome.White, 14, 9));
        top.AddChild(racePill);

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
        Label empLabel = LookChrome.Body("ZESPÓŁ", 11, LookChrome.Paper, bold: true);
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

        ColorRect back = LookChrome.Block(LookChrome.White);
        back.SetAnchorsPreset(LayoutPreset.FullRect);
        settingsWindow.AddChild(back);

        VBoxContainer box = new();
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        box.OffsetLeft = 18;
        box.OffsetTop = 18;
        box.OffsetRight = -18;
        box.OffsetBottom = -18;
        box.AddThemeConstantOverride("separation", 12);
        settingsWindow.AddChild(box);

        box.AddChild(LookChrome.Display("04  USTAWIENIA", 18, LookChrome.Black));
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
        if (dateNumber is not null)
        {
            dateNumber.Text = "DZIEŃ";
        }

        if (dateWord is not null)
        {
            dateWord.Text = (day?.DayNumber ?? 0).ToString(CultureInfo.InvariantCulture);
        }

        if (dateMeta is not null)
        {
            dateMeta.Text = day is null
                ? "Szkielet świata"
                : string.Create(CultureInfo.InvariantCulture, $"Szkielet · dzień {day.DayNumber}");
        }

        if (racePill is not null)
        {
            racePill.Text = day is null
                ? "WYŚCIG"
                : day.RaceDueToday
                    ? "WYŚCIG DZIŚ"
                    : string.Create(CultureInfo.InvariantCulture, $"WYŚCIG ZA {day.DaysUntilNextRace} DNI");
        }

        if (employerName is not null)
        {
            employerName.Text = (day?.EmployerName ?? "—").ToUpperInvariant();
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

        if (nav.TryGetValue(View.Desk, out Button? deskNav))
        {
            deskNav.Text = host.Inbox.Count == 0
                ? "Biurko"
                : string.Create(CultureInfo.InvariantCulture, $"Biurko  {host.Inbox.Count}");
        }

        foreach ((View view, Button button) in nav)
        {
            bool active = view == current;
            button.AddThemeColorOverride("font_color", active ? LookChrome.Paper : LookChrome.TeamOn);
            button.AddThemeStyleboxOverride(
                "normal",
                active
                    ? LookChrome.Fill(LookChrome.Black, 12, 12)
                    : new StyleBoxFlat
                    {
                        BgColor = new Color(0, 0, 0, 0),
                        ContentMarginLeft = 12,
                        ContentMarginRight = 12,
                        ContentMarginTop = 12,
                        ContentMarginBottom = 12,
                    });
        }

        RebuildContent();
    }

    private void RebuildContent()
    {
        foreach (Node child in content!.GetChildren())
        {
            child.QueueFree();
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

    private static VBoxContainer Panel(string title, Control body)
    {
        VBoxContainer stack = new();
        stack.AddThemeConstantOverride("separation", 0);
        stack.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        PanelContainer card = LookChrome.Card();
        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 0);
        ColorRect header = LookChrome.Block(LookChrome.Team);
        header.CustomMinimumSize = new Vector2(0, 36);
        Label head = LookChrome.Display(title, 12, LookChrome.TeamOn);
        head.SetAnchorsPreset(LayoutPreset.FullRect);
        head.OffsetLeft = 14;
        head.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(head);
        inner.AddChild(header);
        inner.AddChild(Pad(body));
        card.AddChild(inner);
        stack.AddChild(card);
        return stack;
    }

    private static MarginContainer Pad(Control child)
    {
        MarginContainer pad = new();
        pad.AddThemeConstantOverride("margin_left", 14);
        pad.AddThemeConstantOverride("margin_top", 12);
        pad.AddThemeConstantOverride("margin_right", 14);
        pad.AddThemeConstantOverride("margin_bottom", 12);
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
            "PREP_ROLES_INCOMPLETE" => "Potrzebujesz lidera i karta.",
            "SAVE_FORBIDDEN_IN_RACE_LIVE" => "Zapis w trakcie etapu jest zablokowany.",
            "LOAD_FORBIDDEN_IN_RACE_LIVE" => "Wczytanie w trakcie etapu jest zablokowane.",
            "INBOX_SOURCE_CANNOT_BE_DISMISSED" => "Terminu wyścigu nie da się schować.",
            "GAME_STATE_INVALID" => "Ta akcja nie jest teraz dostępna.",
            _ => code,
        };
    }

    private static string PrimaryCaption(CareerShellHost host, CareerDayProjection? day)
    {
        return host.State switch
        {
            GameState.RacePreparationFlow => host.Settings.WatchFilmEnabled ? "OGLĄDAJ ETAP" : "JEDŹ WYŚCIG",
            GameState.RaceResultsFlow => "DALEJ",
            GameState.RaceDebriefFlow => "ZAMKNIJ",
            _ => (day?.PrimaryLabel ?? HubPrimaryActionLabels.AdvanceDay).ToUpperInvariant(),
        };
    }
}
