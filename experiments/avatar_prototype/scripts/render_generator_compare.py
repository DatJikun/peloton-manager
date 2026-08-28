#!/usr/bin/env python3
"""Compare public avatar generators against the locked poster pack.

This does NOT change `poster` and does NOT wire a generator into the game.
DiceBear is a seed→PNG HTTP API. Multiavatar returned 403 without a key.
The image-model column is a one-off experiment (not deterministic, not runtime).

    python3 scripts/fetch_external_avatars.py
    python3 scripts/render_generator_compare.py
"""

from __future__ import annotations

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from PIL import Image, ImageDraw, ImageFont

from avatarlab import render
from avatarlab.generate import Rider, generate

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "out" / "demo"
DEMO = ROOT / "demo"
ARTIFACTS = Path("/opt/cursor/artifacts")
DICEBEAR = Path("/tmp/avatar_gens/dicebear")
AI_DIR = Path("/opt/cursor/artifacts/assets")
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
AGES = [19, 21, 22, 23, 24, 25, 26, 27]

COLUMNS = [
    ("teraz", "teraz"),
    ("toon-head", "toon-head"),
    ("personas", "personas"),
    ("avataaars", "avataaars"),
    ("ai", "obraz AI"),
]


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_BOLD if bold else FONT_PATH, size)


def rider_for(i: int) -> Rider:
    rid = 500_000 + i * 13
    rnd = random.Random(rid)
    return Rider(
        rider_id=rid,
        age=AGES[i],
        region=REGIONS[i],
        height_cm=rnd.randint(166, 192),
        weight_kg=rnd.randint(56, 82),
        discipline=DISCIPLINES[i % len(DISCIPLINES)],
        team_id=TEAM_IDS[i % len(TEAM_IDS)],
    )


def tile(img: Image.Image, size: int, title: str, subtitle: str = "", alt: bool = False) -> Image.Image:
    label_h = 36 if subtitle else 26
    border, shadow = 3, 5
    w, h = size + 2 * border, size + label_h + 2 * border
    card = Image.new("RGBA", (w + shadow, h + shadow), (0, 0, 0, 0))
    d = ImageDraw.Draw(card)
    d.rectangle((shadow, shadow, w + shadow - 1, h + shadow - 1), fill=(*INK, 255))
    d.rectangle((0, 0, w - 1, h - 1), fill=(*(CARD_ALT if alt else CARD), 255), outline=(*INK, 255), width=border)
    fitted = img.convert("RGBA").resize((size, size), Image.LANCZOS)
    card.alpha_composite(fitted, (border, border))
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


def missing_card(size: int, title: str, why: str) -> Image.Image:
    img = Image.new("RGBA", (size, size), (*PAPER, 255))
    d = ImageDraw.Draw(img)
    d.rectangle((2, 2, size - 3, size - 3), outline=(*INK, 255), width=3)
    d.text((10, size // 2 - 8), why[:18], fill=GRAY, font=font(11))
    return tile(img, size, title, why)


def load_external(kind: str, rid: int) -> Image.Image | None:
    if kind == "ai":
        path = AI_DIR / f"ai_poster_{rid}.png"
    else:
        path = DICEBEAR / f"{kind}_{rid}.png"
    if not path.exists():
        return None
    return Image.open(path).convert("RGBA")


def save(img: Image.Image, name: str, also_demo: str | None = None) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    img.save(OUT / name)
    img.save(ARTIFACTS / name)
    extra = ""
    if also_demo:
        DEMO.mkdir(parents=True, exist_ok=True)
        img.save(DEMO / also_demo)
        extra = f"  demo/{also_demo}"
    print(f"  wrote {name} {img.size[0]}x{img.size[1]}{extra}")


def main() -> int:
    pack = render.Pack(ROOT / "out" / "pack_poster")
    riders = [rider_for(i) for i in range(8)]
    tiles = []
    for i, rider in enumerate(riders):
        for col_i, (key, label) in enumerate(COLUMNS):
            if key == "teraz":
                img = render.render(generate(rider, pack.manifest), pack)
            else:
                img = load_external(key, rider.rider_id)
                if img is None:
                    tiles.append(missing_card(120, f"#{rider.rider_id}", label))
                    continue
            tiles.append(
                tile(
                    img,
                    120,
                    f"#{rider.rider_id}  {label}",
                    f"{rider.age}y  {rider.region}",
                    alt=(i + col_i) % 2 == 1,
                )
            )
    save(
        grid(
            tiles,
            cols=len(COLUMNS),
            header="Public generators vs locked poster",
            sub="teraz = our pack   |   toon-head / personas / avataaars = DiceBear API   |   obraz AI = one-off image model with our portrait as reference",
        ),
        "avatar_generator_compare.png",
        also_demo="14_generator_compare.png",
    )

    pair = []
    for i, rider in enumerate(riders):
        ours = render.render(generate(rider, pack.manifest), pack)
        ai = load_external("ai", rider.rider_id)
        pair.append(tile(ours, 160, f"#{rider.rider_id}  teraz", f"{rider.age}y  {rider.region}"))
        if ai is None:
            pair.append(missing_card(160, f"#{rider.rider_id}", "obraz AI"))
        else:
            pair.append(tile(ai, 160, f"#{rider.rider_id}  obraz AI", "ten sam rider, inny generator"))
    save(
        grid(
            pair,
            cols=2,
            header="Teraz vs image-model poster (same rider ids)",
            sub="AI can imitate the jersey and ink, but the faces share one handsome template. Not a runtime path.",
        ),
        "avatar_generator_vs_ai.png",
        also_demo="15_generator_vs_ai.png",
    )

    icons = []
    for i, rider in enumerate(riders):
        ours = render.crop_head(render.render(generate(rider, pack.manifest), pack), pack)
        icons.append(tile(ours, 48, f"#{rider.rider_id}", "teraz 48"))
        ai = load_external("ai", rider.rider_id)
        if ai is None:
            icons.append(missing_card(48, f"#{rider.rider_id}", "AI"))
        else:
            icons.append(tile(ai, 48, f"#{rider.rider_id}", "AI 48"))
    save(
        grid(
            icons,
            cols=4,
            header="48 px — does the generator still read as a list icon?",
            sub="teraz uses head_crop. AI is a full-square resize. Owner judges avatars at this size.",
        ),
        "avatar_generator_48px.png",
        also_demo="16_generator_48px.png",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
