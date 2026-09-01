# Career Season Rollover and Aging v0.1 (D-056)

**Status:** DRAFT (owner 2026-09-01: „mocno popchnąć grę do przodu” — infinite career is the biggest missing piece)  
**Authority:** `VISION.md` (manager career, living world), `DECISIONS.md` D-004 / D-036–D-038 / D-046 / D-047 / D-050, `LONG_SAVE_AND_PERFORMANCE_v0.2.md` (stable IDs, compaction later), `RIDER_PROFILE_AND_ROUTE_ENGINE_v0.1.md` (generator, ratings are views), `CAREER_WORLDTOUR_SLICE_v0.1.md`  
**Scope:** Domain + Application + Persistence + Content + SimRunner. **No Godot work** (the shell already shows dates, calendar, squad, market, finance — it must simply keep working in 2027). Lands **after** D-054 and D-055 on `main`; separate tree from both.

## 1. Player value
Today the world ends on 31 December 2026: no 2027 calendar, no new courses, no aging, riders whose contracts expire just vanish from clubs, AI clubs never re-sign anyone. A manager career is a **multi-season** game. After this slice the player can press Advance Day through New Year and get a real 2027: new courses inside the same race identities, a fresh pre-season plan, older riders declining, young riders growing toward POT, AI clubs keeping their squads alive, and the player's own contract situation mattering.

