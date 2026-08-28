using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed partial class WatchLiveHud : Control
{
    private readonly Label raceClock;
    private readonly Label profileMeta;
    private readonly Label livePill;
    private readonly Label leaderName;
    private readonly Label leaderRole;
    private readonly Label leaderDistance;
    private readonly Label leaderToGo;
    private readonly Label leaderSpeed;
    private readonly Label leaderGradient;
    private readonly VBoxContainer riderRows;
    private readonly VBoxContainer gapRows;
    private readonly Button teamTab;
    private readonly Button fieldTab;
    private readonly Label tickerMessage;
    private InterpolatedWatchView? lastView;
    private int lastFilmSeconds;
    private bool lastPaused;
    private bool lastDeciding;
    private bool showSquadGaps = true;

    public WatchLiveHud(Action onContinue, Action onExit)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;

        VBoxContainer root = new();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        HBoxContainer top = new();
        top.AddThemeConstantOverride("separation", 14);
        root.AddChild(top);

        Label liveTitle = WatchChrome.MakeLabel("RACE LIVE", 40, WatchChrome.Black, displayFace: true);
        top.AddChild(liveTitle);

        Control spacer = new();
        spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        top.AddChild(spacer);

        Label clockCaption = WatchChrome.MakeLabel("CZAS WYŚCIGU", 9, WatchChrome.Gray);
        VBoxContainer clockStack = new();
        clockStack.AddThemeConstantOverride("separation", 0);
        clockStack.AddChild(clockCaption);
        raceClock = WatchChrome.MakeLabel("0:00 / 0:00", 22, WatchChrome.Black, displayFace: true);
        clockStack.AddChild(raceClock);
        top.AddChild(clockStack);

        HBoxContainer headerActions = new();
        headerActions.AddThemeConstantOverride("separation", 8);
        HeaderContinue = WatchChrome.MakeHeaderButton("Pauza", onContinue);
        HeaderExit = WatchChrome.MakeHeaderButton("Wyjdź", onExit);
        headerActions.AddChild(HeaderContinue);
        headerActions.AddChild(HeaderExit);
        top.AddChild(headerActions);

        HBoxContainer columns = new();
        columns.AddThemeConstantOverride("separation", 12);
        columns.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(columns);

        VBoxContainer left = new();
        left.AddThemeConstantOverride("separation", 12);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        left.SizeFlagsStretchRatio = 1.7f;
        columns.AddChild(left);

        WatchPanel profile = new("PROFIL ETAPU");
        profile.SizeFlagsVertical = SizeFlags.ExpandFill;
        profile.SizeFlagsStretchRatio = 1.45f;
        profileMeta = WatchChrome.MakeLabel(string.Empty, 11, WatchChrome.Paper);
        livePill = WatchChrome.MakeLabel("WYŚCIG TRWA", 11, WatchChrome.Paper);
        livePill.MouseFilter = MouseFilterEnum.Ignore;
        profile.HeaderTrail.AddChild(profileMeta);
        profile.HeaderTrail.AddChild(livePill);
        Map = new WatchRaceMapView
        {
            DrawOuterFrame = false,
            CustomMinimumSize = new Vector2(0, 240),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        profile.Body.AddChild(Map);
        left.AddChild(profile);

        WatchPanel riders = new("ZAWODNICY");
        riders.SizeFlagsVertical = SizeFlags.ExpandFill;
        riders.SizeFlagsStretchRatio = 1.0f;
        riderRows = new VBoxContainer();
        riderRows.AddThemeConstantOverride("separation", 3);
        riders.Body.AddChild(riderRows);
        left.AddChild(riders);

        VBoxContainer right = new();
        right.AddThemeConstantOverride("separation", 12);
        right.CustomMinimumSize = new Vector2(340, 0);
        right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        right.SizeFlagsStretchRatio = 1.0f;
        columns.AddChild(right);

        WatchPanel leader = new("LIDER WYŚCIGU");
        VBoxContainer leaderBody = new();
        leaderBody.AddThemeConstantOverride("separation", 8);
        leaderName = WatchChrome.MakeLabel("—", 28, WatchChrome.Black, displayFace: true);
        leaderRole = WatchChrome.MakeLabel("LIDER ETAPU", 11, WatchChrome.Gray);
        leaderBody.AddChild(leaderName);
        leaderBody.AddChild(leaderRole);
        leaderDistance = AddStat(leaderBody, "PRZEJECHANE");
        leaderToGo = AddStat(leaderBody, "DO METY");
        leaderSpeed = AddStat(leaderBody, "PRĘDKOŚĆ");
        leaderGradient = AddStat(leaderBody, "NACHYLENIE");
        leader.Body.AddChild(leaderBody);
        right.AddChild(leader);

        WatchPanel gaps = new("RÓŻNICE CZASOWE");
        gaps.SizeFlagsVertical = SizeFlags.ExpandFill;
        teamTab = WatchChrome.MakeButton("Zespół", () => SetGapTab(true), WatchChrome.Kind.Segment);
        fieldTab = WatchChrome.MakeButton("Ogólne", () => SetGapTab(false), WatchChrome.Kind.Segment);
        gaps.HeaderTrail.AddChild(teamTab);
        gaps.HeaderTrail.AddChild(fieldTab);
        gapRows = new VBoxContainer();
        gapRows.AddThemeConstantOverride("separation", 3);
        gaps.Body.AddChild(gapRows);
        right.AddChild(gaps);

        DecisionSlot = new VBoxContainer();
        DecisionSlot.AddThemeConstantOverride("separation", 8);
        right.AddChild(DecisionSlot);

        HBoxContainer tickerRow = new();
        tickerRow.AddThemeConstantOverride("separation", 16);
        PanelContainer tickerBar = new();
        tickerBar.AddThemeStyleboxOverride("panel", WatchChrome.HeaderBar());
        MarginContainer tickerPad = new();
        tickerPad.AddThemeConstantOverride("margin_left", 12);
        tickerPad.AddThemeConstantOverride("margin_right", 12);
        tickerPad.AddThemeConstantOverride("margin_top", 8);
        tickerPad.AddThemeConstantOverride("margin_bottom", 8);
        tickerPad.AddChild(tickerRow);
        tickerBar.AddChild(tickerPad);
        Label tickerCaption = WatchChrome.MakeLabel("KOMUNIKATY NA ŻYWO", 11, WatchChrome.Paper, displayFace: true);
        tickerRow.AddChild(tickerCaption);
        tickerMessage = WatchChrome.MakeLabel(string.Empty, 13, WatchChrome.Paper);
        tickerMessage.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tickerRow.AddChild(tickerMessage);
        Ticker = tickerMessage;
        root.AddChild(tickerBar);
    }

    public WatchRaceMapView Map { get; }

    public Button HeaderContinue { get; }

    public Button HeaderExit { get; }

    public VBoxContainer DecisionSlot { get; }

    public Label Ticker { get; }

    public void ShowCourse(RaceWatchCourse? course)
    {
        Map.ShowCourse(course);
        if (course is null)
        {
            profileMeta.Text = string.Empty;
            return;
        }

        WatchRoutePoint[] points = WatchRouteProfile.Build(course);
        (double lengthM, double climbM, double descentM, double maxElevationM) = WatchRouteProfile.Summarize(points);
        profileMeta.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"{lengthM / 1000.0:0.0} km   ↑ {climbM:0} m   ↓ {descentM:0} m");
        _ = maxElevationM;
    }

    public void Bind(InterpolatedWatchView view, int expectedFilmSeconds, bool paused, bool deciding)
    {
        lastView = view;
        lastFilmSeconds = expectedFilmSeconds;
        lastPaused = paused;
        lastDeciding = deciding;
        raceClock.Text = WatchFilmDuration.Clock(view.WatchSecond, expectedFilmSeconds);
        ApplyPlayback(paused, deciding);
        InterpolatedRiderView? leader = Leader(view);
        if (leader is not null)
        {
            leaderName.Text = WatchObservationText.DisplayName(leader.Label);
            leaderRole.Text = "LIDER ETAPU";
            double remaining = Math.Max(0.0, view.RouteLengthM - leader.DistanceM);
            leaderDistance.Text = string.Create(CultureInfo.InvariantCulture, $"{leader.DistanceM / 1000.0:0.00} km");
            leaderToGo.Text = string.Create(CultureInfo.InvariantCulture, $"{remaining / 1000.0:0.00} km");
            leaderSpeed.Text = WatchObservationText.Speed(leader.SpeedMps);
            leaderGradient.Text = string.Create(CultureInfo.InvariantCulture, $"{leader.Gradient * 100:0.0}%");
        }

        PaintRiderRows(view.Riders);
        IReadOnlyList<InterpolatedRiderView> gapSource = showSquadGaps ? view.Riders : TopField(view, 8);
        PaintGapRows(gapSource);
        WatchChrome.ApplyKind(teamTab, WatchChrome.Kind.Segment, selected: showSquadGaps);
        WatchChrome.ApplyKind(fieldTab, WatchChrome.Kind.Segment, selected: !showSquadGaps);
        Map.ShowView(view);
        if (view.Riders.Count > 0)
        {
            InterpolatedRiderView first = view.Riders[0];
            tickerMessage.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"{view.WatchSecond / 60}:{view.WatchSecond % 60:00}  {WatchObservationText.DisplayName(first.Label)}  {WatchObservationText.Speed(first.SpeedMps)}  {WatchObservationText.Radio(first.SpeedMps, first.ShelterMultiplier, first.Gradient, first.GapM)}");
        }
        else
        {
            tickerMessage.Text = string.Empty;
        }
    }

    private void ApplyPlayback(bool paused, bool deciding)
    {
        livePill.Text = paused || deciding ? "PAUZA" : "WYŚCIG TRWA";
        HeaderContinue.Visible = !deciding;
        HeaderContinue.Disabled = deciding;
        HeaderContinue.MouseFilter = deciding ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        HeaderContinue.Text = paused ? "KONTYNUUJ" : "PAUZA";
        WatchChrome.ApplyKind(HeaderContinue, WatchChrome.Kind.Team, selected: false);
        HeaderContinue.CustomMinimumSize = new Vector2(168, 44);
        HeaderExit.CustomMinimumSize = new Vector2(168, 44);
    }

    private void SetGapTab(bool squad)
    {
        showSquadGaps = squad;
        if (lastView is not null)
        {
            Bind(lastView, lastFilmSeconds, lastPaused, lastDeciding);
        }
    }

    private static InterpolatedRiderView? Leader(InterpolatedWatchView view)
    {
        foreach (InterpolatedRiderView rider in view.Field)
        {
            if (rider.Place == 1)
            {
                return rider;
            }
        }

        return view.Field.Count > 0 ? view.Field[0] : null;
    }

    private static List<InterpolatedRiderView> TopField(InterpolatedWatchView view, int take)
    {
        List<InterpolatedRiderView> rows = new();
        foreach (InterpolatedRiderView rider in view.Field)
        {
            rows.Add(rider);
            if (rows.Count == take)
            {
                break;
            }
        }

        return rows;
    }

    private void PaintRiderRows(IReadOnlyList<InterpolatedRiderView> riders)
    {
        EnsureRows(riderRows, riders.Count, includeRadio: true);
        for (int index = 0; index < riderRows.GetChildCount(); index++)
        {
            if (riderRows.GetChild(index) is not HBoxContainer row)
            {
                continue;
            }

            bool header = index == 0;
            InterpolatedRiderView? rider = !header && index - 1 < riders.Count ? riders[index - 1] : null;
            row.Visible = header || rider is not null;
            if (!header && rider is not null)
            {
                SetRiderRow(row, rider);
            }
        }
    }

    private void PaintGapRows(IReadOnlyList<InterpolatedRiderView> riders)
    {
        EnsureRows(gapRows, riders.Count, includeRadio: false);
        for (int index = 0; index < gapRows.GetChildCount(); index++)
        {
            if (gapRows.GetChild(index) is not HBoxContainer row)
            {
                continue;
            }

            bool header = index == 0;
            InterpolatedRiderView? rider = !header && index - 1 < riders.Count ? riders[index - 1] : null;
            row.Visible = header || rider is not null;
            if (!header && rider is not null)
            {
                SetGapRow(row, rider);
            }
        }
    }

    private static void EnsureRows(VBoxContainer host, int riderCount, bool includeRadio)
    {
        if (host.GetChildCount() == 0)
        {
            host.AddChild(includeRadio ? MakeRiderHeader() : MakeGapHeader());
        }

        while (host.GetChildCount() < riderCount + 1)
        {
            host.AddChild(includeRadio ? MakeRiderRow() : MakeGapRow());
        }
    }

    private static HBoxContainer MakeRiderHeader()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(Col("#", 28, WatchChrome.Gray));
        row.AddChild(Fill("ZAWODNIK", WatchChrome.Gray));
        row.AddChild(Col("KM/H", 56, WatchChrome.Gray));
        row.AddChild(Col("STRATA", 56, WatchChrome.Gray));
        row.AddChild(Col("TEREN", 110, WatchChrome.Gray));
        row.AddChild(Fill("RADIO", WatchChrome.Gray));
        return row;
    }

    private static HBoxContainer MakeRiderRow()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(Col(string.Empty, 28, WatchChrome.Black));
        row.AddChild(Fill(string.Empty, WatchChrome.Black));
        row.AddChild(Col(string.Empty, 56, WatchChrome.Black));
        row.AddChild(Col(string.Empty, 56, WatchChrome.Black));
        row.AddChild(Col(string.Empty, 110, WatchChrome.Black));
        row.AddChild(Fill(string.Empty, WatchChrome.Black));
        return row;
    }

    private static HBoxContainer MakeGapHeader()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(Col("POZ.", 36, WatchChrome.Gray));
        row.AddChild(Fill("ZAWODNIK", WatchChrome.Gray));
        row.AddChild(Col("STRATA", 64, WatchChrome.Gray));
        return row;
    }

    private static HBoxContainer MakeGapRow()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(Col(string.Empty, 36, WatchChrome.Black));
        row.AddChild(Fill(string.Empty, WatchChrome.Black));
        row.AddChild(Col(string.Empty, 64, WatchChrome.Black));
        return row;
    }

    private static void SetRiderRow(HBoxContainer row, InterpolatedRiderView rider)
    {
        SetLabel(row, 0, rider.Place.ToString(CultureInfo.InvariantCulture), PlaceColor(rider.Place));
        SetLabel(row, 1, WatchObservationText.DisplayName(rider.Label), WatchChrome.Black);
        SetLabel(
            row,
            2,
            string.Create(CultureInfo.InvariantCulture, $"{WatchObservationText.SpeedKmh(rider.SpeedMps):0.0}"),
            WatchChrome.Black);
        SetLabel(row, 3, WatchObservationText.GapClock(rider.GapM, rider.SpeedMps), WatchChrome.Black);
        SetLabel(row, 4, WatchObservationText.Terrain(rider.Gradient), WatchChrome.Black);
        SetLabel(
            row,
            5,
            WatchObservationText.Radio(rider.SpeedMps, rider.ShelterMultiplier, rider.Gradient, rider.GapM),
            WatchChrome.Black);
    }

    private static void SetGapRow(HBoxContainer row, InterpolatedRiderView rider)
    {
        SetLabel(row, 0, rider.Place.ToString(CultureInfo.InvariantCulture), PlaceColor(rider.Place));
        SetLabel(row, 1, WatchObservationText.DisplayName(rider.Label), WatchChrome.Black);
        SetLabel(row, 2, WatchObservationText.GapClock(rider.GapM, rider.SpeedMps), WatchChrome.Black);
    }

    private static void SetLabel(HBoxContainer row, int index, string text, Color color)
    {
        if (row.GetChild(index) is Label label)
        {
            label.Text = text;
            label.AddThemeColorOverride("font_color", color);
        }
    }

    private static Label Col(string text, float width, Color color)
    {
        Label label = WatchChrome.MakeLabel(text, 12, color);
        label.CustomMinimumSize = new Vector2(width, 0);
        return label;
    }

    private static Label Fill(string text, Color color)
    {
        Label label = WatchChrome.MakeLabel(text, 12, color);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return label;
    }

    private static Label AddStat(VBoxContainer host, string key)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        Label caption = WatchChrome.MakeLabel(key, 10, WatchChrome.Gray);
        caption.CustomMinimumSize = new Vector2(120, 0);
        Label value = WatchChrome.MakeLabel("—", 14, WatchChrome.Black);
        value.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(caption);
        row.AddChild(value);
        host.AddChild(row);
        return value;
    }

    private static Color PlaceColor(int place)
    {
        return place switch
        {
            1 => WatchChrome.Red,
            2 => WatchChrome.Black,
            3 => WatchChrome.Team,
            _ => WatchChrome.Gray,
        };
    }
}
