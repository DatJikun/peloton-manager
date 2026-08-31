# Godot client

Presentation only. Open `project.godot` in Godot 4.4 .NET.

- Main scene: `CareerShell.tscn` — management chrome copied from `peloton-manager-full-ui-poc-v3.html`. Hub / calendar / inbox / people queries are real. Staff, finance, scouting, market, and look OVR come from `CareerLookCatalog` and do not write to World.
- `WatchRace.tscn` — blocking RaceLive window. Career shell hides while a stage is watched.
- Hosts (`CareerShellHost`, `WatchRaceHost`) are Godot-free and covered by `tests/Peloton.Client.Godot.Tests`.

The client issues Application Commands and reads knowledge-bounded Queries. It does not own World State or open SQLite.
