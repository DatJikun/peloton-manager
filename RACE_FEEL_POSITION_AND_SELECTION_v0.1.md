# Race Feel: Position and Selection v0.1 (D-054)

**Status:** DRAFT (owner agreed to the direction 2026-09-01; implementation contract)  
**Authority:** under `RACE_ENGINE_DESIGN_v0.2.md` (§13–§18, §30, §31, §33), `RACE_FEEL_FIELDS_AND_CLASSIFICATIONS_v0.1.md` (D-049), `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md` (D-046/D-047)  
**Scope:** prototype race engine (`Peloton.Simulation/Race`), official WT Simulate, probes. No schema bump, no new GameState, no Godot work, no Watch UI.

## 1. Player value

The owner review of `SimRunner compare --seed 91234` on today’s `main` shows three things a player sees at once:

1. **Paris–Roubaix top 5 = Evenepoel, Pogačar, Vingegaard, Roglič, Almeida.** The card says **Bruk** (D-046), the result ignores it. Classics riders (van der Poel, van Aert, Pedersen) are nowhere.
2. **Tour stage 1 sprint top 5 = four Alpecin riders** (Philipsen, van der Poel, Groves, Hermans). Milan, Merlier, Girmay exist in the pack and are not there.
3. **Hautacam: Pidcock wins, Pogačar outside the top 5.**

These are not tuning noise. They come from two prototype shortcuts:

- **The start grid is alphabetical.** `WorldRaceScenarioAssembler` orders starters by `OriginDefinitionId` and spaces them 0.7 m apart, so `rider.wt2026.alpecin.*` starts ~120 m ahead of the last team. `PositionAndGroupResolver` derives position slots from `DistanceM` only; `Positioning` is validated and then **never used**. Because every rider holds the same target pace, the bunch never reorders, so the finish (and the sprint kick head start) is the alphabet.
- **Pace is a constant.** `RaceSession.BasePaceMps(gradient)` is `11 − 70·gradient` m/s for everyone. Nobody sets a pace the others cannot hold, so there is no selection on cobbles or climbs. Winners are decided by who happens to fall below a fixed pace, not by who is strongest where it matters.

The fix is the design already written in `RACE_ENGINE_DESIGN_v0.2.md`: position is earned (§14), pace comes from riders (§20), dropping is required-vs-realizable (§17–§18), cobbles cost position and shelter, not just Crr (§33).

## 2. Locked principles (do not violate)

- Deterministic; no `new Random()`; same seed → same checksum and finish order; Spy OFF/ON identical.
- Dropping stays emergent (required power vs realizable power, gap, shelter). No `DropRider()`, no stamina-zero rule, no `Cobbles = +15` in a winner formula.
- Ratings (Climb … Bruk) remain **derived views**; the engine reads physiology (`CriticalPowerW`, `PeakPowerW`, `W′`, `CdAM2`, `BaseCrr`, masses, `Positioning`, `Handling`, durability), never the 1–99 numbers.
- Race decisions and DS knowledge stay observation-based; nothing in this contract adds hidden-truth reads to `TeamRaceObservation`.
- §49 stays `NOT VERIFIED`. Probes are feel guards for the developer, not the owner fun gate.

## 3. Positioning model (replaces the alphabetical grid)

### 3.1 Position score

```text
positionScore(rider) =
    Positioning                          // 0..1 from physiology
  + intentBonus(rider.Intent)
  + finaleBonus(rider, remainingM)
```

| Intent | intentBonus |
|---|---|
| `LaunchSprint` | +0.50 |
| `Attack`, `ForcePace` | +0.40 |
| `HoldPosition` | 0 |
| `Conserve` | −0.30 |

`finaleBonus`: in a classified `Flat` stage with `remainingM ≤ 3 000`, riders whose `PeakPowerW / BodyMassKg` is in the top quarter of the *group* get +0.25 (sprinters move up for the sprint; helpers do not). Elsewhere 0. Ties break on `RiderId`.

### 3.2 Start grid

`WorldRaceScenarioAssembler` (both `Assemble…` paths) orders the grid by `positionScore` at `Intent = HoldPosition` (i.e. by `Positioning`), descending, ties by `RiderId`, then applies the existing 0.7 m spacing. The fixture path used by SimRunner `race` / `watch` keeps its explicit `StartingPositions` from the prototype scenario JSON — do not touch the disconnected gate fixture.

### 3.3 In-group drift (every step)

After `ResolveGroups()`, for each group with ≥ 2 riders:

```text
slotTarget(rider)   = rank of rider inside the group by positionScore (0 = front)
targetDistanceM     = groupLeaderDistanceM − slotTarget · SlotSpacingM      // SlotSpacingM = 0.7
delta               = clamp(targetDistanceM − rider.DistanceM, −DriftMps·dt, +DriftMps·dt)  // DriftMps = 0.6
rider.DistanceM    += delta
```

