# Peloton Manager

Headless .NET 8 architecture skeleton plus a race prototype, with a first Godot Watch Race window. It creates a tiny JSON-defined world, advances the whole world, runs a deterministic prototype race, and saves/loads a career from an embedded SQLite file.

## Requirements

- a .NET SDK capable of targeting `net8.0`;
- no Godot installation for headless build or test;
- Godot 4.4 .NET only if opening the Watch Race window;
- Windows, Linux, or another .NET 8-supported headless environment.

## Build and test

```text
dotnet format --verify-no-changes
dotnet build PelotonManager.sln
dotnet test PelotonManager.sln
```

## Headless simulation

From the repository root:

```text
dotnet run --project tools/Peloton.SimRunner -- run --scenario scenario.peloton.skeleton --years 10 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- race --scenario race-scenario.peloton.prototype-v0 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- watch --scenario race-scenario.peloton.prototype-v0 --seed 91234
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --follow-hub
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --through-races
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --simulate-from-prep --through-results
dotnet run --project tools/Peloton.SimRunner -- day --scenario scenario.peloton.skeleton --seed 91234 --days 13 --watch-from-prep --rate 5
```

`race --scenario race.prototype.gate` is an alias for the same prototype fixture.

The skeleton calendar is deliberately short: one season is 12 calls to `AdvanceDay` followed by one prototype race. The season runner reports crash status, final world day, deterministic checksum, and race count. The `race` command reports winner, checksum, decision count, Spy neutrality, and crash status. Optional `--trace-json` and `--trace-markdown` write Race Spy artifacts. The `day` command creates a skeleton career, advances the requested days, prints the Hub snapshot (date, employer, next race, primary action/label, today's notes), calendar entries (`calendar=day=… kind=… status=… title=…` plus `result=` when completed), inbox items (`inboxCount`, `inbox=identity=…`), and stops with `RACE_DAY_PENDING` when a race is due. Add `--follow-hub` to enter race preparation and print `prep=` without running the race. Add `--simulate-from-prep` to confirm and simulate once from that flow, `--through-results` to acknowledge the committed result and print `result=` / `debrief=`, or `--through-races` to confirm, simulate, acknowledge results, finish the debrief, and keep advancing.

## Godot boundary

`src/Peloton.Client.Godot` is a Watch Race presentation client (Godot 4.4 .NET). Open `src/Peloton.Client.Godot/project.godot`. It issues Application Commands and Queries, interpolates `RaceWatch` icons, pauses on a knowledge-bounded decision, and shows Results from `LastRace`. It does not own World State, does not write SQLite, and does not drive race physics. It is not a Career Hub. Headless tests and `Peloton.SimRunner` remain the default verification surfaces; Godot is not required for `dotnet test`.

## UI lab

Static HTML prototypes live at the repository root (`08e-constructivist-desk.html`, `10-dashboard-constructivist.html`, `12-dashboard-team-mid.html`, `14-race.html`). Rejected variants are under `archive/`. The avatar experiment is `experiments/avatar_prototype/`. These are look references, not Godot and not the headless career loop.

## Known difference from the race contract

Official results come from `PrototypeRaceEngine`, not a seed-ranked stub. The prototype is still below `RACE_ENGINE_DESIGN_v0.2.md`: one-second `double` steps, synthetic content not bound to the career roster, simplified shelter/knowledge. Godot Watch exists; the owner §49 fun gate is not verified. See `KNOWN_DIFFERENCE_FROM_CODE.md`.
