# Godot Watch Race client

Presentation only. Open `project.godot` in Godot 4.4 .NET.

This project issues Application Commands and Queries through `Peloton.Infrastructure`. It interpolates official `RaceWatch` snapshots, pauses on a knowledge-bounded decision, and shows Results from `LastRace`.

It does **not** own World State, open SQLite, or drive race physics. It is not a Career Hub. Headless tests and `Peloton.SimRunner` remain the executable surfaces that do not need the Godot editor.
