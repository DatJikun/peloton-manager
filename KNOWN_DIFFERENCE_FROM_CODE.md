# Known Difference From Code

## Race prototype versus `RACE_ENGINE_DESIGN_v0.2.md`

Official career race results use `PrototypeRaceEngine` with start lists built from world `RiderCareer` rows plus route/tuning from `peloton.race-prototype` (`WorldRaceScenarioAssembler`, D-036 phase 1). `StubRaceEngine` remains removed from production assemblies.

Standalone SimRunner `race` / `watch` still resolve the disconnected fixture scenario for the prototype gate; that path is not the official career bind.

The prototype is still below the accepted Race Engine contract. Remaining intentional limits:

- fixed one-second step and `double` arithmetic; these are prototype choices, not production locks;
- simplified shelter slots, drafting, durability, and knowledge-bounded chase decisions;
- Godot Watch Race exists as an **optional** presentation window over the same D-033 clock, **off by default**; the career shell default is simulate → official result table and a presentation-only team filter (D-043 / D-048);
- Godot career shell may show POC look-catalog names (Beskid–Vetter, OVR) on staff/sponsors/scouting/market; those numbers are not World State and not true ability; desk finance, squad wages, and contract offers read `ClubFinance` / `ClubRoster` / D-044;
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
- SQLite `SchemaVersion` is **9** (D-050 designated leaders). Schema 1–8 saves may refuse to load.
- World checksum label is `peloton-world-checksum-v9`.
- `CalendarEntry.RaceContentId` stores the calendar race id (`race.wt2026.*` for WT; route template resolved via `DefaultRaceTemplateId`).

Phase 1 out of scope for Godot: Career Hub UI is deleted (D-048). WT CreateWorld is landed in phase 5.

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
- Physiology, wages, and budgets are estimated gameplay bands (`content/peloton.wt-2026/README.md`). Riders use per-person `archetype` / `wageBand`. Squad order is captain → protected card → helpers. Evenepoel 2026 is Red Bull `.leader`. Contract end days are varied (not `10000` placeholders). Pack is still 4 names per team: `WT_2026_PHYSIOLOGY_AND_CONTRACTS_RESEARCH_2026-09-01.md`.
- Official WT start lists use the **full entered pack (72 riders)** — all four riders per entered team; still not a UCI 150–200 rider field.
- Prototype race session remains **1-second sequential** `RaceSession.Step` (not real-time or sub-second physics).
- Route geometry at phase-5 landing was still the synthetic proof circuit. **Superseded by D-047:** official WT Simulate compiles the stored `CourseProfile` for that calendar stage. Skeleton soak and standalone SimRunner `race` / `watch` still use the proof circuit.
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

## Results filter + thin negotiation (D-043 / D-044 phase 7 landed)

- `RaceResultPlacement` carries `Place`, `OrganizationId`, and organization display name (from rider club at result time).
- `GameApplication.RaceResultForOrganization(organizationId)` returns that org's finishers with official place numbers; legal for any organization.
- SimRunner `day --through-results` prints optional `resultTeam=` line for the player employer.
- Contract negotiation stays in `Management`: `BeginContractNegotiationCommand`, `SetContractOfferCommand`, `ConfirmContractOfferCommand`, `CancelContractNegotiationCommand`; draft on `GameApplication`; `ContractNegotiationProjection` query.
- Accept formula: `threshold = currentWage == 0 ? 100_000 : floor(currentWage * (1.10 - 0.20 * Loyalty01))`; reject code `CONTRACT_OFFER_REJECTED`; no transfer fee, no RNG.
- On accept: prior active contract ends today (history kept); new `RiderContract` starts today; `RiderCareer.AttachToClub`; at most one active contract (`StartDate <= today <= EndDate`).
- SQLite SchemaVersion **7** / checksum `peloton-world-checksum-v7`. Schema 1–6 saves refuse to load. Same-seed soak checksum hex changes (v7 label only for unchanged worlds).

Dynamic sponsor market, inflation, transfer **fees**, Godot Hub, AI managers, D-032, tenth GameState stay out. Do not expand Watch Race UI.

## Day state (D-036 phase 2 landed)

- `WorldState.AdvanceOneDay` applies the locked rest tick to every `RiderCareer` (deterministic, no RNG).
- `WorldState.RecordRace` applies the locked race-load formula to every starter before appending `RiderCareerResult`.
- `WorldRaceScenarioAssembler.ToRaceProfile` scales `CriticalPowerW` / `PeakPowerW` by readiness from stored form/freshness/fatigue; stored physiology is unchanged.
- `TeamRaceObservation.DecisionAuthorityId` uses the world's human `DecisionAuthority` id (not `organizationId + 100`).
- Career day races after 12 advance days now differ from immediate race-on-create (readiness drift); SimRunner day goldens use winner `20` / `beta-leader` for seed `91234` (with default prep strategy).

