# Peloton Manager

Headless .NET 8 architecture skeleton plus a race prototype. It creates a tiny JSON-defined world, advances the whole world, runs a deterministic prototype race, and saves/loads a career from an embedded SQLite file.

## Requirements

- a .NET SDK capable of targeting `net8.0`;
- no Godot installation for build or test;
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
```

`race --scenario race.prototype.gate` is an alias for the same prototype fixture.

The skeleton calendar is deliberately short: one season is 12 calls to `AdvanceDay` followed by one prototype race. The season runner reports crash status, final world day, deterministic checksum, and race count. The `race` command reports winner, checksum, decision count, Spy neutrality, and crash status. Optional `--trace-json` and `--trace-markdown` write Race Spy artifacts. The `watch` command is a decision digest (start, pauses, finish), not the Watch Race film: playback will be a supervising clock with smooth speed-based motion (`D-033`).

## Godot boundary

`src/Peloton.Client.Godot` is an empty compile-time stub. Future Godot code will call Application Commands and Queries; it will not own World State or write SQLite. Godot was not used to implement or verify Milestone 0 or the race prototype.

## Known difference from the race contract

Official results come from `PrototypeRaceEngine`, not a seed-ranked stub. The prototype is still below `RACE_ENGINE_DESIGN_v0.2.md`: one-second `double` steps, synthetic content, simplified shelter/knowledge, no Godot, and the owner §49 fun gate is not verified. See `KNOWN_DIFFERENCE_FROM_CODE.md`.