- Drift is a bunch reorder, not propulsion: it does **not** change `SpeedMps`, `EnergySpentJ`, or physiology.
- **Drift is never a free tow (amendment 2026-09-01).** Forward drift (`delta > 0`) is allowed only for a rider who was **not power-limited in this step** (`realizablePowerW ≥ requiredPowerW`, i.e. he held the desired speed with capacity to spare). A rider who fell short of the group speed this step may drift **backward only** (`delta ≤ 0`). Without this rule a rider dropped on a surge is pulled back into the draft for nothing and selection cannot happen. Unit test: a rider whose realizable power is below the group requirement on a cobble segment leaves the group within 60 steps despite drift; an identical rider with enough power stays.
- Drift may never push a rider more than `GroupSplitGapM − 0.1` behind the rider ahead (no artificial splits) and never ahead of the group leader’s `DistanceM`.
- Apply drift before the next step’s physics so slot → shelter → required power uses the new order.
- `PositionAndGroupResolver` continues to assign `PositionSlot` / shelter from `DistanceM` order. The drift is what makes `Positioning` matter; the resolver does not need a second ordering rule.

### 3.4 Sprint kick uses positions, not the alphabet

No change to `BunchSprintResolver` thresholds. With 3.1–3.3 the lead group at 800 m has sprinters at the front by `Positioning` + `finaleBonus`, so the 250 m kick is decided by `PeakPowerW`, `CdAM2`, mass, and remaining `W′`, with a small head start for the best-positioned riders. Kick shelter stays 1.0.

## 4. Pace-setter selection (replaces the constant pace where it matters)

### 4.1 Selective zones

A step is in a **selective zone** when any of:

- `segment.Surface == Cobble`;
- `segment.Gradient ≥ 0.03`;
- `remainingM ≤ FinaleM` and `ClassifiedStageType ∈ { CobbleClassic, Hilly, Mixed, Mountain, MountainSummit }` with `FinaleM = 30 000`.

Classified `Flat` outside the last 3 km keeps the D-049 sit-in (fixed pace, capped gradient/wind, shelter 0.62); the sprint model is not changed. Unclassified races (skeleton proof circuit, `race-scenario.peloton.prototype-v0`) use the **gradient rule only**, so the skeleton soak and the SimRunner `race` gate stay close to today’s behaviour (goldens may still move — see §7).

### 4.2 Group target speed in a selective zone

For each group:

```text
tempoFactor      = remainingM ≤ FinaleM ? 1.00 : 0.92
setter           = rider in group maximizing sustainablePowerW / requiredPowerAtFront
                   where sustainablePowerW = effectiveCriticalPowerW · tempoFactor
                   (effectiveCriticalPowerW from CapabilitySolver on the current state)
setterSpeedMps   = BunchSprintResolver.SpeedForPowerW(
                       setter.sustainablePowerW, gradient, airDensity, wind, yaw,
                       setter.CdAM2, shelterMultiplier: 1.0,
                       EffectiveCrr(setter), setter.TotalMassKg)
groupTargetMps   = max(BasePaceMps(gradient), setterSpeedMps)
```

Then the existing per-rider loop runs unchanged: everyone tries `groupTargetMps` at their own slot shelter and Crr; those whose realizable power is below required lose speed (`RealizedSpeed`), the gap grows, shelter degrades, they drop (§17–§18). Intents (`ForcePace`, `Attack`, `Conserve`) keep their existing offsets on top of the group target.

Pick the setter deterministically: highest ratio, tie by `RiderId`. Do **not** let the setter “know” anyone’s hidden state; the setter rule is physics (who can physically ride fastest at the front), not a DS decision.

### 4.3 What this yields

- **Mountain / summit:** the setter is the best CP/kg rider; the group pace becomes his sustainable pace; heavier or weaker riders fall away gradually (gravity dominates). Pogačar / Vingegaard / Evenepoel decide Hautacam; sprinters lose minutes.
- **Cobbled classic:** on sectors the setter is the rider with the best absolute CP against cobble Crr and handling (§5); GC riders with low `Handling` and high W/kg but modest absolute watts are dropped or lose the wheel. In the last 30 km the finale tempo is full CP.
- **Hilly / Mixed finale:** selection on the last climbs, small group finish; no bunch sprint because `IsClassifiedEligible` already excludes these unless they are `Hilly`/`Mixed` with a flat run-in (then the D-049 sprint still fires for whoever is left — that is correct).

## 5. Bruk in the engine (cobble cost beyond Crr)

On `RouteSurface.Cobble` segments, in addition to the existing `EffectiveCrr(baseCrr, handling, surface)`:

1. **Shelter is harder to hold.** `shelterCobble = 1 − (1 − shelter) · (0.25 + 0.75 · Handling)`. A 0.93 handler keeps ~95 % of the draft; a 0.80 handler keeps ~85 %.
2. **Vibration / re-acceleration cost.** Required power is multiplied by `1 + CobbleSurgeCost · (1 − Handling)` with `CobbleSurgeCost = 0.22` (a 0.82 handler pays +4 %, a 0.93 handler +1.5 %). This is §33 “higher acceleration variance” collapsed into a deterministic per-second cost; keep it as one constant in `RaceTuning`.
3. Both apply to the setter’s own front cost in §4.2, so a poor handler is also a worse pace-setter on cobbles.

