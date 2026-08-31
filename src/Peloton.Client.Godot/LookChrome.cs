using System;
using Godot;

namespace Peloton.Client.Godot;

internal static class LookChrome
{
    public static readonly Color Paper = new("f3ede1");
    public static readonly Color Red = new("d11f1f");
    public static readonly Color Black = new("0c0c0d");
    public static readonly Color Gray = new("6f6f72");
    public static readonly Color White = new("fffdf7");
    public static readonly Color Hair = new("d9d2c0");
    public static readonly Color Team = new("2050c8");
    public static readonly Color TeamOn = new("f3ede1");

    private static FontFile? display;
    private static FontFile? body;
    private static FontFile? bodyBold;
    private static bool fontsResolved;

    public static void EnsureFonts()
    {
        if (fontsResolved)
        {
            return;
        }

        fontsResolved = true;
        display = LoadFont("res://fonts/Anton-Regular.ttf");
        body = LoadFont("res://fonts/PTSans-Regular.ttf");
        bodyBold = LoadFont("res://fonts/PTSans-Bold.ttf");
    }

    private static FontFile? LoadFont(string path)
    {
        FontFile font = new();
        return font.LoadDynamicFont(path) == Error.Ok ? font : null;
    }

    public static Label Display(string text, int size, Color color)
    {
        EnsureFonts();
        Label label = BaseLabel(text, size, color);
        if (display is not null)
        {
            label.AddThemeFontOverride("font", display);
        }

        return label;
    }

    public static Label Body(string text, int size, Color color, bool bold = false)
    {
        EnsureFonts();
        Label label = BaseLabel(text, size, color);
        FontFile? font = bold ? bodyBold : body;
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        return label;
    }

    public static Button Solid(string text, Action onPressed, Color background, Color foreground, bool compact = false)
    {
        EnsureFonts();
        Button button = new()
        {
            Text = text,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        button.CustomMinimumSize = new Vector2(compact ? 108 : 188, compact ? 36 : 48);
        ApplyFont(button, compact ? bodyBold : display, compact ? 13 : 15);
        button.AddThemeColorOverride("font_color", foreground);
        button.AddThemeColorOverride("font_hover_color", Paper);
        button.AddThemeColorOverride("font_pressed_color", Paper);
        button.AddThemeColorOverride("font_focus_color", foreground);
        button.AddThemeColorOverride("font_disabled_color", Gray);
        StyleBoxFlat normal = Fill(background, compact ? 10 : 16, compact ? 8 : 10);
        StyleBoxFlat hover = Fill(Black, compact ? 10 : 16, compact ? 8 : 10);
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", Outline(Team, background, compact ? 10 : 16, compact ? 8 : 10));
        button.AddThemeStyleboxOverride("disabled", Fill(Hair, compact ? 10 : 16, compact ? 8 : 10));
        button.Pressed += onPressed;
        return button;
    }

    public static Button Primary(string text, Action onPressed)
    {
        Button button = Solid(text, onPressed, Team, TeamOn);
        button.CustomMinimumSize = new Vector2(220, 54);
        ApplyFont(button, display, 15);
        return button;
    }

    public static Button Ghost(string text, Action onPressed)
    {
        EnsureFonts();
        Button button = new()
        {
            Text = text,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        ApplyFont(button, bodyBold, 15);
        button.AddThemeColorOverride("font_color", TeamOn);
        button.AddThemeColorOverride("font_hover_color", White);
        button.AddThemeColorOverride("font_pressed_color", White);
        button.AddThemeColorOverride("font_focus_color", White);
        StyleBoxFlat idle = new()
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderColor = new Color(0, 0, 0, 0),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
        };
        StyleBoxFlat hover = Fill(new Color(0, 0, 0, 0.22f), 12, 12);
        hover.BorderColor = new Color(0, 0, 0, 0.35f);
        hover.BorderWidthLeft = 2;
        hover.BorderWidthTop = 2;
        hover.BorderWidthRight = 2;
        hover.BorderWidthBottom = 2;
        StyleBoxFlat active = Fill(Black, 12, 12);
        button.AddThemeStyleboxOverride("normal", idle);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", active);
        button.AddThemeStyleboxOverride("focus", Outline(Black, new Color(0, 0, 0, 0), 12, 12));
        button.Pressed += onPressed;
        return button;
    }

    public static PanelContainer Card()
    {
        PanelContainer panel = new();
        panel.AddThemeStyleboxOverride("panel", Frame(White));
        return panel;
    }

    public static Label Chip(string text, string kind = "")
    {
        Color background = kind switch
        {
            "red" => Red,
            "inv" => Black,
            "ok" => White,
            "warn" => Paper,
            _ => White,
        };
        Color foreground = kind is "red" or "inv" ? Paper : Black;
        Label label = Body(text, 11, foreground, bold: true);
        label.AddThemeStyleboxOverride("normal", ChipBox(background));
        return label;
    }

    public static PanelContainer Avatar(string name, bool mini = false)
    {
        int size = mini ? 36 : 52;
        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(size, size);
        panel.AddThemeStyleboxOverride("panel", ChipBox(White));
        Label initials = Display(CareerLookCatalog.Initials(name), mini ? 12 : 18, Black);
        initials.HorizontalAlignment = HorizontalAlignment.Center;
        initials.VerticalAlignment = VerticalAlignment.Center;
        initials.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(initials);
        return panel;
    }

    public static HBoxContainer Stat(string label, int value)
    {
        value = Math.Clamp(value, 0, 100);
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        Label name = Body(label, 12, Gray, bold: true);
        name.CustomMinimumSize = new Vector2(110, 0);
        row.AddChild(name);

        ColorRect track = Block(Hair);
        track.CustomMinimumSize = new Vector2(80, 10);
        track.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        track.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        ColorRect fill = Block(Team);
        fill.SetAnchorsPreset(Control.LayoutPreset.LeftWide);
        fill.AnchorRight = value / 100f;
        fill.OffsetRight = 0;
        track.AddChild(fill);
        row.AddChild(track);

        Label number = Body(value.ToString(System.Globalization.CultureInfo.InvariantCulture), 12, Black, bold: true);
        number.CustomMinimumSize = new Vector2(28, 0);
        number.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(number);
        return row;
    }

    public static HBoxContainer Kv(string label, string value)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        Label left = Body(label, 12, Gray, bold: true);
        left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        Label right = Body(value, 13, Black, bold: true);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(left);
        row.AddChild(right);
        return row;
    }

