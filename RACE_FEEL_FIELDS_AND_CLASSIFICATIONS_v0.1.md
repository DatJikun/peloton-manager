# Peloton Manager — Sprint feel, real fields, classification jerseys

**Title:** Bunch sprint + UCI-shaped start lists + all classification jerseys  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Owner lock 2026-09-01 (same-day follow-up): classified Flat must be a bunch sprint; event fields must look like real WorldTour (teams × riders); stage races must show all jerseys; compare prototype results with real-life analogues (not a script).  
**Authority:** D-001, D-018, D-031, D-032 (still deferred as *mid-race* GC leadership), D-036, D-038, D-043, D-046, D-047, D-049, `RACE_ENGINE_DESIGN_v0.2.md` §30 / §45  
**Last reviewed:** 2026-09-01

---

## 0. For the owner (plain language)

You asked for four things at once:

1. **Sprinters win flat finishes.** Today the engine treats a “flat” as a long time trial of sustainable watts, so Pogačar beats Philipsen. That is wrong feel. A classified Flat (and a flat Grand Tour stage) should end as a **bunch sprint**: the fast men in the front group dump their last match, positioning and peak power matter, the GC engine sits in the wheels.
2. **Real field size.** UCI WorldTour: **7 riders per team** except Grand Tours (**8**). A monument is ~25 teams (18 WorldTour + wildcards) × 7 ≈ **175**. A Grand Tour is **22 × 8 = 176**. Tour Down Under is smaller: **20 × 7 = 140**. The 4 named “card” stars stay the protected roles; extra names are estimated domestiques / public identities, labelled estimated.
3. **All jerseys** after a stage: GC (yellow / ochre), points (green / sprint), mountains (polka), youth (white), team. Not mid-race leadership politics (that is still D-032, later). After the stage you can see who *wears* them.
4. **Compare with real life** (2025 and similar, not a 2026 script). We run the prototype and put the sim next to what actually happened so you can judge. D-001 stays: history is not a replay file.

We will **not** close §49. We will **not** rebuild Career Hub. Watch film stays optional and off. Skeleton soak and the standalone race checksum stay on the short proof circuit with 12 riders.

---

## 1. Locked for this slice (D-049)

- Official WT **start list size** is event-shaped, not “all 4-man cards of every club”.
- Physiology remains the race cause (D-018 / D-046). Sprint order is **not** `Winner = max(SprintStat)`.
- Classifications are **queries** over stored stage times + stage places + birth year + stage type. Prefer **no SQLite schema bump**. If a bump is unavoidable, it is SchemaVersion **9** / checksum `peloton-world-checksum-v9` with tests.
- D-032 (abandon GC leader mid-stage-race) stays **deferred**. Jerseys do not move DS leadership.
- Nine GameStates. No Watch expansion. No Career Hub. No AI managers.
- Commercial licensing of real extra names remains a later legal problem; extras are **estimated, labelled**.

---

## 2. Bunch sprint (feel #1)

### When it applies

A finish is a **bunch sprint** when **all** of:

- remaining distance ≤ **800 m** (and has been ≤ 800 m this step);
- mean gradient of the last **2000 m** of the stored course is **< 1.5%**;
- the lead group (riders within **3.0 s** or **15 m** of the race leader, same `GroupId` as the front) has **≥ 8** riders still racing.

Classified `Flat` stages must satisfy the gradient rule (Copenhagen, TdF Lille-style, TDU Adelaide circuit). Summit / mountain / ITT **must not** use this path.

### What the engine does

In `RaceSession`, for those lead-group riders, last 800 m:

- intent becomes an all-out launch (new `RaceCommandKind.LaunchSprint` **or** reuse `Attack` with a sprint-specific power target — pick one and test it);
- they spend remaining W′ toward `PeakPowerW`, capped by durability / current W′;
- drafting / slot / `Positioning` still apply (lead-out in the wheels is legal);
- riders **not** in the lead group do not teleport into the sprint.

