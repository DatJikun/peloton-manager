#!/usr/bin/env python3
"""Skill comparison: typical peloton under teraz / kształt / znak / archetyp.

Each column (or row, on the lineup) is an independent sample from that look's
mix — that is the cousin-face test. Same-rider-id strips are a second sheet
because weighted picks are sticky and hide mix differences.

    python3 scripts/apply_looks.py
    python3 scripts/render_look_compare.py
"""

from __future__ import annotations

import random
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from PIL import Image, ImageDraw, ImageFont

from avatarlab import render
from avatarlab.bake.looks import LOOKS
from avatarlab.generate import Rider, generate

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "out" / "demo"
DEMO = ROOT / "demo"
ARTIFACTS = Path("/opt/cursor/artifacts")
PAPER = (243, 237, 225)
WHITE = (255, 253, 247)
INK = (12, 12, 13)
RED = (209, 31, 31)
GRAY = (111, 111, 114)
CARD = WHITE
CARD_ALT = (237, 231, 218)
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
TEAM_IDS = [
    "team_01_azure",
    "team_02_verde",
    "team_03_rosso",
    "team_04_noir",
    "team_05_arctic",
    "team_06_terra",
]

COLUMNS = [
    ("teraz", "teraz (0.15.0)", ROOT / "out" / "pack_poster"),
    ("shape", "kształt", ROOT / "out" / "pack_poster_shape"),
    ("landmark", "znak", ROOT / "out" / "pack_poster_landmark"),
    ("archetype", "archetyp", ROOT / "out" / "pack_poster_archetype"),
]


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_BOLD if bold else FONT_PATH, size)


def rider_from_id(rid: int, i: int) -> Rider:
    rnd = random.Random(rid)
    return Rider(
        rider_id=rid,
        age=[19, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 36, 38, 41][i % 18],
        region=REGIONS[i % len(REGIONS)],
        height_cm=rnd.randint(166, 192),
        weight_kg=rnd.randint(56, 82),
        discipline=DISCIPLINES[i % len(DISCIPLINES)],
        team_id=TEAM_IDS[i % len(TEAM_IDS)],
    )


def contact_rider(i: int) -> Rider:
    return rider_from_id(500_000 + i * 13, i)


def typical_rider(look_index: int, i: int) -> Rider:
    """Disjoint id space per look so each column is a fresh peloton sample."""
    return rider_from_id(700_000 + look_index * 50_000 + i * 17, i)


def silhouette(img: Image.Image) -> Image.Image:
    alpha = img.getchannel("A")
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.paste(Image.new("RGBA", img.size, (*INK, 255)), mask=alpha)
    return out


