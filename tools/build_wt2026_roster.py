#!/usr/bin/env python3
"""Build peloton.wt-2026/roster.json from wt2026-riders-source.csv (D-057).

Preserves numeric fields for the original 200 slot riders (leader/card/support-*)
except classics-star calibration (4b). New depth riders use rider.wt2026.<club>.<slug>.
"""

from __future__ import annotations

import csv
import hashlib
import json
import re
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "content" / "peloton.wt-2026"
CSV_PATH = PACK / "wt2026-riders-source.csv"
ROSTER_PATH = PACK / "roster.json"
ORGS_PATH = PACK / "organizations.json"
LEGACY_ROSTER_PATH = ROSTER_PATH

SLOT_SUFFIXES = ("leader", "card", "support-1", "support-2", "support-3", "support-4", "support-5", "support-6")

WT_CLUB_SLUGS = {
    f"organization.wt2026.{slug}": slug
    for slug in (
        "alpecin", "bahrain", "decathlon", "ef", "fdj", "ineos", "lidl-trek", "lotto",
        "movistar", "nsn", "redbull", "soudal", "jayco", "picnic", "visma", "uae", "unox", "astana",
    )
}

# README physiology bands (midpoints used as generator bases).
ARCHETYPE_PHYSIO = {
    "super-gc": dict(mass=(60, 66), cp=(410, 450), cpkg=(6.4, 6.8), wprime=(27, 32), pmax=(1050, 1200), cda=(0.26, 0.28), low=0.88, high=0.84, ovr=90),
    "gc": dict(mass=(60, 67), cp=(378, 418), cpkg=(6.0, 6.5), wprime=(24, 28), pmax=(950, 1100), cda=(0.27, 0.29), low=0.88, high=0.84, ovr=82),
    "classics": dict(mass=(70, 78), cp=(368, 445), cpkg=(5.4, 5.9), wprime=(30, 34), pmax=(1250, 1500), cda=(0.28, 0.30), low=0.90, high=0.84, ovr=80),
    "sprinter": dict(mass=(69, 82), cp=(350, 385), cpkg=(4.8, 5.4), wprime=(32, 35), pmax=(1540, 1720), cda=(0.30, 0.33), low=0.84, high=0.82, ovr=78),
    "tt": dict(mass=(75, 82), cp=(395, 430), cpkg=(5.2, 5.6), wprime=(22, 26), pmax=(1100, 1400), cda=(0.23, 0.25), low=0.88, high=0.80, ovr=80),
    "super-domestique": dict(mass=(60, 68), cp=(370, 400), cpkg=(5.5, 6.2), wprime=(24, 28), pmax=(950, 1150), cda=(0.28, 0.30), low=0.90, high=0.80, ovr=80),
    "diesel": dict(mass=(72, 84), cp=(360, 400), cpkg=(5.0, 5.5), wprime=(24, 28), pmax=(1100, 1180), cda=(0.29, 0.31), low=0.90, high=0.80, ovr=76),
    "neo": dict(mass=(61, 64), cp=(370, 392), cpkg=(5.6, 6.2), wprime=(24, 27), pmax=(1000, 1080), cda=(0.28, 0.30), low=0.84, high=0.80, ovr=86),
}

TT_CDA_FACTOR = {
    "tt": 0.68,
    "super-gc": 0.72,
    "gc": 0.76,
    "classics": 0.80,
    "diesel": 0.80,
    "super-domestique": 0.80,
    "sprinter": 0.84,
    "neo": 0.82,
}

WAGE_BAND_EUR = {
    "star": (3_500_000, 8_000_000),
    "leader": (600_000, 2_500_000),
    "sprinter": (180_000, 1_200_000),
    "super-domestique": (250_000, 1_200_000),
    "neo": (80_000, 450_000),
    "domestique": (100_000, 250_000),
}

ROLE_WAGE_BAND = {
    "leader": "leader",
    "card": "sprinter",
    "support": "domestique",
    "neo": "neo",
}

CLASSICS_STAR_OVERRIDES = {
    "rider.wt2026.alpecin.leader": {
        "criticalPowerW": 455,
        "lowIntensityDurability": 0.96,
        "highIntensityDurability": 0.92,
    },
    "rider.wt2026.visma.support-2": {
        "criticalPowerW": 458,
        "lowIntensityDurability": 0.95,
        "highIntensityDurability": 0.91,
    },
    "rider.wt2026.lidl-trek.mads-pedersen": {
        "criticalPowerW": 448,
        "lowIntensityDurability": 0.94,
        "highIntensityDurability": 0.90,
    },
    "rider.wt2026.redbull.leader": {
        "lowIntensityDurability": 0.90,
    },
}