GC engines in the bunch finish the sprint, but a calibrated sprinter (Philipsen, Bauhaus, Meeus, extra named fast men) must be able to beat Pogačar on that finish.

### Honesty

This is still the prototype (`dt = 1 s`). It is not a complete lead-out train game. It **is** enough that classified Flat is no longer a CP time trial.

### Tests (must fail today, pass after)

- `WorldTourFeelProbeTests`: classified Flat, seed `91234`: Philipsen **place < Pogačar place**. Log the places. Mountain probe **unchanged**: Pogačar ahead of Philipsen.
- Simulation unit test: synthetic flat last 2 km, 12-rider bunch, high-Pmax rider beats high-CP/kg climber.
- Spy OFF/ON still matches finish order. No `new Random()`. Skeleton race gate checksum **unchanged**:

```text
winner=1006
checksum=5A35E88103E2FBB40325EA8BEF15AAAC2F2E1AB70F4E6DE2BBCE584EC7EE6721
```

---

## 3. Real fields (feel #2)

### UCI numbers (source: UCI Part 2, WorldTour special provision)

| Event class | Riders / team | Teams (this pack) | Starters |
|---|---|---|---|
| Grand Tour (`tdf`, `giro`, `vuelta`) | **8** | 18 WT + **4** wildcards = 22 | **176** |
| Monument / cobbled classic (`roubaix`, `Flanders`, `msr`, `lombardia`, `liege`) | **7** | 18 WT + **7** wildcards = 25 | **175** |
| Tour Down Under | **7** | 18 WT + Israel + Australia national = 20 | **140** |
| Other WT one-day / stage race | **7** | 18 WT + **4** wildcards = 22 | **154** |

`startersPerTeam` and `inviteOrganizationIds` live on `race-identities.json` per `raceContentId`. Assembler starts **min(startersPerTeam, roster length)** per **invited and entered** org, captain-first (`RiderSquadOrder`), not alphabetically, not cap 12, not “every org on earth”.

World create: enter an org into a race only if invited (player Alpecin is invited to every WT event in this pack). Pre-season skip still works.

### Roster expansion

Each WT org: **8** riders.

```text
.leader .card .support-1 .support-2 .support-3 .support-4 .support-5 .support-6
```

Keep existing four. Add estimated extras with `archetype` / `wageBand`. Prefer public names where obvious (e.g. Red Bull: Welsford as a sprinter extra; Alpecin: Groves). Label estimated.

Wildcard orgs (ProTeam / national), 7 riders each, `division: ProTeam` or `National`:

- `organization.wt2026.israel` — Israel–Premier Tech  
- `organization.wt2026.tudor` — Tudor  
- `organization.wt2026.q36` — Q36.5  
- `organization.wt2026.totalenergies` — TotalEnergies  
- `organization.wt2026.cofidis` — Cofidis  
- `organization.wt2026.unibet` — Unibet Tietema / analogue  
- `organization.wt2026.australia` — national (TDU only)

`RiderSquadOrder.SlotRank`: `.support-3`…`.support-6` after `.support-2`. Skeleton hyphenated ids unchanged.

JSON catalog already allows 512 riders. Sequential 1 s `RaceSession.Step` stays; wall-clock may grow. Do not add a second engine.

### Tests

- TDU official simulate, seed `91234`: **140** starters; Alpecin has **7** in the result; Pogačar starts; Australia or Israel present.
- Roubaix: **175** starters.
- TdF stage 1: **176** starters.
- Skeleton soak / 12-rider races **unchanged**.
- CreateWorld still 18 WT employers; player employer remains Alpecin.

---

## 4. All jerseys (feel #3)

After each **stage** of a stage race (and as a no-op / empty for one-day except the result table):

