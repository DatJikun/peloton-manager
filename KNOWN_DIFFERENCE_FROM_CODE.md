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
- `Loyalty01` is stored and queried via `ClubRosterProjection`; it is not a transfer modifier;
- manager `Employment` stays manager-only; knowledge stores and `RecruitmentCase` from `DATA_MODEL_v0.1.md` are not implemented;
- multi-stage GC leadership transfer (`D-032`) is not implemented.

This is an explicit prototype boundary, not an accepted simplification of the future Race Engine. Deeper physiology and fun/decision-density claims wait on owner playtest.

## World–race bind (D-036 phase 1 landed)

- `RiderCareer.Id` is the official race `RiderId`; `LastRace` finish order and `RiderCareerResult` history use those world IDs.
- `CreateWorld` materializes riders from `content/peloton.skeleton/skeleton-roster.json` (stable `OriginDefinitionId`s from the prototype pack).
- Prep squad is the player employer's world roster (`RiderCareer.OrganizationId`; null = unattached).
- SQLite `SchemaVersion` is **6** (cash/fee landed in phase 6). Schema 1–5 saves may refuse to load.
- World checksum label is `peloton-world-checksum-v6`.
- `CalendarEntry.RaceContentId` stores the calendar race id (`race.wt2026.*` for WT; route template resolved via `DefaultRaceTemplateId`).

Phase 1 out of scope for Godot: Career Hub stays rejected. WT CreateWorld is landed in phase 5.

## Planning windows (D-036 phase 3 landed)

- `OrganizationRaceEntry` (organization, `RaceContentId`, entered) persisted at SQLite SchemaVersion **4**; world create enters every org into every scheduled race content id.
- `BeginPreSeasonPlanningCommand` / `SetSeasonRaceEntryCommand` / `ConfirmPreSeasonPlanCommand` / `CancelPreSeasonPlanningCommand` — draft until confirm; time does not advance.
- Official start list = `RiderCareer` rows whose organization is entered for that race's `RaceContentId` (skips null `OrganizationId`).
- Player race-due (`Race next` / blocked `AdvanceDay`) = calendar race today **and** employer entered; skipped entry allows `AdvanceDay`, which auto-simulates entered teams with delegated defaults then advances the day.
- `SetRacePreparationStrategyCommand` (leader/support/objective/briefing) required before `ConfirmRacePreparationPlanCommand`; assembler honours player strategy; checkpoint round-trips in saves.

## Rider contracts (D-036 phase 4 landed)

- `RiderContract` (wage, start, inclusive end) is the rider–club system of record; not manager `Employment`.
- `CreateWorld` allocates one contract per `RiderCareer`; expired contracts remain as history.
- Contract expiry runs after the date increment on `AdvanceOneDay`; unattached riders (`OrganizationId = null`) still receive the rest tick but do not start races.
- `ClubRosterProjection` exposes employer roster wages, contract end day, and loyalty (headless only).
- World checksum label is `peloton-world-checksum-v4`. Schema 3 saves may refuse to load.

## WorldTour 2026 pack (D-036 phase 5 landed)

- `scenario.peloton.wt-2026` CreateWorld: 18 orgs, 72 thin 4-rider squads, 36 content calendar races, employer Alpecin.
- Physiology, wages, and budgets are estimated gameplay bands (`content/peloton.wt-2026/README.md`).
- Official start lists capped at **12 riders** (prototype engine limit); not a UCI field size.
- Route geometry remains the **synthetic proof circuit** (`race-scenario.peloton.prototype-v0`); WT `RaceContentId` values map to that template at assemble time.
- `GeneratePeriodicRaces` is false for WT; skeleton keeps periodic race generation.
- Race-due uses calendar entries, not `day % CalendarPeriodDays`.
- SQLite SchemaVersion **5** / checksum `peloton-world-checksum-v5`. Skeleton worlds also save as v5. (Superseded by phase 6 — see below.)

## Thin economy (D-036 phase 6 landed)

- `Organization.CashEur` (may be negative) and `TitleSponsorAnnualFeeEur` tick daily on Advance Day after contract expiry.
- `WorldState.FinancialYearDays`: skeleton = `CalendarPeriodDays` (12); WT = 365.
- `dailySponsor = floor(fee / yearDays)`, `dailyWages = floor(active wage bill / yearDays)`; no luxury tax, no inflation, no auto-firing when overdrawn.
- Skeleton world create: fee 2_000_000 and `TitleSponsor = "Skeleton Sponsor"` when budget is 0. WT: fee = `EstimatedBudgetEur`.
- `ClubFinanceProjection` on Management; SimRunner `day` prints `cash=` and `overdrawn=`.
- `RacePreparationProjection.Title` uses today's calendar race name (WT TDU = `Santos Tour Down Under`).
- SQLite SchemaVersion **6** / checksum `peloton-world-checksum-v6`. Schema 1–5 saves refuse to load.

Phase 7+ out of scope: dynamic sponsor market, inflation, transfer market, Godot Hub, AI managers, D-032, tenth GameState.

## Day state (D-036 phase 2 landed)

- `WorldState.AdvanceOneDay` applies the locked rest tick to every `RiderCareer` (deterministic, no RNG).
- `WorldState.RecordRace` applies the locked race-load formula to every starter before appending `RiderCareerResult`.
- `WorldRaceScenarioAssembler.ToRaceProfile` scales `CriticalPowerW` / `PeakPowerW` by readiness from stored form/freshness/fatigue; stored physiology is unchanged.
- `TeamRaceObservation.DecisionAuthorityId` uses the world's human `DecisionAuthority` id (not `organizationId + 100`).
- Career day races after 12 advance days now differ from immediate race-on-create (readiness drift); SimRunner day goldens use winner `20` / `beta-leader` for seed `91234` (with default prep strategy).

Owner slice contract: `CAREER_WORLDTOUR_SLICE_v0.1.md`.
