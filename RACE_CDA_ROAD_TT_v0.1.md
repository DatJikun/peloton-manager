# CdA Road vs TT v0.1 (D-055)

**Status:** DRAFT (owner-ordered next race-engine task; implementation contract)  
**Authority:** `RACE_ENGINE_DESIGN_v0.2.md` §6–§7, §13, §32; `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md` (D-046); D-054  
**Scope:** rider physiology fields, content pack, race assembler, ratings, save schema. No Godot work beyond reading the existing rating projection.

## 1. Player value
The accepted engine wants two aero numbers per rider (§6–§7): `CdARoad` (hoods/drops in a bunch) and `CdATT` (TT bike, extension position). Today the prototype stores one `CdAM2` used everywhere, so a rider who is aero on a TT bike but sits up on the road (or the reverse) cannot exist, and the **TT** rating is a guess from road aero. Time trials in a Grand Tour must be decided by TT aero + CP, not by road-bunch aero.

## 2. Data
- `RiderCareer`: replace `CdAM2` with `CdARoadM2` and `CdATtM2`. Both `double`, positive. Save as two columns; **SQLite `SchemaVersion` 10**, checksum label `peloton-world-checksum-v10`; schema 1–9 saves may refuse to load (same policy as before, documented).
- Content (`roster.json`, skeleton roster, race-prototype riders): JSON keys `cdARoadM2` and `cdATtM2`. Loader accepts legacy `cdAM2` as a fallback for **both** values (so third-party packs do not break); the shipped packs are migrated to the two keys.
- WT 2026 pack calibration (estimated bands, D-038): `cdATtM2` = `cdARoadM2 × factor` where factor by archetype: `tt` 0.68, `super-gc` 0.72, `gc` 0.76, `classics` 0.80, `diesel` 0.80, `sprinter` 0.84, `neo` 0.82. Keep road values as they are. Named checks: Evenepoel and Ganna (if present) must have the lowest `cdATtM2` in the pack; a pure sprinter must be above every `tt`/`super-gc`.

## 3. Engine
- `RaceRiderProfile` carries both. `WorldRaceScenarioAssembler.ToRaceProfile` passes both; the race chooses per stage:
  - `IndividualTimeTrial` / `TeamTimeTrial` classified stages → `CdATtM2`, **no shelter** for ITT (shelter multiplier 1.0 always; riders start alone — see §4), team shelter allowed for TTT.
  - everything else → `CdARoadM2`.
- No mid-stage switching, no sit-up-on-climb model (D-054 notes: climbs are slow, CdA barely matters). One CdA per stage.
- ITT start: riders start 60 s apart in reverse order of the current GC (last GC rider first); if no GC (one-day ITT), reverse `RiderId`. Result = individual finish times; groups never form because no shelter. No pace-setter (D-054 §4) in ITT; each rider rides at `effectiveCriticalPowerW × 1.0` for the full distance (prototype pacing; §32 optimizer deferred), spending W′ only if the gradient demands it.
- TTT: team rides together at the pace of its 4th-fastest sustainable rider (UCI-style count); team time = 4th rider across the line. Keep it simple and deterministic.

## 4. Ratings (D-046 view)
`RiderRatingQueries.FromPhysiology`: **TimeTrial** rating uses `cdATtM2` (replace the `-cdAM2` term); Flat / Sprint / Hills keep road CdA. Update the named inequalities in `RiderRatingTests` if they move; Evenepoel TT must be ≥ 90 and ≥ Pogačar TT; Philipsen TT < 70.

## 5. Probes
On `scenario.peloton.wt-2026`, seed `91234`, CreateWorld readiness:
- Find the first classified `IndividualTimeTrial` stage in the stored 2026 calendar (assert one exists; if the generator produces none, fix the identity constraints so Tour/Giro/Vuelta have ≥ 1 ITT each — that is a D-047 identity requirement already listed). Simulate it: winner archetype ∈ {`tt`, `super-gc`}; Evenepoel top 3; Philipsen outside top 100; finish times strictly increasing (no ties within 0.01 s except identical physiology).
- Same-seed determinism + spy neutrality for the ITT.
- Save/load round trip at schema 10 preserves both CdA values and the world checksum.
- Legacy `cdAM2` loader fallback unit test.

## 6. Goldens / docs
- `PhysicsContractVersion` stays 2 unless road results change (they must not: road stages use the same road CdA as before — assert the Roubaix/TdF s1/Hautacam probes from D-054 still pass unchanged).
- `KNOWN_DIFFERENCE_FROM_CODE.md`: update “Prototype stores one CdA” lines to landed; add D-055 section (schema 10, ITT start model, TTT rule, deferred: aero tuck on descents, TT pacing optimizer, equipment).
- `HANDOFF.md`: SchemaVersion 10; CdA lines; “Next task” becomes owner playtest of the Windows release (no new system).
- `CODEBASE_MAP.md`: Persistence row schema 10; race engine row mentions ITT/TTT path.

## 7. Out of scope
Aero tuck on descents, TT pacing optimizer, equipment/wheel choice, crosswind echelons, D-032, aging, Godot UI beyond existing rating display.
