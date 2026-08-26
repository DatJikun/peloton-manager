#!/usr/bin/env python3
"""Render every review sheet + the determinism/performance log.

Outputs to out/demo/:
  01_contact_sheet.png   40 riders, mixed age / region / discipline / team
  02_aging.png           one rider from 19 to 44 (identity must not change)
  03_teams.png           one rider across teams and special jerseys
  04_equipment.png       helmet / glasses on the same face
  05_trait_variants.png  asset explorer: one base rider, one trait swapped
  06_skin_and_hair.png   colour-slot sweep (skin tone x hair colour)
  report.txt             determinism, duplicate and performance numbers
"""

from __future__ import annotations

import copy
import random
import sys
import time
from dataclasses import replace
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from PIL import Image, ImageDraw, ImageFont

from avatarlab import render
from avatarlab.generate import Rider, core_fingerprint, generate, generate_pool, similarity_key

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "out" / "demo"

BG = (30, 33, 38)
CARD = (48, 52, 59)
CARD_ALT = (41, 45, 51)
TEXT = (226, 230, 236)
SUBTEXT = (152, 160, 172)

FONT_PATH = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
FONT_BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"

REGIONS = [
    "west_europe",
    "east_europe",
    "scandinavia",
    "iberia",
    "latin_america",
    "north_africa",
    "east_africa",
    "west_africa",
    "middle_east",
    "east_asia",
    "south_asia",
    "oceania",
    "north_america",
]
DISCIPLINES = ["sprinter", "climber", "classics", "tt", "allrounder"]


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_BOLD if bold else FONT_PATH, size)


