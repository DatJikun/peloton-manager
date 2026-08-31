using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;

namespace Peloton.Client.Godot;

public sealed partial class CareerHubScreen : Control
{
    private static readonly Color Paper = new("f3ede1");
    private static readonly Color Red = new("d11f1f");
    private static readonly Color Black = new("0c0c0d");
    private static readonly Color Gray = new("6f6f72");
    private static readonly Color White = new("fffdf7");

    private CareerHubHost? host;
    private Label? dateLabel;
    private Label? employerLabel;
    private Label? status;
    private Label? calendar;
    private Label? inbox;
    private Label? prep;
    private Label? outcome;
    private VBoxContainer? seatBox;
    private Button? primaryButton;
    private Button? watchButton;
    private Button? settingsButton;
    private VBoxContainer? desk;
    private WatchRaceScreen? watchScreen;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        ColorRect background = new() { Color = Paper };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        ColorRect bar = new() { Color = Red };
        bar.SetAnchorsPreset(LayoutPreset.BottomWide);
        bar.OffsetTop = -72;
        bar.OffsetBottom = -28;
        bar.RotationDegrees = -8;
        AddChild(bar);

        desk = new VBoxContainer();
        desk.SetAnchorsPreset(LayoutPreset.FullRect);
        desk.OffsetLeft = 28;
        desk.OffsetTop = 24;
        desk.OffsetRight = -28;
        desk.OffsetBottom = -24;
        desk.AddThemeConstantOverride("separation", 14);
        AddChild(desk);

        dateLabel = MakeLabel("DZIEŃ 0", 42, Black);
        desk.AddChild(dateLabel);
        employerLabel = MakeLabel("Zespół", 18, Red);
        desk.AddChild(employerLabel);
        status = MakeLabel("Advance Day, skrzynka, kalendarz, wynik wyścigu. Film w ustawieniach.", 15, Gray);
        desk.AddChild(status);

        primaryButton = MakeButton("ADVANCE DAY", OnPrimary);
        watchButton = MakeButton("OGLĄDAJ ETAP", OnWatch);
        settingsButton = MakeButton("FILM: WYŁ", OnToggleFilm);
        desk.AddChild(WrapRow(primaryButton, watchButton, settingsButton));

        calendar = MakeLabel("Kalendarz", 15, Black);
        calendar.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        desk.AddChild(Panel("CALENDAR", calendar));
        inbox = MakeLabel("Skrzynka pusta.", 15, Black);
        inbox.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        desk.AddChild(Panel("INBOX", inbox));
        prep = MakeLabel("Przygotowanie pojawi się w dzień wyścigu.", 15, Black);
        prep.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        desk.AddChild(Panel("PREP", prep));
        seatBox = new VBoxContainer();
        seatBox.AddThemeConstantOverride("separation", 6);
        desk.AddChild(seatBox);
        outcome = MakeLabel("Po wyścigu tu będzie wynik i najważniejsze wydarzenia.", 15, Black);
        outcome.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        desk.AddChild(Panel("WYNIK", outcome));

