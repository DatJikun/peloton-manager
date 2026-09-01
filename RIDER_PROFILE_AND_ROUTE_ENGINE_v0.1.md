# Peloton Manager — Rider profile + route engine

**Title:** Rider profile (derived ratings) and detailed route engine  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Owner lock 2026-09-01: readable rider strengths/weaknesses, and a real course system (stored default routes + yearly generator). Not a five-chunk mock.  
**Authority:** D-018, D-022, D-042, D-046, D-047, `RACE_ENGINE_DESIGN_v0.2.md` R-003 / §§30–33  
**Last reviewed:** 2026-09-01

---

## 0. For the owner (plain language)

### What you see today, and why it looks weird

The engine does **not** currently store “Climbing 84 / Sprint 91”.

Each rider has **laboratory numbers** that the physics actually uses:

| Number | What it is | What it is good for |
|---|---|---|
| Critical power (watts) | Sustainable power, roughly “how hard can he go for a long time” | Long climbs, TT, hard flats |
| W′ (joules) | Extra energy above that sustainable level | Attacks, hills, the last kilometre |
| Peak power (watts) | Short explosion cap | Sprint, very steep ramps |
| Body mass (kg) | Weight | Watts per kilo on climbs |
| CdA | How “draggy” he is in the air | Time trial and fast flats |
| Durability | How much he fades after hours of work | Grand tours, long classics |
| Positioning / handling | Bike craft, not watts | Cobbles, corners, holding the wheel |
| Form / freshness / fatigue | **Today’s condition**, not talent | A tired star loses to a fresh domestique |

Those numbers **do** create real strengths and weaknesses — **if they differ**. A light rider with high watts/kg drops people on a long climb. A heavy rider with huge peak power wins a bunch sprint. A low-CdA rider beats a stronger engine in a time trial.

**Right now they barely differ.** The 2026 WorldTour pack mostly copy-pasted the same physiology with tiny budget tweaks. Pogačar and a sprinter look like cousins in the data. That is why it does not read as “this guy is a climber”. That is a content bug, not a design that “stats don’t matter”.

We will **not** replace the physics with Football Manager magic ratings as the thing that decides races (`Climbing = 84` must not be the hidden cause — D-018 / R-003).

We **will**:

1. Recalibrate the pack so a climber, a sprinter, a classics rider, and a time-trialist are actually different people in the lab numbers.
2. Show you **normal 1–99 ratings** derived from those numbers: Climb, Hills, Flat, TT, Sprint, Cobbles, plus OVR and POT.
3. Keep form/freshness/fatigue as day condition, not career talent.

You manage people. You should be able to see “strong on long climbs, weak in a bunch sprint” without opening a sports-science paper.

### Routes

Today every WorldTour race uses the same **5.4 km three-piece proof circuit** (flat / climb / crosswind). That is a physics spike, not a Tour de France.

What we are building instead:

- A **course** is a dense elevation/surface polyline (about **25 m** between points), not five labelled blobs.
- **Default 2026 courses** are generated at New Game and **saved in the world**.
- Each later year the same race **identity** (Tour de France still has about 21 stages, this many time trials, this many mountain days, …) produces a **new** realistic route under those constraints.
- Stage races become **one racing day per stage**. Rest days exist as calendar gaps, not fake stages.
- Official Simulate uses **that day’s stored course**, not the proof circuit.

Skeleton soak stays on the short proof circuit so the 10-year determinism gate remains a short race.

---

## 1. Locked decisions this document implements

- **D-046** — Player-facing ratings are derived from physiology. Physiology remains the race cause.
- **D-047** — Detailed course profiles + identity-constrained yearly generator. Defaults persist in the world. Not a five-fragment mock.
- **D-018 / R-003** — No magic `Climbing` stat as the engine’s result generator.
- **D-022** — Prototype still uses CP / W′ / Pmax / durability. Glycogen/thermal stay deferred.
- **D-042** — All / Guessed / None still filter what you **see**. They do not change how the engine races.
- **D-043** — Simulate + results. Do not expand Godot Watch Race.
- **D-031** — Still nine GameStates. No course-editor GameState.
- **D-008** — RaceLive remains one stage / racing day.

---

## 2. Player-facing ratings (D-046)

### 2.1 What is stored vs what is shown

**Stored (race truth)** — unchanged set, plus one new field:

