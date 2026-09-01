# Peloton Manager — Club cash and contract offers on career screens

**Title:** Career finance / contracts on screen  
**Version:** 0.1  
**Status:** DRAFT  
**Purpose:** Owner lock D-051 (2026-09-01): desk, squad, and finance show the club’s real cash and wages; a squad offer of annual wage + end day writes through D-044 commands.  
**Authority/Owner:** Project owner (player)  
**Related decisions:** D-004, D-031, D-039, D-043, D-044, D-045, D-046, D-048, D-050, D-051  

---

## 1. For the owner (plain language)

You already pick a club and a season. Now the **money on the desk is that club’s money**, in **euro**, not the Beskid look-lab złoty. Opening **Skład**, picking a rider, and offering a new wage + contract end **saves**. If the offer is too low for that rider’s current wage and loyalty, they refuse and nothing changes.

**Not in this slice:** firing riders, transfer fees, agent minigame, sponsor market, staff contracts, scouting, CdA, aging, yearly 2027 routes. Sztab / Sponsorzy / Skauting / Rynek stay a drawing.

**Assumptions (say if wrong):** one offer at a time; renewals on your own eight; the thin poach command already exists in the engine but the Rynek screen stays a drawing until a later slice.

---

## 2. Player flow

```text
Management (career shell)
  → Desk: FINANSE · TYDZIEŃ reads ClubFinance (kasa, sponsor dzień, płace dzień, bilans, debet)
  → Finanse: same world numbers (no Beskid ledger)
  → Skład: kadra wages/end days from ClubRoster; karta zawodnika
       Negocjuj kontrakt → pola pensja/rok + dzień końca
       Złóż ofertę → Begin/Set/Confirm (D-044)
       Anuluj → Cancel
```

Polish UI copy:

| English (code) | Polish (player) |
|---|---|
| Cash | Kasa |
| Title sponsor | Sponsor tytularny |
| Daily sponsor | Sponsor / dzień |
| Daily wages | Płace / dzień |
| Daily net | Bilans dnia |
| Overdrawn | Klub jest na debecie |
| Annual wage | Pensja / rok |
| Contract end day | Koniec kontraktu |
| Negotiate | Negocjuj kontrakt |
| Submit offer | Złóż ofertę |
| Cancel offer | Anuluj |
| Offer accepted | Kontrakt przyjęty |
| Offer rejected | Oferta odrzucona |

Currency on world numbers is **euro** (`1 250 000 €`), never look-catalog złoty. Look-lab screens that stay drawings may still show złoty.

---

## 3. Implementation contract

### 3.1 No new world rules

Reuse existing Application truth. **Do not** add a GameState, SQLite schema bump, checksum label change, or luxury tax.

- `ClubFinanceProjection` (Management only)
- `ClubRosterProjection`
- `ContractNegotiationProjection`
- `BeginContractNegotiationCommand` / `SetContractOfferCommand` / `ConfirmContractOfferCommand` / `CancelContractNegotiationCommand`
- Accept formula stays D-044: `threshold = currentWage == 0 ? 100_000 : floor(currentWage * (1.10 - 0.20 * Loyalty01))`
- Reject: `CONTRACT_OFFER_REJECTED` — world unchanged; draft cleared
- Presentation only: Godot calls Commands + Queries. No wage math in the UI.

### 3.2 CareerShellHost

Expose (thin wrappers, same pattern as D-050):

```text
ClubFinanceProjection? ClubFinance
ContractNegotiationProjection? ContractNegotiation
BeginContractNegotiation(WorldEntityId riderCareerId)
SetContractOffer(int annualWage, int contractEndDay)
ConfirmContractOffer()
CancelContractNegotiation()
```

Do **not** invent fire/release commands. **Zwolnij** stays `CareerLookCatalog.NotInWorld`.

### 3.3 Desk + Finanse

When `host.ClubFinance` is non-null (Management + employer):

- Desk panel **FINANSE · TYDZIEŃ** and view **Finanse** show `CashEur`, title sponsor name + annual fee, `DailySponsor`, `DailyWages`, `DailyNet`, `WageBillAnnual`, `Overdrawn`.
- No weekly Beskid ledger, no „budżet sezonu” from `CareerLookCatalog`.
- If `ClubFinance` is null (not Management), keep a short empty note — do not fill Beskid złoty as a fallback.

### 3.4 Skład — own roster offers

`BuildWorldRiderCard` (already world roster) gains the offer UI. Remove the dead look-catalog `BuildRiderCard` path from **Skład** if it is unused, or leave it unused — do not show Beskid wages on the live squad card.

- Selected rider is a `WorldEntityId` (store `long` / id value; do not keep `int selectedRiderId = 1` as a look-lab id).
- **Negocjuj kontrakt** → `BeginContractNegotiation` for that rider.
- Prefill wage = current `AnnualWage` (or `100_000` if 0). Prefill end day = `max(ContractEndDay, today+1)` or `today+365` if the current end is not after today.
- Inputs: integer annual wage and inclusive end day (`SpinBox` is fine). On **Złóż ofertę**: `SetContractOffer` then `ConfirmContractOffer`.
- Success toast: `Kontrakt przyjęty.` Roster wage/end refresh from `ClubRoster`.
- Reject toast: `Oferta odrzucona.` (map `CONTRACT_OFFER_REJECTED`). Do **not** print the hidden threshold number.
- **Anuluj** / closing negotiations → `CancelContractNegotiation`.
- Changing selected rider while a draft is open → cancel first, then begin the new rider (or cancel only). One draft at a time.
- Do **not** show accept threshold, loyalty formula, or rival physiology.

Wages on the squad table: format as euro from `AnnualWage`, not `CareerLookCatalog.Zloty`.

### 3.5 Stay drawings

Staff, sponsors, scouting, market, ranking, staff notes: still `CareerLookCatalog` + toast. Do not wire poach through the Beskid Rynek list (those people are not world riders).

### 3.6 Locks

- No `PlayerTeam`. No God-eye live physiology. No mid-race save. No unseeded gameplay RNG.
- Watch film stays optional and off by default. Do not rebuild Career Hub.
- Do not close §49. Do not restore `StubRaceEngine`.
- SchemaVersion stays **9**. Race checksum for `race.prototype.gate` seed `91234` must stay  
  `winner=1006` / `5A35E88103E2FBB40325EA8BEF15AAAC2F2E1AB70F4E6DE2BBCE584EC7EE6721`.

---

## 4. Tests

`CareerShellHostTests` (Godot.Tests, no Godot binary required):

1. After `OpenWorldTour("organization.wt2026.uae")` + confirm pre-season, `ClubFinance` is non-null; title sponsor is the UAE world sponsor (not Vetter); `CashEur` starts at 0; wage bill > 0.
2. After `OpenSkeleton`, `ClubFinance` matches skeleton employer (fee 2_000_000, `TitleSponsor` Skeleton Sponsor) — still not Beskid.
3. Begin + Set(threshold) + Confirm on a skeleton own rider updates `ClubRoster` wage; `ContractNegotiation` is null afterwards.
4. Too-low offer returns `CONTRACT_OFFER_REJECTED`; roster wage unchanged.

Existing phase 6/7 Application tests stay green. Do not retune physiology or start lists.

---

## 5. Out of scope

- Transfer fees, counters, agent board
- Firing / releasing
- Market / sponsor / staff world wiring
- CdA Road vs TT, aging, 28-man roster, 2027 courses, living UCI promotion
- New GameStates, schema 10
