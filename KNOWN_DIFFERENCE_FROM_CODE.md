# Known Difference From Code

## Race prototype versus `RACE_ENGINE_DESIGN_v0.2.md`

Official career race results use `PrototypeRaceEngine` with start lists built from world `RiderCareer` rows plus route/tuning from `peloton.race-prototype` (`WorldRaceScenarioAssembler`, D-036 phase 1). `StubRaceEngine` remains removed from production assemblies.

Standalone SimRunner `race` / `watch` still resolve the disconnected fixture scenario for the prototype gate; that path is not the official career bind.

The prototype is still below the accepted Race Engine contract. Remaining intentional limits:

- fixed one-second step and `double` arithmetic; these are prototype choices, not production locks;
- simplified shelter slots, drafting, durability, and knowledge-bounded chase decisions;
- Godot Watch Race exists as a presentation window over the same D-033 clock; it is not a Career Hub and not a fun-gate result;
- owner engagement gate in `RACE_ENGINE_DESIGN_v0.2.md` §49 remains `NOT VERIFIED`;
- SimRunner `watch` implements the D-033 headless supervising clock (rates ×1 / ×2 / ×5 / ×20, decision pauses, RNG-neutral focal-rider motion); CLI Watch is not the Godot renderer or an owner §49 playtest;
- `Form01` / `Freshness01` / `Fatigue01` on `RiderCareer` are stored but not applied on Advance Day yet (phase 2);
- `Loyalty01` is stored but unused until contracts phase;
- rider `Employment` / contracts, knowledge stores, and `RecruitmentCase` from `DATA_MODEL_v0.1.md` are not implemented;
- multi-stage GC leadership transfer (`D-032`) is not implemented.

This is an explicit prototype boundary, not an accepted simplification of the future Race Engine. Deeper physiology and fun/decision-density claims wait on owner playtest.

## World–race bind (D-036 phase 1 landed)

- `RiderCareer.Id` is the official race `RiderId`; `LastRace` finish order and `RiderCareerResult` history use those world IDs.
- `CreateWorld` materializes riders from `content/peloton.skeleton/skeleton-roster.json` (stable `OriginDefinitionId`s from the prototype pack).
- Prep squad is the player employer's world roster (`RiderCareer.OrganizationId`).
- SQLite `SchemaVersion` is **2** and includes `RiderCareer` + results. Schema 1 skeleton saves may refuse to load.
- World checksum label is `peloton-world-checksum-v2` (ten-season golden checksums changed).
- `CalendarEntry.RaceContentId` stores the route/tuning scenario id.

Phase 1 out of scope: form tick on Advance Day, pre-season picker, strategy window UI, contracts, 2026 WT pack, Career Hub.

Owner slice contract: `CAREER_WORLDTOUR_SLICE_v0.1.md`.
