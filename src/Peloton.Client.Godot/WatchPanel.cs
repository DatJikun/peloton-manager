using Godot;

namespace Peloton.Client.Godot;

public sealed partial class WatchPanel : PanelContainer
{
    public Label Title { get; }

    public HBoxContainer HeaderTrail { get; }

    public MarginContainer Body { get; }

    public WatchPanel(string title)
    {
        AddThemeStyleboxOverride("panel", WatchChrome.Frame(WatchChrome.White, shadow: true));
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 0);
        AddChild(column);

        PanelContainer headerBar = new();
        headerBar.AddThemeStyleboxOverride("panel", WatchChrome.HeaderBar());
        column.AddChild(headerBar);

        HBoxContainer headerRow = new();
        headerRow.AddThemeConstantOverride("separation", 10);
        headerBar.AddChild(headerRow);

        Title = WatchChrome.MakeLabel(title, 13, WatchChrome.Paper, displayFace: true);
        Title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        headerRow.AddChild(Title);

        HeaderTrail = new HBoxContainer();
        HeaderTrail.AddThemeConstantOverride("separation", 8);
        headerRow.AddChild(HeaderTrail);

        Body = new MarginContainer();
        Body.AddThemeConstantOverride("margin_left", 10);
        Body.AddThemeConstantOverride("margin_right", 10);
        Body.AddThemeConstantOverride("margin_top", 8);
        Body.AddThemeConstantOverride("margin_bottom", 8);
        Body.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddChild(Body);
    }
}
