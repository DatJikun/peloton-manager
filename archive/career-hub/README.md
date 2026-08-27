# Career Hub HTML prototypes

**Status:** ARCHIVED — owner rejected this KPI dashboard. Do not revive as the product Hub.

**Status (original):** throwaway presentation prototypes (not Godot, not gameplay code)  
**Authority:** `UI_SITEMAP_v0.1.md` (PR #2, DRAFT), `ARCHITECTURE.md` §16 HQ / §72 season rail, D-004, D-006, D-009  
**Purpose:** let the owner *see and click* the main dashboard before `GAME_STATES` / Godot.

These files are **static HTML**. They emit no Application Commands. Sample names, dates, and numbers are fiction for layout. They must not be treated as simulation truth, save format, or a locked visual identity.

## What to open

| File | Variant |
|---|---|
| `index.html` | Index / chooser |
| `hub-employed.html` | Employed Career Hub (primary) |
| `hub-unemployed.html` | Unemployed Career Hub (explores OQ-UI-002: dedicated shell, not empty org panels) |
| `hub-decision-interrupt.html` | Same employed Hub with a **blocking** Decision Request overlay after Advance Day (explores OQ-UI-001) |

Open any of the three hub files in a browser, or start a local static server from this folder.

## Layout contract (all variants)

Persistent Management shell, **not** Card Flow:

1. **Left nav** — HQ (active), Inbox, Calendar, Squad, Recruitment, Organization, World, Manager, Settings. Non-HQ items may toast “prototype: screen not built”.
2. **Top bar** — world date, organization (or “Unemployed”), manager name, Inbox badge, Save/Load (disabled with tooltip: not RaceLive; just not prototyped).
3. **Season context rail** — `PRE-SEASON → SPRING → GIRO → TOUR → VUELTA → WORLDS → OFF-SEASON` with a `TODAY` marker. Orientation only (ARCHITECTURE §72).
4. **Primary action** — large **ADVANCE DAY** control (D-006).
5. **Main columns**
   - **Problems / risks** — each item has STATE / WHY (and FORECAST when a recommendation exists). No silent “process inactive”.
   - **Next events & deadlines** — calendar-ish list with provenance (“why this is on the calendar”).
   - **Staff recommendations** — sourced, confidence, not hidden truth. Player can dismiss visually only.
   - **Feed** — NotificationProjection snippets. Label that the feed is **not** the system of record (ARCHITECTURE §17).
6. **Finance strip** — STATE / WHY / FORECAST; **cash ≠ budget** (cash, committed costs, free operating budget as separate figures).
7. Knowledge-bounded copy: scout/coach text is interpretation; no `truePotential`, no rival W′, no PlayerTeam.

## Interactions that must work

**Employed**

- Click **ADVANCE DAY** → world date advances by one day; feed prepends at least one world event the player did not “open a screen” to cause; problems/deadlines can shift.
- Click a problem or deadline → detail panel (right drawer or inline) with STATE/WHY/FORECAST and a primary CTA that is clearly a *stand-in* for an Application Command (e.g. “Open Recruitment Case — prototype”).
- Inbox badge is visible; clicking Inbox toasts or switches a local panel, not a fake mail database.

**Unemployed**

- No org finances, roster, or recruitment workload.
- Hub emphasises job market, personal reputation, public world results.
- Copy states that former-employer confidential knowledge is **not** shown (D-009).

**Decision interrupt**

- First **ADVANCE DAY** opens a modal Decision Request (example: sponsor deadline or race briefing required) **without** changing the calendar date (mid-day stop).
- Modal has Respond / Delegate-if-shown / Open Inbox. Closing without a choice leaves the request pending.
- Second Advance Day after dismissing is not required; resolving the modal returns to the Hub on the **same** date.

## Visual

Desktop-first (~1280–1440). Readable sports-desk / editorial manager UI. Dark theme is fine. Not a racing HUD, not neon, not tiny grey-on-grey. Difficulty must not come from hiding the Advance button or burying cash vs budget.

English UI labels (domain identifiers). Polish is allowed only in a small footer note that this is a prototype.

## Out of scope

Godot, C#, real Commands, RaceLive, Card Flows, pixel-perfect brand, mobile layout as a product requirement (phone viewing is nice-to-have).
