#!/usr/bin/env python3
"""Download DiceBear samples and compose review sheets (research only)."""

from __future__ import annotations

import io
import sys
import urllib.parse
import urllib.request
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

API = "https://api.dicebear.com/10.x"
PAPER = (243, 237, 225)
INK = (12, 12, 13)
RED = (209, 31, 31)
CELL = 192
PAD = 12

RIDERS = [
    "pogacar",
    "vingegaard",
    "evenepoel",
    "vanaert",
    "pidcock",
    "bernal",
    "alaphilippe",
    "roglic",
]

JERSEYS = [
    ("team", "1B4F8A", "Druzyna"),
    ("tour", "FFD400", "Tour"),
    ("giro", "E66FA2", "Giro"),
    ("vuelta", "D11F1F", "Vuelta"),
]

# Shared: paper background, no glasses/helmet. Hair locked to short/male-ish
# lists so the peloton does not arrive in beanies and bobs.
STYLES = {
    "toon-head": {
        "label": "Toon Head",
        "color_key": "clothesColor",
        "base": {
            "clothesVariant": "tShirt",
            "hairVariant": "sideComed,spiky,undercut",
            "rearHairVariant": "neckHigh",
            "eyesVariant": "happy,humble,wink,wide",
            "mouthVariant": "smile,laugh",
            "eyebrowsVariant": "happy,neutral,raised",
        },
    },
    "micah": {
        "label": "Micah / Nice Avatar",
        "color_key": "shirtColor",
        "base": {
            "glassesProbability": "0",
            "earringsProbability": "0",
            "clothesVariant": "crew",
            "hairVariant": "fonze,dougFunny,mrT",
            "eyebrowsVariant": "down,up",
            "eyesVariant": "eyes,round,smiling",
            "mouthVariant": "smile,smirk",
        },
    },
    "avataaars": {
        "label": "Avataaars",
        "color_key": "clothesColor",
        "base": {
            "accessoriesProbability": "0",
            "clothesVariant": "shirtCrewNeck",
            "topVariant": (
                "shortFlat,shortRound,shortWaved,theCaesar,"
                "theCaesarAndSidePart,shortCurly,shavedSides"
            ),
            "eyesVariant": "default,happy,squint,wink,side",
            "mouthVariant": "default,smile,serious,twinkle",
            "eyebrowsVariant": "default,defaultNatural,flatNatural,raisedExcited",
        },
    },
    "open-peeps": {
        "label": "Open Peeps",
        "color_key": "clothingColor",
        "base": {
            "accessoriesProbability": "0",
            "maskProbability": "0",
            "facialHairProbability": "20",
            "headVariant": (
                "short1,short2,short3,short4,short5,pomp,"
                "shaved1,shaved2,shaved3,flatTop,grayShort"
            ),
            "expressionVariant": "smile,calm,serious,driven,blank,solemn",
        },
    },
    "personas": {
        "label": "Personas",
        "color_key": "clothingColor",
        "base": {
            "clothesVariant": "rounded",
            "eyesVariant": "happy,open,wink",
            "mouthVariant": "smile,smirk,bigSmile",
            "hairVariant": (
                "buzzcut,fade,shortCombover,shortComboverChops,"
                "sideShave,curly,curlyHighTop"
            ),
        },
    },
    "pixel-art": {
        "label": "Pixel Art",
        "color_key": "clothingColor",
        "base": {
            "glassesProbability": "0",
            "hatProbability": "0",
            "accessoriesProbability": "0",
            "mouthVariant": (
                "happy01,happy02,happy03,happy04,happy05,happy06"
            ),
            "hairVariant": (
                "short01,short02,short03,short04,short05,short06,"
                "short07,short08,short09,short10,short11,short12"
            ),
        },
    },
}

FACE_ONLY = {
    "adventurer": {
        "label": "Adventurer (twarz)",
        "base": {
            "glassesProbability": "0",
            "earringsProbability": "0",
            "hairVariant": (
                "short01,short02,short03,short04,short05,short06,"
                "short07,short08,short09,short10,short11,short12"
            ),
        },
    },
    "lorelei": {
        "label": "Lorelei (twarz)",
        "base": {
            "glassesProbability": "0",
            "hairAccessoriesProbability": "0",
        },
    },
}


def dicebear_url(style: str, seed: str, extra: dict[str, str], size: int = CELL) -> str:
    q = {
        "seed": seed,
        "size": str(size),
        "backgroundColor": "f3ede1",
        **extra,
    }
    return f"{API}/{style}/png?{urllib.parse.urlencode(q, safe=',')}"


