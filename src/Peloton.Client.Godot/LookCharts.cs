using System;
using Godot;

namespace Peloton.Client.Godot;

internal sealed partial class LookSparkline : Control
{
    private int[] heights = [];

    public LookSparkline()
    {
        CustomMinimumSize = new Vector2(64, 20);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetHeights(int[] points)
    {
        heights = points;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (heights.Length < 2)
        {
            return;
        }

        Vector2[] pts = new Vector2[heights.Length];
        float w = Math.Max(Size.X, 64f);
        float h = Math.Max(Size.Y, 20f);
        for (int i = 0; i < heights.Length; i++)
        {
            float x = i * (w / (heights.Length - 1));
            float y = h - (heights[i] * 0.9f);
            pts[i] = new Vector2(x, y);
        }

        DrawPolyline(pts, LookChrome.Black, 1.6f, true);
    }
}

internal sealed partial class LookRouteProfile : Control
{
    private int[] heights = [];
    private float keyX;
    private string keyLabel = string.Empty;
    private string dist = string.Empty;

    public LookRouteProfile()
    {
        CustomMinimumSize = new Vector2(0, 96);
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
    }

    public void SetRace(LookUpcomingRace race)
    {
        heights = race.Heights;
        keyX = race.KeyX;
        keyLabel = race.KeyLabel;
        dist = race.DistanceKm + " KM";
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (heights.Length < 2)
        {
            return;
        }

        float w = Math.Max(Size.X, 8f);
        float h = Math.Max(Size.Y, 76f);
        float left = 8f;
        float right = w - 8f;
        float top = 12f;
        float baseY = h - 18f;
        Vector2[] line = new Vector2[heights.Length];
        for (int i = 0; i < heights.Length; i++)
        {
            float x = left + (i * ((right - left) / (heights.Length - 1)));
            float y = baseY - (heights[i] * 1.9f);
            line[i] = new Vector2(x, y);
        }

        for (int i = 0; i < line.Length - 1; i++)
        {
            Vector2[] quad =
            [
                line[i],
                line[i + 1],
                new Vector2(line[i + 1].X, baseY),
                new Vector2(line[i].X, baseY),
            ];
            DrawColoredPolygon(quad, new Color(LookChrome.Hair, 0.7f));
        }

        DrawPolyline(line, LookChrome.Black, 3f, true);
        float keyPx = left + (keyX * (right - left));
        DrawDashedLine(new Vector2(keyPx, top), new Vector2(keyPx, baseY + 4), LookChrome.Team, 1.5f, 4);
        Font font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(keyPx - 40, baseY + 14), keyLabel, HorizontalAlignment.Center, 80, 11, LookChrome.Team);
        DrawString(font, new Vector2(left, baseY + 14), "0 KM", HorizontalAlignment.Left, -1, 11, LookChrome.Gray);
        DrawString(font, new Vector2(right - 70, baseY + 14), dist, HorizontalAlignment.Right, 70, 11, LookChrome.Gray);
    }
}

internal sealed partial class LookRaceMap : Control
{
    private LookPoint[] map = [];
    private string climb = string.Empty;
    private int dist;

    public LookRaceMap()
    {
        CustomMinimumSize = new Vector2(0, 110);
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
    }

    public void SetRace(LookCalendarRace race)
    {
        map = race.Map;
        climb = race.Climb.ToUpperInvariant();
        dist = race.DistanceKm;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (map.Length < 2)
        {
            return;
        }

        float sx = Math.Max(Size.X, 8f) / 290f;
        float sy = Math.Max(Size.Y, 8f) / 100f;
        Vector2[] pts = new Vector2[map.Length];
        for (int i = 0; i < map.Length; i++)
        {
            pts[i] = new Vector2(map[i].X * sx, map[i].Y * sy);
        }

        DrawLine(new Vector2(10 * sx, 84 * sy), new Vector2(280 * sx, 84 * sy), LookChrome.Hair, 2);
        DrawPolyline(pts, LookChrome.Black, 5f, true);
        DrawCircle(pts[0], 6, LookChrome.Team);
        DrawRect(new Rect2(pts[^1] - new Vector2(5, 5), new Vector2(10, 10)), LookChrome.Red);
        Font font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(10 * sx, 96 * sy), "START", HorizontalAlignment.Left, -1, 11, LookChrome.Gray);
        DrawString(
            font,
            new Vector2(200 * sx, 96 * sy),
            "META · " + dist + " KM",
            HorizontalAlignment.Right,
            90,
            11,
            LookChrome.Gray);
        DrawString(font, new Vector2(200 * sx, 16 * sy), climb, HorizontalAlignment.Left, -1, 11, LookChrome.Team);
    }
}
