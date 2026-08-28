using System.Collections.Generic;
using Godot;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed partial class WatchRaceMapView : Control
{
    private static readonly Color Paper = new("f3ede1");
    private static readonly Color Red = new("d11f1f");
    private static readonly Color Black = new("0c0c0d");
    private static readonly Color Gray = new("6f6f72");
    private static readonly Color HairFill = new("d9d2c0", 0.55f);
    private static readonly Color ClimbFill = new("d11f1f", 0.18f);
    private static readonly Color ClimbStroke = new("d11f1f");
    private static readonly Color FlatStroke = new("0c0c0d");
    private static readonly Color DescentStroke = new("2050c8");
    private static readonly Color[] RiderFills =
    {
        Red,
        Black,
        new("2050c8"),
    };

    public bool DrawOuterFrame { get; set; } = true;

    private RaceWatchCourse? course;
    private InterpolatedWatchView? view;
    private WatchRoutePoint[] profile = System.Array.Empty<WatchRoutePoint>();

    public WatchRaceMapView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void ShowCourse(RaceWatchCourse? nextCourse)
    {
        course = nextCourse;
        profile = nextCourse is null ? System.Array.Empty<WatchRoutePoint>() : WatchRouteProfile.Build(nextCourse);
        QueueRedraw();
    }

    public void ShowView(InterpolatedWatchView? nextView)
    {
        view = nextView;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 area = new(Vector2.Zero, Size);
        DrawRect(area, Paper);
        if (DrawOuterFrame)
        {
            DrawRect(area, Black, filled: false, width: 3);
        }

        float left = area.Position.X + 44;
        float right = area.End.X - 16;
        float top = area.Position.Y + 22;
        float bottom = area.End.Y - 48;
        Vector2[] screen = ToScreen(left, right, top, bottom);
        if (screen.Length >= 2 && profile.Length >= 2)
        {
            var fill = new List<Vector2>(screen)
            {
                new(screen[^1].X, bottom + 12),
                new(screen[0].X, bottom + 12),
            };
            DrawColoredPolygon(fill.ToArray(), HairFill);
            DrawClimbFills(screen, bottom);
            DrawTerrainStroke(screen);
        }

        DrawAnnotations(left, right, top, bottom);
        if (view is null || course is null)
        {
            return;
        }

        double totalLengthM = course.TotalLengthM;
        HashSet<long> drawn = new();
        void DrawRider(InterpolatedRiderView rider, int colorIndex, bool labelLeader)
        {
            if (!drawn.Add(rider.RiderId))
            {
                return;
            }

            (double x, double y) = WatchRouteProfile.PointOnPolyline(
                profile,
                rider.Progress * totalLengthM,
                left,
                right,
                top,
                bottom);
            Vector2 point = new((float)x, (float)y);
            float radius = rider.Place == 1 ? 11f : 9f;
            Color fill = rider.Place == 1 ? new Color("2050c8") : RiderFills[colorIndex % RiderFills.Length];
            DrawCircle(point, radius + 2f, Black);
            DrawCircle(point, radius, fill);
            string caption = rider.Place == 1 && labelLeader
                ? "LIDER WYŚCIGU"
                : WatchObservationText.DisplayName(rider.Label);
            DrawCaption(point + new Vector2(12, -10), caption, rider.Place == 1 ? new Color("2050c8") : Black, 11);
        }

        foreach (InterpolatedRiderView rider in view.Field)
        {
            if (rider.Place == 1)
            {
                DrawRider(rider, 0, labelLeader: true);
                break;
            }
        }

        for (int index = 0; index < view.Riders.Count; index++)
        {
            DrawRider(view.Riders[index], index, labelLeader: false);
        }
    }

    private Vector2[] ToScreen(float left, float right, float top, float bottom)
    {
        if (profile.Length == 0)
        {
            return new[] { new Vector2(left, bottom - 20), new Vector2(right, bottom - 20) };
        }

        var screen = new Vector2[profile.Length];
        for (int index = 0; index < profile.Length; index++)
        {
            (double x, double y) = WatchRouteProfile.PointOnPolyline(
                profile,
                profile[index].DistanceM,
                left,
                right,
                top,
                bottom);
            screen[index] = new Vector2((float)x, (float)y);
        }

        return screen;
    }

    private void DrawTerrainStroke(Vector2[] screen)
    {
        int last = System.Math.Min(screen.Length, profile.Length) - 1;
        for (int index = 0; index < last; index++)
        {
            double gradient = profile[index].Gradient;
            Color color = gradient >= 0.03 ? ClimbStroke : gradient <= -0.03 ? DescentStroke : FlatStroke;
            float width = gradient >= 0.03 ? 6.5f : 3.5f;
            DrawLine(screen[index], screen[index + 1], color, width, antialiased: true);
        }
    }

    private void DrawClimbFills(Vector2[] screen, float bottom)
    {
        int index = 0;
        while (index < profile.Length)
        {
            if (profile[index].Gradient < 0.03)
            {
                index++;
                continue;
            }

            int end = index;
            while (end + 1 < profile.Length && profile[end + 1].Gradient >= 0.03)
            {
                end++;
            }

            if (end > index)
            {
                var fill = new List<Vector2>();
                for (int point = index; point <= end && point < screen.Length; point++)
                {
                    fill.Add(screen[point]);
                }

                fill.Add(new Vector2(screen[System.Math.Min(end, screen.Length - 1)].X, bottom + 12));
                fill.Add(new Vector2(screen[index].X, bottom + 12));
                if (fill.Count >= 3)
                {
                    DrawColoredPolygon(fill.ToArray(), ClimbFill);
                }
            }

            index = end + 1;
        }
    }

    private void DrawAnnotations(float left, float right, float top, float bottom)
    {
        if (course is null || profile.Length == 0)
        {
            return;
        }

        for (double km = 0.0; km <= course.TotalLengthM + 0.5; km += 1000.0)
        {
            double mark = System.Math.Min(km, course.TotalLengthM);
            (double x, double _) = WatchRouteProfile.PointOnPolyline(profile, mark, left, right, top, bottom);
            DrawLine(new Vector2((float)x, bottom + 2), new Vector2((float)x, bottom + 10), Gray, 1.5f);
            string label = mark >= course.TotalLengthM
                ? $"{course.TotalLengthM / 1000.0:0.0}"
                : $"{mark / 1000.0:0}";
            DrawCaption(new Vector2((float)x - 8, bottom + 12), label, Gray, 11);
        }

        DrawCaption(new Vector2(left, bottom + 12), "KM", Gray, 11);
        DrawLegend(left, right, bottom);
        LabelTerrainRuns(left, right, top, bottom);
        DrawElevationAxis(left, top, bottom);
    }

    private void DrawElevationAxis(float left, float top, float bottom)
    {
        if (profile.Length == 0)
        {
            return;
        }

        double min = profile[0].ElevationM;
        double max = profile[0].ElevationM;
        for (int index = 1; index < profile.Length; index++)
        {
            min = System.Math.Min(min, profile[index].ElevationM);
            max = System.Math.Max(max, profile[index].ElevationM);
        }

        DrawCaption(new Vector2(4, top), $"{max:0} m", Gray, 10);
        DrawCaption(new Vector2(4, bottom - 12), $"{min:0} m", Gray, 10);
    }

    private void DrawLegend(float left, float right, float bottom)
    {
        float y = bottom + 26;
        DrawCaption(new Vector2(left, y), "NACHYLENIE", Gray, 9);
        float x = left + 90;
        DrawRect(new Rect2(x, y + 2, 18, 8), new Color("e8d9b8"));
        DrawCaption(new Vector2(x + 22, y), "0–3%", Gray, 9);
        x += 68;
        DrawRect(new Rect2(x, y + 2, 18, 8), new Color("d11f1f", 0.35f));
        DrawCaption(new Vector2(x + 22, y), "3–6%", Gray, 9);
        x += 68;
        DrawRect(new Rect2(x, y + 2, 18, 8), new Color("d11f1f", 0.7f));
        DrawCaption(new Vector2(x + 22, y), "6–9%", Gray, 9);
        x += 68;
        DrawRect(new Rect2(x, y + 2, 18, 8), ClimbStroke);
        DrawCaption(new Vector2(x + 22, y), ">9%", Gray, 9);
        _ = right;
    }

    private void LabelTerrainRuns(float left, float right, float top, float bottom)
    {
        int index = 0;
        while (index < profile.Length)
        {
            RouteTerrainKind kind = profile[index].Kind;
            int end = index;
            double maxGradient = profile[index].Gradient;
            int steepest = index;
            double minWidth = profile[index].RoadWidthM;
            while (end + 1 < profile.Length && profile[end + 1].Kind == kind)
            {
                end++;
                if (profile[end].Gradient > maxGradient)
                {
                    maxGradient = profile[end].Gradient;
                    steepest = end;
                }

                minWidth = System.Math.Min(minWidth, profile[end].RoadWidthM);
            }

            double lengthM = profile[end].DistanceM - profile[index].DistanceM;
            if (kind == RouteTerrainKind.Climb && lengthM >= 250.0)
            {
                WatchRoutePoint mark = profile[steepest];
                (double x, double y) = WatchRouteProfile.PointOnPolyline(
                    profile,
                    mark.DistanceM,
                    left,
                    right,
                    top,
                    bottom);
                string climb = $"PODJAZD {lengthM / 1000.0:0.0} KM · max {maxGradient * 100:0}%";
                DrawCaption(new Vector2((float)x - 48, (float)y - 18), climb, Red, 11);
            }
            else if (kind == RouteTerrainKind.Crosswind && lengthM >= 250.0)
            {
                double mid = (profile[index].DistanceM + profile[end].DistanceM) / 2.0;
                (double x, double y) = WatchRouteProfile.PointOnPolyline(profile, mid, left, right, top, bottom);
                DrawCaption(
                    new Vector2((float)x - 58, (float)y - 18),
                    minWidth < 4.0 ? "WĘŻSZA JEZDNIA · WIATR" : "WIATR BOCZNY",
                    Black,
                    11);
            }

            index = end + 1;
        }
    }

    private void DrawCaption(Vector2 position, string text, Color color, int size)
    {
        Font font = WatchChrome.BodyBoldFont();
        DrawString(font, position, text, HorizontalAlignment.Left, -1, size, color);
    }
}
