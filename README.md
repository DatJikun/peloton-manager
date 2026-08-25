# Peloton Manager

Milestone 0 is a headless .NET 8 architecture skeleton. It creates a tiny JSON-defined world, advances the whole world, runs a deterministic stub race, and saves/loads a career from an embedded SQLite file.

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
```

The skeleton calendar is deliberately short: one season is 12 calls to `AdvanceDay` followed by one stub race. The runner reports crash status, final world day, deterministic checksum, and race count.

## Godot boundary

`src/Peloton.Client.Godot` is an empty compile-time stub. Future Godot code will call Application Commands and Queries; it will not own World State or write SQLite. Godot was not used to implement or verify Milestone 0.

## Known difference from the race contract

The race implementation is only deterministic seeded ordering from a start list and route ID. It does not implement physiology, drafting, crosswinds, tactics, information, DS decisions, or Race Spy and must not be treated as the `RACE_ENGINE_DESIGN_v0.2.md` prototype.
