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
    private static readonly Color[] RiderFills =
    {
        Red,
        Black,
        new("2050c8"),
    };

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
        DrawRect(area, Black, filled: false, width: 3);
        float left = area.Position.X + 16;
        float right = area.End.X - 16;
        float top = area.Position.Y + 28;
        float bottom = area.End.Y - 28;
        Vector2[] screen = ToScreen(left, right, top, bottom);
        if (screen.Length >= 2)
        {
            var fill = new List<Vector2>(screen)
            {
                new(screen[^1].X, bottom + 12),
                new(screen[0].X, bottom + 12),
            };
            DrawColoredPolygon(fill.ToArray(), HairFill);
            DrawPolyline(screen, Black, 3.5f, antialiased: true);
        }

        DrawAnnotations(left, right, top, bottom);
        if (view is null || course is null)
        {
            return;
        }

        for (int index = 0; index < view.Riders.Count; index++)
        {
            InterpolatedRiderView rider = view.Riders[index];
            (double x, double y) = WatchRouteProfile.PointOnPolyline(
                profile,
                rider.Progress * course.TotalLengthM,
                left,
                right,
                top,
                bottom);
            Vector2 point = new((float)x, (float)y);
            float radius = index == 0 ? 11f : 9f;
            Color fill = RiderFills[index % RiderFills.Length];
            DrawCircle(point, radius + 2f, Black);
            DrawCircle(point, radius, fill);
            if (rider.ShelterMultiplier >= 0.99)
            {
                DrawArc(point, radius + 5f, 0, Mathf.Tau, 24, Red, 2f, antialiased: true);
            }

            DrawCaption(point + new Vector2(12, -8), string.IsNullOrWhiteSpace(rider.Name) ? $"{rider.RiderId}" : rider.Name, Black, 13);
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
        LabelTerrainRuns(left, right, top, bottom);
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
        Font? font = ThemeDB.FallbackFont;
        if (font is null)
        {
            return;
        }

        DrawString(font, position, text, HorizontalAlignment.Left, -1, size, color);
    }
}