def fetch(url: str) -> Image.Image:
    req = urllib.request.Request(url, headers={"User-Agent": "PelotonManager-avatar-research/1"})
    with urllib.request.urlopen(req, timeout=30) as resp:
        data = resp.read()
    im = Image.open(io.BytesIO(data)).convert("RGBA")
    if im.size != (CELL, CELL):
        im = im.resize((CELL, CELL), Image.Resampling.NEAREST)
    return im


def font(size: int) -> ImageFont.ImageFont:
    for path in (
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def label_bar(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, w: int) -> None:
    x, y = xy
    draw.rectangle((x, y, x + w, y + 28), fill=INK)
    draw.text((x + 8, y + 5), text, fill=(255, 253, 247), font=font(13))


def compose_style_sheet(cache: dict[str, Image.Image], out: Path) -> None:
    styles = list(STYLES.items()) + list(FACE_ONLY.items())
    cols = len(RIDERS)
    rows = len(styles)
    header_h = 64
    row_h = CELL + 28 + PAD
    width = PAD + cols * (CELL + PAD)
    height = header_h + rows * row_h + PAD
    canvas = Image.new("RGB", (width, height), PAPER)
    draw = ImageDraw.Draw(canvas)
    draw.text((PAD, 16), "Generatory z internetu — ci sami 8 kolarzy, bez kasku i okularow", fill=INK, font=font(18))
    draw.text((PAD, 40), "Kolor koszulki = druzyna (niebieski). Twarz z seedu. DiceBear 10.x", fill=RED, font=font(12))

    y = header_h
    for style_id, spec in styles:
        extra = dict(spec["base"])
        if "color_key" in spec:
            extra[spec["color_key"]] = "1B4F8A"
        label_bar(draw, (PAD, y), spec["label"], width - 2 * PAD)
        y += 28
        x = PAD
        for seed in RIDERS:
            url = dicebear_url(style_id, seed, extra)
            key = f"{style_id}|{seed}|team"
            if key not in cache:
                print(f"fetch {key}", flush=True)
                cache[key] = fetch(url)
            canvas.paste(cache[key], (x, y), cache[key])
            x += CELL + PAD
        y += CELL + PAD

    canvas.save(out, "PNG")
    print(f"wrote {out}", flush=True)


def compose_jersey_sheet(cache: dict[str, Image.Image], out: Path) -> None:
    seed = "pogacar"
    styles = list(STYLES.items())
    cols = len(JERSEYS)
    rows = len(styles)
    header_h = 72
    row_h = CELL + 28 + PAD
    width = PAD + cols * (CELL + PAD)
    height = header_h + rows * row_h + PAD
    canvas = Image.new("RGB", (width, height), PAPER)
    draw = ImageDraw.Draw(canvas)
    draw.text((PAD, 14), "Ta sama twarz, inna koszulka — Tour / Giro / Vuelta", fill=INK, font=font(18))
    draw.text((PAD, 40), "Seed = pogacar. Zmienia sie tylko kolor tułowia.", fill=RED, font=font(12))

    y = header_h
    for style_id, spec in styles:
        label_bar(draw, (PAD, y), spec["label"], width - 2 * PAD)
        y += 28
        x = PAD
        for jid, hex_color, jlabel in JERSEYS:
            extra = dict(spec["base"])
            extra[spec["color_key"]] = hex_color
            url = dicebear_url(style_id, seed, extra)
            key = f"{style_id}|{seed}|{jid}"
            if key not in cache:
                print(f"fetch {key}", flush=True)
                cache[key] = fetch(url)
            canvas.paste(cache[key], (x, y), cache[key])
            draw.rectangle((x, y + CELL - 22, x + CELL, y + CELL), fill=INK)
            draw.text((x + 8, y + CELL - 18), jlabel, fill=(255, 253, 247), font=font(12))
            x += CELL + PAD
        y += CELL + PAD

    canvas.save(out, "PNG")
    print(f"wrote {out}", flush=True)


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    out_dir = root / "demo" / "generators"
    out_dir.mkdir(parents=True, exist_ok=True)
    cache: dict[str, Image.Image] = {}
    compose_style_sheet(cache, out_dir / "01_style_comparison.png")
    compose_jersey_sheet(cache, out_dir / "02_jersey_swap.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