### 5.1 Amendment (2026-09-01, after the first implementation pass)
Items 1–2 alone leave Roubaix to light GC riders: with cobbles as “a bit more Crr”, a 61 kg rider with 425 W and CdA 0.25 is physically the fastest thing on the flat. Real sectors are different in three ways the model must carry (all §33 items: *increased effective Crr, positioning difficulty, higher acceleration variance*):

1. **Realistic cobble Crr.** `RouteSurface.Cobble` delta becomes **0.018** (was 0.0085) and the handling factor becomes `(1.60 − 1.00 · Handling)` (line choice: crown vs gutter). Rolling power then dominates aero on a sector (~200 W at 45 km/h), so **absolute watts** matter more than CdA there.
2. **Almost no draft on the stones.** Shelter multiplier on `Cobble` segments is `max(shelterCobble, 0.85)` — riders string out; nobody sits at 0.62.
3. **Sector surges.** Every transition asphalt→cobble and cobble→asphalt of the group’s reference rider (the rider used for pacing in §4.2) starts a **surge**: for `CobbleSurgeSeconds = 12` the group target speed is `+ CobbleSurgeSpeedMps = 2.5` above the §4.2 target. Each rider realizes it through `CapabilitySolver` at the 1-second step — so `PeakPowerW` and remaining `W′` cap who can follow, and `W′` is spent. Roubaix has ~30 sectors → ~60 surges; riders with small `W′` / low `PeakPowerW` (light GC) lose the wheel repeatedly and, with item 2, cannot hide afterwards. Surges are deterministic (from route geometry), never RNG.
4. **Durability.** No change: `CapabilitySolver` already degrades CP with `LowIntensityWorkJ`; classics archetypes carry higher `LowIntensityDurability` in the pack, which now matters over 250 km with surges.

The tuning band for these four constants is **±40 %**; the `Handling` in the CobbleClassic positioning scale already added by the implementation stays. Still no incident probability, no mechanicals, no equipment (deferred, §33 / §50). Roubaix probe stays as written in §6 (Pogačar in the top 5 is acceptable and realistic; Evenepoel/Vingegaard ahead of van der Poel is not).

## 6. Probes (tests that must pass; feel guards, not fun proof)

All on `scenario.peloton.wt-2026`, seed `91234`, CreateWorld readiness (same setup as `WorldTourFeelProbeTests` / SimRunner `compare`). Use archetype from the roster JSON via `OriginDefinitionId`.

| Probe | Assertion |
|---|---|
| Roubaix (`course.wt2026.roubaix.2026.s1`) | winner archetype is `classics`; ≥ 3 of top 5 are `classics`; van der Poel finishes ahead of Evenepoel **and** Vingegaard. |
| TdF stage 1 (`course.wt2026.tdf.2026.s1`, Flat) | top 5 span ≥ 3 organizations; ≥ 3 of top 5 are `sprinter`; Philipsen in top 3. |
| TDU stage 6 (Flat) | winner archetype is `sprinter` (keeps the D-049 sprint feel). |
| Hautacam (`course.wt2026.tdf.2026.s13`, Mountain) | winner archetype ∈ {`gc`, `super-gc`}; Pogačar in top 3; Philipsen outside top 100. |
| Determinism | two Simulate runs with the same seed give identical checksum and finish order; Spy OFF/ON identical (`spyNeutral=true`). |
| Positioning unit | with equal physiology, a rider with `Positioning 0.95` ends at a lower `PositionSlot` than one with `0.80` after 600 steps on a flat segment. |
| Cobble unit | `shelterCobble` and surge cost formulas (values above); `Handling 0.93` pays less than `0.80` on a `Cobble` segment at equal speed. |

Tuning constants (`DriftMps`, `SlotSpacingM`, `FinaleM`, `tempoFactor`, `CobbleSurgeCost`, bonuses) may be adjusted **within ±30 %** of the numbers here to make the probes pass; write the final values in `KNOWN_DIFFERENCE_FROM_CODE.md`. Do not add special cases for named riders or teams.

## 7. Goldens and honesty

- Skeleton soak (`run --years 10 --seed 91234`), SimRunner `race`, and `day` goldens **may change** (winner ids, checksums). Update them in the same tree with one line of justification per golden in the test; keep the same-seed equality assertions.
- Do not change `SchemaVersion`, `RaceResult` shape, `PhysicsContractVersion` semantics beyond bumping it to `2` (the finish order for the same input changes; that is what the version is for).
- Update `SimRunner compare` output only if a field is needed for the probes (e.g. `simTop5Archetypes=`). Keep `honesty=` line.
- `KNOWN_DIFFERENCE_FROM_CODE.md`: add a **D-054 landed** section (positioning drift, pace-setter, cobble cost; final constants; what is still missing: crosswind echelons, real lead-out trains, incident/mechanicals, D-032, CdA Road/TT).

## 8. Out of scope (do not start in this tree)

CdA Road vs TT (next task, D-055), lead-out trains as a team command, crosswind echelon model, incidents/mechanicals, D-032 GC leadership, training/aging, sponsor market, Watch UI expansion, Career Hub. §49 stays `NOT VERIFIED`.