NUMERIC_FIELDS = (
    "criticalPowerW", "wPrimeCapacityJ", "peakPowerW", "wPrimeRecoveryJPerSecond",
    "lowIntensityDurability", "highIntensityDurability", "bodyMassKg", "systemMassKg",
    "cdARoadM2", "cdATtM2", "baseCrr", "positioning", "handling", "tacticalAwareness",
    "annualWage", "contractEndDay", "potentialOvr",
)


def stable_unit(seed: str) -> float:
    digest = hashlib.sha256(seed.encode()).hexdigest()
    return int(digest[:8], 16) / 0xFFFFFFFF


def lerp(seed: str, low: float, high: float) -> float:
    return low + stable_unit(seed) * (high - low)


def slugify(name: str) -> str:
    normalized = unicodedata.normalize("NFKD", name).encode("ascii", "ignore").decode()
    normalized = normalized.lower().replace("'", "").replace(".", "")
    return re.sub(r"[^a-z0-9]+", "-", normalized).strip("-")


def slot_for_order(order: int) -> str | None:
    if 1 <= order <= len(SLOT_SUFFIXES):
        return SLOT_SUFFIXES[order - 1]
    return None


def wage_band_for(role: str, archetype: str) -> str:
    if role == "leader" and archetype in {"super-gc", "gc"}:
        return "star" if archetype == "super-gc" else "leader"
    if role == "leader":
        return "leader"
    if role == "card" and archetype == "sprinter":
        return "sprinter"
    if role == "card":
        return "leader"
    if role == "neo" or archetype == "neo":
        return "neo"
    if archetype in {"tt", "diesel"} and role == "support":
        return "super-domestique"
    return ROLE_WAGE_BAND.get(role, "domestique")


def tt_cda(road: float, archetype: str) -> float:
    return round(road * TT_CDA_FACTOR.get(archetype, 0.80), 4)


def vary_contract_end(rider_id: str, wage_band: str, annual_wage: int) -> int:
    is_star = wage_band in {"star", "leader"} or annual_wage >= 2_000_000
    span = 730 if is_star else 730
    base = 730 if is_star else 365
    return base + int(stable_unit(rider_id + ":contract") * span)


def generate_new_rider(row: dict, rider_id: str) -> dict:
    archetype = row["archetype"]
    role = row["role"]
    band = ARCHETYPE_PHYSIO[archetype]
    wage_band = wage_band_for(role, archetype)
    w_lo, w_hi = WAGE_BAND_EUR[wage_band]

    mass = round(lerp(rider_id + ":mass", *band["mass"]), 1)
    cp = int(round(lerp(rider_id + ":cp", *band["cp"])))
    wprime = int(round(lerp(rider_id + ":wprime", band["wprime"][0] * 1000, band["wprime"][1] * 1000)))
    pmax = int(round(lerp(rider_id + ":pmax", *band["pmax"])))
    cda_road = round(lerp(rider_id + ":cda", *band["cda"]), 3)
    low = round(lerp(rider_id + ":low", band["low"] - 0.03, band["low"] + 0.03), 2)
    high = round(lerp(rider_id + ":high", band["high"] - 0.03, band["high"] + 0.03), 2)
    ovr = int(round(lerp(rider_id + ":ovr", band["ovr"] - 3, band["ovr"] + 3)))
    wage = int(round(lerp(rider_id + ":wage", w_lo, w_hi) / 5000) * 5000)
    wage = max(w_lo, min(w_hi, wage))

    if archetype == "neo":
        ovr = max(ovr, int(round(lerp(rider_id + ":ovrneo", 82, 88))))

    rider = {
        "id": rider_id,
        "name": row["name"],
        "organizationId": row["organizationId"],
        "nationality": row["nationality"],
        "birthYear": int(row["birthYear"]),
        "archetype": archetype,
        "wageBand": wage_band,
        "criticalPowerW": cp,
        "wPrimeCapacityJ": wprime,
        "peakPowerW": max(pmax, cp + 40),
        "wPrimeRecoveryJPerSecond": round(lerp(rider_id + ":rec", 40, 44), 1),
        "lowIntensityDurability": min(0.99, max(0.55, low)),
        "highIntensityDurability": min(0.99, max(0.55, high)),
        "bodyMassKg": mass,
        "systemMassKg": 8.0,
        "cdARoadM2": cda_road,
        "cdATtM2": tt_cda(cda_road, archetype),
        "baseCrr": 0.004 if archetype != "tt" else 0.0038,
        "positioning": round(lerp(rider_id + ":pos", 0.70, 0.88), 2),
        "handling": round(lerp(rider_id + ":han", 0.70, 0.88), 2),
        "tacticalAwareness": round(lerp(rider_id + ":tac", 0.70, 0.86), 2),
        "annualWage": wage,
        "contractEndDay": vary_contract_end(rider_id, wage_band, wage),
        "potentialOvr": ovr,
    }
    return rider


