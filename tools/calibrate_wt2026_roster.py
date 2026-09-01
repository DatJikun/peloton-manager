#!/usr/bin/env python3
"""Apply research overlays onto peloton.wt-2026/roster.json.

Preserves per-rider archetype and wageBand from the captain-first pack.
Does not re-roll physiology for the whole grid.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROSTER_PATH = ROOT / "content" / "peloton.wt-2026" / "roster.json"

# Slot copies: source id -> destination id (identity + org stay on the destination slot).
EVENPOEL_2026_TRANSFER = (
    ("rider.wt2026.soudal.leader", "rider.wt2026.redbull.leader"),
    ("rider.wt2026.redbull.leader", "rider.wt2026.redbull.support-1"),
    ("rider.wt2026.redbull.support-1", "rider.wt2026.soudal.support-1"),
    ("rider.wt2026.soudal.support-1", "rider.wt2026.soudal.leader"),
)

NAMED_OVERLAY = {
    "rider.wt2026.redbull.leader": {
        "annualWage": 6_600_000,
        "wageBand": "star",
        "archetype": "super-gc",
        "contractEndDay": 1095,
    },
    "rider.wt2026.redbull.support-1": {
        "wageBand": "star",
        "archetype": "gc",
        "contractEndDay": 730,
    },
    "rider.wt2026.soudal.leader": {
        "annualWage": 1_200_000,
        "wageBand": "leader",
        "archetype": "gc",
        "contractEndDay": 730,
    },
    "rider.wt2026.soudal.support-1": {
        "wageBand": "neo",
        "archetype": "neo",
        "contractEndDay": 1095,
    },
    "rider.wt2026.uae.leader": {"contractEndDay": 1460},
    "rider.wt2026.visma.leader": {"contractEndDay": 1095},
    "rider.wt2026.alpecin.leader": {"contractEndDay": 1095},
    "rider.wt2026.alpecin.card": {"contractEndDay": 730},
    "rider.wt2026.visma.support-2": {"contractEndDay": 1095},
}


def stable_unit(seed: str) -> float:
    digest = hashlib.sha256(seed.encode()).hexdigest()
    return int(digest[:8], 16) / 0xFFFFFFFF


def apply_transfer(riders_by_id: dict[str, dict]) -> None:
    snapshots = {rider_id: dict(rider) for rider_id, rider in riders_by_id.items()}
    for source_id, dest_id in EVENPOEL_2026_TRANSFER:
        dest = riders_by_id[dest_id]
        source = snapshots[source_id]
        keep_id = dest["id"]
        keep_org = dest["organizationId"]
        dest.clear()
        dest.update(source)
        dest["id"] = keep_id
        dest["organizationId"] = keep_org


def vary_contract_end(rider: dict) -> None:
    if rider.get("contractEndDay") != 10000:
        return
    rider_id = rider["id"]
    is_star = rider.get("wageBand") in {"star", "leader"} or rider.get("annualWage", 0) >= 2_000_000
    u = stable_unit(rider_id + ":contract")
    rider["contractEndDay"] = (730 + int(u * 730)) if is_star else (365 + int(u * 730))


def main() -> None:
    data = json.loads(ROSTER_PATH.read_text(encoding="utf-8"))
    riders_by_id = {rider["id"]: rider for rider in data["riders"]}
    apply_transfer(riders_by_id)
    for rider_id, overlay in NAMED_OVERLAY.items():
        riders_by_id[rider_id].update(overlay)
    for rider in data["riders"]:
        vary_contract_end(rider)
        rider["contractEndDay"] = max(1, int(rider["contractEndDay"]))

    ROSTER_PATH.write_text(
        json.dumps(data, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Calibrated {len(data['riders'])} riders -> {ROSTER_PATH}")


if __name__ == "__main__":
    main()