def tile(img: Image.Image, size: int, title: str, subtitle: str = "", alt: bool = False) -> Image.Image:
    label_h = 36 if subtitle else 26
    border, shadow = 3, 5
    w, h = size + 2 * border, size + label_h + 2 * border
    card = Image.new("RGBA", (w + shadow, h + shadow), (0, 0, 0, 0))
    d = ImageDraw.Draw(card)
    d.rectangle((shadow, shadow, w + shadow - 1, h + shadow - 1), fill=(*INK, 255))
    d.rectangle((0, 0, w - 1, h - 1), fill=(*(CARD_ALT if alt else CARD), 255), outline=(*INK, 255), width=border)
    card.alpha_composite(img.resize((size, size), Image.LANCZOS), (border, border))
    d.text((border + 5, border + size + 3), title.upper(), fill=INK, font=font(max(10, size // 18), bold=True))
    if subtitle:
        d.text((border + 5, border + size + 4 + max(11, size // 16)), subtitle, fill=GRAY, font=font(max(9, size // 22)))
    return card


def grid(tiles: list[Image.Image], cols: int, pad: int = 6, header: str = "", sub: str = "") -> Image.Image:
    tw, th = tiles[0].size
    rows = (len(tiles) + cols - 1) // cols
    head_h = 0 if not header else (58 if not sub else 108)
    width = cols * (tw + pad) + pad
    if header:
        need = max(font(22, bold=True).getlength(header.upper()), font(13).getlength(sub) if sub else 0)
        width = max(width, int(need) + 2 * pad + 12)
    sheet = Image.new("RGBA", (width, head_h + rows * (th + pad) + pad), (*PAPER, 255))
    d = ImageDraw.Draw(sheet)
    if header:
        d.text((pad + 4, 12), header.upper(), fill=INK, font=font(22, bold=True))
        d.rectangle((pad + 4, 40, min(width - pad, pad + 4 + 210), 46), fill=(*RED, 255))
        if sub:
            d.text((pad + 4, 54), sub, fill=GRAY, font=font(13))
    for i, t in enumerate(tiles):
        x = pad + (i % cols) * (tw + pad)
        y = head_h + pad + (i // cols) * (th + pad)
        sheet.alpha_composite(t, (x, y))
    return sheet


def stack(sheets: list[Image.Image], pad: int = 10) -> Image.Image:
    width = max(s.width for s in sheets) + 2 * pad
    height = pad + sum(s.height + pad for s in sheets)
    out = Image.new("RGBA", (width, height), (*PAPER, 255))
    y = pad
    for sheet in sheets:
        out.alpha_composite(sheet, ((width - sheet.width) // 2, y))
        y += sheet.height + pad
    return out


def load_columns() -> list[tuple[str, str, render.Pack]]:
    out = []
    for key, label, path in COLUMNS:
        if not (path / "manifest.json").exists():
            raise SystemExit(f"missing {path}; run scripts/apply_looks.py first")
        out.append((key, label, render.Pack(path)))
    return out


def portrait(pack: render.Pack, rider: Rider, mode: str) -> tuple[Image.Image, str]:
    app = generate(rider, pack.manifest)
    img = render.render(app, pack)
    if mode == "sil":
        img = silhouette(img)
    elif mode == "icon":
        img = render.crop_head(img, pack)
    hair = (app.mutable.get("hair") or "").replace("hair_", "")
    sub = f"{app.identity['head'].replace('head_', '')}  {hair}"
    return img, sub


def pack_grid(
    pack: render.Pack,
    riders: list[Rider],
    *,
    size: int,
    mode: str,
    cols: int,
    header: str,
    sub: str,
) -> Image.Image:
    tiles = []
    for i, rider in enumerate(riders):
        img, feat = portrait(pack, rider, mode)
        tiles.append(tile(img, size, f"#{rider.rider_id}", feat, alt=i % 2 == 1))
    return grid(tiles, cols=cols, header=header, sub=sub)


def mix_report(columns: list[tuple[str, str, render.Pack]], n: int = 4000) -> str:
    lines = [f"=== mix over {n} riders (same rider_ids per column) ==="]
    riders = [
        Rider(
            rider_id=800_000 + i,
            age=19 + (i % 22),
            region=REGIONS[i % len(REGIONS)],
            discipline=DISCIPLINES[i % len(DISCIPLINES)],
            team_id=TEAM_IDS[i % len(TEAM_IDS)],
        )
        for i in range(n)
    ]
    for key, label, pack in columns:
        heads: Counter[str] = Counter()
        hair: Counter[str] = Counter()
        noses: Counter[str] = Counter()
        mouths: Counter[str] = Counter()
        for rider in riders:
            app = generate(rider, pack.manifest)
            heads[app.identity["head"]] += 1
            hair[app.mutable.get("hair") or "none"] += 1
            noses[app.identity.get("nose") or "none"] += 1
            mouths[app.identity.get("mouth") or "none"] += 1
        lines.append(f"\n-- {label}  pack={pack.manifest.asset_pack_version} look={pack.manifest.look or 'default'} --")
        for title, counter in (("head", heads), ("hair", hair), ("nose", noses), ("mouth", mouths)):
            top = ", ".join(f"{k.split('_', 1)[-1]} {v / n * 100:.1f}%" for k, v in counter.most_common(6))
            lines.append(f"  {title}: {top}")
        if key != "teraz" and key in LOOKS:
            lines.append(f"  skill: {LOOKS[key]['skill']}")
            lines.append(f"  src:   {LOOKS[key]['source']}")
    return "\n".join(lines) + "\n"


def save(img: Image.Image, name: str, *, also_demo: str | None = None) -> Path:
    OUT.mkdir(parents=True, exist_ok=True)
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    dest = OUT / name
    img.save(dest)
    art = ARTIFACTS / name
    img.save(art)
    if also_demo:
        DEMO.mkdir(parents=True, exist_ok=True)
        img.save(DEMO / also_demo)
        print(f"  wrote {dest}  {art}  demo/{also_demo}  ({img.size[0]}x{img.size[1]})")
    else:
        print(f"  wrote {dest}  {art}  ({img.size[0]}x{img.size[1]})")
    return dest


def main() -> int:
    columns = load_columns()
    n = 12
    print("rendering typical-peloton skill comparison")
    typical_sheets = []
    for col_i, (key, label, pack) in enumerate(columns):
        riders = [typical_rider(col_i, i) for i in range(n)]
        blurb = LOOKS[key]["blurb"] if key in LOOKS else "locked poster 0.15.0 — current default"
        typical_sheets.append(
            pack_grid(
                pack,
                riders,
                size=140,
                mode="color",
                cols=4,
                header=label,
                sub=blurb,
            )
        )
    save(
        stack(typical_sheets),
        "avatar_look_skills_typical.png",
        also_demo="11_look_skills_typical.png",
    )

    lineup_tiles = []
    for col_i, (key, label, pack) in enumerate(columns):
        for i in range(n):
            rider = typical_rider(col_i, i)
            img, feat = portrait(pack, rider, "sil")
            lineup_tiles.append(tile(img, 96, label, feat, alt=(col_i + i) % 2 == 1))
    save(
        grid(
            lineup_tiles,
            cols=n,
            header="3-read lineup — one row per skill, 12 different riders",
            sub="black silhouette only. If a row is one blob in twelve wigs, that skill has not broken the cousin peloton.",
        ),
        "avatar_look_skills_lineup.png",
        also_demo="12_look_skills_lineup.png",
    )

    icon_sheets = []
    for col_i, (key, label, pack) in enumerate(columns):
        riders = [typical_rider(col_i, i) for i in range(n)]
        icon_sheets.append(
            pack_grid(pack, riders, size=48, mode="icon", cols=6, header=label, sub="head_crop at 48 px")
        )
    save(
        stack(icon_sheets),
        "avatar_look_skills_48px.png",
        also_demo="13_look_skills_48px.png",
    )

    same_id = []
    same_riders = [contact_rider(i) for i in range(8)]
    for rider in same_riders:
        for key, label, pack in columns:
            img, feat = portrait(pack, rider, "color")
            same_id.append(tile(img, 112, f"#{rider.rider_id}  {label}", feat))
    save(
        grid(
            same_id,
            cols=4,
            header="Same rider_id across looks (sticky picks — mix change is milder here)",
            sub="weighted_pick often keeps the same hair on one seed. Judge cousin-ness on the typical peloton sheet, not this strip.",
        ),
        "avatar_look_skills_same_id.png",
    )

    report = mix_report(columns)
    (OUT / "avatar_look_skills_report.txt").write_text(report, encoding="utf-8")
    (ARTIFACTS / "avatar_look_skills_report.txt").write_text(report, encoding="utf-8")
    (DEMO / "11_look_skills_report.txt").write_text(report, encoding="utf-8")
    print(report)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