## 2. Locked principles
- Deterministic (`StableSeedDerivation`, labels below); no `new Random()`; same seed → same checksum after N seasons.
- Stable IDs never reused; retired riders stay in the world as history (`RiderCareer` keeps results), flagged retired, not deleted.
- Human and AI clubs use the **same** commands/rules; the AI renewal rule below is a world rule, not a `PlayerTeam` exception.
- Ratings stay derived; aging changes **physiology**, never the 1–99 numbers directly.
- No hidden luxury tax, no inflation (D-013). No sponsor market yet (out of scope). No God-eye: AI renewal uses only public facts (age, contract, results table, wage).
- Save schema bump is allowed once: **SchemaVersion 11** (after D-055's 10), checksum `peloton-world-checksum-v11`.

## 3. Season boundary
- `WorldState` gains `SeasonYear` (2026 at CreateWorld) and `SeasonStartDayNumber`. `CareerCalendarDates` already maps day → date; **31 Dec → 1 Jan** of `SeasonYear + 1` is the rollover day. Rollover runs inside `AdvanceOneDay` **after** the date increment, before finance/contract ticks, exactly once per year.
- Rollover steps, in this order (all deterministic):
  1. **Aging tick** (§4) for every living `RiderCareer`.
  2. **Retirements** (§4.3).
  3. **Contract cycle** (§5): expired contracts already fell off during the year; on rollover AI clubs run the renewal/sign rule so every AI club has ≥ 8 riders again (cap 30).
  4. **Courses 2027**: `CourseCatalogGenerator.GenerateSeason(identities, seasonYear + 1, …)` with seed label `course-catalog:{year}:{race.Id}` (already how it is labelled); store new `CourseProfile`s; **old profiles are kept** (results reference them).
  5. **Calendar 2027**: one `CalendarEntry` per racing stage, same race content ids (`race.wt2026.*` stay the identity ids; the *year* lives on the entry/course), dates shifted to the new year by the identity's calendar rule (same month/week pattern as the content calendar; simple rule: same day-of-year, clamped so stage races keep their length).
  6. **Entries**: `OrganizationRaceEntry` for the new year default to "entered, leader = null" for every club (like CreateWorld); the **player's** pre-season planning flow (`BeginPreSeasonPlanningCommand` …) reopens: `GameState` returns to the same pre-season state the New Game uses, with the desk showing „Plan sezonu 2027”. AI clubs keep default entries.
  7. **Season summary** inbox item (Polish): last season's top results of the employer, retirements from the squad, contracts ending this year. Item is dismissible.
- `FinancialYearDays` stays 365; cash is not reset.

## 4. Aging (physiology, not ratings)
Age = `SeasonYear − BirthYear` (riders without `BirthYear` get one at CreateWorld from the seed, uniform 20–34, label `birth-year:{riderOriginId}`; keep the content value when present).

### 4.1 Yearly multipliers (applied once at rollover)
```text
growth(age)   = age ≤ 22: +0.030 | 23–25: +0.018 | 26–28: +0.006 | 29–31: 0.000 | 32–34: −0.012 | 35–37: −0.025 | ≥ 38: −0.040
CP, W′, Pmax          ×= 1 + growth(age) · talentGate
LowIntensityDurability = clamp(+0.010/yr until 30, −0.010/yr after 33)
HighIntensityDurability = clamp(−0.005/yr after 31)
Positioning, Handling, TacticalAwareness += 0.010/yr until 30 (cap 0.98); unchanged after
BodyMassKg              unchanged (no diet sim)
CdARoad / CdATt         unchanged
```
`talentGate` for growth years (age ≤ 28): `clamp((PotentialOvr − currentOvr) / 15, 0.2, 1.0)` where `currentOvr` is the derived OVR view — the **only** place the view feeds back, and only as a brake so nobody overshoots their POT; never as a bonus. Decline years ignore `talentGate`. After the tick, `EnsurePotentialOvrAtLeast(currentOvr)` so POT never sits below reality.

Add a **deterministic per-rider variance** ±0.006 on growth from `StableSeedDerivation` label `aging:{year}:{riderId}` so two same-age riders do not age identically. No other randomness.

### 4.2 Form reset
`Form01 = 1.0`, `Freshness01 = 1.0`, `Fatigue01 = 0.0` on rollover (winter). Career results untouched.

### 4.3 Retirement
A rider retires on rollover when `age ≥ 40`, or `age ≥ 35 and currentOvr < 60 and no active contract`, or `age ≥ 33 and no active contract and no top-20 result in the last two seasons`. Retired riders: `IsRetired = true`, detached, never start, never renewed, still listed in results history. Clubs never fall under 8 because of §5.

### 4.4 Neo-pros (world does not shrink)
For every retirement, the world creates one new `RiderCareer` (age 19–21, archetype drawn deterministically from the retiring rider's club needs, physiology from the archetype's `neo` band in `content/peloton.wt-2026/README.md`, POT 65–90 from seed label `neo:{year}:{index}`), unattached, on the market. Names come from a small deterministic name pack (`content/peloton.wt-2026/names.json`: ≥ 60 first names, ≥ 60 surnames, by nationality group); stable id, never reused. Cap total living riders at 512 (content ceiling); if at cap, no neo that year.

## 5. Contract cycle (AI clubs behave; player decides)
- Player: nothing automatic. Expiring riders leave on their end day (already implemented). Inbox warns 60 and 30 days before each expiry (item exists? if not, add).
- AI clubs, on rollover only, run a **public-facts** rule, deterministic order by `OrganizationId`:
  1. **Renew** riders whose contract ends this coming season if `age < 35` and (rider is in club's top 8 by last season's points table **or** age ≤ 25). New end = +2 years (+1 if age ≥ 32). Wage: `max(current, min(1.6 × current, wageBand cap))` where the band comes from archetype; no inflation.
  2. **Sign** unattached riders until the roster has ≥ 8: pick by best `lastSeasonPoints` then youngest; wage = archetype band floor; 1-year deal (2 years if age ≤ 24).
  3. **Release**: none in this slice (no firing sim).
- Player is never auto-renewed or auto-signed. Same formula objects (`WageBands`) are exposed so the D-044 negotiation can show „rynek płaci X–Y” later (not this slice).

## 6. Player employment (thin, no firing yet)
`ManagerCareer` keeps its employment; no sacking sim this slice (D-007 later). The season summary tells the player how the club did vs last year (points table position of the club). No more.

## 7. Persistence and SimRunner
- SchemaVersion **11**: `SeasonYear`, `SeasonStartDayNumber`, `RiderCareer.IsRetired`, `BirthYear` materialized, course profiles per year, calendar entries with `SeasonYear`, names pack identity. Schema 1–10 saves may refuse to load (documented).
- SimRunner `day --scenario scenario.peloton.wt-2026 --days 400 --employer …` must cross New Year and print `season=2027`, `retired=N`, `neo=N`, `calendar2027=N entries`, `courses2027=N`.
- New SimRunner `seasons --scenario scenario.peloton.wt-2026 --years 5 --seed 91234`: runs five full seasons with delegated defaults (player skips races; AI auto-sim), prints per-season checksum, rider count, retired, neo, oldest rider age, and the three best OVR riders by year. Must finish (no exception) and be deterministic across two runs.

## 8. Probes (tests)
- Rollover happens exactly once per 365 days; day after 31 Dec 2026 is 1 Jan 2027; `SeasonYear` = 2027.
- Aging: a 21-year-old with POT 90 and OVR 70 gains CP; a 36-year-old loses CP; nobody's OVR exceeds POT after tick; retired riders never start.
- Courses 2027 exist for every race identity; 2026 profiles still resolve for old results; Roubaix 2027 is still `CobbleClassic`, TdF 2027 has ≥ 1 ITT.
- Every AI club has ≥ 8 riders on 2 Jan 2027; the player's club is untouched by the AI rule.
- Neo count == retirement count (unless at 512 cap); total living riders never shrinks.
- Save/load round trip at schema 11 after rollover preserves checksum.
- Five seasons via `seasons`: deterministic; no crash; ≥ 1 retirement and ≥ 1 neo in total; World checksum differs between seasons.

## 9. Out of scope
Firing/hiring the manager, sponsor market, training minigame, Continental promotion/relegation logic (data only, D-038), transfer fees, agents, knowledge stores, Godot screens (existing ones must keep working — run `Peloton.Client.Godot.Tests`). Watch off, Career Hub gone, §49 `NOT VERIFIED`.
