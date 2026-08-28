# Known Difference From Code

## Race prototype versus `RACE_ENGINE_DESIGN_v0.2.md`

Official race results no longer come from `StubRaceEngine`. That seed-ranking path is removed from production assemblies. `GameApplication` and SimRunner use `PrototypeRaceEngine` plus the validated `peloton.race-prototype` fixture.

The prototype is still below the accepted Race Engine contract. Remaining intentional limits:

- fixed one-second step and `double` arithmetic; these are prototype choices, not production locks;
- synthetic multi-team pack and transient numeric participant IDs, not career roster integration;
- simplified shelter slots, drafting, durability, and knowledge-bounded chase decisions;
- Godot Watch Race exists as a presentation window over the same D-033 clock; it is not a Career Hub and not a fun-gate result. The radio board is knowledge-bounded (place, km/h, gap, terrain, shelter) and does not expose W'/durability. Prep UI is film length + DS autonomy + Oglądaj (plan confirm is automatic). Film rate uses 6 m/s effective pace so the 5.4 km prototype (~15 min of physics with fatigue) fits in about 5 min of watching at ×3; live km/h on the board is still ~40 on the flat;
- Godot Watch map expands coarse prototype segments through an authored route-profile library (3 variants each for flat / climb / descent / rolling / crosswind) and a seeded generator. Official `race-prototype.json` physics stay three constant-gradient ramps so goldens do not move. Climb vs flat speed still follows those coarse gradients; the map paints the authored shape;
- owner engagement gate in `RACE_ENGINE_DESIGN_v0.2.md` §49 remains `NOT VERIFIED`;
- SimRunner `watch` still has the D-033 headless supervising clock, rates ×1 / ×2 / ×5 / ×20, decision pauses, and RNG-neutral focal-rider motion; CLI Watch is not the Godot renderer or an owner §49 playtest;
- multi-stage GC leadership transfer (`D-032`) is not implemented.

This is an explicit prototype boundary, not an accepted simplification of the future Race Engine. Deeper physiology, career calendars, and fun/decision-density claims wait on owner playtest.

The Milestone 0 checksum and shared high-water ID allocator are likewise skeleton contracts. They do not close OQ-TS-001 or OQ-DM-001; changing their durable representation later requires normal schema/version review and migration. SQLite `SchemaVersion` remains 1; the persisted last-race JSON shape is unchanged.