        string autosave = Path.Combine(Path.GetTempPath(), "peloton-hub-prerace.peloton");
        host = new CareerHubHost(
            ApplicationFactory.Create(WatchContentPath.FindContentRoot()),
            autosave);
        CommandResult opened = host.Open(91234);
        if (!opened.Succeeded)
        {
            status.Text = $"Nie udało się otworzyć kariery ({opened.ReasonCode}).";
            primaryButton.Disabled = true;
            watchButton.Disabled = true;
            settingsButton.Disabled = true;
            return;
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

    private void OnToggleFilm()
    {
        if (host is null)
        {
            return;
        }

        host.SetWatchFilmEnabled(!host.Settings.WatchFilmEnabled);
        Refresh();
    }

    private void OnWatch()
    {
        if (host is null)
        {
            return;
        }

        CommandResult started = host.OpenWatch();
        if (!started.Succeeded)
        {
            status!.Text = Reason(started.ReasonCode);
            Refresh();
            return;
        }

        ShowWatch();
        Refresh();
    }

    private void ShowWatch()
    {
        if (host?.Watch is null)
        {
            return;
        }

        watchScreen?.QueueFree();
        watchScreen = new WatchRaceScreen
        {
            ExternalHost = host.Watch,
        };
        watchScreen.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(watchScreen);
        if (desk is not null)
        {
            desk.Visible = false;
        }
    }

    private void Apply(CommandResult result)
    {
        if (!result.Succeeded)
        {
            status!.Text = Reason(result.ReasonCode);
        }

        Refresh();
    }

    private void Refresh()
    {
        if (host is null)
        {
            return;
        }

        if (host.State == GameState.RaceLive && host.Watch is not null)
        {
            if (watchScreen is null)
            {
                ShowWatch();
            }

            return;
        }

        if (host.State != GameState.RaceLive)
        {
            HideWatch();
        }

        CareerDayProjection? day = host.Day;
        dateLabel!.Text = host.State switch
        {
            GameState.RaceResultsFlow => "WYNIK",
            GameState.RaceDebriefFlow => "DEBRIEF",
            _ => day is null
                ? "DZIEŃ —"
                : string.Create(CultureInfo.InvariantCulture, $"DZIEŃ {day.DayNumber}"),
        };
        employerLabel!.Text = day?.EmployerName ?? host.Result?.Title ?? "Zespół";
        status!.Text = StatusLine(host, day);
        primaryButton!.Text = PrimaryCaption(host, day);
        primaryButton.Visible = true;
        primaryButton.Disabled = false;
        watchButton!.Visible = host.Settings.WatchFilmEnabled &&
            host.State is GameState.Management or GameState.RacePreparationFlow;
        watchButton.Disabled = host.State == GameState.Management && day is { RaceDueToday: false };
        settingsButton!.Text = host.Settings.WatchFilmEnabled ? "FILM: WŁ" : "FILM: WYŁ";
        settingsButton.Visible = host.State is GameState.Management or GameState.RacePreparationFlow;

        calendar!.Text = string.Join('\n', host.Calendar.Select(entry =>
            string.Create(
                CultureInfo.InvariantCulture,
                $"dzień {entry.DayNumber}  {entry.Title}  {entry.Status}")));
        inbox!.Text = host.Inbox.Count == 0
            ? "Skrzynka pusta."
            : string.Join('\n', host.Inbox.Select(item => $"{item.Category}: {item.Body}"));
        RacePreparationProjection? preparation = host.Preparation;
        prep!.Text = preparation is null
            ? "Czwórka z Beskid–Vetter. W dzień wyścigu wybierasz, kto prowadzi i kto finiszuje."
            : $"{preparation.Title} · {preparation.Objective}";
        RebuildSeats(preparation);
        outcome!.Text = OutcomeText(host);
    }

    private static string StatusLine(CareerHubHost host, CareerDayProjection? day)
    {
        if (host.State == GameState.RaceResultsFlow && host.Result is RaceResultProjection result)
        {
            return $"{result.Title} · wygrał {result.WinnerLabel}";
        }

        if (host.State == GameState.RaceDebriefFlow && host.Debrief is RaceDebriefProjection debrief)
        {
            return debrief.Notes.Count == 0 ? debrief.Objective : debrief.Notes[0];
        }

        return day is null
            ? string.Empty
            : $"{day.ManagerName} · {day.PrimaryLabel} · next {day.NextRaceDayNumber}";
    }

    private static string PrimaryCaption(CareerHubHost host, CareerDayProjection? day)
    {
        return host.State switch
        {
            GameState.RacePreparationFlow => host.Settings.WatchFilmEnabled ? "OGLĄDAJ ETAP" : "JEDŹ WYŚCIG",
            GameState.RaceResultsFlow => "DALEJ",
            GameState.RaceDebriefFlow => "ZAMKNIJ",
            _ => day is null ? "ADVANCE DAY" : day.PrimaryLabel.ToUpperInvariant(),
        };
    }

    private static string OutcomeText(CareerHubHost host)
    {
        if (host.Result is RaceResultProjection result)
        {
            string finish = string.Join(
                '\n',
                result.FinishOrder.Select((place, index) =>
                    string.Create(CultureInfo.InvariantCulture, $"{index + 1}. {place.Label}")));
            return string.Join('\n', result.Headlines) + "\n\n" + finish;
        }

        if (host.Debrief is RaceDebriefProjection debrief)
        {
            return string.Join('\n', debrief.Notes);
        }

        return "Po wyścigu tu będzie wynik i najważniejsze wydarzenia.";
    }

    private void RebuildSeats(RacePreparationProjection? preparation)
    {
        if (seatBox is null)
        {
            return;
        }

        foreach (Node child in seatBox.GetChildren())
        {
            child.QueueFree();
        }

        if (preparation is null)
        {
            return;
        }

        foreach (SquadSeat seat in preparation.Seats)
        {
            SquadSeat captured = seat;
            Button button = MakeButton(
                $"{captured.Name} · {captured.Role} — {captured.Why}",
                () => OnCycleRole(captured.RiderId, captured.Role));
            button.CustomMinimumSize = new Vector2(0, 44);
            seatBox.AddChild(button);
        }
    }

    private void OnCycleRole(WorldEntityId riderId, string currentRole)
    {
        if (host is null)
        {
            return;
        }

        string next = currentRole switch
        {
            SquadRoles.Worker => SquadRoles.Card,
            SquadRoles.Card => SquadRoles.Leader,
            _ => SquadRoles.Worker,
        };
        Apply(host.AssignRole(riderId, next));
    }

    private void HideWatch()
    {
        watchScreen?.QueueFree();
        watchScreen = null;
        if (desk is not null)
        {
            desk.Visible = true;
        }
    }

    private static string Reason(string reasonCode) => reasonCode switch
    {
        "RACE_DAY_PENDING" => "Najpierw jedź wyścig.",
        "PREP_ROLES_INCOMPLETE" => "Potrzebujesz lidera i karta.",
        "GAME_STATE_INVALID" => "Ta akcja nie jest teraz dostępna.",
        _ => reasonCode,
    };

    private static VBoxContainer Panel(string heading, Label body)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 6);
        box.AddChild(MakeLabel(heading, 13, Red));
        box.AddChild(body);
        return box;
    }

    private static HBoxContainer WrapRow(params Control[] children)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        foreach (Control child in children)
        {
            row.AddChild(child);
        }

        return row;
    }

    private static Label MakeLabel(string text, int size, Color color)
    {
        Label label = new() { Text = text };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        Button button = new() { Text = text };
        button.CustomMinimumSize = new Vector2(220, 48);
        button.AddThemeColorOverride("font_color", White);
        button.AddThemeColorOverride("font_hover_color", Paper);
        StyleBoxFlat normal = new() { BgColor = Black, ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8 };
        StyleBoxFlat hover = new() { BgColor = Red, ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8 };
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.Pressed += onPressed;
        return button;
    }
}