```text
CriticalPowerW, WPrimeCapacityJ, PeakPowerW, WPrimeRecoveryJPerSecond
LowIntensityDurability, HighIntensityDurability
BodyMassKg, SystemMassKg, CdAM2, BaseCrr
Positioning, Handling, TacticalAwareness
Form01, Freshness01, Fatigue01, Loyalty01
PotentialOvr          // NEW, integer 1–99, developmental ceiling of OVR
```

**Shown (derived, never fed back into physics):**

```text
Climb, Hills, Flat, TimeTrial, Sprint, Cobbles   // each 1–99
OVR                                               // 1–99 composite
POT                                               // stored PotentialOvr
```

Forbidden:

```text
raceWinner = argmax(Climb)          // illegal
physics.criticalPower = f(Climb)    // illegal — ratings are a view, not a source
```

### 2.2 Closed derivation (no RNG)

Let

```text
cpPerKg   = CriticalPowerW / BodyMassKg
pmaxPerKg = PeakPowerW / BodyMassKg
```

Helper (deterministic):

```text
Scale01(x, min, max) = clamp((x - min) / (max - min), 0, 1)
Score(x, min, max)   = round(1 + 98 * Scale01(x, min, max))   // 1–99
```

Bands are WorldTour-adult gameplay bands (estimated, not licensed lab tests):

```text
Climb = round(
    0.55 * Score(cpPerKg,               4.80, 6.55) +
    0.20 * Score(LowIntensityDurability, 0.70, 0.98) +
    0.15 * Score(1.0 / BodyMassKg,       1/82, 1/56) +
    0.10 * Score(WPrimeCapacityJ,        18000, 32000))

Hills = round(
    0.35 * Score(cpPerKg,                5.00, 6.30) +
    0.30 * Score(WPrimeCapacityJ,        20000, 35000) +
    0.20 * Score(pmaxPerKg,              12.0, 22.0) +
    0.15 * Score(HighIntensityDurability, 0.70, 0.96))

Flat = round(
    0.40 * Score(CriticalPowerW, 340, 430) +
    0.25 * Score(-CdAM2,        -0.34, -0.24) +
    0.20 * Score(Positioning,   0.55, 0.95) +
    0.15 * Score(PeakPowerW,    850, 1600))

TimeTrial = round(
    0.45 * Score(CriticalPowerW, 350, 440) +
    0.40 * Score(-CdAM2,        -0.34, -0.22) +
    0.15 * Score(-BaseCrr,      -0.0055, -0.0034))

Sprint = round(
    0.40 * Score(PeakPowerW,    900, 1800) +
    0.25 * Score(WPrimeCapacityJ, 20000, 38000) +
    0.20 * Score(Positioning,   0.55, 0.95) +
    0.15 * Score(pmaxPerKg,     13.0, 24.0))

Cobbles = round(
    0.30 * Score(Handling,      0.50, 0.95) +
    0.25 * Score(Positioning,   0.55, 0.95) +
    0.20 * Score(BodyMassKg,    62, 82) +
    0.15 * Score(HighIntensityDurability, 0.70, 0.96) +
    0.10 * Score(PeakPowerW,    900, 1600))

sortedDesc = sort(Climb, Hills, Flat, TimeTrial, Sprint, Cobbles) descending
OVR = round(0.55 * sortedDesc[0] + 0.45 * mean(sortedDesc[0..2]))
```

Clamp every displayed integer to `[1, 99]`.

`PotentialOvr` is stored. Invariant: `PotentialOvr >= OVR` after create (raise stored POT if the derived OVR would exceed it). Development that grows physiology toward POT is **out of this slice**.

Form/freshness/fatigue do **not** change the 1–99 career ratings. They already scale CP/Pmax at assemble time (`ComputeReadiness`). Day condition is a separate sentence in the UI/CLI (“tired”), not a fake drop of Climb from 92 to 71.

### 2.3 Visibility (D-042)

Query: `RiderRatingProjection` (and `ClubRosterEntry` for the employer).

| Mode | Own club | Other riders |
|---|---|---|
| All | exact | exact |
| Guessed | exact | each rating as `[clamp(v-4,1,99), clamp(v+4,1,99)]`; OVR/POT same width. No extra RNG. |
| None | exact | ratings omitted; results remain public evidence |

