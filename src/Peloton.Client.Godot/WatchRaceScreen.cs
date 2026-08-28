using System;
using System.Globalization;
using System.IO;
using Godot;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;

namespace Peloton.Client.Godot;

public sealed partial class WatchRaceScreen : Control
{
    private static readonly Color Paper = new("f3ede1");
    private static readonly Color Red = new("d11f1f");
    private static readonly Color Black = new("0c0c0d");
    private static readonly Color Gray = new("6f6f72");
    private static readonly Color White = new("fffdf7");

    private WatchRaceHost? host;
    private WatchRaceMapView? map;
    private Label? title;
    private Label? status;
    private Label? clock;
    private Label? observations;
    private Label? board;
    private Label? result;
    private HBoxContainer? rateRow;
    private HBoxContainer? liveRow;
    private VBoxContainer? decisionBox;
    private Button? startButton;
    private Button? autonomyButton;
    private Button? pauseButton;
    private Button? exitButton;
    private Button? continueButton;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        ColorRect background = new()
        {
            Color = Paper,
        };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        VBoxContainer root = new();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 28;
        root.OffsetTop = 24;
        root.OffsetRight = -28;
        root.OffsetBottom = -24;
        root.AddThemeConstantOverride("separation", 14);
        AddChild(root);

        title = MakeLabel("WATCH RACE", 42, Black);
        root.AddChild(title);

        status = MakeLabel("Wybierz czas filmu, autonomię DS i oglądaj.", 16, Gray);
        root.AddChild(status);

        rateRow = new HBoxContainer();
        rateRow.AddThemeConstantOverride("separation", 8);
        foreach (int seconds in WatchFilmDuration.ChoicesSeconds)
        {
            int captured = seconds;
            Button button = MakeButton(
                WatchFilmDuration.Label(captured),
                () => OnSelectFilm(captured),
                compact: true);
            button.Name = $"Film{captured}";
            rateRow.AddChild(button);
        }

        root.AddChild(rateRow);

        autonomyButton = MakeButton("AUTONOMIA DS: NIE", OnToggleAutonomy, compact: true);
        autonomyButton.CustomMinimumSize = new Vector2(220, 40);
        autonomyButton.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        root.AddChild(autonomyButton);

        startButton = MakeButton("OGLĄDAJ", onPressed: OnStart);
        startButton.CustomMinimumSize = new Vector2(220, 48);
        startButton.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        root.AddChild(startButton);

        map = new WatchRaceMapView
        {
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(map);

        clock = MakeLabel("zegar oglądania —", 15, Black);
        root.AddChild(clock);

        board = MakeLabel("Tablica sztabu pojawi się po starcie.", 14, Black);
        board.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(board);

        observations = MakeLabel(string.Empty, 15, Black);
        observations.Visible = false;
        root.AddChild(observations);

        liveRow = new HBoxContainer();
        liveRow.AddThemeConstantOverride("separation", 8);
        pauseButton = MakeButton("PAUZA", OnTogglePause, compact: true);
        exitButton = MakeButton("WYJŚCIE", OnExit, compact: true);
        liveRow.AddChild(pauseButton);
        liveRow.AddChild(exitButton);
        liveRow.Visible = false;
        root.AddChild(liveRow);

        decisionBox = new VBoxContainer();
        decisionBox.AddThemeConstantOverride("separation", 8);
        decisionBox.Visible = false;
        root.AddChild(decisionBox);

        result = MakeLabel(string.Empty, 18, Black);
        result.Visible = false;
        root.AddChild(result);

        continueButton = MakeButton("WYNIK ZATWIERDZONY", OnAcknowledge);
        continueButton.Visible = false;
        root.AddChild(continueButton);

        string autosavePath = Path.Combine(Path.GetTempPath(), "peloton-watch-prerace.peloton");
        host = new WatchRaceHost(
            ApplicationFactory.Create(WatchContentPath.FindContentRoot()),
            autosavePath);
        CommandResult opened = host.OpenPrototype(91234);
        if (!opened.Succeeded)
        {
            status.Text = $"Nie udało się otworzyć prototypu ({opened.ReasonCode}).";
            startButton.Disabled = true;
            return;
        }

        RefreshFilmButtons();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (host is null || host.State != GameState.RaceLive)
        {
            return;
        }

        CommandResult ticked = host.Tick(delta);
        if (!ticked.Succeeded)
        {
            status!.Text = $"Zegar oglądania zatrzymał się ({ticked.ReasonCode}).";
            return;
        }

        Refresh();
    }

    private void OnSelectFilm(int seconds)
    {
        if (host is null)
        {
            return;
        }

        Apply(host.SelectFilmDuration(seconds));
        RefreshFilmButtons();
    }