def legacy_slot_id(org_id: str, squad_order: int) -> str | None:
    slug = WT_CLUB_SLUGS.get(org_id)
    if slug is None:
        wc_slug = org_id.split(".")[-1]
        slot = slot_for_order(squad_order)
        return f"rider.wt2026.{wc_slug}.{slot}" if slot else None
    slot = slot_for_order(squad_order)
    return f"rider.wt2026.{slug}.{slot}" if slot else None


def load_legacy_riders() -> dict[str, dict]:
    data = json.loads(LEGACY_ROSTER_PATH.read_text(encoding="utf-8"))
    return {rider["id"]: rider for rider in data["riders"]}


def copy_legacy_numeric(target: dict, source: dict) -> None:
    for key in NUMERIC_FIELDS:
        if key in source:
            target[key] = source[key]
    if "wageBand" in source:
        target["wageBand"] = source["wageBand"]
    if "archetype" in source:
        target["archetype"] = source["archetype"]
    if "cdARoadM2" in source:
        target["cdARoadM2"] = source["cdARoadM2"]
        target["cdATtM2"] = source.get("cdATtM2", tt_cda(source["cdARoadM2"], source["archetype"]))
    elif "cdAM2" in source:
        target["cdARoadM2"] = source["cdAM2"]
        target["cdATtM2"] = tt_cda(source["cdAM2"], source["archetype"])


def apply_classics_star_overrides(riders_by_id: dict[str, dict]) -> None:
    for rider_id, overlay in CLASSICS_STAR_OVERRIDES.items():
        if rider_id not in riders_by_id:
            continue
        riders_by_id[rider_id].update(overlay)


def fit_club_budgets(riders: list[dict], org_budgets: dict[str, int]) -> None:
    by_org: dict[str, list[dict]] = {}
    for rider in riders:
        by_org.setdefault(rider["organizationId"], []).append(rider)

    for org_id, club_riders in by_org.items():
        budget = org_budgets.get(org_id)
        if budget is None:
            continue
        total = sum(r["annualWage"] for r in club_riders)
        if total <= budget:
            continue
        support = [
            r for r in club_riders
            if r.get("wageBand") in {"domestique", "neo"} or r["annualWage"] <= 250_000
        ]
        support.sort(key=lambda r: r["annualWage"], reverse=True)
        for rider in support:
            if total <= budget:
                break
            band = rider.get("wageBand", "domestique")
            floor = WAGE_BAND_EUR.get(band, WAGE_BAND_EUR["domestique"])[0]
            if rider["annualWage"] > floor:
                delta = rider["annualWage"] - floor
                rider["annualWage"] = floor
                total -= delta


def read_csv_rows() -> list[dict]:
    with CSV_PATH.open(encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def build_roster() -> dict:
    legacy = load_legacy_riders()
    org_budgets = {
        org["id"]: org["estimatedBudgetEur"]
        for org in json.loads(ORGS_PATH.read_text(encoding="utf-8"))["organizations"]
    }
    used_ids: set[str] = set()
    riders: list[dict] = []

    for row in read_csv_rows():
        org_id = row["organizationId"]
        order = int(row["squadOrder"])
        legacy_id = legacy_slot_id(org_id, order)
        if legacy_id and legacy_id in legacy:
            rider = dict(legacy[legacy_id])
            rider["name"] = row["name"]
            rider["nationality"] = row["nationality"]
            rider["birthYear"] = int(row["birthYear"])
            copy_legacy_numeric(rider, legacy[legacy_id])
            rider_id = legacy_id
        else:
            slug = WT_CLUB_SLUGS.get(org_id, org_id.split(".")[-1])
            base_slug = slugify(row["name"])
            rider_id = f"rider.wt2026.{slug}.{base_slug}"
            suffix = 2
            while rider_id in used_ids:
                rider_id = f"rider.wt2026.{slug}.{base_slug}-{suffix}"
                suffix += 1
            rider = generate_new_rider(row, rider_id)

        used_ids.add(rider_id)
        rider["id"] = rider_id
        riders.append(rider)

    riders_by_id = {r["id"]: r for r in riders}
    apply_classics_star_overrides(riders_by_id)
    fit_club_budgets(riders, org_budgets)

    manager = json.loads(LEGACY_ROSTER_PATH.read_text(encoding="utf-8"))["manager"]
    return {"manager": manager, "teamMappings": [], "riders": riders}


def main() -> None:
    roster = build_roster()
    if len(roster["riders"]) > 512:
        raise SystemExit(f"Roster exceeds 512 riders: {len(roster['riders'])}")
    ROSTER_PATH.write_text(json.dumps(roster, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Built {len(roster['riders'])} riders -> {ROSTER_PATH}")


if __name__ == "__main__":
    main()