Headless `ClubRosterProjection` is the employer roster → always exact.

### 2.4 WorldTour content calibration (required)

`content/peloton.wt-2026/roster.json` must stop being a copy-paste. Every rider gets an **archetype** whose lab numbers match the public identity well enough for gameplay (estimated, not a licensed dataset).

Role slots:

| Slot suffix | Typical job |
|---|---|
| `.leader` | The named star; archetype from that person |
| `.card` | Second protected rider (often sprinter or second GC) |
| `.support-1` / `.support-2` | Distinct jobs: climber helper, lead-out, diesel, classics, TT, young talent — **not clones** |

Hard tests after calibration (CreateWorld `scenario.peloton.wt-2026`):

- Pogačar (`rider.wt2026.uae.leader`) Climb > Philipsen (`rider.wt2026.alpecin.card`) Climb by at least 12.
- Philipsen Sprint > Pogačar Sprint by at least 12.
- van der Poel (`rider.wt2026.alpecin.leader`) Cobbles > Almeida (`rider.wt2026.uae.support-1`) Cobbles by at least 8.
- Pogačar TimeTrial > Philipsen TimeTrial.
- Pogačar OVR ≥ 88. Philipsen Sprint ≥ 88.
- A team’s four riders are not within 3 OVR of each other with identical rating shapes (max pairwise cosine-ish: at least two ratings differ by ≥ 8).

Skeleton roster physiology **may stay** as the proof pack (do not churn the 10-year soak finish order unless required). Add `potentialOvr` to skeleton JSON (default 75–92 by role). Derived ratings still work on skeleton numbers.

Suggested lab bands (gameplay, not science papers):

| Archetype | kg | CP W | W′ kJ | Pmax W | CdA | notes |
|---|---|---|---|---|---|---|
| Elite GC climber (Pogačar-like) | 64–67 | 410–425 | 28–32 | 1050–1150 | 0.27–0.29 | high W/kg, high durability |
| Elite sprinter (Philipsen-like) | 72–76 | 365–385 | 30–36 | 1550–1750 | 0.30–0.33 | positioning ≥ 0.90 |
| Classics / punch (MvDP-like) | 69–72 | 395–415 | 30–34 | 1300–1500 | 0.28–0.30 | handling ≥ 0.90, cobbles |
| TT specialist | 70–74 | 400–420 | 22–26 | 950–1100 | 0.22–0.25 | low Crr |
| Super-domestique climber | 58–63 | 370–395 | 22–26 | 900–1050 | 0.28 | |
| Diesel rouleur | 74–80 | 380–400 | 24–28 | 1000–1150 | 0.29 | |
| Young talent | slightly below leader | | | | | POT 4–12 above OVR |

Apply budget-band CP deltas **after** choosing the archetype, not instead of an archetype.

### 2.5 CLI / projections

- `ClubRosterEntry` includes the six ratings, OVR, POT, plus existing wage/loyalty.
- SimRunner `day` hub prints a compact roster line: `name climb=.. hills=.. flat=.. tt=.. sprint=.. cobbles=.. ovr=.. pot=..`.
- No Godot rider card in this slice.

---

## 3. Route engine (D-047)

### 3.1 What a course is

A **course** is a first-class world object: `CourseProfile`.

```text
CourseProfile
    CourseProfileId              // WorldEntityId, never reused
    OriginDefinitionId           // e.g. course.wt2026.tdf.2026.s12  (stable for the season)
    RaceContentId                // event: race.wt2026.tdf
    SeasonYear                   // 2026
    StageIndex                   // 1-based; 1 for one-day races
    Name                         // "Tour de France — Stage 12"
    Kind                         // Road | IndividualTimeTrial | TeamTimeTrial
    Country
    SampleSpacingM               // 25
    Samples[]                    // polyline vertices
    Derived metrics (cached, must match a recompute)
        LengthM, ElevationGainM, ElevationLossM
        CobbleM, GravelM, MaxGradient, MinGradient
        ClassifiedStageType
```

**Sample vertex** (the system of record):

```text
DistanceM          // 0, 25, 50, ... LengthM
ElevationM
WidthM             // 3.0–8.0 typical
HeadingDegrees     // 0–360, road direction
Surface            // Asphalt | Cobble | Gravel | WhiteRoad
Curvature01        // 0 straight … 1 hairpin
Exposure01         // 0 sheltered … 1 ridge / sea / plateau
```