    private void OnToggleAutonomy()
    {
        if (host is null)
        {
            return;
        }

        Apply(host.SelectDsAutonomy(!host.DsAutonomy));
        RefreshAutonomyButton();
    }

    private void OnStart()
    {
        if (host is null)
        {
            return;
        }

        Apply(host.StartWatch());
        if (map is not null)
        {
            map.ShowCourse(host.Course);
        }
    }

    private void OnTogglePause()
    {
        if (host is null)
        {
            return;
        }

        Apply(host.SetPresentationPaused(!host.PresentationPaused));
    }

    private void OnExit()
    {
        if (host is null)
        {
            return;
        }

        Apply(host.Abandon());
        decisionBox!.Visible = false;
        liveRow!.Visible = false;
        result!.Visible = false;
        continueButton!.Visible = false;
        if (map is not null)
        {
            map.ShowView(null);
        }
    }

    private void OnAcknowledge()
    {
        if (host is null)
        {
            return;
        }

        Apply(host.State == GameState.RaceDebriefFlow ? host.CompleteDebrief() : host.AcknowledgeResults());
    }

    private void Apply(CommandResult result)
    {
        if (!result.Succeeded)
        {
            status!.Text = ReasonMessage(result.ReasonCode);
        }

        Refresh();
    }

    private void Refresh()
    {
        if (host is null)
        {
            return;
        }

        bool live = host.State == GameState.RaceLive;
        bool prep = host.State == GameState.RacePreparationFlow;
        startButton!.Visible = prep;
        rateRow!.Visible = prep || live;
        autonomyButton!.Visible = prep || live;
        liveRow!.Visible = live;
        continueButton!.Visible = host.State is GameState.RaceResultsFlow or GameState.RaceDebriefFlow;
        continueButton.Text = host.State == GameState.RaceDebriefFlow ? "KONIEC" : "WYNIK ZATWIERDZONY";
        pauseButton!.Text = host.PresentationPaused ? "WZNÓW" : "PAUZA";
        RefreshFilmButtons();
        RefreshAutonomyButton();

        if (prep)
        {
            title!.Text = "WATCH RACE";
            status!.Text = host.DsAutonomy
                ? $"Seed 91234 · film {WatchFilmDuration.Label(host.SelectedFilmSeconds)} · DS sam podejmuje decyzje."
                : $"Seed 91234 · film {WatchFilmDuration.Label(host.SelectedFilmSeconds)} · oglądaj.";
            clock!.Text = "Film ruszy po Oglądaj.";
            if (board is not null)
            {
                board.Text = "Tablica sztabu pojawi się po starcie.";
                board.Visible = true;
            }

            decisionBox!.Visible = false;
            result!.Visible = false;
        }

        if (host.State == GameState.Management)
        {
            title!.Text = "WATCH RACE";
            status!.Text = "Etap zamknięty. To nie jest Career Hub.";
            liveRow!.Visible = false;
            decisionBox!.Visible = false;
        }

        if (live && host.Interpolated is InterpolatedWatchView view)
        {
            title!.Text = "RACE LIVE";
            status!.Text = host.PendingDecision is null
                ? host.DsAutonomy
                    ? "Autonomia DS. Film jest przyspieszoną symulacją, bez pauzy na decyzjach."
                    : "Kariera zablokowana. Autosave przed startem. Bez zapisu w trakcie etapu."
                : "Pauza na decyzji. Fizyka stoi, aż sztab odpowie.";
            clock!.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"film {WatchFilmDuration.Clock(view.WatchSecond, host.ExpectedFilmSeconds)} · etap {view.RaceSecond}s{(view.Paused ? " · pauza" : string.Empty)}");
            board!.Text = FormatBoard(view);
            board.Visible = true;
            map?.ShowView(view);
        }