Owner slice contract: `CAREER_WORLDTOUR_SLICE_v0.1.md`.

## Rider ratings + courses (D-046 / D-047 landed)
Contract: `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md`.

Landed:

- Derived 1–99 ratings (`RiderRatingQueries.FromPhysiology`); `PotentialOvr` on `RiderCareer`; `ClubRosterProjection` exposes ratings; SimRunner hub prints compact roster lines.
- WT `roster.json` recalibrated to archetypes (Pogačar / Philipsen / MvDP inequalities in tests).
- Dense `CourseProfile` catalog at CreateWorld (`race-identities.json` + `CourseCatalogGenerator`); calendar is **one entry per racing stage**; assembler compiles stored profile when `CourseProfileId` is set.
- `RaceRouteSegment.Surface` + handling-aware cobble Crr; `RaceDefinition.SegmentAt` is O(log n).
- Thin `RiderStageTime` GC rows persisted at schema 8.
- Skeleton soak still uses the proof circuit (`GeneratePeriodicRaces`); standalone SimRunner `race`/`watch` gate unchanged.

Remaining limits:

- Official WT start lists are **event-shaped (D-049)**: Grand Tours 176, monuments 175, TDU 140, other WT 154. Still a prototype, not a licensed 28-man roster.
- Classified Flat uses a bunch-sprint kick (last 250 m at `PeakPowerW`) after sitting in the pack. Feel probe seed `91234`: Philipsen place 1, Pogačar 135 on the flattest stored Flat; mountain probe still has Pogačar ahead of Philipsen.
- Prototype stores **one** `CdAM2` per rider. The accepted engine wants `CdARoad` and `CdATT` (`RACE_ENGINE_DESIGN_v0.2.md` §6–7). Drafting still does `CdA_effective = CdA * shelter`. There is no sit-up-on-climb vs aero-tuck-on-TT switch. Owner asked 2026-09-01; that split is the next aero honesty, not this D-049 tree. A third “mountain CdA” is not worth a rating — climbs are slow, gravity/W/kg dominate.
- Prototype race session is sequential 1-second `RaceSession.Step` for every rider; wall-clock is CPU-fast, not real-time.
- No yearly re-generation after season 2026 in play yet (generator exists; Advance Day does not roll new seasons).
- Jersey tables exist as after-stage queries (GC / points / KOM / youth / team). D-032 mid-race GC leadership stays deferred.
- §49 still `NOT VERIFIED`.

## Sprint feel, UCI fields, jerseys (D-049 landed)

Contract: `RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md`.

- Classified Flat: sit-in until the last 250 m, then `LaunchSprint` spends `PeakPowerW`. Noisy 25 m samples do not string the bunch. Feel probe seed `91234`: Philipsen 1, Pogačar 135 on the flattest stored Flat; Pogačar 8, Philipsen 133 on the biggest mountain. TDU starts 140.
- Official start lists: Grand Tours 22×8=176, monuments 25×7=175, TDU 20×7=140, other WT 22×7=154. Wildcard orgs have 8 riders so Grand Tours can take 8. Skeleton soak and standalone `race` stay 12 on the proof circuit.
- Jerseys are queries (`ClassificationQueries`): GC, points, KOM, youth, team. SimRunner `day --through-results` prints them. Godot result table has thin jersey lines. D-032 stays deferred.
- SimRunner `compare --scenario scenario.peloton.wt-2026 --seed 91234` writes analogues vs 2025 (not a script, D-001). TdF stage-1 analogue can land Philipsen; names are not forced to match history.
- SchemaVersion stays **9**.

## Club pick, calendar entries, per-event leaders (D-050 landed)

Contract: `CAREER_CLUB_CALENDAR_LEADERS_v0.1.md`.