Gradient is **derived** from neighbouring elevations. Do not store a second conflicting gradient.

Native spacing: **25 m**. A 180 km stage has ~7201 vertices. A 250 km classic has ~10 001. That is the product. Not five fragments.

Wind is **not** baked into the catalog. Race-time weather (deterministic from race seed) plus `HeadingDegrees` and `Exposure01` produce headwind/crosswind for the compiler.

### 3.2 Stage type is derived

`ClassifiedStageType` is computed from the profile. Author constraints request a type; the classifier must agree after generation (reject and retry with the next subseed, bounded).

```text
IndividualTimeTrial
TeamTimeTrial
Flat
Hilly
Mountain
MountainSummit      // mountain + finish in the last 3 km is still climbing
CobbleClassic
Mixed
```

Classifier rules (closed):

```text
if Kind is ITT/TTT → that type
else if cobbleM / lengthM ≥ 0.12 → CobbleClassic
else if elevationGainM ≥ 2800 and maxElevationInLast3km is within 80 m of stage max
        and last 3 km mean gradient ≥ 0.04 → MountainSummit
else if elevationGainM ≥ 2800 or (elevationGainM ≥ 2000 and longest climb ≥ 8 km at mean ≥ 0.06)
        → Mountain
else if elevationGainM ≥ 1200 or count(climbs with length≥2 km and mean≥0.04) ≥ 4 → Hilly
else if elevationGainM < 800 and cobbleM < 8 km → Flat
else → Mixed
```

A **climb** for classification: uninterrupted run with mean gradient ≥ 0.04, length ≥ 1000 m, elevation gain ≥ 50 m.

### 3.3 Compiler → race engine

`CourseCompiler.ToRaceDefinition(profile, weather)`:

1. Consecutive vertices become `RaceRouteSegment` edges of 25 m (last vertex has no edge).
2. Gradient = Δelevation / 25.
3. `RoadWidthM` = sample width.
4. Wind: `yaw = Weather.WindFromDegrees - HeadingDegrees` (normalised); speed *= Exposure01 * 0.65 + 0.35.
5. Surface travels on the segment (`RaceRouteSegment.Surface` + extra Crr).

`RaceRouteSegment` gains `Surface`. Rolling resistance at step time:

```text
surfaceDelta =
    Asphalt   0
    WhiteRoad 0.0025
    Gravel    0.0050
    Cobble    0.0085

effectiveCrr = rider.BaseCrr + surfaceDelta * (1.35 - 0.50 * rider.Handling)
```

High handling pays less on cobbles. This is how handling becomes a real cobbled strength without a `Cobbles` magic stat in the winner formula.

**`RaceDefinition.SegmentAt` must be O(log n)** (prefix sums + binary search). Linear scan over thousands of edges per rider per second is illegal. Add a test: 10 000-edge profile, 20 000 lookups, finishes quickly.

`MaximumDurationSeconds` for a compiled course:

```text
max(3600, ceil(LengthM / 3.0) + 1800)
```

Do **not** copy prototype scripted `RaceCommand` second-offsets onto generated courses. WT/career path already prefers strategy-only plans.

### 3.4 Bricks (generator internals, not the saved course)

The generator **composes** detailed polylines from a **brick library**. Bricks are 3–20 km of **already-dense** 25 m samples with realistic gradient noise, not labels like `kind=mountain`.

Required bricks (deterministic from brick id + local seed):

| Brick | What it actually builds |
|---|---|
| `FlatRoad` | Gentle undulation ± 8 m, occasional 400–800 m false ramps at 2–3% |
| `Rolling` | Repeated 1–3 km rises at 3–6% and descents |
| `Climb` | Named shape: length, mean gradient, roughness; 25 m variation, steep ramps, false flats, hairpin curvature on the steepest third |
| `SummitFinish` | Climb brick whose last vertex is the finish |
| `Descent` | Inverse of a climb with higher curvature, narrower width |
| `ValleyConnector` | False-flat 1–2% roads between massifs |
| `CobbleSector` | 0.8–3.5 km cobbles, width 3.0–4.2 m, lumpy elevation ± 4 m |
| `Berg` | Short Flemish wall: 400–1400 m at 7–12% |
| `IttOutAndBack` | Smooth, wider, lower curvature, distance 8–40 km |
| `CoastalExposed` | Flat/rolling with Exposure01 ≥ 0.75 |

