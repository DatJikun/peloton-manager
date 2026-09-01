# Peloton.Client.Godot

Godot .NET presentation host. The main scene is a **career shell** (desk, squad, staff,
calendar, sponsors, finance, scouting, market, history, manager, help). Empty domains
draw **look-catalog** numbers from `CareerLookCatalog` (the HTML POC) so the screens
are not blank. That catalog is **not** World. Real Application lives on the desk:
calendar day, inbox, next command, save, and **simulate → result table** (D-043).

Career Hub UI is **deleted** (D-048). The shell is look / presentation for empty
domains; desk date, Advance Day / Race next, inbox, calendar, people, and default
results are real Queries.

Watch Race is an optional overlay. Film is a presentation setting (`FILM: WYŁ/WŁ`),
**off by default**. Simulate → results stays the default race-day path.

## What this is

- Godot 4.x + .NET 8 C# (Godot Mono / .NET).
- Windows-first target for a later playable client.
- Today: career shell with Advance Day / Race next and default simulate → results.
  Watch overlay is optional and off until the player turns it on. Headless tests stay
  in `tests/Peloton.Client.Godot.Tests` (`dotnet test` from repo root). Those tests do
  not need a Godot editor.

## What this is not

- Not the simulation. World, Commands, and Simulation stay in Application.
- Not a second client. HTML files are design references (`HTML_UI_LAB.md`), not a
  playable surface.
- Not Career Hub. That dashboard is gone; do not rebuild it.
- Not a reason to close §49.

## Opening in Godot (Windows)

Godot is **not** installed in the Cloud Agent Linux environment. On a Windows machine
with Godot 4.x .NET:

1. Install [Godot 4.x .NET](https://godotengine.org/download) (not the non-.NET build).
2. Open this folder as a Godot project (`project.godot`).
3. Confirm the .NET solution builds from Godot's C# / Build panel.
4. Press Play. The career shell should open. Race day defaults to simulate → a result
   table. Watch film stays off unless you turn it on (`FILM: WŁ`).

If the C# build fails, run `dotnet build` from the repo root first so the Godot
project can see `Peloton.Application` and `Peloton.Contracts`.

Player-facing description: `HOW_RACE_DAY_WORKS.md`.

## Layout

```
project.godot                 Godot project (main scene = CareerShell)
CareerShell.tscn / .cs        Career shell: desk + look catalog for empty domains
CareerShellViews.cs           Per-screen layout (desk, squad, staff, …)
CareerLookCatalog.cs          Static POC numbers (Godot-free; used by tests)
LookChrome.cs / LookCharts.cs Shared widgets (fonts, chips, avatars, charts)
WatchRace.tscn / .cs          Optional Watch overlay (off by default)
Main.tscn / Main.cs           Compile stub (kept; not the main scene)
```