- `CreateWorldCommand(ScenarioId, Seed, EmployerOrganizationOriginId?)` — `null` keeps recipe default (Alpecin on WT, skeleton red on skeleton). Non-null must pass `EmployerEligibility` (`playerStartDivisions` on the scenario).
- `OrganizationRaceEntry` adds optional `DesignatedLeaderId`; pre-season draft stores entered + leader per `RaceContentId`; confirm writes both.
- `SetSeasonRaceLeaderCommand` — leader must be on employer roster (`PREP_STRATEGY_RIDERS_INVALID` otherwise).
- `RacePreparationSupport.SetDefaultStrategy` uses designated leader when still on roster, else squad-order captain.
- Godot `_Ready` opens **Nowa gra** (18 WT clubs), then **Plan sezonu**; desk squad + squad + calendar read world projections, not `CareerLookCatalog` Beskid riders.
- Desk **FINANSE · TYDZIEŃ**, view **Finanse**, and squad wage column read `ClubFinanceProjection` / `ClubRoster` (euro). Skład **Negocjuj kontrakt** → D-044 offer commands. Staff/sponsors/scouting/market still look catalog.
- `OpenSkeleton` remains on `CareerShellHost` for tests; `OpenWorldTour(employerOriginId)` for WT play path.
- SimRunner `day` accepts optional `--employer organization.wt2026.*`.

## Position and selection (D-054 landed)

Contract: `RACE_FEEL_POSITION_AND_SELECTION_v0.1.md`. `PhysicsContractVersion` is **2**.

Landed in `Peloton.Simulation/Race`:

- **Position drift** after each step (`DriftMps`, `SlotSpacingM`, intent/finale bonuses via `PositionScoreResolver`). **§3.3 amendment:** forward drift (`delta > 0`) only when the rider was not power-limited this step (`realizablePowerW ≥ requiredPowerW` / `realizedSpeed ≥ desiredSpeed`); power-limited riders may drift backward only, and the gap floor clamp does not pull them forward.
- **Start grid** ordered by `Positioning` in `WorldRaceScenarioAssembler` (WT + skeleton paths; SimRunner `race`/`watch` fixture unchanged).
- **Pace-setter** in selective zones: max sustainable front speed at shelter 1.0 (`MaxSustainableFrontSpeedMps`), group target `max(BasePace, setterSpeed)`.
- **Cobble bruk (§5 + §5.1):** shelter `max(1 − (1 − shelter)·(0.25 + 0.75·Handling), 0.85)` on `Cobble`; required-power surge `1 + CobbleSurgeCost·(1 − Handling)`; `EffectiveCrr` cobble delta **0.018** with handling factor `(1.60 − 1.00·Handling)`; sector surges on asphalt↔cobble transitions of each group's pacing reference (`+CobbleSurgeSpeedMps` for `CobbleSurgeSeconds`, path-scanned per step).
- **CobbleClassic positioning scale** (§33 extension): effective positioning `Positioning · (CobblePositioningBase + CobblePositioningHandlingWeight · Handling)` on `CobbleClassic` stages so low-handling riders lose slots on cobbled races.

Final tuning constants (`RaceTuning`):

| Constant | Value |
|---|---|
| `DriftMps` | 0.78 |
| `SlotSpacingM` | 0.7 |
| `FinaleM` | 24_000 |
| `TempoFactorFinale` | 1.00 |
| `TempoFactorOutsideFinale` | 0.92 |
| `CobbleSurgeCost` | 0.286 |
| `CobbleCrrDelta` | 0.018 |
| `CobbleCrrHandlingIntercept` | 1.60 |
| `CobbleCrrHandlingSlope` | 1.00 |
| `CobbleShelterFloor` | 0.85 |
| `CobbleSurgeSeconds` | 12 |
| `CobbleSurgeSpeedMps` | 2.5 |
| Intent bonuses | 0.50 / 0.40 / 0.40 / −0.30 |
| `SprintFinaleBonus` | 0.25 |
| `SprintFinaleDistanceM` | 3_000 |
| `CobblePositioningBase` | 0.21 |
| `CobblePositioningHandlingWeight` | 0.91 |

Probes at seed `91234`: TdF stage 1 sprint, TDU stage 6 sprinter, Hautacam GC, determinism/spy neutrality, positioning + cobble + drift unit tests pass. **Roubaix probe still fails** at seed `91234` after §3.3 drift fix and contract-baseline §5.1 constants (two ±40% tuning passes did not pass the probe). Sim top 5: Evenepoel (super-gc), Pogačar (super-gc), Vingegaard (super-gc), Ganna (tt), Yates (gc) — van der Poel outside top 10; ahead of neither Evenepoel nor Vingegaard. Next lever within band: raise `CobbleCrrDelta` toward +40% (0.0252) with drift fix in place.

Still missing (deferred): crosswind echelons, lead-out trains, incidents/mechanicals, D-032 GC leadership, CdA Road/TT (D-055).