Iconic shapes the library must include as parameterised climbs (not one 5% slab):

- long HC: ~13.8 km @ ~8.1% with ramps to 11–12% (Alpe-like)
- Pyrenean: ~18 km @ ~7.5%
- Alpine wall: ~7 km @ ~9.5%
- punch: ~2.4 km @ ~8.5% (Cipressa-like)
- very late punch: ~3.7 km @ ~6.5% then short steep (Poggio-like)

Gradient noise: bounded, seedable, mean gradient of the brick stays within ±0.004 of the requested mean.

The **saved** `CourseProfile.Samples` is the concatenation after smoothing a 200 m blend (linear elevation) at brick joins. The player/engine never sees “5 fragments”; they see thousands of points.

### 3.5 Race identity constraints

Content file: `content/peloton.wt-2026/race-identities.json`.

Each WorldTour event has:

- `raceContentId`, `kind` (`oneDay` | `stageRace` | `grandTour`)
- `racingStageCount`, rest days implied by calendar inclusive span
- numeric ranges: ITT count, TTT count, mountain, hilly, flat, summit finishes, total km, cobble km
- `terrainPalette` (brick families allowed)

Locked Grand Tour bands (gameplay, TdF-shaped):

| Identity | Racing stages | ITT | TTT | Mountain | Hilly | Flat | Summit finishes | Total km |
|---|---|---|---|---|---|---|---|---|
| Tour de France | 21 | 1–3 | 0–1 | 7–9 | 4–6 | 6–10 | 3–5 | 3200–3600 |
| Giro d'Italia | 21 | 1–3 | 0–1 | 7–10 | 4–7 | 5–9 | 3–6 | 3300–3600 |
| Vuelta | 21 | 1–3 | 0–1 | 6–10 | 5–8 | 5–9 | 3–6 | 3100–3500 |

Tour Down Under: 6 stages, 0 ITT required (0–1 allowed), 1–2 mountain/hilly, rest flat/rolling, 700–850 km.

Monuments / one-day (distance and character):

| Event | Type | Distance km | Extra |
|---|---|---|---|
| Milano–Sanremo | Mixed/flat + late punches | 280–300 | Cipressa-like + Poggio-like in last 30 km |
| Ronde van Vlaanderen | CobbleClassic | 260–275 | ≥ 16 berg/cobble sectors |
| Paris–Roubaix | CobbleClassic | 250–265 | cobble 50–65 km |
| Liège–Bastogne–Liège | Hilly | 250–265 | many 2–4 km walls, no cobbles |
| Il Lombardia | Hilly/Mountain | 230–255 | late long descent + punch |
| Strade Bianche | Mixed | 200–220 | white roads 50–70 km |

Fill the other WT events with honest bands in the JSON (UAE Tour = desert flats + 1–2 summit finishes, etc.). Do not leave “use proof circuit” as a fallback for any `race.wt2026.*` event.

### 3.6 Yearly generator

```text
CourseCatalogGenerator.GenerateSeason(
    identities,
    calendarRaces,     // dates
    seasonYear,
    seed) -> IReadOnlyList<CourseProfile>
```

Determinism: `StableSeedDerivation` stream `course-catalog` / year / raceContentId / stageIndex. Same inputs → identical samples.

Rules:

- Place `racingStageCount` stages on calendar days from `start` to `end`.
- Rest days = inclusive span − racing stages. Never rest on the first or last day. Spread remaining rest days deterministically (split the stage list into equal blocks).
- Draw a stage-type sequence that **satisfies the ranges** (not a single exact recipe every year). Year 2026 and year 2027 must both validate and must **not** be sample-identical for TdF stage 1.
- ITT/TTT days use `IttOutAndBack` (TTT may be slightly wider).
- Mountain days use the event’s mountain palette; summit finishes are a subset of mountain days.
- Total distance across stages must land in the identity band; if not, scale valley connectors (not climb means) and regenerate once.

Validation throws if a season catalog fails identity checks. Tests cover TdF, Roubaix, TDU, MSR.

### 3.7 Persistence and calendar

SQLite **SchemaVersion 8**. Checksum label `peloton-world-checksum-v8`. Schema 1–7 may refuse to load.

World create (`scenario.peloton.wt-2026`):

