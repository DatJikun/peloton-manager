using Godot;

namespace Peloton.Client.Godot;

internal sealed partial class LookIcon : Control
{
    private const float Stroke = 2f;

    public string IconKey { get; set; } = string.Empty;

    private Color iconColor = Colors.White;

    public Color IconColor
    {
        get => iconColor;
        set
        {
            iconColor = value;
            QueueRedraw();
        }
    }

    public LookIcon()
    {
        CustomMinimumSize = new Vector2(20, 20);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        Vector2 size = Size;
        if (size.X < 4 || size.Y < 4)
        {
            return;
        }

        float pad = 2f;
        Rect2 box = new(pad, pad, size.X - (pad * 2), size.Y - (pad * 2));
        Color stroke = IconColor;
        switch (IconKey)
        {
            case "home":
                DrawHome(box, stroke);
                break;
            case "person":
                DrawPerson(box, stroke);
                break;
            case "id-card":
                DrawIdCard(box, stroke);
                break;
            case "calendar":
                DrawCalendar(box, stroke);
                break;
            case "tag":
                DrawTag(box, stroke);
                break;
            case "wallet":
                DrawWallet(box, stroke);
                break;
            case "magnifier":
                DrawMagnifier(box, stroke);
                break;
            case "arrows-swap":
                DrawArrowsSwap(box, stroke);
                break;
            case "clock":
                DrawClock(box, stroke);
                break;
            case "question":
                DrawQuestion(box, stroke);
                break;
            case "sliders":
                DrawSliders(box, stroke);
                break;
        }
    }

    private void StrokeLine(Vector2 from, Vector2 to, Color color)
    {
        DrawLine(from, to, color, Stroke, true);
    }

    private void DrawRectOutline(Rect2 rect, Color color)
    {
        DrawRect(rect, color, false, Stroke, true);
    }

    private void DrawHome(Rect2 box, Color color)
    {
        Vector2 roof = new(box.Position.X + (box.Size.X * 0.5f), box.Position.Y + 1);
        StrokeLine(roof, new(box.End.X - 1, box.Position.Y + (box.Size.Y * 0.42f)), color);
        StrokeLine(roof, new(box.Position.X + 1, box.Position.Y + (box.Size.Y * 0.42f)), color);
        Rect2 body = new(box.Position.X + 3, box.Position.Y + (box.Size.Y * 0.4f), box.Size.X - 6, box.Size.Y * 0.55f);
        DrawRectOutline(body, color);
    }

    private void DrawPerson(Rect2 box, Color color)
    {
        Vector2 head = new(box.Position.X + (box.Size.X * 0.5f), box.Position.Y + 4);
        DrawArc(head, 3f, 0, Mathf.Tau, 16, color, Stroke, true);
        Rect2 shoulders = new(box.Position.X + 3, box.Position.Y + 9, box.Size.X - 6, box.Size.Y - 11);
        DrawArc(
            new Vector2(shoulders.Position.X + (shoulders.Size.X * 0.5f), shoulders.Position.Y + 2),
            shoulders.Size.X * 0.5f,
            Mathf.Pi,
            Mathf.Tau,
            16,
            color,
            Stroke,
            true);
    }

    private void DrawIdCard(Rect2 box, Color color)
    {
        Rect2 card = new(box.Position.X + 1, box.Position.Y + 2, box.Size.X - 2, box.Size.Y - 4);
        DrawRectOutline(card, color);
        StrokeLine(new(card.Position.X + 4, card.Position.Y + 5), new(card.End.X - 4, card.Position.Y + 5), color);
        StrokeLine(new(card.Position.X + 4, card.Position.Y + 9), new(card.End.X - 8, card.Position.Y + 9), color);
        StrokeLine(new(card.Position.X + 4, card.Position.Y + 13), new(card.End.X - 10, card.Position.Y + 13), color);
    }

    private void DrawCalendar(Rect2 box, Color color)
    {
        Rect2 cal = new(box.Position.X + 2, box.Position.Y + 3, box.Size.X - 4, box.Size.Y - 5);
        DrawRectOutline(cal, color);
        StrokeLine(new(cal.Position.X, cal.Position.Y + 5), new(cal.End.X, cal.Position.Y + 5), color);
        StrokeLine(new(cal.Position.X + 4, cal.Position.Y + 1), new(cal.Position.X + 4, cal.Position.Y + 5), color);
        StrokeLine(new(cal.End.X - 4, cal.Position.Y + 1), new(cal.End.X - 4, cal.Position.Y + 5), color);
    }

