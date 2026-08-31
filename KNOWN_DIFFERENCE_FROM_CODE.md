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
- `Form01` / `Freshness01` / `Fatigue01` on `RiderCareer` are applied on Advance Day and official races (phase 2 landed); stored physiology is not mutated — readiness scales CP/Pmax at assemble time only;
- `Loyalty01` is stored; phase 4 will query it with contracts and still will not use it as a transfer modifier;
- rider `RiderContract` (wage + expiry) is specified for phase 4 and not yet in code; manager `Employment` stays manager-only; knowledge stores and `RecruitmentCase` from `DATA_MODEL_v0.1.md` are not implemented;
- multi-stage GC leadership transfer (`D-032`) is not implemented.

This is an explicit prototype boundary, not an accepted simplification of the future Race Engine. Deeper physiology and fun/decision-density claims wait on owner playtest.

## World–race bind (D-036 phase 1 landed)

- `RiderCareer.Id` is the official race `RiderId`; `LastRace` finish order and `RiderCareerResult` history use those world IDs.
- `CreateWorld` materializes riders from `content/peloton.skeleton/skeleton-roster.json` (stable `OriginDefinitionId`s from the prototype pack).
- Prep squad is the player employer's world roster (`RiderCareer.OrganizationId`).
- SQLite `SchemaVersion` is **3** and includes `RiderCareer`, results, and `OrganizationRaceEntry`. Schema 1–2 saves may refuse to load.
- World checksum label is `peloton-world-checksum-v3` (ten-season golden checksums changed again from v2).
- `CalendarEntry.RaceContentId` stores the route/tuning scenario id.

Phase 1 out of scope (now landed in phase 3 headless): pre-season picker and strategy step are implemented as commands; Godot UI for them is still out of scope. Remaining: contracts, 2026 WT pack, Career Hub.

## Planning windows (D-036 phase 3 landed)

- `OrganizationRaceEntry` (organization, `RaceContentId`, entered) persisted at SQLite SchemaVersion **3**; world create enters every org into every scheduled race content id.
- `BeginPreSeasonPlanningCommand` / `SetSeasonRaceEntryCommand` / `ConfirmPreSeasonPlanCommand` / `CancelPreSeasonPlanningCommand` — draft until confirm; time does not advance.
- Official start list = `RiderCareer` rows whose organization is entered for that race's `RaceContentId`.
- Player race-due (`Race next` / blocked `AdvanceDay`) = calendar race today **and** employer entered; skipped entry allows `AdvanceDay`, which auto-simulates entered teams with delegated defaults then advances the day.
- `SetRacePreparationStrategyCommand` (leader/support/objective/briefing) required before `ConfirmRacePreparationPlanCommand`; assembler honours player strategy; checkpoint round-trips in saves.
- World checksum label is `peloton-world-checksum-v3`. Schema 2 saves may refuse to load.

Phase 4 specified: `RiderContract` wage + inclusive expiry, nullable `RiderCareer.OrganizationId`, SchemaVersion 4. Not in code yet.

Phase 4+ out of scope: transfer market, 2026 WT pack wired to `CreateWorld`, AI managers, D-032, tenth GameState.

## Day state (D-036 phase 2 landed)

- `WorldState.AdvanceOneDay` applies the locked rest tick to every `RiderCareer` (deterministic, no RNG).
- `WorldState.RecordRace` applies the locked race-load formula to every starter before appending `RiderCareerResult`.
- `WorldRaceScenarioAssembler.ToRaceProfile` scales `CriticalPowerW` / `PeakPowerW` by readiness from stored form/freshness/fatigue; stored physiology is unchanged.
- `TeamRaceObservation.DecisionAuthorityId` uses the world's human `DecisionAuthority` id (not `organizationId + 100`).
- Career day races after 12 advance days now differ from immediate race-on-create (readiness drift); SimRunner day goldens use winner `15` / `beta-leader` for seed `91234` (with default prep strategy).

Owner slice contract: `CAREER_WORLDTOUR_SLICE_v0.1.md`.