1. Generate 2026 catalog from identities + calendar + world seed.
2. Persist every `CourseProfile` (samples included).
3. Emit **one `CalendarEntry` per racing stage** (not one per event).
   - `RaceContentId` stays the **event** id (`race.wt2026.tdf`) so pre-season entry is still per event.
   - New fields: `StageIndex`, `CourseProfileId`.
   - Title includes the stage: `Tour de France — Stage 12`.
4. `OrganizationRaceEntry` remains per event id (enter the Tour, you race every stage).

Skeleton worlds: **no** generated catalog. Calendar unchanged. Official skeleton races still use `race-scenario.peloton.prototype-v0`.

Assembler: if today’s calendar entry has a `CourseProfileId`, compile that profile (weather from race seed) and replace `template.Route` / duration. Starters still come from world careers. Prototype three-segment route remains the standalone `race` / `watch` **gate** fixture only.

### 3.8 Multi-stage without full GC board game

This slice **does** race every stage on its own course.

Thin classification (required so a six-day TDU is not amnesia):

```text
RiderStageTime  (event RaceContentId, stageIndex, riderId, finishTimeSeconds)
```

Query GC as sum of times for that event (DNF = no GC, or large penalty — pick **DNF out of GC**). Store on world, persist schema 8. No yellow-jersey physics, no D-032 leadership transfer.

### 3.9 Non-goals

- Godot map renderer beyond the existing optional Watch overlay
- Rebuilding Career Hub
- Real GPS traces of the 2026 UCI routes (legal/data). The generator is the 2026 default.
- 150-rider pelotons (starter cap 12 stays)
- Closing §49
- Full fueling/thermal physiology
- A player-facing route editor screen
- AI managers (D-041)

---

## 4. Acceptance tests

Ratings:

- Closed formula matches a hand-calculated fixture rider.
- WT CreateWorld: Pogačar vs Philipsen inequalities in §2.4.
- Guessed ranges hide exact rival OVR; All shows it; None omits it.
- Own club roster always exact.
- Ratings do not change after Advance Day (form ticks, ratings stay).
- Confirming a contract / poach does not change ratings.

Routes:

- TdF 2026 catalog: 21 stages, constraint band holds, every stage ≥ 200 samples (ITT may be shorter distance but still 25 m resolution, so an 8 km ITT has ~321 vertices).
- No WT stage has fewer than `LengthM/25 - 1` edges after compile (no 5-segment collapse).
- Roubaix cobble km in band; classifier `CobbleClassic`.
- Year 2026 vs 2027 TdF stage 1 samples differ; both validate.
- `SegmentAt` binary-search test on 10 000 edges.
- Handling reduces cobble Crr (unit test on the formula).
- WT TDU stage 1 official Simulate uses the stored TDU stage-1 profile length (kilometres, not 5400 m).
- Skeleton 10-year soak still runs; uses proof circuit; same-seed equality; update checksum label v8 / hex if the label or `PotentialOvr` changes the hash.
- Schema 8 save/load round-trips a WT world including samples and GC times after one stage.

Engine:

- A compiled hilly course of ≥ 40 km with ≥ 1600 edges finishes `RunBatch` (12 starters) without exceeding duration.
- Climber-like profile beats sprinter-like profile on a long summit-finish fixture (same seed); sprinter-like beats climber-like on a flat 8 km finishing straight fixture. This proves physiology + route, not ratings, decide races.

---

## 5. Implementation notes

- New code: `Peloton.Simulation/Course/` (profile, classifier, bricks, generator, compiler). Domain holds `CourseProfile` + calendar extras. Application wires assembler + rating queries.
- `WorldChecksum` includes catalog origin ids, stage counts, and a hash of every sample (or an equivalent canonical binary dump). Same seed must match.
- Do not generate catalogs inside every unit test that only needs ratings — ratings tests can CreateWorld WT (catalog will run once per test; keep generator cheap: milliseconds, not seconds).
- `dotnet format`, `dotnet build`, `dotnet test`, then HANDOFF SimRunner commands. Skeleton soak must stay on the short circuit.
- Two logical git commits if practical: (1) ratings + WT calibration, (2) course engine + calendar-per-stage. Do not expand Watch UI.

## 6. Schema

SQLite SchemaVersion **8**. Checksum `peloton-world-checksum-v8`.