        RefreshDecision();
        RefreshResult();
    }

    private void RefreshDecision()
    {
        foreach (Node child in decisionBox!.GetChildren())
        {
            child.QueueFree();
        }

        PendingRaceDecision? pending = host?.PendingDecision;
        decisionBox.Visible = pending is not null;
        if (pending is null)
        {
            return;
        }

        decisionBox.AddChild(MakeLabel("DECYZJA", 22, Red));
        decisionBox.AddChild(MakeLabel(pending.Trigger, 15, Black));
        string dsCaption = WatchObservationText.DsAction(pending.DelegatedDefaultOption.ToString());
        Button dsButton = MakeButton(dsCaption.ToUpperInvariant(), () => OnDecide(pending.DelegatedDefaultOption));
        dsButton.CustomMinimumSize = new Vector2(420, 44);
        decisionBox.AddChild(dsButton);
        foreach (RaceDecisionOption option in pending.LegalOptions)
        {
            if (option == pending.DelegatedDefaultOption || option == RaceDecisionOption.TrustDs)
            {
                continue;
            }

            RaceDecisionOption captured = option;
            string caption = WatchObservationText.DecisionOption(captured.ToString());
            Button button = MakeButton(caption.ToUpperInvariant(), () => OnDecide(captured), compact: true);
            decisionBox.AddChild(button);
        }
    }

    private void OnDecide(RaceDecisionOption option)
    {
        if (host is null)
        {
            return;
        }

        Apply(host.Respond(option));
    }

    private void RefreshResult()
    {
        RaceResultProjection? projection = host?.Result;
        RaceDebriefProjection? debrief = host?.Debrief;
        result!.Visible = projection is not null || debrief is not null;
        if (projection is not null)
        {
            title!.Text = "WYNIK";
            status!.Text = "Oficjalny wynik z LastRace. Bez drugiego RunBatch.";
            result.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"Zwycięzca {projection.WinnerLabel} ({projection.WinnerId.Value})\n{host!.LastChecksum}");
            map?.ShowView(null);
            liveRow!.Visible = false;
            decisionBox!.Visible = false;
            if (board is not null)
            {
                board.Visible = false;
            }
            return;
        }

        if (debrief is not null)
        {
            title!.Text = "DEBRIEF";
            status!.Text = debrief.Objective;
            result.Text = string.Join('\n', debrief.Notes);
        }
    }

    private void RefreshFilmButtons()
    {
        if (host is null || rateRow is null)
        {
            return;
        }

        foreach (Node child in rateRow.GetChildren())
        {
            if (child is Button button)
            {
                bool selected = button.Name == $"Film{host.SelectedFilmSeconds}";
                button.Modulate = selected ? Red : Colors.White;
                button.Disabled = host.State == GameState.RaceLive;
            }
        }
    }

    private void RefreshAutonomyButton()
    {
        if (host is null || autonomyButton is null)
        {
            return;
        }

        autonomyButton.Text = host.DsAutonomy ? "AUTONOMIA DS: TAK" : "AUTONOMIA DS: NIE";
        autonomyButton.Modulate = host.DsAutonomy ? Red : Colors.White;
        autonomyButton.Disabled = host.State == GameState.RaceLive;
    }

    private static string FormatBoard(InterpolatedWatchView view)
    {
        if (view.Riders.Count == 0)
        {
            return "Brak zawodników w radiu sztabu.";
        }

        string[] lines = new string[view.Riders.Count + 1];
        lines[0] = "#   zawodnik            km/h   strata     teren              radio";
        for (int index = 0; index < view.Riders.Count; index++)
        {
            InterpolatedRiderView rider = view.Riders[index];
            lines[index + 1] = string.Create(
                CultureInfo.InvariantCulture,
                $"{rider.Place,-3} {rider.Label,-18} {WatchObservationText.Speed(rider.SpeedMps),-7} {WatchObservationText.Gap(rider.GapM),-10} {WatchObservationText.Terrain(rider.Gradient),-18} {WatchObservationText.Radio(rider.SpeedMps, rider.ShelterMultiplier, rider.Gradient, rider.GapM)}");
        }

        return string.Join('\n', lines);
    }

    private static string ReasonMessage(string reasonCode)
    {
        return reasonCode switch
        {
            "SAVE_FORBIDDEN_IN_RACE_LIVE" => "Zapis w trakcie etapu jest zablokowany.",
            "LOAD_FORBIDDEN_IN_RACE_LIVE" => "Wczytanie w trakcie etapu jest zablokowane.",
            "WATCH_FILM_LOCKED" => "Czas filmu ustala się przed startem.",
            "WATCH_FILM_INVALID" => "Wybierz 30 s, 1 min, 2 min, 3 min albo 5 min.",
            "WATCH_AUTONOMY_LOCKED" => "Autonomię DS ustala się przed startem.",
            "WATCH_RATE_LOCKED" => "Tempo ustala się przed startem.",
            "GAME_STATE_INVALID" => "Ta akcja nie jest teraz dostępna.",
            _ => reasonCode,
        };
    }

    private static Label MakeLabel(string text, int size, Color color)
    {
        Label label = new()
        {
            Text = text,
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static Button MakeButton(string text, Action onPressed, bool compact = false)
    {
        Button button = new()
        {
            Text = text,
        };
        button.CustomMinimumSize = new Vector2(compact ? 120 : 220, compact ? 40 : 48);
        button.AddThemeColorOverride("font_color", White);
        button.AddThemeColorOverride("font_hover_color", Paper);
        button.AddThemeColorOverride("font_pressed_color", Paper);
        StyleBoxFlat normal = new()
        {
            BgColor = Black,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        StyleBoxFlat hover = new()
        {
            BgColor = Red,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.Pressed += onPressed;
        return button;
    }
}