    public static ColorRect Hairline()
    {
        ColorRect line = Block(Hair);
        line.CustomMinimumSize = new Vector2(0, 2);
        return line;
    }

    public static PanelContainer ClickRow(bool active, Action onPressed)
    {
        PanelContainer panel = new();
        panel.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
        panel.AddThemeStyleboxOverride("panel", active ? Fill(Black, 10, 8) : Fill(Paper, 10, 8));
        panel.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                onPressed();
                panel.AcceptEvent();
            }
        };
        return panel;
    }

    public static StyleBoxFlat ChipBox(Color background)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = Black,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
    }

    public static StyleBoxFlat Frame(Color background)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = Black,
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
            ShadowColor = Black,
            ShadowSize = 1,
            ShadowOffset = new Vector2(6, 6),
        };
    }

    public static StyleBoxFlat Fill(Color background, int horizontal, int vertical)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = Black,
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            ContentMarginLeft = horizontal,
            ContentMarginRight = horizontal,
            ContentMarginTop = vertical,
            ContentMarginBottom = vertical,
        };
    }

    public static ColorRect Block(Color color)
    {
        return new ColorRect { Color = color };
    }

    private static Label BaseLabel(string text, int size, Color color)
    {
        Label label = new()
        {
            Text = text,
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static void ApplyFont(Button button, FontFile? font, int size)
    {
        if (font is not null)
        {
            button.AddThemeFontOverride("font", font);
        }

        button.AddThemeFontSizeOverride("font_size", size);
    }

    private static StyleBoxFlat Outline(Color border, Color background, int horizontal, int vertical)
    {
        StyleBoxFlat box = Fill(background, horizontal, vertical);
        box.BorderColor = border;
        return box;
    }
}
