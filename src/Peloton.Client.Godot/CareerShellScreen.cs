using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;

namespace Peloton.Client.Godot;

public sealed partial class CareerShellScreen : Control
{
    private enum View
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
    private WorldEntityId? selectedRaceId;
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

        string user = OS.GetUserDataDir();
        host = new CareerShellHost(
            ApplicationFactory.Create(WatchContentPath.FindContentRoot()),
            Path.Combine(user, "career-skeleton.peloton"),
            Path.Combine(Path.GetTempPath(), "peloton-career-prerace.peloton"));
        CommandResult opened = host.OpenSkeleton();
        if (!opened.Succeeded)
        {
            ShowToast($"Nie udało się otworzyć świata ({opened.ReasonCode}).");
        }

        Refresh();
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
        Label club = LookChrome.Display("SZKIELET", 13, LookChrome.TeamOn);
        Label sub = LookChrome.Body("PROTEAM · SEED 91234", 10, LookChrome.TeamOn, bold: true);
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

        CommandResult result = host.FollowPrimary();
        if (!result.Succeeded)
        {
            ShowToast(Reason(result.ReasonCode));
            Refresh();
            return;
        }

        if (host.State == GameState.RacePreparationFlow)
        {
            OpenWatch();
            return;
        }

        Refresh();
    }

    private void OpenWatch()
    {
        if (host is null || shellRoot is null)
        {
            return;
        }

        watchScreen?.QueueFree();
        watchScreen = new WatchRaceScreen();
        watchScreen.Attach(host.CreateWatchHost());
        watchScreen.ReturnedToManagement += OnWatchClosed;
        watchScreen.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(watchScreen);
        shellRoot.Visible = false;
    }

    private void OnWatchClosed()
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

        HBoxContainer rates = new();
        rates.AddThemeConstantOverride("separation", 8);
        rates.AddChild(LookChrome.Body("Tempo oglądania", 12, LookChrome.Gray, bold: true));
        foreach (int rate in new[] { 1, 2, 5, 20 })
        {
            int captured = rate;
            rates.AddChild(LookChrome.Solid($"×{captured}", () =>
            {
                host?.SelectWatchRate(captured);
                ShowToast($"Tempo oglądania ×{captured} od następnego etapu.");
            }, LookChrome.White, LookChrome.Black, compact: true));
        }

        box.AddChild(rates);
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
            cta.Text = (day?.PrimaryLabel ?? HubPrimaryActionLabels.AdvanceDay).ToUpperInvariant();
            cta.Disabled = host.State != GameState.Management;
        }

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
                BuildPeople();
                break;
            case View.Calendar:
                BuildCalendar();
                break;
            case View.History:
                BuildHistory();
                break;
            case View.Manager:
                BuildManager();
                break;
            case View.Help:
                BuildHelp();
                break;
            default:
                BuildEmpty(current);
                break;
        }
    }

    private void BuildDesk()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 14);
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        VBoxContainer left = Panel("01  NADCHODZĄCE WYŚCIGI", BuildRaceList());
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        left.SizeFlagsStretchRatio = 5;
        VBoxContainer right = new();
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        right.SizeFlagsStretchRatio = 7;
        right.AddThemeConstantOverride("separation", 14);
        right.AddChild(Panel("02  WYŚCIG", BuildRaceDetail()));
        right.AddChild(Panel("03  SKRZYNKA", BuildInbox()));
        row.AddChild(left);
        row.AddChild(right);
        content!.AddChild(row);

        CareerDayProjection? day = host!.Day;
        if (day is not null && day.TodayNotes.Count > 0)
        {
            VBoxContainer notes = new();
            notes.AddThemeConstantOverride("separation", 6);
            foreach (string note in day.TodayNotes)
            {
                notes.AddChild(LookChrome.Body(note, 13, LookChrome.Black));
            }

            content.AddChild(Panel("NOTATKI DNIA", notes));
        }
    }

    private VBoxContainer BuildRaceList()
    {
        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 8);
        IReadOnlyList<CalendarEntryProjection> entries = host!.Calendar;
        if (entries.Count == 0)
        {
            list.AddChild(LookChrome.Body("Brak wpisów kalendarza.", 13, LookChrome.Gray));
            return list;
        }

        selectedRaceId ??= entries[0].Id;
        foreach (CalendarEntryProjection entry in entries)
        {
            CalendarEntryProjection captured = entry;
            bool active = selectedRaceId == captured.Id;
            Button row = LookChrome.Solid(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"DZIEŃ {captured.DayNumber}   {captured.Title.ToUpperInvariant()}   {captured.Status}"),
                () =>
                {
                    selectedRaceId = captured.Id;
                    Refresh();
                },
                active ? LookChrome.Black : LookChrome.Paper,
                active ? LookChrome.Paper : LookChrome.Black);
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            list.AddChild(row);
        }

        return list;
    }

    private VBoxContainer BuildRaceDetail()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        CalendarEntryProjection? entry = SelectedRace();
        if (entry is null)
        {
            box.AddChild(LookChrome.Body("Wybierz wyścig z listy.", 13, LookChrome.Gray));
            return box;
        }

        box.AddChild(LookChrome.Display(entry.Title.ToUpperInvariant(), 28, LookChrome.Black));
        box.AddChild(LookChrome.Body(
            string.Create(
                CultureInfo.InvariantCulture,
                $"dzień {entry.DayNumber} · {entry.Kind} · {entry.Status}"),
            13,
            LookChrome.Gray,
            bold: true));
        if (entry.OfficialResult is not null)
        {
            box.AddChild(LookChrome.Body(entry.OfficialResult, 14, LookChrome.Black, bold: true));
        }
        else
        {
            box.AddChild(LookChrome.Body(
                "Profil trasy z rysunku HTML tu nie wraca. To szkieletowy wpis kalendarza, nie Milano–Torino.",
                13,
                LookChrome.Gray));
        }

        return box;
    }

    private VBoxContainer BuildInbox()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        if (host!.Inbox.Count == 0)
        {
            box.AddChild(LookChrome.Body("Skrzynka pusta.", 13, LookChrome.Gray));
            return box;
        }

        foreach (InboxItemProjection item in host.Inbox)
        {
            InboxItemProjection captured = item;
            VBoxContainer mail = new();
            mail.AddThemeConstantOverride("separation", 4);
            mail.AddChild(LookChrome.Body(captured.Category.ToUpperInvariant(), 10, LookChrome.Team, bold: true));
            mail.AddChild(LookChrome.Body(captured.Body, 14, LookChrome.Black, bold: true));
            if (captured.Category == "race-due")
            {
                mail.AddChild(LookChrome.Body("Skrzynka nie otwiera wyścigu. Użyj Race next.", 12, LookChrome.Gray));
            }
            else
            {
                mail.AddChild(LookChrome.Solid("Archiwizuj", () =>
                {
                    CommandResult result = host.ArchiveInbox(captured.Identity);
                    ShowToast(result.Succeeded ? "Zarchiwizowano." : Reason(result.ReasonCode));
                    Refresh();
                }, LookChrome.Paper, LookChrome.Black, compact: true));
            }

            PanelContainer card = LookChrome.Card();
            MarginContainer pad = Pad(mail);
            card.AddChild(pad);
            box.AddChild(card);
        }

        return box;
    }

    private void BuildPeople()
    {
        VBoxContainer table = new();
        table.AddThemeConstantOverride("separation", 6);
        table.AddChild(LookChrome.Body(
            "Imiona ze szkieletu świata. Bez OVR, POT i zmęczenia z rysunku HTML.",
            13,
            LookChrome.Gray));
        foreach (PersonNameProjection person in host!.People)
        {
            table.AddChild(LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"{person.Id.Value}   {person.Name}"),
                15,
                LookChrome.Black,
                bold: true));
        }

        content!.AddChild(Panel("SKŁAD ŚWIATA", table));
    }

    private void BuildCalendar()
    {
        content!.AddChild(Panel("KALENDARZ", BuildRaceList()));
        content.AddChild(Panel("WPIS", BuildRaceDetail()));
    }

    private void BuildHistory()
    {
        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 8);
        bool any = false;
        foreach (CalendarEntryProjection entry in host!.Calendar)
        {
            if (entry.OfficialResult is null)
            {
                continue;
            }

            any = true;
            list.AddChild(LookChrome.Body(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"dzień {entry.DayNumber} · {entry.Title} · {entry.OfficialResult}"),
                14,
                LookChrome.Black,
                bold: true));
        }

        if (!any)
        {
            list.AddChild(LookChrome.Body("Brak ukończonych wyścigów w tym save.", 13, LookChrome.Gray));
        }

        CareerDayProjection? day = host.Day;
        if (day is not null)
        {
            list.AddChild(LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"Liczba wyścigów: {day.RaceCount}"),
                13,
                LookChrome.Gray));
        }

        content!.AddChild(Panel("KRONIKA", list));
    }

    private void BuildManager()
    {
        CareerDayProjection? day = host!.Day;
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(LookChrome.Display((day?.ManagerName ?? "—").ToUpperInvariant(), 28, LookChrome.Black));
        box.AddChild(LookChrome.Body(
            $"Pracodawca: {day?.EmployerName ?? "bez klubu"}",
            14,
            LookChrome.Black,
            bold: true));
        box.AddChild(LookChrome.Body("Reputacja, pensja i osiągnięcia z rysunku HTML tu nie wracają.", 13, LookChrome.Gray));
        content!.AddChild(Panel("PROFIL MANAGERA", box));
    }

    private void BuildHelp()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 10);
        box.AddChild(LookChrome.Body("Advance Day przesuwa cały świat o jeden dzień.", 14, LookChrome.Black));
        box.AddChild(LookChrome.Body("W dzień wyścigu ten sam przycisk nazywa się Race next i wchodzi w przygotowanie.", 14, LookChrome.Black));
        box.AddChild(LookChrome.Body("Oglądanie etapu blokuje biurko. Nie ma zapisu w trakcie wyścigu.", 14, LookChrome.Black));
        box.AddChild(LookChrome.Body("Skrzynka nie startuje wyścigu. Sponsorzy, skauting i rynek czekają na prawdziwe dane.", 14, LookChrome.Black));
        content!.AddChild(Panel("POMOC", box));
    }

    private void BuildEmpty(View view)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(LookChrome.Display(view.ToString().ToUpperInvariant(), 28, LookChrome.Black));
        box.AddChild(LookChrome.Body(
            "Ten dział nie ma jeszcze danych w szkielecie. Nie wstawiamy tu liczb z HTML.",
            14,
            LookChrome.Gray));
        content!.AddChild(Panel(view.ToString().ToUpperInvariant(), box));
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

    private CalendarEntryProjection? SelectedRace()
    {
        foreach (CalendarEntryProjection entry in host!.Calendar)
        {
            if (selectedRaceId is WorldEntityId id && entry.Id == id)
            {
                return entry;
            }
        }

        return host.Calendar.Count == 0 ? null : host.Calendar[0];
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
            "SAVE_FORBIDDEN_IN_RACE_LIVE" => "Zapis w trakcie etapu jest zablokowany.",
            "LOAD_FORBIDDEN_IN_RACE_LIVE" => "Wczytanie w trakcie etapu jest zablokowane.",
            "INBOX_SOURCE_CANNOT_BE_DISMISSED" => "Terminu wyścigu nie da się schować.",
            "GAME_STATE_INVALID" => "Ta akcja nie jest teraz dostępna.",
            _ => code,
        };
    }
}
