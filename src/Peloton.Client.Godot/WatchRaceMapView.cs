using System.Collections.Generic;
using Godot;
using Peloton.Simulation.Race;

namespace Peloton.Client.Godot;

public sealed partial class WatchRaceMapView : Control
{
    private static readonly Color Paper = new("f3ede1");
    private static readonly Color Red = new("d11f1f");
    private static readonly Color Black = new("0c0c0d");
    private static readonly Color Hair = new("d9d2c0");
    private static readonly Color[] RiderFills =
    {
        Red,
        Black,
        new("2050c8"),
    };

    private RaceWatchCourse? course;
    private InterpolatedWatchView? view;

    public WatchRaceMapView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void ShowCourse(RaceWatchCourse? nextCourse)
    {
        course = nextCourse;
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

        Vector2[] profile = BuildProfile(area);
        if (profile.Length >= 2)
        {
            var fill = new List<Vector2>(profile)
            {
                new(profile[^1].X, area.End.Y - 8),
                new(profile[0].X, area.End.Y - 8),
            };
            DrawColoredPolygon(fill.ToArray(), Hair);
            DrawPolyline(profile, Black, 3.5f, antialiased: true);
        }

        if (view is null)
        {
            return;
        }

        for (int index = 0; index < view.Riders.Count; index++)
        {
            InterpolatedRiderView rider = view.Riders[index];
            Vector2 point = PointOnRoute(area, rider.Progress);
            float radius = index == 0 ? 11f : 9f;
            Color fill = RiderFills[index % RiderFills.Length];
            DrawCircle(point, radius + 2f, Black);
            DrawCircle(point, radius, fill);
            if (rider.ShelterMultiplier >= 0.99)
            {
                DrawArc(point, radius + 5f, 0, Mathf.Tau, 24, Red, 2f, antialiased: true);
            }

            Font? font = ThemeDB.FallbackFont;
            if (font is not null)
            {
                DrawString(
                    font,
                    point + new Vector2(12, -8),
                    $"{rider.RiderId}",
                    HorizontalAlignment.Left,
                    -1,
                    13,
                    Black);
            }
        }
    }

    private Vector2[] BuildProfile(Rect2 area)
    {
        float left = area.Position.X + 16;
        float right = area.End.X - 16;
        float top = area.Position.Y + 18;
        float bottom = area.End.Y - 28;
        if (course is null || course.TotalLengthM <= 0 || course.Segments.Count == 0)
        {
            return new[] { new Vector2(left, bottom - 20), new Vector2(right, bottom - 20) };
        }

        var elevations = new List<(double Distance, double Elevation)>(course.Segments.Count + 1)
        {
            (0.0, 0.0),
        };
        double distance = 0.0;
        double elevation = 0.0;
        foreach (RaceWatchCourseSegment segment in course.Segments)
        {
            distance += segment.LengthM;
            elevation += segment.LengthM * segment.Gradient;
            elevations.Add((distance, elevation));
        }

        double minElevation = 0.0;
        double maxElevation = 0.0;
        foreach ((double _, double sample) in elevations)
        {
            minElevation = System.Math.Min(minElevation, sample);
            maxElevation = System.Math.Max(maxElevation, sample);
        }

        double span = System.Math.Max(8.0, maxElevation - minElevation);
        var points = new Vector2[elevations.Count];
        for (int index = 0; index < elevations.Count; index++)
        {
            (double sampleDistance, double sampleElevation) = elevations[index];
            float x = left + (float)((sampleDistance / course.TotalLengthM) * (right - left));
            float y = bottom - (float)(((sampleElevation - minElevation) / span) * (bottom - top));
            points[index] = new Vector2(x, y);
        }

        return points;
    }

    private Vector2 PointOnRoute(Rect2 area, double progress)
    {
        Vector2[] profile = BuildProfile(area);
        if (profile.Length == 0)
        {
            return area.GetCenter();
        }

        double clamped = System.Math.Clamp(progress, 0.0, 1.0);
        double cursor = clamped * (profile.Length - 1);
        int from = System.Math.Clamp((int)cursor, 0, profile.Length - 1);
        int to = System.Math.Clamp(from + 1, 0, profile.Length - 1);
        float local = (float)(cursor - from);
        return profile[from].Lerp(profile[to], local);
    }
}
