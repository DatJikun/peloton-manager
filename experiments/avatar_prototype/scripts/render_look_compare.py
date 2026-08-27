#!/usr/bin/env python3
"""Four-column skill comparison: teraz vs kształt vs znak vs archetyp.

Same rider_ids in every column so a row is one person under four mixes.
Writes review sheets into out/demo/ and /opt/cursor/artifacts/.

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


def contact_rider(i: int) -> Rider:
    rid = 500_000 + i * 13
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


def load_columns() -> list[tuple[str, str, render.Pack]]:
    out = []
    for key, label, path in COLUMNS:
        if not (path / "manifest.json").exists():
            raise SystemExit(f"missing {path}; run scripts/apply_looks.py first")
        out.append((key, label, render.Pack(path)))
    return out


def render_row_sheet(
    columns: list[tuple[str, str, render.Pack]],
    riders: list[Rider],
    *,
    size: int,
    mode: str,
    header: str,
    sub: str,
) -> Image.Image:
    tiles: list[Image.Image] = []
    for i, rider in enumerate(riders):
        for col_i, (key, label, pack) in enumerate(columns):
            app = generate(rider, pack.manifest)
            img = render.render(app, pack)
            if mode == "sil":
                img = silhouette(img)
            elif mode == "icon":
                img = render.crop_head(img, pack)
            hair = (app.mutable.get("hair") or "").replace("hair_", "")
            tiles.append(
                tile(
                    img,
                    size,
                    f"#{rider.rider_id}  {label}",
                    f"{app.identity['head'].replace('head_', '')}  {hair}",
                    alt=(i + col_i) % 2 == 1,
                )
            )
    return grid(tiles, cols=len(columns), header=header, sub=sub)


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


def save(img: Image.Image, name: str) -> Path:
    OUT.mkdir(parents=True, exist_ok=True)
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    dest = OUT / name
    img.save(dest)
    art = ARTIFACTS / name
    img.save(art)
    print(f"  wrote {dest}  and  {art}  ({img.size[0]}x{img.size[1]})")
    return dest


def main() -> int:
    columns = load_columns()
    riders12 = [contact_rider(i) for i in range(12)]
    riders24 = [contact_rider(i) for i in range(24)]
    print("rendering skill comparison sheets")
    save(
        render_row_sheet(
            columns,
            riders12,
            size=140,
            mode="color",
            header="Skill comparison — same rider_id in every column",
            sub="teraz = locked poster 0.15.0   |   kształt = 3-read / circle-square-triangle   |   znak = one loud identity mark   |   archetyp = ActorMIXER families",
        ),
        "avatar_look_compare_skills.png",
    )
    save(
        render_row_sheet(
            columns,
            riders12,
            size=120,
            mode="sil",
            header="3-read silhouette test — can you tell them apart in black?",
            sub="shape-language skill: if two columns still share a blob, the mix has not broken the cousin peloton",
        ),
        "avatar_look_compare_silhouettes.png",
    )
    save(
        render_row_sheet(
            columns,
            riders12,
            size=48,
            mode="icon",
            header="48 px UI crop — does the look still read on a list icon?",
            sub="head_crop then 48 px. The owner judges avatars at this size, not as portraits.",
        ),
        "avatar_look_compare_48px.png",
    )
    save(
        render_row_sheet(
            columns,
            riders24,
            size=96,
            mode="color",
            header="Denser peloton — 24 riders × 4 looks",
            sub="same contact-sheet ids as 01_contact_sheet (500000 + i*13)",
        ),
        "avatar_look_compare_peloton.png",
    )
    report = mix_report(columns)
    (OUT / "avatar_look_compare_report.txt").write_text(report, encoding="utf-8")
    (ARTIFACTS / "avatar_look_compare_report.txt").write_text(report, encoding="utf-8")
    print(report)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
