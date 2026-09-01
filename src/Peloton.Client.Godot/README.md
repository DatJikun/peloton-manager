# Godot client

This project is the Godot presentation client. Headless tests and `Peloton.SimRunner` remain the executable surfaces without the Godot editor.

Open `project.godot` in Godot 4.4 .NET.

- Main scene: `CareerShell.tscn` — management chrome copied from `peloton-manager-full-ui-poc-v3.html`. Hub date, Advance Day / Race next, inbox, calendar, people, and **results by default** are real Application queries. Staff, finance, scouting, market, and look OVR come from `CareerLookCatalog` and do not write to World.
- Watch film is a presentation setting (`FILM: WYŁ/WŁ`), off by default (D-043). `WatchRace.tscn` still opens the stage window on its own.
- Hosts (`CareerShellHost`, `WatchRaceHost`) are Godot-free and covered by `tests/Peloton.Client.Godot.Tests`.

The client issues Application Commands and knowledge-bounded Queries. It does not own World State or open SQLite. It is not Career Hub (PR #4).

Player-facing description: `HOW_RACE_DAY_WORKS.md`.