def tile(img: Image.Image, size: int, title: str, subtitle: str = "", alt: bool = False) -> Image.Image:
    label_h = 34 if subtitle else 24
    card = Image.new("RGBA", (size, size + label_h), (*(CARD_ALT if alt else CARD), 255))
    card.alpha_composite(img.resize((size, size), Image.LANCZOS), (0, 0))
    d = ImageDraw.Draw(card)
    d.text((7, size + 3), title, fill=TEXT, font=font(max(10, size // 18), bold=True))
    if subtitle:
        d.text((7, size + 3 + max(11, size // 16)), subtitle, fill=SUBTEXT, font=font(max(9, size // 22)))
    return card


def grid(tiles: list[Image.Image], cols: int, pad: int = 6, header: str = "", sub: str = "") -> Image.Image:
    tw, th = tiles[0].size
    rows = (len(tiles) + cols - 1) // cols
    head_h = 0 if not header else (54 if not sub else 78)
    sheet = Image.new("RGBA", (cols * (tw + pad) + pad, head_h + rows * (th + pad) + pad), (*BG, 255))
    d = ImageDraw.Draw(sheet)
    if header:
        d.text((pad + 4, 14), header, fill=TEXT, font=font(26, bold=True))
        if sub:
            d.text((pad + 4, 48), sub, fill=SUBTEXT, font=font(15))
    for i, t in enumerate(tiles):
        x = pad + (i % cols) * (tw + pad)
        y = head_h + pad + (i // cols) * (th + pad)
        sheet.alpha_composite(t, (x, y))
    return sheet


def rider_for(rid: int) -> Rider:
    rnd = random.Random(rid * 7919)
    return Rider(
        rider_id=rid,
        age=rnd.choice([19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 38, 40]),
        region=rnd.choice(REGIONS),
        height_cm=rnd.randint(166, 192),
        weight_kg=rnd.randint(56, 82),
        discipline=rnd.choice(DISCIPLINES),
        team_id=f"team_0{rnd.randint(1, 6)}_" + ["azure", "verde", "rosso", "noir", "arctic", "terra"][rnd.randint(1, 6) - 1],
    )


TEAM_IDS = ["team_01_azure", "team_02_verde", "team_03_rosso", "team_04_noir", "team_05_arctic", "team_06_terra"]


def contact_sheet(pack: render.Pack) -> Image.Image:
    tiles = []
    for i in range(40):
        rid = 500_000 + i * 13
        rnd = random.Random(rid)
        rider = Rider(
            rider_id=rid,
            age=[19, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 36, 38, 41][i % 18],
            region=REGIONS[i % len(REGIONS)],
            height_cm=rnd.randint(166, 192),
            weight_kg=rnd.randint(56, 82),
            discipline=DISCIPLINES[i % len(DISCIPLINES)],
            team_id=TEAM_IDS[i % len(TEAM_IDS)],
        )
        app = generate(rider, pack.manifest)
        img = render.render(app, pack)
        tiles.append(
            tile(
                img,
                168,
                f"#{rider.rider_id}  {rider.age}y",
                f"{rider.region}  {rider.discipline}",
                alt=(i % 2 == 1),
            )
        )
    return grid(
        tiles,
        8,
        header="1. Contact sheet - 40 riders",
        sub="one asset pack, one camera, one light rig; everything below is deterministic from rider_id + rider row",
    )


def aging_sheet(pack: render.Pack) -> Image.Image:
    tiles = []
    for rid in (770_101, 770_202):
        for age in (19, 24, 29, 34, 39, 44):
            rider = Rider(rider_id=rid, age=age, region="west_europe" if rid % 2 else "iberia", discipline="climber", team_id="team_02_verde")
            app = generate(rider, pack.manifest)
            img = render.render(app, pack)
            tiles.append(
                tile(
                    img,
                    216,
                    f"#{rid}  age {age}",
                    f"wrinkles {app.mutable['wrinkle_strength']:.2f}  gray {app.mutable['gray']:.2f}  recession {app.mutable['hairline_recession']:.2f}",
                )
            )
    return grid(
        tiles,
        6,
        header="2. Age progression - same rider, same identity block",
        sub="skull / eyes / nose / mouth / ears / skin tone are frozen; only hair, gray, wrinkles, beard and hairline move",
    )


def teams_sheet(pack: render.Pack) -> Image.Image:
    rid = 880_404
    tiles = []
    for team in TEAM_IDS:
        rider = Rider(rider_id=rid, age=27, region="west_europe", discipline="classics", team_id=team)
        app = generate(rider, pack.manifest)
        img = render.render(app, pack)
        tiles.append(tile(img, 216, pack.manifest.teams[team]["name"], team))
    for override, label in (("world_champion", "world champion"), ("national_champion", "national champion"), ("leader", "race leader")):
        rider = Rider(rider_id=rid, age=27, region="west_europe", discipline="classics", team_id="team_03_rosso", jersey_override=override)
        app = generate(rider, pack.manifest)
        tiles.append(tile(render.render(app, pack), 216, label, "jersey_override"))
    return grid(
        tiles,
        3,
        header="3. Team identity is separate from rider identity",
        sub="six transfers and three special jerseys on rider #880404 - the face never regenerates",
    )


def equipment_sheet(pack: render.Pack) -> Image.Image:
    tiles = []
    for rid in (990_501, 990_502):
        rider = Rider(rider_id=rid, age=30, region="west_europe", discipline="tt", team_id="team_05_arctic")
        base = generate(rider, pack.manifest)
        for label, glasses, helmet in (
            ("portrait", False, False),
            ("glasses", True, False),
            ("helmet", False, True),
            ("race kit", True, True),
        ):
            app = copy.deepcopy(base)
            app.equipment["glasses_worn"] = glasses
            app.equipment["helmet_worn"] = helmet
            tiles.append(tile(render.render(app, pack), 216, f"#{rid} {label}", render.cache_key(app)))
    return grid(
        tiles,
        4,
        header="4. Equipment layers on the same face",
        sub="helmet/glasses are runtime flags, not part of identity; the cache key (small text) changes, the identity does not",
    )


def trait_variants_sheet(pack: render.Pack) -> Image.Image:
    """One base rider, one trait swapped per tile: the sheet an artist reviews."""
    base_rider = Rider(rider_id=123_456, age=28, region="west_europe", discipline="classics", team_id="team_01_azure")
    base = generate(base_rider, pack.manifest)
    rows: list[tuple[str, str, list[str]]] = [
        ("head", "identity", [a.asset_id for a in pack.manifest.by_category("head")]),
        ("eyes", "identity", [a.asset_id for a in pack.manifest.by_category("eyes")]),
        ("nose", "identity", [a.asset_id for a in pack.manifest.by_category("nose")]),
        ("mouth", "identity", [a.asset_id for a in pack.manifest.by_category("mouth")]),
        ("eyebrows", "identity", [a.asset_id for a in pack.manifest.by_category("eyebrows")]),
        ("hair", "mutable", [a.asset_id for a in pack.manifest.by_category("hair")]),
        ("facial_hair", "mutable", [a.asset_id for a in pack.manifest.by_category("facial_hair")]),
        ("helmet", "equipment", [a.asset_id for a in pack.manifest.by_category("helmet")]),
        ("glasses", "equipment", [a.asset_id for a in pack.manifest.by_category("glasses")]),
    ]
    cols = max(len(ids) for _, _, ids in rows)
    tw = 132
    pad = 5
    label_w = 118
    sheet = Image.new("RGBA", (label_w + cols * (tw + pad) + pad, 78 + len(rows) * (tw + 24 + pad) + pad), (*BG, 255))
    d = ImageDraw.Draw(sheet)
    d.text((10, 14), "5. Asset explorer - one base rider, one trait swapped", fill=TEXT, font=font(26, bold=True))
    d.text((10, 48), "this is the sheet used to accept or reject a newly generated asset: only the named layer may change", fill=SUBTEXT, font=font(15))
    for r, (cat, block, ids) in enumerate(rows):
        y = 78 + pad + r * (tw + 24 + pad)
        d.text((10, y + 8), cat, fill=TEXT, font=font(15, bold=True))
        d.text((10, y + 28), block, fill=SUBTEXT, font=font(12))
        for c, asset_id in enumerate(ids):
            app = copy.deepcopy(base)
            if block == "identity":
                app.identity[cat] = asset_id
            elif block == "mutable":
                app.mutable["facial_hair" if cat == "facial_hair" else cat] = asset_id
            else:
                app.equipment[cat] = asset_id
                app.equipment[f"{cat}_worn"] = True
            img = render.render(app, pack)
            sheet.alpha_composite(tile(img, tw, asset_id.split("_", 1)[1][:18]), (label_w + pad + c * (tw + pad), y))
    return sheet


def skin_hair_sheet(pack: render.Pack) -> Image.Image:
    base_rider = Rider(rider_id=222_333, age=26, region="west_europe", discipline="allrounder", team_id="team_04_noir")
    base = generate(base_rider, pack.manifest)
    hair_ids = [a.asset_id for a in pack.manifest.by_category("hair_color")]
    tiles = []
    for tone in (0.05, 0.2, 0.35, 0.5, 0.65, 0.8, 0.95):
        for hid in hair_ids[::2]:
            app = copy.deepcopy(base)
            app.identity["skin_tone"] = tone
            app.mutable["hair_color"] = hid
            tiles.append(tile(render.render(app, pack), 132, f"skin {tone:.2f}", hid.split("_", 1)[1]))
    return grid(
        tiles,
        len(hair_ids[::2]),
        header="6. Colour slots - continuous skin tone x hair colour",
        sub="grayscale assets tinted at composite time; no extra PNG per colour",
    )


def report(pack: render.Pack, pool_size: int = 20_000) -> str:
    lines: list[str] = []
    m = pack.manifest
    lines.append("=== pack ===")
    lines.append(f"pack_id={m.pack_id} asset_pack_version={m.asset_pack_version} schema={m.avatar_schema_version} seed_version={m.seed_version}")
    cats: dict[str, int] = {}
    parts = 0
    for a in m.assets:
        cats[a.category] = cats.get(a.category, 0) + 1
        parts += len(a.parts)
    lines.append(f"assets={len(m.assets)} png_parts={parts}")
    lines.append("assets per category: " + ", ".join(f"{k}={v}" for k, v in sorted(cats.items())))

    lines.append("")
    lines.append("=== determinism ===")
    rider = Rider(rider_id=4242, age=27, region="iberia", discipline="sprinter", team_id="team_01_azure")
    a1 = generate(rider, m)
    a2 = generate(rider, m)
    lines.append(f"same rider generated twice -> identical appearance: {a1.to_json() == a2.to_json()}")
    img1 = render.render(a1, pack).tobytes()
    img2 = render.render(a2, pack).tobytes()
    lines.append(f"same appearance rendered twice -> identical pixels: {img1 == img2}")
    moved = replace(rider, team_id="team_06_terra", age=33)
    a3 = generate(moved, m)
    lines.append(f"after transfer + 6 birthdays -> identity block unchanged: {a1.identity == a3.identity}")
    lines.append(f"                             -> shape block unchanged:    {a1.shape == a3.shape}")
    lines.append(f"                             -> mutable block changed:    {a1.mutable != a3.mutable}")
    lines.append(f"cache key before/after: {render.cache_key(a1)} -> {render.cache_key(a3)}")

    lines.append("")
    lines.append(f"=== duplicate prevention on a {pool_size} rider pool ===")
    riders = [rider_for(i) for i in range(1, pool_size + 1)]
    t0 = time.perf_counter()
    pool, rep = generate_pool(riders, m)
    gen_s = time.perf_counter() - t0
    lines.append(f"generate_pool: {gen_s:.2f}s total, {gen_s / pool_size * 1e3:.3f} ms per rider")
    lines.append(f"riders={rep.riders} rerolled={rep.rerolled} unresolved_clones={rep.unresolved}")
    lines.append(f"distinct similarity keys={rep.distinct_similar} distinct identity cores={rep.distinct_core}")
    apps = list(pool.values())
    dup_core = pool_size - rep.distinct_core
    lines.append(f"identity-core collisions remaining: {dup_core} ({dup_core / pool_size * 100:.2f}%)")
    without = [generate(r, m, 0) for r in riders]
    clashes = len(without) - len({similarity_key(a) for a in without})
    lines.append(f"same pool WITHOUT salt re-rolls would ship {clashes} look-alike pairs")

    lines.append("")
    lines.append("=== distribution sanity (weighted, not uniform) ===")
    for cat, key, block in (("head", "head", "identity"), ("hair", "hair", "mutable"), ("facial_hair", "facial_hair", "mutable")):
        counts: dict[str, int] = {}
        for app in apps:
            v = (app.identity if block == "identity" else app.mutable).get(key)
            counts[str(v)] = counts.get(str(v), 0) + 1
        top = sorted(counts.items(), key=lambda kv: -kv[1])[:6]
        lines.append(f"{cat}: " + ", ".join(f"{k.split('_', 1)[-1]} {v / pool_size * 100:.1f}%" for k, v in top))

    lines.append("")
    lines.append("=== render performance (single thread, Pillow + numpy) ===")
    sample = apps[:200]
    t0 = time.perf_counter()
    for app in sample:
        render.render(app, pack)
    dt = time.perf_counter() - t0
    lines.append(f"{len(sample)} portraits in {dt:.2f}s -> {dt / len(sample) * 1e3:.1f} ms each, {len(sample) / dt:.0f} portraits/s")
    lines.append(f"extrapolated cold render of {pool_size} portraits: {dt / len(sample) * pool_size:.0f}s single threaded")

    cache_dir = ROOT / "out" / "cache"
    for app in sample[:50]:
        render.render_cached(app, pack, cache_dir)
    t0 = time.perf_counter()
    for app in sample[:50]:
        render.render_cached(app, pack, cache_dir)
    dt_cached = time.perf_counter() - t0
    lines.append(f"cache hit read: {dt_cached / 50 * 1e3:.2f} ms each")
    return "\n".join(lines) + "\n"


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    pack = render.Pack(ROOT / "out" / "pack")
    sheets = {
        "01_contact_sheet.png": contact_sheet,
        "02_aging.png": aging_sheet,
        "03_teams.png": teams_sheet,
        "04_equipment.png": equipment_sheet,
        "05_trait_variants.png": trait_variants_sheet,
        "06_skin_and_hair.png": skin_hair_sheet,
    }
    for name, fn in sheets.items():
        t0 = time.perf_counter()
        img = fn(pack)
        img.convert("RGB").save(OUT / name, quality=95)
        print(f"{name}: {img.size[0]}x{img.size[1]} in {time.perf_counter() - t0:.1f}s")
    text = report(pack)
    (OUT / "report.txt").write_text(text, encoding="utf-8")
    print()
    print(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
