# peloton.wt-2026 — WorldTour 2026 content pack

Men's UCI WorldTour 2026 pack wired to `scenario.peloton.wt-2026`.

## Honesty labels

| What | Source / status |
|---|---|
| Team names, countries, sponsors, bikes | Public UCI licence list and team announcements |
| Rider names | Thin public identity layer (4 riders per team), not a licensed 28-rider UCI roster |
| Calendar dates | UCI 2026 WorldTour calendar (36 events) |
| Physiology (CP/W'/Pmax) | Estimated gameplay bands by role + org budget band |
| Wages and budgets | Estimated gameplay numbers (`budgetBand` multipliers) |
| Route geometry | **Synthetic prototype circuit** (`race-scenario.peloton.prototype-v0`), not real cobbles or climbs |

Commercial licensing of real names and jerseys remains a later legal problem. The engine must still run on fictional packs (`scenario.peloton.skeleton`).

## Prototype limits

- Official start lists are capped at **12 riders** (prototype engine limit), not a full UCI field.
- `GeneratePeriodicRaces` is false: the season is the 36 content calendar races only.

## Budget bands

| band | CP delta | wage multiplier |
|---|---|---|
| elite | +8 | 1.35 |
| high | 0 | 1.00 |
| mid | −8 | 0.75 |
| tight | −15 | 0.55 |

Division `WorldTour` and `licenceYearsRemaining` exist so a 3-year cycle and lower tiers can be added later. Living promotion/relegation is not in this pack.

Women's WorldTour is out of this pack.
