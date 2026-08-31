# peloton.wt-2026 — content draft (not wired)

Men’s UCI WorldTour 2026 pack. **Not loaded by CreateWorld yet.** Phase 1 of `CAREER_WORLDTOUR_SLICE_v0.1.md` binds the skeleton world to the race engine first.

## Honesty

| Field | Source quality |
|---|---|
| Team names, countries, UCI codes | UCI 2026–2028 WorldTeam licence list (public) |
| Licence length | 3 years except Picnic PostNL (1 year, extendable) |
| Bike / groupset | Public team equipment tables (may lag mid-season) |
| Calendar races and dates | UCI 2026 WorldTour calendar (36 events) |
| Physiology (CP/W'/Pmax), wages, budgets | **Estimated gameplay bands**, not official accounts |
| Stage-by-stage route profiles | Not in this draft; placeholders come later |

Real names are here because the owner asked. Selling the game with them is a later legal problem. The engine must still run on fictional packs.

Division `WorldTour` and `licenceYearsRemaining` exist so a 3-year cycle and ProTeam/Continental tiers can be added without a rewrite. Living promotion/relegation is not in this draft.

Women’s WorldTour is out of this pack.

## Ineos name

UCI licence text used **INEOS GRENADIERS**. Some 2026 start lists use **Netcompany–INEOS**. Content id is `organization.wt2026.ineos`. Display name follows the UCI licence string; `aliases` holds the other.