    private void DrawTag(Rect2 box, Color color)
    {
        Vector2 a = new(box.Position.X + 2, box.Position.Y + 4);
        Vector2 b = new(box.End.X - 3, box.Position.Y + 4);
        Vector2 c = new(box.End.X - 1, box.Position.Y + (box.Size.Y * 0.5f));
        Vector2 d = new(box.End.X - 3, box.End.Y - 4);
        Vector2 e = new(box.Position.X + 2, box.End.Y - 4);
        StrokeLine(a, b, color);
        StrokeLine(b, c, color);
        StrokeLine(c, d, color);
        StrokeLine(d, e, color);
        StrokeLine(e, a, color);
        DrawCircle(new(box.Position.X + 5, box.Position.Y + (box.Size.Y * 0.5f)), 1.2f, color);
    }

    private void DrawWallet(Rect2 box, Color color)
    {
        Rect2 wallet = new(box.Position.X + 2, box.Position.Y + 5, box.Size.X - 4, box.Size.Y - 8);
        DrawRectOutline(wallet, color);
        StrokeLine(new(wallet.End.X - 6, wallet.Position.Y + 2), new(wallet.End.X - 6, wallet.End.Y - 2), color);
        DrawCircle(new(wallet.End.X - 3, wallet.Position.Y + (wallet.Size.Y * 0.5f)), 1.2f, color);
    }

    private void DrawMagnifier(Rect2 box, Color color)
    {
        Vector2 center = new(box.Position.X + 8, box.Position.Y + 8);
        DrawArc(center, 5f, 0, Mathf.Tau, 24, color, Stroke, true);
        StrokeLine(center + new Vector2(3.5f, 3.5f), new(box.End.X - 2, box.End.Y - 2), color);
    }

    private void DrawArrowsSwap(Rect2 box, Color color)
    {
        float midY = box.Position.Y + (box.Size.Y * 0.5f);
        StrokeLine(new(box.Position.X + 2, midY - 3), new(box.End.X - 2, midY - 3), color);
        StrokeLine(new(box.End.X - 5, midY - 6), new(box.End.X - 2, midY - 3), color);
        StrokeLine(new(box.End.X - 5, midY), new(box.End.X - 2, midY - 3), color);
        StrokeLine(new(box.End.X - 2, midY + 3), new(box.Position.X + 2, midY + 3), color);
        StrokeLine(new(box.Position.X + 5, midY), new(box.Position.X + 2, midY + 3), color);
        StrokeLine(new(box.Position.X + 5, midY + 6), new(box.Position.X + 2, midY + 3), color);
    }

    private void DrawClock(Rect2 box, Color color)
    {
        Vector2 center = new(box.Position.X + (box.Size.X * 0.5f), box.Position.Y + (box.Size.Y * 0.5f));
        DrawArc(center, 7f, 0, Mathf.Tau, 32, color, Stroke, true);
        StrokeLine(center, center + new Vector2(0, -4), color);
        StrokeLine(center, center + new Vector2(3, 1), color);
    }

    private void DrawQuestion(Rect2 box, Color color)
    {
        Vector2 center = new(box.Position.X + (box.Size.X * 0.5f), box.Position.Y + (box.Size.Y * 0.5f));
        DrawArc(center + new Vector2(0, -1), 5f, Mathf.Pi * 1.1f, Mathf.Pi * 1.95f, 20, color, Stroke, true);
        StrokeLine(center + new Vector2(0, 3), center + new Vector2(0, 5), color);
        DrawCircle(center + new Vector2(0, 7), 1.2f, color);
    }

    private void DrawSliders(Rect2 box, Color color)
    {
        float x1 = box.Position.X + 4;
        float x2 = box.End.X - 4;
        float y1 = box.Position.Y + 5;
        float y2 = box.Position.Y + (box.Size.Y * 0.5f);
        float y3 = box.End.Y - 5;
        StrokeLine(new(x1, y1), new(x2, y1), color);
        StrokeLine(new(x1, y2), new(x2, y2), color);
        StrokeLine(new(x1, y3), new(x2, y3), color);
        DrawCircle(new(x1 + 6, y1), 2f, color);
        DrawCircle(new(x2 - 6, y2), 2f, color);
        DrawCircle(new(x1 + 9, y3), 2f, color);
    }
}

internal sealed partial class LookCrest : Control
{
    public LookCrest()
    {
        CustomMinimumSize = new Vector2(34, 34);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        Rect2 rect = new(Vector2.Zero, Size);
        Vector2[] topLeft =
        [
            rect.Position,
            new(rect.End.X, rect.Position.Y),
            rect.End,
        ];
        Vector2[] bottomRight =
        [
            rect.Position,
            rect.End,
            new(rect.Position.X, rect.End.Y),
        ];
        DrawColoredPolygon(topLeft, LookChrome.Team);
        DrawColoredPolygon(bottomRight, LookChrome.Black);
        DrawRect(rect, LookChrome.Black, false, 2f, true);
    }
}
