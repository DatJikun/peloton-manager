# peloton.wt-2026 — WorldTour 2026 content pack

Men's UCI WorldTour 2026 pack wired to `scenario.peloton.wt-2026`.

## Honesty labels

| What | Source / status |
|---|---|
| Team names, countries, sponsors, bikes | Public UCI licence list and team announcements |
| Rider names | Thin public identity layer (4 riders per team), not a licensed 28-rider UCI roster |
| Calendar dates | UCI 2026 WorldTour calendar (36 events) |
| Physiology (CP/W'/Pmax/mass/CdA) | **Estimated gameplay bands by archetype** (`archetype` on each rider). Not a UCI power dump. |
| Wages | **Estimated gameplay bands by wage role** (`wageBand` on each rider). Not one salary per club. |
| Org budgets | Estimated (`budgetBand` / `estimatedBudgetEur`). No longer multiplied onto rider CP or wages. |
| Route geometry | **Generated dense profiles** (~25 m samples) from `race-identities.json` at CreateWorld; stored on world. Skeleton soak still uses the proof circuit. |

Public 2026 source bands: `WT_2026_PHYSIOLOGY_AND_CONTRACTS_RESEARCH_2026-09-01.md` (research, not a lock). Do not treat pack numbers as a UCI dump.

Commercial licensing of real names and jerseys remains a later legal problem. The engine must still run on fictional packs (`scenario.peloton.skeleton`).

## Four-rider card (who is who)

Origin id `rider.wt2026.{team}.{slot}`:

| Slot | Job | Alpecin example |
|---|---|---|
| `.leader` | Designated captain. Default race leader. | Mathieu van der Poel (classics) |
| `.card` | Second protected rider (often the sprinter). Default support. | Jasper Philipsen (sprinter) |
| `.support-1` | Super-domestique / specialist helper | Søren Wærenskjold (diesel/TT) |
| `.support-2` | Depth domestique | Quinten Hermans (classics helper) |

Roster lists and default prep use that slot order — **not** alphabetical origin ids (`.card` would otherwise beat `.leader`) and not display-name order (Bauhaus before Buitrago).

## Physiology bands (gameplay)

Wide ranges. A named star is pinned inside the band; helpers jitter inside it. Race truth is still CP / W′ / Pmax / mass / CdA (`D-018`, `D-046`).

| `archetype` | mass kg | CP W | CP W/kg | W′ kJ | Pmax W | CdA |
|---|---|---|---|---|---|---|
| `super-gc` | 60–66 | 410–450 | 6.4–6.8 | 27–32 | 1050–1200 | 0.26–0.28 |
| `gc` | 60–67 | 378–418 | 6.0–6.5 | 24–28 | 950–1100 | 0.27–0.29 |
| `classics` | 70–78 | 368–445 | 5.4–5.9 | 30–34 | 1250–1500 | 0.28–0.30 |
| `sprinter` | 69–82 | 350–385 | 4.8–5.4 | 32–35 | 1540–1720 | 0.30–0.33 |
| `tt` | 75–82 | 395–430 | 5.2–5.6 | 22–26 | 1100–1400 | 0.23–0.25 |
| `super-domestique` | 60–68 | 370–400 | 5.5–6.2 | 24–28 | 950–1150 | 0.28–0.30 |
| `diesel` | 72–84 | 360–400 | 5.0–5.5 | 24–28 | 1100–1180 | 0.29–0.31 |
| `neo` | 61–64 | 370–392 | 5.6–6.2 | 24–27 | 1000–1080 | 0.28–0.30 |

Elite bunch-sprint Pmax is a **gameplay** 15 s cap (~22 W/kg), not a 1 s lab peak (~17 W/kg). Still below the old 1800 W / 24 W/kg copy-paste.

## Wage bands (gameplay EUR / year)

Not one number per team. Helpers stay in the hundreds of thousands; captains can be millions.

| `wageBand` | Typical job | EUR / year |
|---|---|---|
| `star` | Super-GC / monument captain | 3.5M–8.0M |
| `leader` | WT captain who is not the global rich list | 0.6M–2.5M |
| `sprinter` | Protected fast man | 0.18M–1.2M |
| `super-domestique` | Lieutenant / TT engine | 0.25M–1.2M |
| `neo` | Young talent | 0.08M–0.45M |
| `domestique` | Depth | 0.10M–0.25M |

Alpecin example: van der Poel €4.0M, Philipsen €1.2M, Wærenskjold €320k, Hermans €180k.

## Org budget bands

`organizations.json` `budgetBand` still labels club spending (sponsor fee). It does **not** rescale rider CP or wages.

| band | typical `estimatedBudgetEur` |
|---|---|
| elite | 28M–50M |
| high | 18M–22M |
| mid | 14M–16M |
| tight | ~12M (Picnic) |

## Prototype limits

- Official start lists are **event-shaped (D-049)**: Grand Tours 22×8=176, monuments 25×7=175, TDU 20×7=140, other WT 22×7=154. Each WT org has 8 riders (`.leader` `.card` `.support-1`…`.support-6`); extras are estimated. Wildcard ProTeams / Australia national start invited events only.
- Prototype race session is sequential 1-second `RaceSession.Step`; wall-clock is CPU-fast, not real-time.
- `GeneratePeriodicRaces` is false: the season is the 36 content calendar races only.
- Evenepoel 2026 is **Red Bull** (`.leader`); Roglič is Red Bull `.support-1`; Landa leads Soudal.

Division `WorldTour` and `licenceYearsRemaining` exist so a 3-year cycle and lower tiers can be added later. Living promotion/relegation is not in this pack.

Women's WorldTour is out of this pack.
