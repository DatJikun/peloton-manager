using System.IO;
using Godot;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;

namespace Peloton.Client.Godot;

public sealed partial class WatchRaceScreen : Control
{
    private WatchRaceHost? host;
    private Control? prepRoot;
    private WatchLiveHud? liveHud;
    private Label? title;
    private Label? result;
    private HBoxContainer? rateRow;
    private VBoxContainer? decisionBox;
    private Button? startButton;
    private Button? autonomyButton;
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
        root.OffsetLeft = 20;
        root.OffsetTop = 16;
        root.OffsetRight = -20;
        root.OffsetBottom = -16;
        root.AddThemeConstantOverride("separation", 12);
        AddChild(root);

        prepRoot = new VBoxContainer();
        prepRoot.AddThemeConstantOverride("separation", 14);
        root.AddChild(prepRoot);

        title = WatchChrome.MakeLabel("WATCH RACE", 42, WatchChrome.Black, displayFace: true);
        prepRoot.AddChild(title);

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

        prepRoot.AddChild(rateRow);

        autonomyButton = WatchChrome.MakeButton("Autonomia DS: nie", OnToggleAutonomy, WatchChrome.Kind.Secondary);
        prepRoot.AddChild(autonomyButton);

        startButton = WatchChrome.MakeButton("Oglądaj", OnStart, WatchChrome.Kind.Primary);
        prepRoot.AddChild(startButton);

        liveHud = new WatchLiveHud(OnTogglePause, OnExit);
        liveHud.SizeFlagsVertical = SizeFlags.ExpandFill;
        liveHud.Visible = false;
        root.AddChild(liveHud);
        decisionBox = liveHud.DecisionSlot;

        result = WatchChrome.MakeLabel(string.Empty, 28, WatchChrome.Black, displayFace: true);
        result.Visible = false;
        root.AddChild(result);

        continueButton = WatchChrome.MakeButton("Dalej", OnAcknowledge, WatchChrome.Kind.Team);
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
        if (host is null || liveHud is null)
        {
            return;
        }

        Apply(host.StartWatch());
        liveHud.ShowCourse(host.Course);
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
        if (liveHud is not null)
        {
            liveHud.Visible = false;
            liveHud.ShowCourse(null);
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
        prepRoot!.Visible = prep;
        if (liveHud is not null)
        {
            liveHud.Visible = live;
        }

        continueButton!.Visible = host.State is GameState.RaceResultsFlow or GameState.RaceDebriefFlow;
        continueButton.Text = host.State == GameState.RaceDebriefFlow ? "KONIEC" : "DALEJ";
        WatchChrome.ApplyKind(continueButton, WatchChrome.Kind.Team, selected: false);
        RefreshFilmButtons();
        RefreshAutonomyButton();

        if (prep)
        {
            title!.Text = "WATCH RACE";
            result!.Visible = false;
            decisionBox!.Visible = false;
        }

        if (live)
        {
            RefreshLive();
            return;
        }

        RefreshDecision();
        RefreshResult();
    }

    private void RefreshLive()
    {
        if (host is null || liveHud is null || host.Interpolated is not InterpolatedWatchView view)
        {
            return;
        }

        bool deciding = host.PendingDecision is not null;
        liveHud.Bind(view, host.ExpectedFilmSeconds, host.PresentationPaused, deciding);

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
        dsButton.CustomMinimumSize = new Vector2(280, 48);
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
            if (liveHud is not null)
            {
                liveHud.Visible = false;
            }

            winnerLabel = projection.WinnerLabel;
            result.Text = winnerLabel;
            decisionBox!.Visible = false;
            return;
        }

        if (debrief is not null)
        {
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
}
