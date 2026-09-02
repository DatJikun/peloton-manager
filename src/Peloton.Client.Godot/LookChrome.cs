using System;
using System.Collections.Generic;
using Godot;

namespace Peloton.Client.Godot;

internal enum TableAlign
{
    Left,
    Center,
    Right,
}

internal sealed record TableColumn(
    string Title,
    string SortKey,
    TableAlign Align = TableAlign.Left,
    bool Key = false,
    float MinWidth = 0);

internal sealed record TableCell(string Text, string? Micro = null, string? ChipText = null, string? ChipKind = null);

internal sealed record TableRow(IReadOnlyList<TableCell> Cells);

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
    public static readonly Color SelectedMicro = new("aaa69a");

    private const float MetaSpacingEm = 0.12f;

    private static FontFile? display;
    private static FontFile? body;
    private static FontFile? bodyBold;
    private static FontVariation? metaFont;
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
        if (bodyBold is not null)
        {
            metaFont = new FontVariation
            {
                BaseFont = bodyBold,
                SpacingGlyph = 1,
                SpacingSpace = 1,
            };
        }
    }

    private static FontFile? LoadFont(string path)
    {
        FontFile? imported = ResourceLoader.Load<FontFile>(path);
        if (imported is not null)
        {
            ConfigureFont(imported);
            return imported;
        }

        FontFile font = new();
        if (font.LoadDynamicFont(path) != Error.Ok)
        {
            return null;
        }

        ConfigureFont(font);
        return font;
    }

    private static void ConfigureFont(FontFile font)
    {
        font.Antialiasing = TextServer.FontAntialiasing.Lcd;
        font.Hinting = TextServer.Hinting.Light;
        font.MultichannelSignedDistanceField = true;
        font.MsdfPixelRange = 8;
        font.MsdfSize = 48;
        font.Oversampling = 0.0f;
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

    public static Label Title(string text, int size = 30) => Display(text.ToUpperInvariant(), size, Black);

    public static Label Number(string text, int size = 26) => Display(text, size, Black);

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

    public static Label Meta(string text, int size = 10, Color? color = null)
    {
        EnsureFonts();
        Label label = BaseLabel(text.ToUpperInvariant(), size, color ?? Gray);
        if (metaFont is not null)
        {
            label.AddThemeFontOverride("font", metaFont);
        }
        else if (bodyBold is not null)
        {
            label.AddThemeFontOverride("font", bodyBold);
        }

        return label;
    }

    public static Control Pill(string text, string? accentPart = null)
    {
        PanelContainer pill = new();
        pill.AddThemeStyleboxOverride("panel", PillBox());

        HBoxContainer row = new();
        row.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddThemeConstantOverride("separation", 0);
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 14;
        row.OffsetRight = -14;
        row.OffsetTop = 9;
        row.OffsetBottom = -9;

        if (!string.IsNullOrEmpty(accentPart))
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                Label prefix = Meta(text.Trim(), 13, Black);
                prefix.VerticalAlignment = VerticalAlignment.Center;
                row.AddChild(prefix);
            }

            Label accent = Meta(accentPart, 13, Team);
            accent.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(accent);
        }
        else
        {
            Label whole = Meta(text, 13, Black);
            whole.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(whole);
        }

        pill.AddChild(row);
        return pill;
    }

    public static PanelContainer SectionBar(
        string title,
        string? linkText = null,
        Action? onLink = null,
        Control? rightAccessory = null)
    {
        PanelContainer bar = new();
        bar.AddThemeStyleboxOverride("panel", SectionBarBox());
        bar.CustomMinimumSize = new Vector2(0, 36);

        HBoxContainer row = new();
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 14;
        row.OffsetRight = -14;
        row.AddThemeConstantOverride("separation", 10);
        Label head = Meta(title, 10, TeamOn);
        head.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(head);

        Control? spacer = null;
        if (rightAccessory is not null || !string.IsNullOrEmpty(linkText))
        {
            spacer = new Control();
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(spacer);
        }

        if (rightAccessory is not null)
        {
            row.AddChild(rightAccessory);
        }

        if (!string.IsNullOrEmpty(linkText) && onLink is not null)
        {
            Button link = SectionLink(linkText, onLink);
            row.AddChild(link);
        }

        bar.AddChild(row);
        return bar;
    }

    public static Control Crest(string clubName, string subtitle)
    {
        HBoxContainer crest = new();
        crest.AddThemeConstantOverride("separation", 10);
        crest.AddChild(new LookCrest());

        VBoxContainer text = new();
        text.AddThemeConstantOverride("separation", 2);
        Label name = Display(clubName.ToUpperInvariant(), 13, TeamOn);
        Label sub = Meta(subtitle, 9, TeamOn);
        sub.Modulate = new Color(1, 1, 1, 0.55f);
        text.AddChild(name);
        text.AddChild(sub);
        crest.AddChild(text);
        return crest;
    }

    public static void UpdateCrest(Control crest, string clubName, string subtitle)
    {
        if (crest.GetChildCount() < 2)
        {
            return;
        }

        if (crest.GetChild(1) is VBoxContainer text && text.GetChildCount() >= 2)
        {
            if (text.GetChild(0) is Label name)
            {
                name.Text = clubName.ToUpperInvariant();
            }

            if (text.GetChild(1) is Label sub)
            {
                sub.Text = subtitle.ToUpperInvariant();
            }
        }
    }

    public static void SetNavActive(Button button, bool active)
    {
        SetNavItem(button, string.Empty, string.Empty, 0, active, updateText: false);
    }

    public static void SetNavItem(
        Button button,
        string iconKey,
        string text,
        int badge,
        bool active,
        bool updateText = true)
    {
        button.AddThemeStyleboxOverride("normal", NavBox(active, false));
        button.AddThemeStyleboxOverride("hover", NavBox(active, true));
        button.AddThemeStyleboxOverride("pressed", NavBox(true, false));
        button.AddThemeStyleboxOverride("focus", NavBox(active, false));
        Color fg = active ? Paper : TeamOn;
        button.AddThemeColorOverride("font_color", fg);
        button.AddThemeColorOverride("font_hover_color", Paper);
        button.AddThemeColorOverride("font_pressed_color", Paper);
        button.AddThemeColorOverride("font_focus_color", fg);
        if (button.GetChildCount() == 0 || button.GetChild(0) is not HBoxContainer row)
        {
            return;
        }

        if (row.GetChildCount() == 0)
        {
            return;
        }

        if (row.GetChild(0) is LookIcon icon && !string.IsNullOrEmpty(iconKey))
        {
            icon.IconKey = iconKey;
            icon.IconColor = fg;
        }

        if (updateText && row.GetChildCount() > 1 && row.GetChild(1) is Label label)
        {
            label.Text = text;
            label.AddThemeColorOverride("font_color", fg);
        }

        if (row.GetChildCount() > 2 && row.GetChild(2) is Label badgeLabel)
        {
            badgeLabel.Visible = badge > 0;
            if (badge > 0)
            {
                badgeLabel.Text = badge.ToString(System.Globalization.CultureInfo.InvariantCulture);
                badgeLabel.AddThemeStyleboxOverride("normal", ChipBox(active ? Black : Paper));
                badgeLabel.AddThemeColorOverride("font_color", active ? Paper : Black);
            }
        }
    }

    public static Button NavItem(string iconKey, string text, int badge, bool active, Action onPressed)
    {
        EnsureFonts();
        Button button = new()
        {
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            Alignment = HorizontalAlignment.Left,
        };
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.AddThemeStyleboxOverride("normal", NavBox(active, false));
        button.AddThemeStyleboxOverride("hover", NavBox(active, true));
        button.AddThemeStyleboxOverride("pressed", NavBox(true, false));
        button.AddThemeStyleboxOverride("focus", NavBox(active, false));
        Color fg = active ? Paper : TeamOn;
        button.AddThemeColorOverride("font_color", fg);
        button.AddThemeColorOverride("font_hover_color", Paper);
        button.AddThemeColorOverride("font_pressed_color", Paper);
        button.AddThemeColorOverride("font_focus_color", fg);
        if (bodyBold is not null)
        {
            button.AddThemeFontOverride("font", bodyBold);
        }

        button.AddThemeFontSizeOverride("font_size", 13);
        button.Text = string.Empty;
        button.CustomMinimumSize = new Vector2(0, 38);

        HBoxContainer row = new();
        row.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddThemeConstantOverride("separation", 12);
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 10;
        row.OffsetRight = -10;
        row.OffsetTop = 9;
        row.OffsetBottom = -9;
        LookIcon icon = new() { IconKey = iconKey, IconColor = fg };
        row.AddChild(icon);
        Label label = Body(text, 13, fg, bold: true);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);
        if (badge > 0)
        {
            Label badgeLabel = Meta(badge.ToString(System.Globalization.CultureInfo.InvariantCulture), 10, active ? Paper : Black);
            badgeLabel.AddThemeStyleboxOverride("normal", ChipBox(active ? Black : Paper));
            badgeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            badgeLabel.CustomMinimumSize = new Vector2(20, 18);
            row.AddChild(badgeLabel);
        }

        button.AddChild(row);
        button.Pressed += onPressed;
        return button;
    }

    public static Control NavSection(string text)
    {
        MarginContainer wrap = new();
        wrap.AddThemeConstantOverride("margin_left", 4);
        wrap.AddThemeConstantOverride("margin_top", 16);
        wrap.AddThemeConstantOverride("margin_right", 4);
        wrap.AddThemeConstantOverride("margin_bottom", 6);
        Label label = Meta(text, 9, TeamOn);
        label.Modulate = new Color(1, 1, 1, 0.45f);
        wrap.AddChild(label);
        return wrap;
    }

    public static Button ManagerFoot(string initials, string name, string subtitle, Action onPressed)
    {
        EnsureFonts();
        Button button = new()
        {
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            Alignment = HorizontalAlignment.Left,
        };
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.CustomMinimumSize = new Vector2(0, 54);
        button.Text = string.Empty;
        StyleBoxFlat idle = new()
        {
            BgColor = new Color(0, 0, 0, 0),
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
            BorderWidthTop = 2,
            BorderColor = new Color(0, 0, 0, 0.28f),
        };
        button.AddThemeStyleboxOverride("normal", idle);
        button.AddThemeStyleboxOverride("hover", idle);
        button.AddThemeStyleboxOverride("pressed", idle);
        button.AddThemeStyleboxOverride("focus", idle);

        HBoxContainer row = new();
        row.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddThemeConstantOverride("separation", 10);
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        Label chip = Display(initials, 12, Black);
        chip.CustomMinimumSize = new Vector2(30, 30);
        chip.HorizontalAlignment = HorizontalAlignment.Center;
        chip.VerticalAlignment = VerticalAlignment.Center;
        chip.AddThemeStyleboxOverride("normal", ChipBox(Paper));
        row.AddChild(chip);
        VBoxContainer text = new();
        text.AddThemeConstantOverride("separation", 2);
        text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        text.AddChild(Body(name, 12, TeamOn, bold: true));
        Label role = Body(subtitle, 11, TeamOn);
        role.AddThemeFontSizeOverride("font_size", 11);
        role.Modulate = new Color(1, 1, 1, 0.55f);
        text.AddChild(role);
        row.AddChild(text);
        button.AddChild(row);
        button.Pressed += onPressed;
        return button;
    }

    public static ScrollContainer Table(
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        int selectedIndex,
        string sortKey,
        int sortDir,
        Action<string>? onSort,
        Action<int>? onSelect)
    {
        ScrollContainer scroll = new();
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;

        GridContainer grid = new()
        {
            Columns = columns.Count,
        };
        grid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        grid.AddThemeConstantOverride("h_separation", 0);
        grid.AddThemeConstantOverride("v_separation", 0);

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            TableColumn column = columns[columnIndex];
            bool sorted = string.Equals(column.SortKey, sortKey, StringComparison.Ordinal);
            string arrow = sorted ? (sortDir > 0 ? " ▲" : " ▼") : string.Empty;
            Button head = new()
            {
                Text = string.Empty,
                MouseDefaultCursorShape = onSort is null
                    ? Control.CursorShape.Arrow
                    : Control.CursorShape.PointingHand,
            };
            head.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            if (column.MinWidth > 0)
            {
                head.CustomMinimumSize = new Vector2(column.MinWidth, 34);
            }
            else
            {
                head.CustomMinimumSize = new Vector2(0, 34);
            }

            StyleBoxFlat headStyle = TableHeadBox(sorted);
            head.AddThemeStyleboxOverride("normal", headStyle);
            head.AddThemeStyleboxOverride("hover", headStyle);
            head.AddThemeStyleboxOverride("pressed", headStyle);
            head.AddThemeStyleboxOverride("focus", headStyle);
            HBoxContainer headRow = new();
            headRow.MouseFilter = Control.MouseFilterEnum.Ignore;
            headRow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            headRow.OffsetLeft = 8;
            headRow.OffsetRight = -8;
            Label title = Meta(column.Title + arrow, 10, sorted ? Team : Gray);
            title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            title.HorizontalAlignment = column.Align switch
            {
                TableAlign.Center => HorizontalAlignment.Center,
                TableAlign.Right => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left,
            };
            title.VerticalAlignment = VerticalAlignment.Center;
            headRow.AddChild(title);
            head.AddChild(headRow);
            if (onSort is not null)
            {
                string key = column.SortKey;
                head.Pressed += () => onSort(key);
            }

            grid.AddChild(head);
        }

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            TableRow row = rows[rowIndex];
            bool selected = rowIndex == selectedIndex;
            Color fg = selected ? Paper : Black;
            Color microColor = selected ? SelectedMicro : Gray;
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                TableColumn column = columns[columnIndex];
                TableCell cell = columnIndex < row.Cells.Count ? row.Cells[columnIndex] : new TableCell("—");
                PanelContainer cellPanel = TableCellPanel(selected, rowIndex, onSelect);
                if (column.MinWidth > 0)
                {
                    cellPanel.CustomMinimumSize = new Vector2(column.MinWidth, 34);
                }
                else
                {
                    cellPanel.CustomMinimumSize = new Vector2(0, 34);
                }

                if (!string.IsNullOrEmpty(cell.ChipText))
                {
                    Label chip = Chip(cell.ChipText, cell.ChipKind ?? string.Empty);
                    MarginContainer chipPad = new();
                    chipPad.AddThemeConstantOverride("margin_left", 8);
                    chipPad.AddThemeConstantOverride("margin_top", 6);
                    chipPad.AddChild(chip);
                    cellPanel.AddChild(chipPad);
                    grid.AddChild(cellPanel);
                    continue;
                }

                VBoxContainer stack = new();
                stack.MouseFilter = Control.MouseFilterEnum.Ignore;
                stack.AddThemeConstantOverride("separation", 0);
                stack.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                MarginContainer pad = new();
                pad.AddThemeConstantOverride("margin_left", 8);
                pad.AddThemeConstantOverride("margin_right", 8);
                pad.AddThemeConstantOverride("margin_top", cell.Micro is null ? 8 : 5);
                pad.AddThemeConstantOverride("margin_bottom", 6);
                Label main = Body(cell.Text, 13, fg, bold: column.Key);
                main.HorizontalAlignment = column.Align switch
                {
                    TableAlign.Center => HorizontalAlignment.Center,
                    TableAlign.Right => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Left,
                };
                stack.AddChild(main);
                if (!string.IsNullOrEmpty(cell.Micro))
                {
                    Label micro = Meta(cell.Micro, 9, microColor);
                    micro.HorizontalAlignment = main.HorizontalAlignment;
                    stack.AddChild(micro);
                }

                pad.AddChild(stack);
                cellPanel.AddChild(pad);
                grid.AddChild(cellPanel);
            }
        }

        scroll.AddChild(grid);
        return scroll;
    }

    public static OptionButton CompactSelect(IReadOnlyList<string> items, int selectedIndex, Action<int> onSelected)
    {
        OptionButton box = new();
        for (int index = 0; index < items.Count; index++)
        {
            box.AddItem(items[index], index);
        }

        box.Selected = Math.Clamp(selectedIndex, 0, Math.Max(0, items.Count - 1));
        box.CustomMinimumSize = new Vector2(140, 30);
        box.AddThemeFontSizeOverride("font_size", 11);
        if (bodyBold is not null)
        {
            box.AddThemeFontOverride("font", bodyBold);
        }

        StyleBoxFlat style = new()
        {
            BgColor = Paper,
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
        box.AddThemeStyleboxOverride("normal", style);
        box.AddThemeStyleboxOverride("hover", style);
        box.AddThemeStyleboxOverride("pressed", style);
        box.AddThemeStyleboxOverride("focus", style);
        box.ItemSelected += index => onSelected((int)index);
        return box;
    }

    public static PanelContainer ContractFrame(string title, Control body)
    {
        PanelContainer frame = new();
        frame.AddThemeStyleboxOverride("panel", Frame(Paper));
        VBoxContainer stack = new();
        stack.AddThemeConstantOverride("separation", 6);
        MarginContainer pad = new();
        pad.AddThemeConstantOverride("margin_left", 10);
        pad.AddThemeConstantOverride("margin_top", 9);
        pad.AddThemeConstantOverride("margin_right", 10);
        pad.AddThemeConstantOverride("margin_bottom", 9);
        stack.AddChild(Meta(title, 10, Black));
        stack.AddChild(body);
        pad.AddChild(stack);
        frame.AddChild(pad);
        return frame;
    }

    public static Button Solid(string text, Action onPressed, Color background, Color foreground, bool compact = false)
    {
        EnsureFonts();
        Button button = new()
        {
            Text = text.ToUpperInvariant(),
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
            "ok" => Team,
            "warn" => Paper,
            _ => White,
        };
        Color foreground = kind is "red" or "inv" or "ok" ? Paper : Black;
        Label label = Meta(text, 11, foreground);
        label.AddThemeStyleboxOverride("normal", ChipBox(background));
        return label;
    }

    public static PanelContainer Avatar(string name, bool mini = false)
    {
        int width = mini ? 48 : 110;
        int height = mini ? 58 : 130;
        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(width, height);
        panel.AddThemeStyleboxOverride("panel", ChipBox(White));

        ColorRect lower = Block(Team);
        lower.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        lower.AnchorTop = mini ? 0.62f : 0.68f;
        lower.OffsetTop = 0;
        lower.OffsetBottom = 0;
        lower.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddChild(lower);

        Label initials = Display(CareerLookCatalog.Initials(name), mini ? 14 : 28, Black);
        initials.HorizontalAlignment = HorizontalAlignment.Center;
        initials.VerticalAlignment = VerticalAlignment.Center;
        initials.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        initials.OffsetBottom = mini ? -18 : -34;
        initials.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddChild(initials);
        return panel;
    }

    public static HBoxContainer Stat(string label, int value)
    {
        value = Math.Clamp(value, 0, 100);
        HBoxContainer row = new();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 7);
        Label name = Body(label, 11, Black, bold: true);
        name.CustomMinimumSize = new Vector2(88, 0);
        row.AddChild(name);

        PanelContainer track = new();
        track.ClipContents = true;
        track.CustomMinimumSize = new Vector2(80, 10);
        track.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        track.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        track.AddThemeStyleboxOverride("panel", StatTrackBox());
        ColorRect fill = Block(Team);
        fill.SetAnchorsPreset(Control.LayoutPreset.LeftWide);
        fill.AnchorRight = value / 100f;
        fill.OffsetRight = 0;
        fill.MouseFilter = Control.MouseFilterEnum.Ignore;
        track.AddChild(fill);
        row.AddChild(track);

        Label number = Body(value.ToString(System.Globalization.CultureInfo.InvariantCulture), 11, Black, bold: true);
        number.CustomMinimumSize = new Vector2(30, 0);
        number.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(number);
        return row;
    }

    public static HBoxContainer Kv(string label, string value)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        Label left = Meta(label, 10, Gray);
        left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        Label right = Body(value, 14, Black, bold: true);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(left);
        row.AddChild(right);
        return row;
    }

    public static ColorRect Hairline()
    {
        ColorRect line = Block(Hair);
        line.CustomMinimumSize = new Vector2(0, 1);
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

    private static StyleBoxFlat PillBox()
    {
        return new StyleBoxFlat
        {
            BgColor = White,
            BorderColor = Black,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
    }

    private static MarginContainer PillPadding(Control child)
    {
        MarginContainer pad = new();
        pad.AddThemeConstantOverride("margin_top", 8);
        pad.AddThemeConstantOverride("margin_bottom", 8);
        pad.AddChild(child);
        return pad;
    }

    private static StyleBoxFlat SectionBarBox()
    {
        return new StyleBoxFlat
        {
            BgColor = Team,
            BorderColor = Black,
            BorderWidthBottom = 3,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
    }

    private static Button SectionLink(string text, Action onPressed)
    {
        Button button = new()
        {
            Text = text.ToUpperInvariant(),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            Flat = true,
        };
        if (metaFont is not null)
        {
            button.AddThemeFontOverride("font", metaFont);
        }

        button.AddThemeFontSizeOverride("font_size", 10);
        button.AddThemeColorOverride("font_color", TeamOn);
        button.AddThemeColorOverride("font_hover_color", Paper);
        button.AddThemeColorOverride("font_pressed_color", Paper);
        button.Pressed += onPressed;
        return button;
    }

    private static StyleBoxFlat NavBox(bool active, bool hover)
    {
        Color bg = active ? Black : hover ? new Color(0, 0, 0, 0.22f) : new Color(0, 0, 0, 0);
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = hover && !active ? new Color(0, 0, 0, 0.35f) : new Color(0, 0, 0, 0),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
    }

    private static StyleBoxFlat TableHeadBox(bool sorted)
    {
        return new StyleBoxFlat
        {
            BgColor = White,
            BorderColor = sorted ? Team : Hair,
            BorderWidthBottom = sorted ? 2 : 1,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
    }

    private static PanelContainer TableCellPanel(bool selected, int rowIndex, Action<int>? onSelect)
    {
        PanelContainer panel = new();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        StyleBoxFlat box = new()
        {
            BgColor = selected ? Black : White,
            BorderColor = Hair,
            BorderWidthBottom = 1,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
        panel.AddThemeStyleboxOverride("panel", box);
        if (onSelect is not null)
        {
            panel.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            panel.MouseFilter = Control.MouseFilterEnum.Stop;
            int captured = rowIndex;
            panel.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    onSelect(captured);
                    panel.AcceptEvent();
                }
            };
        }

        return panel;
    }

    private static StyleBoxFlat StatTrackBox()
    {
        return new StyleBoxFlat
        {
            BgColor = Hair,
            BorderColor = Black,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
    }
}