| Jersey | How (derived) |
|---|---|
| **GC** | Sum `RiderStageTime` for that `RaceContentId`. DNF (no stage time) = **out of GC**. Ties: stage finish place, then `RiderId`. |
| **Points** | Stage-place points. Flat/cobble/hilly finish: 50-30-20-18-16-14-12-10-8-7-6-5-4-3-2-1 for places 1–16. Mountain/summit/ITT: 20-17-15-13-11-10-9-8-7-6-5-4-3-2-1. No intermediate sprints in this slice (honesty). |
| **KOM** | If the race session records **crest primes** (first rider over a local elevation max with gain ≥ 200 m from the last valley, or summit finish): 10-8-6-4-2. Else **fallback**: mountain/summit stage places 10-8-6-4-2. Label which path ran. |
| **Youth** | Same as GC, restricted to `seasonYear - birthYear <= 24` (U25 on 1 Jan). Season year = 2026 for this pack. Missing birth year = not youth. |
| **Team** | Each stage: sum of the **3 best** finish times of that org; then sum stages. |

One-day races: no jersey table (only the result). Combativity award is **out of scope**.

Queries (headless first):

- `ClassificationProjection` on `GameApplication` after a result: leaders + top 10 each jersey.
- SimRunner `day --through-results` prints `gc=` `points=` `kom=` `youth=` `team=` when the calendar race is a stage race.

Godot career shell: **thin** extra lines on the existing result table (jersey leaders). Do not rebuild Career Hub. Do not expand Watch.

Persist nothing extra if derived data is enough. `RiderStageTime` already stores times.

---

## 5. Compare with real life (feel #4)

File: `content/peloton.wt-2026/historical-comparisons.json` (research analogue, **not a lock**, **not a script**).

SimRunner:

```text
dotnet run --project tools/Peloton.SimRunner -- compare --scenario scenario.peloton.wt-2026 --seed 91234
```

Runs the listed cases (or a documented subset if a Grand Tour 21-stage loop is too slow — then TdF **stage 1 + one mountain stage + GC after those stages only**, labelled). Prints for each case:

- sim winner / top 5 (names + origin ids)
- real analogue winner / top 3
- field size sim vs real
- jersey leaders if a stage race
- a one-line verdict: `sprint_feel` / `climb_feel` / `classics_feel` / `mismatch` (heuristic, owner judges)

Do **not** force the sim to match the real names. D-001.

---

## 6. Out of scope

- Closing §49
- GPS traces
- Mid-race GC leadership transfer (D-032)
- Intermediate sprint points
- Fueling / thermal
- AI managers
- Career Hub / Watch expansion
- Licensed 28-man UCI dumps
- Changing the skeleton proof-circuit gate

---

## 7. File ownership for parallel Composer 2.5 work

| Stream | Owns | Must not touch |
|---|---|---|
| **Sprint** | `src/Peloton.Simulation/Race/**`, `tests/Peloton.Simulation.Tests/**`, feel-probe sprint assertion in `WorldTourFeelProbeTests.cs` | content JSON except if a test fixture is required |
| **Fields** | `content/peloton.wt-2026/**` (not `historical-comparisons.json` unless adding invite ids), `WorldRaceScenarioAssembler.cs`, `JsonScenarioCatalog.cs` (validation only), `RiderSquadOrder.cs`, Phase 5 field-count tests | RaceSession physics |
| **Jerseys** | `ClassificationQueries` / projections, SimRunner print, thin Godot result lines, tests for GC/points/KOM/youth/team | assembler start-list cap, RaceSession power |

Main agent owns this contract, `DECISIONS.md` D-049, `HANDOFF.md`, `DOCS.md`, `KNOWN_DIFFERENCE_FROM_CODE.md`.

---

## 8. Gate

From repo root, after code:

- `dotnet format --verify-no-changes`
- `dotnet build`
- `dotnet test`
- existing SimRunner skeleton + `race` checksum commands in `HANDOFF.md`
- `dotnet run --project tools/Peloton.SimRunner -- compare --scenario scenario.peloton.wt-2026 --seed 91234`

Feel probe log: `/opt/cursor/artifacts/wt-2026-feel-probe.log`  
Compare log: `/opt/cursor/artifacts/wt-2026-historical-compare.log`
