# Known Difference From Code

## Race prototype versus `RACE_ENGINE_DESIGN_v0.2.md`

Official race results no longer come from `StubRaceEngine`. That seed-ranking path is removed from production assemblies. `GameApplication` and SimRunner use `PrototypeRaceEngine` plus the validated `peloton.race-prototype` fixture.

The prototype is still below the accepted Race Engine contract. Remaining intentional limits:

- fixed one-second step and `double` arithmetic; these are prototype choices, not production locks;
- synthetic multi-team pack and transient numeric participant IDs, not career roster integration;
- simplified shelter slots, drafting, durability, and knowledge-bounded chase decisions;
- no Godot RaceLive UI;
- owner engagement gate in `RACE_ENGINE_DESIGN_v0.2.md` §49 remains `NOT VERIFIED`;
- SimRunner `watch` now has the D-033 headless supervising clock, rates ×1 / ×2 / ×5 / ×20, decision pauses, and RNG-neutral focal-rider motion; it is still not the Godot Watch Race renderer or an owner §49 playtest;
- multi-stage GC leadership transfer (`D-032`) is not implemented.

This is an explicit prototype boundary, not an accepted simplification of the future Race Engine. Deeper physiology, career calendars, and fun/decision-density claims wait on owner playtest.

The Milestone 0 checksum and shared high-water ID allocator are likewise skeleton contracts. They do not close OQ-TS-001 or OQ-DM-001; changing their durable representation later requires normal schema/version review and migration. SQLite `SchemaVersion` remains 1; the persisted last-race JSON shape is unchanged.
