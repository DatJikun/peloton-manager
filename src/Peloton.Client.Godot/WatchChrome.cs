using System;
using Godot;

namespace Peloton.Client.Godot;

internal static class WatchChrome
{
    public static readonly Color Paper = new("f3ede1");
    public static readonly Color Red = new("d11f1f");
    public static readonly Color Black = new("0c0c0d");
    public static readonly Color White = new("fffdf7");

    public enum Kind
    {
        Primary,
        Secondary,
        Segment,
    }

    private static FontFile? display;
    private static FontFile? body;
    private static FontFile? bodyBold;

    public static Font DisplayFont() => display ??= LoadFont("res://fonts/Anton-Regular.ttf");

    public static Font BodyFont() => body ??= LoadFont("res://fonts/PTSans-Regular.ttf");

    public static Font BodyBoldFont() => bodyBold ??= LoadFont("res://fonts/PTSans-Bold.ttf");

    public static Label MakeLabel(string text, int size, Color color, bool displayFace = false)
    {
        Label label = new()
        {
            Text = text,
        };
        label.AddThemeFontOverride("font", displayFace ? DisplayFont() : BodyBoldFont());
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    public static Button MakeButton(string text, Action onPressed, Kind kind)
    {
        Button button = new()
        {
            Text = text.ToUpperInvariant(),
            Flat = false,
            FocusMode = Control.FocusModeEnum.None,
        };
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        bool compact = kind == Kind.Segment;
        button.CustomMinimumSize = new Vector2(compact ? 88 : 200, compact ? 40 : 52);
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        ApplyKind(button, kind, selected: false);
        button.Pressed += onPressed;
        return button;
    }

    public static void ApplyKind(Button button, Kind kind, bool selected)
    {
        Color fill = kind switch
        {
            Kind.Primary => selected ? Black : Red,
            Kind.Secondary => selected ? Black : Paper,
            _ => selected ? Black : White,
        };
        Color hoverFill = kind == Kind.Primary ? Black : Black;
        Color ink = fill == Paper || fill == White ? Black : Paper;
        Color hoverInk = kind == Kind.Primary ? Red : Paper;
        bool shadow = kind != Kind.Segment;
        int border = kind == Kind.Segment ? 3 : 3;
        int padX = kind == Kind.Segment ? 12 : 22;
        int padY = kind == Kind.Segment ? 8 : 12;
        int fontSize = kind == Kind.Segment ? 13 : kind == Kind.Primary ? 20 : 15;

        button.AddThemeFontOverride("font", BodyBoldFont());
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", ink);
        button.AddThemeColorOverride("font_hover_color", hoverInk);
        button.AddThemeColorOverride("font_pressed_color", hoverInk);
        button.AddThemeColorOverride("font_focus_color", ink);
        button.AddThemeStyleboxOverride("normal", Box(fill, border, padX, padY, shadow));
        button.AddThemeStyleboxOverride("hover", Box(hoverFill, border, padX, padY, shadow));
        button.AddThemeStyleboxOverride("pressed", Box(hoverFill, border, padX, padY, shadow: false));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("disabled", Box(fill, border, padX, padY, shadow: false));
    }

    private static StyleBoxFlat Box(Color fill, int border, int padX, int padY, bool shadow)
    {
        StyleBoxFlat box = new()
        {
            BgColor = fill,
            BorderColor = Black,
            BorderWidthLeft = border,
            BorderWidthTop = border,
            BorderWidthRight = border,
            BorderWidthBottom = border,
            ShadowColor = Black,
            ShadowSize = shadow ? 6 : 0,
            ShadowOffset = shadow ? new Vector2(6, 6) : Vector2.Zero,
            AntiAliasing = false,
            ContentMarginLeft = padX,
            ContentMarginRight = padX,
            ContentMarginTop = padY,
            ContentMarginBottom = padY,
        };
        return box;
    }

    private static FontFile LoadFont(string resPath)
    {
        FontFile font = new();
        string path = ProjectSettings.GlobalizePath(resPath);
        Error loaded = font.LoadDynamicFont(path);
        if (loaded != Error.Ok)
        {
            throw new InvalidOperationException($"Missing font {resPath} ({loaded}).");
        }

        return font;
    }
}
