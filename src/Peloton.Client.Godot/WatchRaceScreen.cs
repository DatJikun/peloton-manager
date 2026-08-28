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
    private WatchRaceHost? host;
    private WatchRaceMapView? map;
    private Label? title;
    private Label? clock;
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
    private string paintedDecisionKey = string.Empty;
    private string winnerLabel = string.Empty;
    private GameState paintedState;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        ColorRect background = new()
        {
            Color = WatchChrome.Paper,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        VBoxContainer root = new();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 28;
        root.OffsetTop = 24;
        root.OffsetRight = -28;
        root.OffsetBottom = -24;
        root.AddThemeConstantOverride("separation", 16);
        AddChild(root);

        title = WatchChrome.MakeLabel("WATCH RACE", 42, WatchChrome.Black, displayFace: true);
        root.AddChild(title);

        rateRow = new HBoxContainer();
        rateRow.AddThemeConstantOverride("separation", 8);
        foreach (int seconds in WatchFilmDuration.ChoicesSeconds)
        {
            int captured = seconds;
            Button button = WatchChrome.MakeButton(
                WatchFilmDuration.Label(captured),
                () => OnSelectFilm(captured),
                WatchChrome.Kind.Segment);
            button.Name = $"Film{captured}";
            rateRow.AddChild(button);
        }

        root.AddChild(rateRow);

        autonomyButton = WatchChrome.MakeButton("Autonomia DS: nie", OnToggleAutonomy, WatchChrome.Kind.Secondary);
        root.AddChild(autonomyButton);

        startButton = WatchChrome.MakeButton("Oglądaj", OnStart, WatchChrome.Kind.Primary);
        root.AddChild(startButton);

        map = new WatchRaceMapView
        {
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(map);

        clock = WatchChrome.MakeLabel(string.Empty, 14, WatchChrome.Black);
        root.AddChild(clock);

        board = WatchChrome.MakeLabel(string.Empty, 14, WatchChrome.Black);
        board.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(board);

        liveRow = new HBoxContainer();
        liveRow.AddThemeConstantOverride("separation", 12);
        pauseButton = WatchChrome.MakeButton("Pauza", OnTogglePause, WatchChrome.Kind.Secondary);
        exitButton = WatchChrome.MakeButton("Wyjdź", OnExit, WatchChrome.Kind.Secondary);
        liveRow.AddChild(pauseButton);
        liveRow.AddChild(exitButton);
        liveRow.Visible = false;
        root.AddChild(liveRow);

        decisionBox = new VBoxContainer();
        decisionBox.AddThemeConstantOverride("separation", 12);
        decisionBox.Visible = false;
        root.AddChild(decisionBox);

        result = WatchChrome.MakeLabel(string.Empty, 22, WatchChrome.Black, displayFace: true);
        result.Visible = false;
        root.AddChild(result);

        continueButton = WatchChrome.MakeButton("Dalej", OnAcknowledge, WatchChrome.Kind.Primary);
        continueButton.Visible = false;
        root.AddChild(continueButton);

        string autosavePath = Path.Combine(Path.GetTempPath(), "peloton-watch-prerace.peloton");
        host = new WatchRaceHost(
            ApplicationFactory.Create(WatchContentPath.FindContentRoot()),
            autosavePath);
        CommandResult opened = host.OpenPrototype(91234);
        if (!opened.Succeeded)
        {
            startButton.Disabled = true;
            return;
        }

        RefreshFilmButtons();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (host is null)
        {
            return;
        }

        if (host.State == GameState.RaceLive)
        {
            CommandResult ticked = host.Tick(delta);
            if (ticked.Succeeded)
            {
                RefreshLive();
            }
        }

        if (paintedState != host.State)
        {
            Refresh();
        }
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
        paintedDecisionKey = string.Empty;
        ClearDecisionBox();
        decisionBox!.Visible = false;
        liveRow!.Visible = false;
        result!.Visible = false;
        continueButton!.Visible = false;
        if (map is not null)
        {
            map.ShowView(null);
            map.ShowCourse(null);
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

    private void Apply(CommandResult command)
    {
        _ = command;
        Refresh();
    }

    private void Refresh()
    {
        if (host is null)
        {
            return;
        }

        paintedState = host.State;
        bool live = host.State == GameState.RaceLive;
        bool prep = host.State == GameState.RacePreparationFlow;
        startButton!.Visible = prep;
        rateRow!.Visible = prep;
        autonomyButton!.Visible = prep;
        liveRow!.Visible = live;
        continueButton!.Visible = host.State is GameState.RaceResultsFlow or GameState.RaceDebriefFlow;
        continueButton.Text = host.State == GameState.RaceDebriefFlow ? "KONIEC" : "DALEJ";
        WatchChrome.ApplyKind(
            continueButton,
            WatchChrome.Kind.Primary,
            selected: false);
        pauseButton!.Text = host.PresentationPaused ? "WZNÓW" : "PAUZA";
        WatchChrome.ApplyKind(pauseButton, WatchChrome.Kind.Secondary, selected: host.PresentationPaused);
        RefreshFilmButtons();
        RefreshAutonomyButton();

        if (prep)
        {
            title!.Text = "WATCH RACE";
            clock!.Text = string.Empty;
            board!.Text = string.Empty;
            decisionBox!.Visible = false;
            result!.Visible = false;
        }

        if (host.State == GameState.Management)
        {
            title!.Text = "WATCH RACE";
            liveRow!.Visible = false;
            decisionBox!.Visible = false;
        }

        if (live)
        {
            title!.Text = "RACE LIVE";
            RefreshLive();
            return;
        }

        RefreshDecision();
        RefreshResult();
    }

    private void RefreshLive()
    {
        if (host is null || host.Interpolated is not InterpolatedWatchView view)
        {
            return;
        }

        clock!.Text = WatchFilmDuration.Clock(view.WatchSecond, host.ExpectedFilmSeconds);
        board!.Text = FormatBoard(view);
        map?.ShowView(view);
        bool paused = host.PresentationPaused || host.PendingDecision is not null;
        string pauseCaption = paused ? "WZNÓW" : "PAUZA";
        if (pauseButton!.Text != pauseCaption)
        {
            pauseButton.Text = pauseCaption;
            WatchChrome.ApplyKind(pauseButton, WatchChrome.Kind.Secondary, selected: paused);
        }

        RefreshDecision();
        RefreshResult();
    }

    private void RefreshDecision()
    {
        PendingRaceDecision? pending = host?.PendingDecision;
        string key = pending is null ? string.Empty : pending.RequestId.Value;
        if (key == paintedDecisionKey)
        {
            decisionBox!.Visible = pending is not null;
            return;
        }

        paintedDecisionKey = key;
        ClearDecisionBox();
        decisionBox!.Visible = pending is not null;
        if (pending is null)
        {
            return;
        }

        Button dsButton = WatchChrome.MakeButton(
            WatchObservationText.DsAction(pending.DelegatedDefaultOption.ToString()),
            () => OnDecide(pending.DelegatedDefaultOption),
            WatchChrome.Kind.Primary);
        dsButton.CustomMinimumSize = new Vector2(420, 52);
        decisionBox.AddChild(dsButton);
        foreach (RaceDecisionOption option in pending.LegalOptions)
        {
            if (option == pending.DelegatedDefaultOption || option == RaceDecisionOption.TrustDs)
            {
                continue;
            }

            RaceDecisionOption captured = option;
            Button button = WatchChrome.MakeButton(
                WatchObservationText.DecisionOption(captured.ToString()),
                () => OnDecide(captured),
                WatchChrome.Kind.Secondary);
            decisionBox.AddChild(button);
        }
    }

    private void ClearDecisionBox()
    {
        foreach (Node child in decisionBox!.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnDecide(RaceDecisionOption option)
    {
        if (host is null)
        {
            return;
        }

        paintedDecisionKey = string.Empty;
        Apply(host.Respond(option));
    }

    private void RefreshResult()
    {
        RaceResultProjection? projection = host?.Result;
        RaceDebriefProjection? debrief = host?.Debrief;
        result!.Visible = projection is not null;
        if (projection is not null)
        {
            title!.Text = "WYNIK";
            clock!.Text = string.Empty;
            winnerLabel = projection.WinnerLabel;
            result.Text = winnerLabel;
            map?.ShowView(null);
            liveRow!.Visible = false;
            decisionBox!.Visible = false;
            board!.Text = string.Empty;
            return;
        }

        if (debrief is not null)
        {
            title!.Text = "WYNIK";
            result.Visible = winnerLabel.Length > 0;
            result.Text = winnerLabel;
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
                WatchChrome.ApplyKind(button, WatchChrome.Kind.Segment, selected);
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
        WatchChrome.ApplyKind(autonomyButton, WatchChrome.Kind.Secondary, selected: host.DsAutonomy);
    }

    private static string FormatBoard(InterpolatedWatchView view)
    {
        if (view.Riders.Count == 0)
        {
            return string.Empty;
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
}
