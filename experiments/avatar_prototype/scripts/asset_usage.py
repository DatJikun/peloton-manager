#!/usr/bin/env python3
"""Prove that an asset lands, at the share its weight implies, with its gate firing.

    python3 scripts/asset_usage.py hair_26_short_wave
    python3 scripts/asset_usage.py fh_04_beard_full --style poster --pool 20000

`out/demo/report.txt` only prints the six most common assets per category, so it
cannot confirm a rare or newly added asset, and it says nothing about whether a
`requires` / `excludes` rule actually fires. This does both.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab import render  # noqa: E402
from avatarlab.generate import Rider, active_tags, generate  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
REGIONS = [
    "west_europe", "east_europe", "scandinavia", "iberia", "latin_america", "north_africa",
    "east_africa", "west_africa", "middle_east", "east_asia", "south_asia", "oceania", "north_america",
]
BLOCK_OF = {"head": "identity", "ears": "identity", "eyes": "identity", "eyebrows": "identity",
            "nose": "identity", "mouth": "identity", "iris_color": "identity",
            "hair": "mutable", "hair_color": "mutable", "facial_hair": "mutable",
            "helmet": "equipment", "glasses": "equipment", "jersey": "equipment"}


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("asset_id")
    ap.add_argument("--style", default="poster")
    ap.add_argument("--pool", type=int, default=20_000)
    ap.add_argument("--role", default="rider")
    args = ap.parse_args(argv[1:])

    pack = render.Pack(ROOT / "out" / f"pack_{args.style}")
    m = pack.manifest
    asset = m.get(args.asset_id)
    block = BLOCK_OF.get(asset.category, "equipment")
    key = {"iris_color": "iris_color", "hair_color": "hair_color", "jersey": "jersey_template"}.get(
        asset.category, asset.category
    )

    total_w = sum(a.weight for a in m.by_category(asset.category))
    print(f"{asset.asset_id}  category={asset.category}  block={block}")
    print(f"  weight {asset.weight} of {total_w:.3f} in the category -> expected share {asset.weight / total_w * 100:.2f}%")
    if asset.min_age or asset.max_age:
        print(f"  age window: {asset.min_age or '-'} .. {asset.max_age or '-'}")
    if asset.requires_tags:
        print(f"  requires tags: {list(asset.requires_tags)}")
    if asset.excludes_tags:
        print(f"  excludes tags: {list(asset.excludes_tags)}")
    if asset.region_weights:
        print(f"  region weights: {asset.region_weights}")

    holders = 0
    violations = 0
    eligible = 0
    tag_carriers: dict[str, int] = {t: 0 for t in (*asset.requires_tags, *asset.excludes_tags)}
    for i in range(1, args.pool + 1):
        rider = Rider(rider_id=i, age=18 + i % 27, region=REGIONS[i % len(REGIONS)], role=args.role)
        app = generate(rider, m)
        tags = active_tags(app, m)
        for t in tag_carriers:
            if t in tags:
                tag_carriers[t] += 1
        age_ok = (asset.min_age is None or rider.age >= asset.min_age) and (
            asset.max_age is None or rider.age <= asset.max_age
        )
        gate_ok = all(t in tags for t in asset.requires_tags) and not any(t in tags for t in asset.excludes_tags)
        if age_ok and gate_ok:
            eligible += 1
        has = (getattr(app, block).get(key) == asset.asset_id) or (
            asset.asset_id in getattr(app, block).get("skin_details", ())
        )
        if has:
            holders += 1
            if not (age_ok and gate_ok):
                violations += 1

    print(f"\n  pool of {args.pool} riders (ages 18..44, all regions, role={args.role})")
    print(f"  eligible riders: {eligible} ({eligible / args.pool * 100:.1f}%)")
    print(f"  riders using it: {holders} ({holders / args.pool * 100:.2f}%)")
    for tag, count in tag_carriers.items():
        print(f"  riders carrying {tag!r}: {count} ({count / args.pool * 100:.1f}%)")
    print(f"  GATE VIOLATIONS: {violations}")
    if holders == 0:
        print("  -> the asset never appears: check its weight, age window and tag gates")
    return 1 if violations else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
