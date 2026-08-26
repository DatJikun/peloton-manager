#!/usr/bin/env python3
"""Self-test for the avatar prototype: the invariants that must never break.

Plain asserts, no test framework, so it can run anywhere:
    python3 scripts/selftest.py
"""

from __future__ import annotations

import copy
import sys
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab import render, validate
from avatarlab.generate import Rider, _eligible, generate, generate_pool, similarity_key, weighted_pick
from avatarlab.rng import RiderRng

ROOT = Path(__file__).resolve().parents[1]
STYLE = sys.argv[1] if len(sys.argv) > 1 else "poster"
PACK = ROOT / "out" / f"pack_{STYLE}"

checks = 0


def check(cond: bool, label: str) -> None:
    global checks
    checks += 1
    if not cond:
        raise AssertionError(f"FAILED: {label}")
    print(f"  ok  {label}")


def main() -> int:
    if not (PACK / "manifest.json").exists():
        print("run scripts/bake_pack.py first")
        return 2
    pack = render.Pack(PACK)
    m = pack.manifest

    print("pack validation")
    for path in sorted((ROOT / "out").glob("pack_*")):
        rep = validate.validate(path)
        check(rep.ok, f"{path.name} passes validation ({rep.checked_files} files)")

    print("determinism")
    rider = Rider(rider_id=99, age=26, region="east_europe", discipline="climber", team_id="team_01_azure")
    a = generate(rider, m)
    b = generate(rider, m)
    check(a.to_json() == b.to_json(), "appearance is a pure function of the rider row")
    check(render.render(a, pack).tobytes() == render.render(b, pack).tobytes(), "render is byte-identical")
    check(render.cache_key(a) == render.cache_key(b), "cache key is stable")

    print("rng domain separation")
    r1 = RiderRng(99, 1)
    check(
        r1.stream("identity.head").next_u64() != r1.stream("identity.eyes").next_u64(),
        "different domains give different streams",
    )
    check(
        RiderRng(99, 1).stream("identity.head").next_u64() == RiderRng(99, 1).stream("identity.head").next_u64(),
        "same domain replays identically",
    )
    check(
        RiderRng(99, 1, salt=3).stream("identity.head").next_u64()
        == RiderRng(99, 1, salt=0).stream("identity.head").next_u64(),
        "salt does not touch unsalted (identity) streams",
    )
    check(
        RiderRng(99, 1, salt=3).stream("mutable.hair", salted=True).next_u64()
        != RiderRng(99, 1, salt=0).stream("mutable.hair", salted=True).next_u64(),
        "salt does move salted (secondary) streams",
    )
    check(
        RiderRng(99, 1).stream("identity.head").next_u64() != RiderRng(99, 2).stream("identity.head").next_u64(),
        "seed_version bump reshuffles everything (opt-in migration only)",
    )

    print("identity stability")
    for age in range(18, 46):
        aged = generate(replace(rider, age=age), m)
        check_silent = aged.identity == a.identity and aged.shape == a.shape
        if not check_silent:
            raise AssertionError(f"identity moved at age {age}")
    check(True, "identity + shape unchanged across ages 18..45")
    moved = generate(replace(rider, team_id="team_04_noir", jersey_override="world_champion"), m)
    check(moved.identity == a.identity, "transfer does not touch identity")
    check(moved.equipment != a.equipment, "transfer changes equipment")
    older = generate(replace(rider, age=40), m)
    check(older.mutable["wrinkle_strength"] > a.mutable["wrinkle_strength"], "wrinkles grow with age")
    check(older.mutable["gray"] >= a.mutable["gray"], "gray never decreases with age")
    younger = generate(replace(rider, age=19), m)
    check(younger.mutable["wrinkle_strength"] == 0.0, "no wrinkles on a 19 year old")

    print("monotonic aging")
    prev_w, prev_g, prev_r = -1.0, -1.0, -1.0
    for age in range(18, 55):
        mu = generate(replace(rider, age=age), m).mutable
        if mu["wrinkle_strength"] < prev_w - 1e-9 or mu["gray"] < prev_g - 1e-9 or mu["hairline_recession"] < prev_r - 1e-9:
            raise AssertionError(f"aging went backwards at age {age}")
        prev_w, prev_g, prev_r = mu["wrinkle_strength"], mu["gray"], mu["hairline_recession"]
    check(True, "wrinkles / gray / recession are monotonic in age")

    print("compatibility rules")
    teen = Rider(rider_id=7, age=18, region="west_europe")
    bad = 0
    for rid in range(1, 400):
        young = generate(Rider(rider_id=rid, age=18, region="west_europe"), m)
        fh = young.mutable["facial_hair"]
        if fh and (m.get(fh).min_age or 0) > 18:
            bad += 1
    check(bad == 0, "no under-age rider gets an age-restricted beard asset")
    receded_only = [a2 for a2 in m.by_category("hair") if "hairline_receded" in a2.requires_tags]
    check(bool(receded_only), "pack has hair assets gated on a receding hairline")
    check(
        weighted_pick(RiderRng(1, 1), "t", receded_only, teen, set()) is None,
        "gated hair is unreachable without the tag",
    )
    check(
        weighted_pick(RiderRng(1, 1), "t", receded_only, teen, {"hairline_receded"}) is not None,
        "gated hair becomes reachable with the tag",
    )
    excl = [a2 for a2 in m.by_category("skin_details") if "beard_dense" in a2.excludes_tags]
    check(bool(excl), "pack has an excludes_tags rule (stubble shadow vs dense beard)")
    check(not _eligible(excl[0], teen, {"beard_dense"}), "excludes_tags blocks the conflicting asset")

    print("weighted selection is append-stable")
    import dataclasses
    import math
    from collections import Counter

    from avatarlab.manifest import Asset
    from avatarlab.rng import neg_log2_q32

    worst_log = max(
        abs(neg_log2_q32(u) / 2**32 + math.log2(u / 2**64))
        for u in (1, 2**32, 2**60, 2**63, 2**64 - 1, 12345678901234567)
    )
    check(worst_log < 1e-6, f"fixed-point log2 matches libm to {worst_log:.1e} (platform independent)")

    pool_riders = [Rider(rider_id=i, age=20 + i % 18, region="west_europe") for i in range(1, 8001)]
    heads_before = {r.rider_id: generate(r, m).identity["head"] for r in pool_riders}
    counts = Counter(heads_before.values())
    total_w = sum(a.weight for a in m.by_category("head"))
    worst = max(abs(counts[a.asset_id] / len(pool_riders) - a.weight / total_w) for a in m.by_category("head"))
    check(worst < 0.02, f"asset frequencies follow their weights (worst deviation {worst * 100:.2f} pp)")

    appended = dataclasses.replace(
        m, assets=m.assets + (Asset(asset_id="head_99_probe", category="head", parts=(), weight=0.10, tags=("jaw_medium",)),)
    )
    heads_after = {r.rider_id: generate(r, appended).identity["head"] for r in pool_riders}
    moved = [k for k in heads_before if heads_before[k] != heads_after[k]]
    swapped = [k for k in moved if heads_after[k] != "head_99_probe"]
    check(not swapped, f"appending an asset moves riders only to it ({len(moved)} moved, 0 reshuffled)")
    expected = 0.10 / (total_w + 0.10)
    check(
        abs(len(moved) / len(pool_riders) - expected) < 0.02,
        f"the moved share matches the new weight ({len(moved) / len(pool_riders) * 100:.1f}% vs {expected * 100:.1f}%)",
    )

    retired = dataclasses.replace(
        m, assets=tuple(dataclasses.replace(a, weight=0.0) if a.asset_id == "head_03_square" else a for a in m.assets)
    )
    heads_retired = {r.rider_id: generate(r, retired).identity["head"] for r in pool_riders}
    moved_r = [k for k in heads_before if heads_before[k] != heads_retired[k]]
    check(
        bool(moved_r) and all(heads_before[k] == "head_03_square" for k in moved_r),
        "retiring an asset (weight 0) only moves the riders who had it",
    )

    print("duplicate prevention")
    riders = [Rider(rider_id=i, age=20 + i % 18, region="west_europe") for i in range(1, 3001)]
    pool, rep = generate_pool(riders, m)
    check(rep.unresolved == 0, f"no unresolved look-alikes in {rep.riders} riders (re-rolled {rep.rerolled})")
    check(len({similarity_key(x) for x in pool.values()}) == rep.riders, "similarity keys are unique")
    pool2, rep2 = generate_pool(list(reversed(riders)), m)
    check(
        all(pool[k].to_json() == pool2[k].to_json() for k in pool),
        "pool generation is independent of input order",
    )
    check(rep.distinct_core > rep.riders * 0.99, "identity cores are >99% distinct")

    print("renderer")
    img = render.render(a, pack)
    check(img.size == (512, 512) and img.mode == "RGBA", "portrait is 512x512 RGBA")
    check(img.getchannel("A").getextrema()[0] == 0, "background stays transparent")
    alpha = img.getchannel("A")
    check(alpha.getbbox()[1] > 0, "nothing touches the top edge of the canvas")
    cropped = render.crop_head(img, pack)
    check(cropped.size[0] == cropped.size[1] and cropped.size[0] < 512, "head_crop is a smaller square")
    kit = copy.deepcopy(a)
    kit.equipment["helmet_worn"] = True
    check(render.cache_key(kit) != render.cache_key(a), "putting a helmet on changes the cache key")
    check(
        render.render(kit, pack).tobytes() != img.tobytes(),
        "helmet flag actually changes pixels",
    )

    print(f"\n{checks} checks passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
