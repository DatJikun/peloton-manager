"""Bakes the PLACEHOLDER asset pack + manifest.

In production this whole module is replaced by an art pipeline (AI generation
from the master reference, or hand-painted assets) that writes the same folder
layout and the same manifest.json. The game code never imports this module.

All geometry is expressed relative to the master reference (`hy(f)` = fraction
of the head height, `HEAD_HW` = half head width), so a recipe stays valid if the
framing is ever re-scaled.

Asset id convention:   <category>_<nn>_<descriptor>
Part file convention:  <category>/<asset_id>__<part>.png
"""

from __future__ import annotations

import math
import random
from pathlib import Path
from typing import Any, Iterable

from PIL import Image, ImageChops

from .. import manifest as manifest_mod
from .draw import (
    BROW_Y,
    CANVAS_SPEC,
    CHIN_Y,
    CX,
    EAR_BOTTOM,
    EAR_TOP,
    EYE_DX,
    EYE_Y,
    HEAD_H,
    HEAD_HW,
    JAW_Y,
    MOUTH_Y,
    NECK_TOP,
    NOSE_TIP_Y,
    SHOULDER_Y,
    SIZE,
    SKULL_TOP,
    TORSO_HW,
    blur,
    brighten,
    down,
    ellipse_mask,
    flat,
    grad_h,
    grad_radial,
    grad_v,
    harden_edges,
    head_contour,
    hy,
    mirror_x,
    mul,
    new_l,
    erode,
    poly_mask,
    quantize_tones,
    rim,
    set_style,
    st,
    STYLES,
    scale_l,
    stroke_mask,
)

INK_RGB = (12, 12, 13)  # UI lab --black, so the portrait line weight matches the panels
SHADOW_RGB = (74, 50, 42)
LIGHT_RGB = (255, 250, 242)
COOL_SHADOW_RGB = (70, 70, 86)

LAYER_ORDER = (
    "neck",
    "jersey",
    "jersey_overlay",
    "ears",
    "head",
    "nose",
    "mouth",
    "eyes",
    "eyebrows",
    "skin_details",
    "wrinkles",
    "facial_hair",
    "hair",
    "glasses",
    "helmet",
)


# --------------------------------------------------------------------------- #
# layer packing helpers
# --------------------------------------------------------------------------- #


def _styled(shade: Image.Image, outline: Image.Image | None) -> Image.Image:
    """Apply the style's tone quantisation, then the outline on top of it."""
    out = quantize_tones(shade)
    if outline is not None:
        out = ImageChops.subtract(out, scale_l(outline, st().outline_darkness))
    return out


def keyline(mask: Image.Image, width_scale: float = 1.0) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    """Ink contour drawn INSIDE the silhouette.

    Inside, not centred on the edge, because an outer stroke would overlap the
    neighbouring layers and could drift when a continuous parameter scales the
    feature. Returns a list so callers can splice it into their part list.
    """
    w = st().line_art * width_scale
    if w <= 0.0:
        return []
    return [("line", solid_layer(INK_RGB, harden_edges(scale_l(rim(mask, w, 0.5), 1.7))), {"blend": "normal"})]


def outline_of(mask: Image.Image) -> Image.Image | None:
    """Inner outline: darker version of the fill, drawn inside the silhouette so
    it can never break alignment with the neighbouring layers."""
    w = st().outline
    if w <= 0.0:
        return None
    return rim(mask, w, 0.7)


def gray_layer(
    shade: Image.Image, alpha: Image.Image, outline: Image.Image | None = None, crisp: bool = True
) -> Image.Image:
    """`crisp=False` for layers whose soft falloff is the point (brows, stubble):
    hardening those turns a gradient into an amoeba."""
    sh = down(_styled(shade, outline))
    return Image.merge("RGBA", (sh, sh, sh, down(harden_edges(alpha) if crisp else alpha)))


def solid_layer(rgb: tuple[int, int, int], alpha: Image.Image) -> Image.Image:
    al = down(alpha)
    r, g, b = Image.new("RGB", (SIZE, SIZE), rgb).split()
    return Image.merge("RGBA", (r, g, b, al))


def shaded_color_layer(
    rgb: tuple[int, int, int], shade: Image.Image, alpha: Image.Image, outline: Image.Image | None = None
) -> Image.Image:
    sh = down(_styled(shade, outline))
    chans = tuple(sh.point(lambda v, c=c: int(min(255, v * c / 255.0))) for c in rgb)  # type: ignore[misc]
    return Image.merge("RGBA", (*chans, down(harden_edges(alpha))))


def darken(shade: Image.Image, mask: Image.Image, strength: float) -> Image.Image:
    return ImageChops.subtract(shade, scale_l(mask, strength * st().form_strength))


def lighten(shade: Image.Image, mask: Image.Image, strength: float) -> Image.Image:
    return brighten(shade, mask, strength * st().highlight_strength)


def detail(rgb: tuple[int, int, int], alpha: Image.Image, strength: float) -> Image.Image:
    """Skin/wrinkle overlay whose intensity follows the style."""
    return solid_layer(rgb, scale_l(alpha, strength * st().detail_alpha))


# --------------------------------------------------------------------------- #
# pack builder
# --------------------------------------------------------------------------- #


class PackBuilder:
    def __init__(self, root: Path, pack_id: str, pack_version: str, style: str) -> None:
        self.root = root
        self.pack_id = pack_id
        self.pack_version = pack_version
        self.style = style
        self.assets: list[dict[str, Any]] = []
        self.palettes: dict[str, dict[str, list[int]]] = {}
        self.teams: dict[str, dict[str, Any]] = {}

    def save(self, category: str, asset_id: str, part: str, img: Image.Image) -> str:
        rel = f"{category}/{asset_id}__{part}.png"
        path = self.root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        img.save(path, optimize=True)
        return rel

    def asset(
        self,
        category: str,
        asset_id: str,
        parts: Iterable[tuple[str, Image.Image, dict[str, Any]]],
        *,
        weight: float = 1.0,
        anchor: tuple[float, float] = (CX, EYE_Y),
        mirrored: bool = False,
        tags: Iterable[str] = (),
        min_age: int | None = None,
        max_age: int | None = None,
        requires_tags: Iterable[str] = (),
        excludes_tags: Iterable[str] = (),
        roles: Iterable[str] = (),
        region_weights: dict[str, float] | None = None,
    ) -> None:
        part_defs = []
        for name, img, meta in parts:
            if img.getchannel("A").getextrema()[1] <= 2:
                continue  # empty or below the validator's visible-alpha floor
            rel = self.save(category, asset_id, name, img)
            part_defs.append({"file": rel, **meta})
        if not part_defs:
            return  # style suppressed every part (e.g. road rash at very low detail_alpha)
        entry: dict[str, Any] = {
            "id": asset_id,
            "category": category,
            "weight": weight,
            "anchor": [anchor[0], anchor[1]],
            "parts": part_defs,
        }
        if mirrored:
            entry["mirrored"] = True
        if tags:
            entry["tags"] = list(tags)
        if min_age is not None:
            entry["min_age"] = min_age
        if max_age is not None:
            entry["max_age"] = max_age
        if requires_tags:
            entry["requires_tags"] = list(requires_tags)
        if excludes_tags:
            entry["excludes_tags"] = list(excludes_tags)
        if roles:
            entry["roles"] = list(roles)
        if region_weights:
            entry["region_weights"] = region_weights
        self.assets.append(entry)

    def virtual(
        self,
        category: str,
        asset_id: str,
        rgb: list[int],
        weight: float,
        region_weights: dict[str, float] | None = None,
    ) -> None:
        """Colour-only 'asset' (hair colour, iris colour): weighted, no pixels."""
        entry: dict[str, Any] = {"id": asset_id, "category": category, "weight": weight, "parts": []}
        if region_weights:
            entry["region_weights"] = region_weights
        self.assets.append(entry)
        self.palettes.setdefault(category, {})[asset_id] = rgb

    def write_manifest(self) -> None:
        manifest_mod.dump(
            {
                "pack_id": self.pack_id,
                "style": self.style,
                "asset_pack_version": self.pack_version,
                "avatar_schema_version": 1,
                "seed_version": 1,
                "canvas": CANVAS_SPEC,
                "layer_order": list(LAYER_ORDER),
                "palettes": self.palettes,
                "teams": self.teams,
                "assets": self.assets,
            },
            self.root / "manifest.json",
        )


# --------------------------------------------------------------------------- #
# head
# --------------------------------------------------------------------------- #

HEAD_RECIPES: list[tuple[str, float, dict[str, float], tuple[str, ...]]] = [
    ("head_01_oval", 0.15, {}, ("jaw_medium",)),
    ("head_02_long", 0.11, {"cranium_w": 0.93, "temple_w": 0.95, "cheek_w": 0.93, "jaw_w": 0.88, "chin_len": 14.0}, ("jaw_narrow",)),
    ("head_03_square", 0.12, {"jaw_w": 1.14, "chin_w": 1.26, "cheek_w": 1.04, "temple_w": 1.04, "chin_len": -8.0}, ("jaw_wide",)),
    ("head_04_round", 0.11, {"cheek_w": 1.10, "jaw_w": 1.07, "chin_w": 1.10, "cranium_w": 1.04, "chin_len": -16.0}, ("jaw_medium",)),
    ("head_05_angular", 0.11, {"temple_w": 1.07, "cheek_w": 1.02, "jaw_w": 0.92, "chin_w": 0.76, "chin_len": 8.0}, ("jaw_narrow",)),
    ("head_06_broad", 0.10, {"cranium_w": 1.08, "temple_w": 1.08, "cheek_w": 1.11, "jaw_w": 1.08, "chin_w": 1.06, "chin_len": -6.0}, ("jaw_wide",)),
    ("head_07_tapered", 0.09, {"cranium_w": 1.05, "temple_w": 1.02, "jaw_w": 0.84, "chin_w": 0.70, "chin_len": 6.0}, ("jaw_narrow",)),
    ("head_08_heavy_jaw", 0.09, {"jaw_w": 1.16, "chin_w": 1.20, "crown_w": 0.94, "cheek_w": 1.02, "chin_len": 2.0}, ("jaw_wide",)),
    ("head_09_high_crown", 0.07, {"crown": 12.0, "crown_w": 1.08, "cheek_w": 0.97, "jaw_w": 0.94, "chin_w": 0.88}, ("jaw_medium",)),
    ("head_10_wide_short", 0.05, {"cranium_w": 1.07, "temple_w": 1.06, "cheek_w": 1.08, "jaw_w": 1.02, "chin_w": 1.00, "chin_len": -20.0}, ("jaw_medium",)),
]


def bake_head(p: dict[str, float]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    mask = poly_mask(head_contour(p))
    chin_y = CHIN_Y + p.get("chin_len", 0.0)
    cw = p.get("cheek_w", 1.0)

    painterly = st().tone_steps < 2
    shade = mul(flat(1.0), grad_radial(CX - 0.34 * HEAD_HW, hy(0.42), 2.35 * HEAD_HW, 1.0, 0.70), grad_v(1.03, 0.94))
    if painterly:
        # many soft terms: cheekbones, temples, hollows. Posterising these would
        # turn every one of them into a hard-edged patch, so a flat style gets a
        # small number of deliberate shapes instead.
        shade = lighten(shade, ImageChops.multiply(mask, blur(ellipse_mask(CX - 0.2 * HEAD_HW, hy(0.26), 0.58 * HEAD_HW, 0.15 * HEAD_H), 26)), 0.10)
        for sx in (-1, 1):
            shade = lighten(
                shade,
                ImageChops.multiply(mask, blur(ellipse_mask(CX + sx * 0.62 * HEAD_HW * cw, hy(0.60), 0.32 * HEAD_HW, 0.085 * HEAD_H), 22)),
                0.075,
            )
            shade = darken(
                shade,
                ImageChops.multiply(mask, blur(ellipse_mask(CX + sx * 0.70 * HEAD_HW * cw, hy(0.79), 0.27 * HEAD_HW, 0.095 * HEAD_H), 18)),
                0.12,
            )
            shade = darken(shade, ImageChops.multiply(mask, blur(ellipse_mask(CX + sx * 0.88 * HEAD_HW, hy(0.37), 0.20 * HEAD_HW, 0.13 * HEAD_H), 16)), 0.09)
        shade = lighten(shade, ImageChops.multiply(mask, blur(ellipse_mask(CX, chin_y - 0.075 * HEAD_H, 32, 15), 14)), 0.05)
    else:
        # one clean crescent on the shaded side of the face
        side = ImageChops.subtract(mask, erode(poly_mask(head_contour({**p, "cheek_w": 0.86, "jaw_w": 0.84, "cranium_w": 0.88, "temple_w": 0.88})), 0.0))
        shade = darken(shade, ImageChops.multiply(mask, blur(ImageChops.multiply(side, grad_h(0.0, 1.6)), 9)), 0.20)
    for sx in (-1, 1):
        shade = darken(shade, ImageChops.multiply(mask, blur(ellipse_mask(CX + sx * EYE_DX, EYE_Y - 4, 38, 22), 14)), 0.13)
        shade = darken(shade, ImageChops.multiply(mask, blur(ellipse_mask(CX + sx * EYE_DX, BROW_Y + 11, 40, 10), 10)), 0.09)
    shade = darken(shade, rim(mask, 13.0, 7.0), 0.32)
    shade = darken(shade, blur(ellipse_mask(CX, NOSE_TIP_Y + 11, 32, 9), 9), 0.10)
    shade = darken(shade, blur(ellipse_mask(CX, MOUTH_Y + 22, 28, 11), 10), 0.10)
    shade = darken(shade, ImageChops.multiply(blur(ellipse_mask(CX, chin_y + 0.03 * HEAD_H, 0.92 * HEAD_HW, 0.10 * HEAD_H), 16), mask), 0.16)
    return [("skin", gray_layer(shade, mask, outline_of(mask)), {"blend": "normal", "color_slot": "skin"})] + keyline(mask)


# --------------------------------------------------------------------------- #
# neck + jersey
# --------------------------------------------------------------------------- #


def bake_neck() -> tuple[Image.Image, Image.Image, Image.Image]:
    pts = [
        (CX - 44, NECK_TOP - 40),
        (CX - 47, NECK_TOP + 18),
        (CX - 70, SHOULDER_Y - 6),
        (CX + 70, SHOULDER_Y - 6),
        (CX + 47, NECK_TOP + 18),
        (CX + 44, NECK_TOP - 40),
    ]
    mask = poly_mask(pts, iters=2)
    shade = mul(flat(1.0), grad_h(1.02, 0.86), grad_v(1.02, 0.88))
    shade = darken(shade, blur(ellipse_mask(CX, NECK_TOP + 4, 76, 40), 18), 0.34 if st().line_art <= 0 else 0.62)
    shade = darken(shade, rim(mask, 12.0, 8.0), 0.30)
    jaw_shadow = solid_layer(SHADOW_RGB, scale_l(blur(ellipse_mask(CX, NECK_TOP - 6, 80, 26), 16), 0.26))
    return gray_layer(shade, mask), jaw_shadow, mask


COLLAR_TOP = SHOULDER_Y - 62.0

TORSO_PTS = [
    (CX - TORSO_HW, SIZE + 30),
    (CX - 214, SHOULDER_Y + 22),
    (CX - 138, SHOULDER_Y - 6),
    (CX - 92, SHOULDER_Y - 30),
    (CX - 58, COLLAR_TOP + 10),
    (CX - 48, COLLAR_TOP - 2),
    (CX + 48, COLLAR_TOP - 2),
    (CX + 58, COLLAR_TOP + 10),
    (CX + 92, SHOULDER_Y - 30),
    (CX + 138, SHOULDER_Y - 6),
    (CX + 214, SHOULDER_Y + 22),
    (CX + TORSO_HW, SIZE + 30),
]


def torso_mask() -> Image.Image:
    return poly_mask(TORSO_PTS, iters=3)


COLLAR_EDGE = [
    (CX - 57, COLLAR_TOP + 8),
    (CX - 52, COLLAR_TOP + 26),
    (CX, COLLAR_TOP + 37),
    (CX + 52, COLLAR_TOP + 26),
    (CX + 57, COLLAR_TOP + 8),
]


def collar_mask() -> Image.Image:
    outer = poly_mask(
        [
            (CX - 57, COLLAR_TOP + 8),
            (CX - 52, COLLAR_TOP + 26),
            (CX, COLLAR_TOP + 37),
            (CX + 52, COLLAR_TOP + 26),
            (CX + 57, COLLAR_TOP + 8),
            (CX + 50, COLLAR_TOP - 2),
            (CX - 50, COLLAR_TOP - 2),
        ]
    )
    inner = poly_mask(
        [
            (CX - 50, COLLAR_TOP + 4),
            (CX - 46, COLLAR_TOP + 20),
            (CX, COLLAR_TOP + 30),
            (CX + 46, COLLAR_TOP + 20),
            (CX + 50, COLLAR_TOP + 4),
        ]
    )
    ring = ImageChops.subtract(outer, inner)
    # keep the front arc only; a full ring around the neck reads as jewellery
    front = poly_mask(
        [(CX - 70, COLLAR_TOP + 13), (CX + 70, COLLAR_TOP + 13), (CX + 70, SIZE), (CX - 70, SIZE)], iters=1
    )
    return ImageChops.multiply(ring, front)


def bake_jersey(template: str) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    body = torso_mask()
    shade = mul(flat(1.0), grad_h(1.05, 0.90))
    shade = darken(shade, rim(body, 11.0, 8.0), 0.20)
    for sx in (-1, 1):
        shade = darken(shade, ImageChops.multiply(blur(ellipse_mask(CX + sx * 168, 500, 34, 46), 20), body), 0.16)
        shade = darken(shade, ImageChops.multiply(blur(ellipse_mask(CX + sx * 108, SHOULDER_Y + 6, 26, 30), 18), body), 0.08)
    shade = darken(shade, ImageChops.multiply(blur(ellipse_mask(CX, COLLAR_TOP + 40, 66, 24), 16), body), 0.16)
    shade = lighten(shade, ImageChops.multiply(blur(ellipse_mask(CX - 118, SHOULDER_Y - 4, 54, 26), 24), body), 0.08)

    if template == "jersey_01_raglan":
        panel = new_l()
        for sx in (-1, 1):
            panel = ImageChops.lighter(
                panel,
                ImageChops.multiply(
                    body,
                    poly_mask(
                        [
                            (CX + sx * TORSO_HW, SIZE + 24),
                            (CX + sx * (TORSO_HW - 6), SHOULDER_Y - 4),
                            (CX + sx * 104, SHOULDER_Y - 26),
                            (CX + sx * 158, SIZE + 24),
                        ]
                    ),
                ),
            )
    else:  # horizontal chest band
        panel = ImageChops.multiply(
            body,
            poly_mask(
                [
                    (CX - TORSO_HW, SHOULDER_Y + 4),
                    (CX + TORSO_HW, SHOULDER_Y - 2),
                    (CX + TORSO_HW, SHOULDER_Y + 28),
                    (CX - TORSO_HW, SHOULDER_Y + 34),
                ],
                iters=1,
            ),
        )

    collar = ImageChops.multiply(collar_mask(), body)
    piping = ImageChops.multiply(collar, blur(stroke_mask(COLLAR_EDGE, 2.2), 0.8))
    zip_line = ImageChops.multiply(body, stroke_mask([(CX, COLLAR_TOP + 30), (CX + 2, SIZE + 10)], 3.4))
    return [
        ("base", shaded_color_layer((255, 255, 255), shade, body, outline_of(body)), {"blend": "normal", "color_slot": "team_primary"}),
        ("panel", shaded_color_layer((255, 255, 255), shade, panel), {"blend": "normal", "color_slot": "team_secondary"}),
        ("collar", shaded_color_layer((255, 255, 255), shade, collar), {"blend": "normal", "color_slot": "team_primary"}),
        ("piping", shaded_color_layer((255, 255, 255), shade, piping), {"blend": "normal", "color_slot": "team_accent"}),
        ("seam", solid_layer(SHADOW_RGB, scale_l(blur(zip_line, 1.4), 0.32)), {"blend": "multiply"}),
        ("sheen", solid_layer(LIGHT_RGB, scale_l(ImageChops.multiply(blur(ellipse_mask(CX - 130, 496, 52, 30), 24), body), 0.14)), {"blend": "screen"}),
    ] + keyline(body)


def bake_outfit(kind: str) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    """Manager torsos: same canvas and shoulder line as a jersey, civilian cut."""
    body = torso_mask()
    shade = mul(flat(1.0), grad_h(1.05, 0.90))
    shade = darken(shade, rim(body, 11.0, 8.0), 0.20)
    for sx in (-1, 1):
        shade = darken(shade, ImageChops.multiply(blur(ellipse_mask(CX + sx * 168, 500, 34, 46), 20), body), 0.16)
    shade = darken(shade, ImageChops.multiply(blur(ellipse_mask(CX, COLLAR_TOP + 40, 66, 24), 16), body), 0.16)

    if kind == "outfit_03_suit":
        shirt = ImageChops.multiply(
            body,
            poly_mask([(CX - 34, COLLAR_TOP + 6), (CX + 34, COLLAR_TOP + 6), (CX + 46, SIZE + 10), (CX - 46, SIZE + 10)]),
        )
        lapel = new_l()
        for sx in (-1, 1):
            lapel = ImageChops.lighter(
                lapel,
                ImageChops.multiply(
                    body,
                    poly_mask(
                        [
                            (CX + sx * 30, COLLAR_TOP + 4),
                            (CX + sx * 76, COLLAR_TOP + 16),
                            (CX + sx * 62, SIZE + 10),
                            (CX + sx * 18, SIZE + 10),
                        ]
                    ),
                ),
            )
        tie = ImageChops.multiply(
            body, poly_mask([(CX - 9, COLLAR_TOP + 26), (CX + 9, COLLAR_TOP + 26), (CX + 15, SIZE + 10), (CX - 15, SIZE + 10)])
        )
        return [
            ("base", shaded_color_layer((58, 60, 68), shade, body, outline_of(body)), {"blend": "normal"}),
            ("shirt", shaded_color_layer((240, 242, 246), shade, shirt), {"blend": "normal"}),
            ("lapel", shaded_color_layer((44, 46, 54), shade, lapel), {"blend": "normal"}),
            ("tie", shaded_color_layer((255, 255, 255), shade, tie), {"blend": "normal", "color_slot": "team_primary"}),
        ]

    collar = ImageChops.multiply(collar_mask(), body)
    if kind == "outfit_01_polo":
        placket = ImageChops.multiply(
            body, poly_mask([(CX - 13, COLLAR_TOP + 30), (CX + 13, COLLAR_TOP + 30), (CX + 15, SHOULDER_Y + 40), (CX - 15, SHOULDER_Y + 40)])
        )
        buttons = new_l()
        for y in (SHOULDER_Y + 2, SHOULDER_Y + 24):
            buttons = ImageChops.lighter(buttons, ImageChops.multiply(body, blur(ellipse_mask(CX + 1, y, 3.0, 3.0), 0.8)))
        return [
            ("base", shaded_color_layer((255, 255, 255), shade, body, outline_of(body)), {"blend": "normal", "color_slot": "team_primary"}),
            ("collar", shaded_color_layer((255, 255, 255), shade, collar), {"blend": "normal", "color_slot": "team_primary"}),
            ("piping", shaded_color_layer((255, 255, 255), shade, ImageChops.multiply(collar, blur(stroke_mask(COLLAR_EDGE, 2.2), 0.8))), {"blend": "normal", "color_slot": "team_secondary"}),
            ("placket", shaded_color_layer((255, 255, 255), shade, placket), {"blend": "normal", "color_slot": "team_secondary"}),
            ("buttons", solid_layer((246, 246, 248), scale_l(buttons, 0.85)), {"blend": "normal"}),
        ]

    # outfit_02_softshell: zipped team jacket
    zip_line = ImageChops.multiply(body, stroke_mask([(CX, COLLAR_TOP + 26), (CX + 2, SIZE + 10)], 3.6))
    yoke = ImageChops.multiply(
        body, poly_mask([(CX - TORSO_HW, SHOULDER_Y + 30), (CX + TORSO_HW, SHOULDER_Y + 24), (CX + TORSO_HW, SIZE + 30), (CX - TORSO_HW, SIZE + 30)], iters=1)
    )
    return [
        ("base", shaded_color_layer((255, 255, 255), shade, body, outline_of(body)), {"blend": "normal", "color_slot": "team_primary"}),
        ("yoke", shaded_color_layer((255, 255, 255), shade, yoke), {"blend": "normal", "color_slot": "team_accent"}),
        ("collar", shaded_color_layer((255, 255, 255), shade, collar), {"blend": "normal", "color_slot": "team_primary"}),
        ("piping", shaded_color_layer((255, 255, 255), shade, ImageChops.multiply(collar, blur(stroke_mask(COLLAR_EDGE, 2.2), 0.8))), {"blend": "normal", "color_slot": "team_secondary"}),
        ("zip", solid_layer(SHADOW_RGB, scale_l(blur(zip_line, 1.2), 0.42)), {"blend": "multiply"}),
    ]


def _chest_band(y0: float, h: float, body: Image.Image) -> Image.Image:
    return ImageChops.multiply(
        body,
        poly_mask(
            [(CX - TORSO_HW, y0 + 4), (CX + TORSO_HW, y0), (CX + TORSO_HW, y0 + h), (CX - TORSO_HW, y0 + h + 4)],
            iters=1,
        ),
    )


def bake_overlay_rainbow() -> list[tuple[str, Image.Image, dict[str, Any]]]:
    body = torso_mask()
    bands = [(64, 96, 196), (196, 52, 48), (34, 34, 38), (238, 196, 44), (44, 148, 78)]
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    for i, rgb in enumerate(bands):
        out = Image.alpha_composite(out, solid_layer(rgb, _chest_band(SHOULDER_Y + 12 + i * 9.0, 9.0, body)))
    return [("bands", out, {"blend": "normal"})]


def bake_overlay_champion() -> list[tuple[str, Image.Image, dict[str, Any]]]:
    body = torso_mask()
    return [
        ("band_a", solid_layer((255, 255, 255), _chest_band(SHOULDER_Y + 14, 13.0, body)), {"blend": "normal", "color_slot": "team_secondary"}),
        ("band_b", solid_layer((255, 255, 255), _chest_band(SHOULDER_Y + 32, 13.0, body)), {"blend": "normal", "color_slot": "team_accent"}),
    ]


# --------------------------------------------------------------------------- #
# ears
# --------------------------------------------------------------------------- #

EAR_RECIPES = [
    ("ears_01_medium", 0.44, {"w": 23.0, "out": 0.0}),
    ("ears_02_small_flat", 0.30, {"w": 20.0, "out": -2.0, "h": -5.0}),
    ("ears_03_large", 0.18, {"w": 26.0, "out": 3.0, "h": 6.0}),
    ("ears_04_protruding", 0.08, {"w": 25.0, "out": 8.0}),
]


def bake_ear(p: dict[str, float]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    x = CX + HEAD_HW - 4 + p.get("out", 0.0)
    top = EAR_TOP - p.get("h", 0.0)
    bot = EAR_BOTTOM + p.get("h", 0.0)
    w = p["w"]
    pts = [
        (x - 8, top + 4),
        (x + w * 0.55, top - 2),
        (x + w, top + 18),
        (x + w * 0.94, (top + bot) / 2 + 4),
        (x + w * 0.52, bot - 4),
        (x + 2, bot),
        (x - 10, bot - 14),
    ]
    mask = poly_mask(pts)
    shade = mul(flat(1.0), grad_h(1.04, 0.82))
    shade = darken(shade, rim(mask, 5.0, 4.0), 0.24)
    inner = ImageChops.multiply(
        mask,
        blur(poly_mask([(x + 1, top + 14), (x + w * 0.54, top + 20), (x + w * 0.42, bot - 18), (x + 1, bot - 22)]), 5),
    )
    shade = darken(shade, inner, 0.28)
    # contact shadow where the head overlaps the ear, otherwise the ear
    # disappears into the cheek once the head layer is drawn on top
    shade = darken(shade, ImageChops.multiply(mask, blur(ellipse_mask(x - 4, (top + bot) / 2, 10, (bot - top) * 0.7), 6)), 0.34)
    return [("skin", gray_layer(shade, mask, outline_of(mask)), {"blend": "normal", "color_slot": "skin"})] + keyline(mask, 0.8)


# --------------------------------------------------------------------------- #
# eyes
# --------------------------------------------------------------------------- #

EYE_RECIPES = [
    ("eyes_01_almond", 0.16, {}),
    ("eyes_02_wide", 0.13, {"hw": 25.0, "th": 10.4, "bh": 8.0}),
    ("eyes_03_narrow", 0.13, {"hw": 24.0, "th": 7.0, "bh": 5.2}),
    ("eyes_04_hooded", 0.12, {"th": 7.6, "hood": 4.5, "lash": 1.3}),
    ("eyes_05_downturned", 0.10, {"tilt": 3.0}),
    ("eyes_06_upturned", 0.10, {"tilt": -3.5}),
    ("eyes_07_deepset", 0.09, {"hood": 6.0, "th": 8.0, "crease": 11.0}),
    ("eyes_08_round", 0.09, {"hw": 21.0, "th": 10.8, "bh": 9.0}),
    ("eyes_09_monolid", 0.08, {"hood": 3.5, "crease": 14.0, "crease_a": 0.10, "th": 7.8}),
]


def eye_shape(p: dict[str, float]) -> list[tuple[float, float]]:
    cx, cy = CX + EYE_DX, EYE_Y
    k = st().feature_boost
    hw = p.get("hw", 23.0) * k
    th = p.get("th", 8.8) * k
    bh = p.get("bh", 6.6) * k
    tilt = p.get("tilt", 0.0)
    return [
        (cx - hw, cy + 1.5 + tilt * 0.4),
        (cx - hw * 0.45, cy - th * 0.86),
        (cx + hw * 0.15, cy - th),
        (cx + hw * 0.72, cy - th * 0.66 + tilt * 0.5),
        (cx + hw, cy + 1.0 - tilt),
        (cx + hw * 0.6, cy + bh * 0.82 - tilt * 0.4),
        (cx - hw * 0.2, cy + bh),
        (cx - hw * 0.7, cy + bh * 0.66),
    ]


def bake_eye(p: dict[str, float]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    cx, cy = CX + EYE_DX, EYE_Y
    shape = eye_shape(p)
    sclera = poly_mask(shape)

    # --- under: sclera, with the shadow the upper lid casts on it -----------
    sc_shade = mul(flat(0.92), grad_v(1.06, 0.92))
    sc_shade = darken(sc_shade, rim(sclera, 4.0, 3.0), 0.34)
    sc_shade = darken(sc_shade, ImageChops.multiply(sclera, blur(ellipse_mask(cx, cy - p.get("th", 8.8) * 0.62, 22, 7), 5)), 0.22)
    under = shaded_color_layer((242, 238, 232), sc_shade, sclera)

    # --- iris + pupil (iris is tinted at runtime) --------------------------
    r = p.get("iris_r", 9.9) * st().feature_boost
    ix = cx + p.get("iris_dx", 0.0)
    iris_mask = ImageChops.multiply(sclera, ellipse_mask(ix, cy - 0.5, r, r))
    iris_shade = mul(flat(1.0), grad_v(1.16, 0.74))
    iris_shade = darken(iris_shade, rim(ellipse_mask(ix, cy - 0.5, r, r), 2.4, 1.5), 0.42)
    iris = shaded_color_layer((255, 255, 255), iris_shade, iris_mask)
    pupil = solid_layer((16, 13, 12), ImageChops.multiply(sclera, blur(ellipse_mask(ix, cy - 0.5, r * 0.42, r * 0.42), 0.8)))

    # --- over: lid line, lashes, crease, catchlight ------------------------
    upper = shape[:5]
    lash = p.get("lash", 1.0)
    lid_line = blur(stroke_mask(upper, 3.2 * lash), 0.7)
    lid_line = ImageChops.lighter(lid_line, scale_l(blur(stroke_mask(upper, 5.6 * lash), 2.6), 0.42))
    lower_line = scale_l(blur(stroke_mask([shape[4], shape[5], shape[6], shape[7]], 2.0), 1.1), 0.30)
    crease_y = cy - p.get("crease", 9.0) - p.get("hood", 0.0)
    hw = p.get("hw", 23.0)
    crease = scale_l(
        blur(stroke_mask([(cx - hw * 0.9, crease_y + 5), (cx, crease_y - 1.5), (cx + hw * 0.95, crease_y + 4)], 2.6), 2.2),
        p.get("crease_a", 0.28),
    )
    over = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    over = Image.alpha_composite(over, solid_layer((44, 34, 30), lid_line))
    over = Image.alpha_composite(over, solid_layer((96, 68, 58), lower_line))
    over = Image.alpha_composite(over, solid_layer((104, 74, 62), crease))
    glint = ImageChops.multiply(sclera, blur(ellipse_mask(ix - r * 0.36, cy - r * 0.44, 2.4, 2.0), 0.8))
    over = Image.alpha_composite(over, solid_layer((255, 253, 248), scale_l(glint, 0.82)))

    return (
        [("under", under, {"blend": "normal"})]
        + keyline(sclera, 0.55)
        + [
            ("iris", iris, {"blend": "normal", "color_slot": "iris"}),
            ("pupil", pupil, {"blend": "normal"}),
            ("over", over, {"blend": "normal"}),
        ]
    )


# --------------------------------------------------------------------------- #
# eyebrows
# --------------------------------------------------------------------------- #

BROW_RECIPES = [
    ("brows_01_straight", 0.18, {}),
    ("brows_02_arched", 0.15, {"arch": 6.5}),
    ("brows_03_thick", 0.15, {"th": 8.0, "arch": 3.0}),
    ("brows_04_bushy", 0.12, {"th": 9.8, "len": 5.0, "rough": 1.0}),
    ("brows_05_thin", 0.12, {"th": 4.4}),
    ("brows_06_angled", 0.11, {"angle": 4.5, "th": 6.6}),
    ("brows_07_short", 0.09, {"len": -9.0, "th": 6.2}),
    ("brows_08_low", 0.08, {"drop": 4.5, "th": 7.6}),
]


def bake_brow(p: dict[str, float]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    inner_x = CX + 0.14 * HEAD_HW
    outer_x = CX + 0.72 * HEAD_HW + p.get("len", 0.0)
    y = BROW_Y + p.get("drop", 0.0)
    th = p.get("th", 6.2) * (st().feature_boost + (0.18 if st().line_art > 0 else 0.0))
    arch = p.get("arch", 2.0)
    ang = p.get("angle", 0.0)
    span = outer_x - inner_x
    top = [
        (inner_x, y - th * 0.45 + ang),
        (inner_x + span * 0.3, y - th * 0.72 - arch),
        (inner_x + span * 0.62, y - th * 0.62 - arch * 0.9),
        (outer_x, y - th * 0.08 - arch * 0.2),
    ]
    bottom = [
        (outer_x, y + th * 0.14 - arch * 0.2),
        (inner_x + span * 0.62, y + th * 0.42 - arch * 0.6),
        (inner_x + span * 0.3, y + th * 0.55 - arch * 0.3),
        (inner_x, y + th * 0.55 + ang),
    ]
    body = poly_mask(top + bottom, iters=3)
    mask = blur(body, 1.5)
    if p.get("rough"):
        rnd = random.Random(7)
        strands = new_l()
        for _ in range(30):
            t = rnd.random()
            bx = inner_x + span * t
            by = y - arch * (1 - abs(t - 0.4)) + rnd.uniform(-th * 0.4, th * 0.4)
            strands = ImageChops.lighter(strands, stroke_mask([(bx, by + 3), (bx + rnd.uniform(2, 7), by - 5)], 1.6))
        mask = ImageChops.lighter(mask, ImageChops.multiply(blur(strands, 1.0), blur(body, 5)))
    shade = mul(flat(1.0), grad_h(1.0, 0.86))
    return [("hair", gray_layer(shade, scale_l(mask, 0.96), crisp=False), {"blend": "normal", "color_slot": "brow"})]


# --------------------------------------------------------------------------- #
# nose
# --------------------------------------------------------------------------- #

NOSE_RECIPES = [
    ("nose_01_straight", 0.14, {}),
    ("nose_02_long_narrow", 0.12, {"len": 8.0, "bridge": 0.85, "tip": 0.84}),
    ("nose_03_wide", 0.12, {"tip": 1.22, "bridge": 1.16, "flare": 1.2}),
    ("nose_04_short", 0.11, {"len": -10.0, "tip": 1.02}),
    ("nose_05_aquiline", 0.11, {"hook": 3.0, "len": 5.0, "bridge": 0.94}),
    ("nose_06_upturned", 0.10, {"upturn": 4.0, "len": -6.0}),
    ("nose_07_broad_flat", 0.09, {"tip": 1.30, "bridge": 1.24, "flare": 1.35, "depth": 0.85}),
    ("nose_08_narrow", 0.09, {"tip": 0.82, "bridge": 0.80, "flare": 0.86}),
    ("nose_09_bulbous", 0.07, {"tip": 1.14, "bulb": 1.0, "len": 3.0}),
    ("nose_10_thin_bridge", 0.05, {"bridge": 0.72, "tip": 0.94, "len": 5.0}),
]


def bake_nose(p: dict[str, float]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    tip_y = NOSE_TIP_Y + p.get("len", 0.0) * 0.5
    bw = 8.5 * p.get("bridge", 1.0)
    tw = 19.0 * p.get("tip", 1.0)
    depth = p.get("depth", 1.0)
    bridge_top = BROW_Y + 16.0

    shadow = new_l()
    # bridge shadow on the shaded (right) side
    shadow = ImageChops.lighter(
        shadow,
        scale_l(
            blur(
                stroke_mask(
                    [
                        (CX + bw * 0.55, bridge_top),
                        (CX + bw * 0.80 + p.get("hook", 0.0), (bridge_top + tip_y) / 2),
                        (CX + tw * 0.52, tip_y - 8),
                    ],
                    7.0,
                ),
                4.5,
            ),
            0.22 * depth,
        ),
    )
    # nose base / under-tip shadow
    shadow = ImageChops.lighter(
        shadow,
        scale_l(blur(ellipse_mask(CX, tip_y + 5 - p.get("upturn", 0.0), tw * 0.98, 6.0 + 7.0 * p.get("bulb", 0.0)), 4.0), 0.30 * depth),
    )
    for sx in (-1, 1):
        shadow = ImageChops.lighter(
            shadow,
            scale_l(blur(ellipse_mask(CX + sx * tw * 0.80, tip_y - 2, 8.5 * p.get("flare", 1.0), 7.0), 3.2), 0.22 * depth),
        )
        shadow = ImageChops.lighter(
            shadow, scale_l(blur(ellipse_mask(CX + sx * tw * 0.52, tip_y + 1.5, 4.2, 2.6), 1.4), 0.46)
        )

    light = new_l()
    light = ImageChops.lighter(
        light,
        scale_l(
            blur(stroke_mask([(CX - bw * 0.38, bridge_top + 2), (CX - bw * 0.42, (bridge_top + tip_y) / 2), (CX - tw * 0.16, tip_y - 11)], 6.0), 4.0),
            0.18,
        ),
    )
    light = ImageChops.lighter(light, scale_l(blur(ellipse_mask(CX - 2, tip_y - 7, tw * 0.42, 5.5), 3.0), 0.22))

    style = st()
    shadow = scale_l(shadow, style.form_strength)
    light = scale_l(light, style.highlight_strength)
    parts = [
        ("shade", solid_layer(SHADOW_RGB, shadow), {"blend": "multiply"}),
        ("light", solid_layer(LIGHT_RGB, light), {"blend": "screen"}),
    ]
    if style.line_features:
        # a flat-vector nose reads as line work, not as a soft gradient
        line = new_l()
        for sx in (-1, 1):
            line = ImageChops.lighter(
                line,
                scale_l(
                    blur(
                        stroke_mask(
                            [
                                (CX + sx * tw * 0.86, tip_y - 5),
                                (CX + sx * tw * 0.78, tip_y + 3),
                                (CX + sx * tw * 0.34, tip_y + 4.5),
                            ],
                            2.4,
                        ),
                        0.9,
                    ),
                    0.60,
                ),
            )
            line = ImageChops.lighter(line, scale_l(blur(ellipse_mask(CX + sx * tw * 0.52, tip_y + 1.0, 3.6, 2.4), 0.9), 0.72))
        if style.line_art > 0:
            line = ImageChops.lighter(
                line,
                scale_l(
                    blur(
                        stroke_mask(
                            [
                                (CX + bw * 0.75, bridge_top + 16),
                                (CX + bw * 0.95 + p.get("hook", 0.0), (bridge_top + tip_y) / 2 + 6),
                                (CX + tw * 0.62, tip_y - 7),
                            ],
                            2.6,
                        ),
                        0.9,
                    ),
                    0.44,
                ),
            )
        parts.append(("line", solid_layer(INK_RGB if style.line_art > 0 else SHADOW_RGB, line), {"blend": "multiply"}))
    return parts


# --------------------------------------------------------------------------- #
# mouth
# --------------------------------------------------------------------------- #

MOUTH_RECIPES = [
    ("mouth_01_medium", 0.18, {}),
    ("mouth_02_wide_thin", 0.15, {"hw": 47.0, "upper": 4.6, "lower": 6.2}),
    ("mouth_03_full", 0.14, {"upper": 7.6, "lower": 10.2}),
    ("mouth_04_narrow", 0.13, {"hw": 35.0}),
    ("mouth_05_downturned", 0.12, {"droop": 3.5}),
    ("mouth_06_thin_upper", 0.12, {"upper": 3.8, "lower": 8.2}),
    ("mouth_07_bow", 0.09, {"bow": 2.6, "upper": 6.8}),
    ("mouth_08_flat", 0.07, {"bow": 0.4, "upper": 5.0, "lower": 6.4}),
    ("mouth_09_smiling", 0.10, {"smile": 5.0, "lower": 8.4}),
    ("mouth_10_wide_smile", 0.07, {"hw": 45.0, "smile": 6.0, "upper": 5.0, "lower": 7.4}),
]


def bake_mouth(p: dict[str, float]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    hw = p.get("hw", 41.0)
    up = p.get("upper", 5.8)
    lo = p.get("lower", 7.8)
    bow = p.get("bow", 1.6)
    # every mouth carries a slight smile: a neutral straight line reads as sullen
    # at portrait size. `droop` still lets a variant pull the corners back down.
    droop = p.get("droop", 0.0) - p.get("smile", 2.4)
    y = MOUTH_Y

    upper_pts = [
        (CX - hw, y + droop),
        (CX - hw * 0.55, y - up * 0.72),
        (CX - hw * 0.16, y - up * 0.5 - bow),
        (CX, y - up * 0.22),
        (CX + hw * 0.16, y - up * 0.5 - bow),
        (CX + hw * 0.55, y - up * 0.72),
        (CX + hw, y + droop),
        (CX + hw * 0.5, y + 0.6),
        (CX, y + 1.4),
        (CX - hw * 0.5, y + 0.6),
    ]
    lower_pts = [
        (CX - hw, y + droop),
        (CX - hw * 0.5, y + 1.4),
        (CX, y + 2.0),
        (CX + hw * 0.5, y + 1.4),
        (CX + hw, y + droop),
        (CX + hw * 0.58, y + lo * 0.86),
        (CX, y + lo),
        (CX - hw * 0.58, y + lo * 0.86),
    ]
    upper = poly_mask(upper_pts)
    lower = poly_mask(lower_pts)
    lips = ImageChops.lighter(upper, lower)

    shade = mul(flat(1.0), grad_h(1.02, 0.92))
    shade = darken(shade, blur(upper, 3.0), 0.20)
    shade = darken(shade, rim(lips, 4.0, 3.0), 0.16)
    lips_layer = shaded_color_layer((255, 255, 255), shade, scale_l(blur(lips, 0.9), 0.97))

    line = scale_l(
        blur(stroke_mask([(CX - hw, y + droop), (CX - hw * 0.4, y + 0.4), (CX, y + 1.0), (CX + hw * 0.4, y + 0.4), (CX + hw, y + droop)], 2.4), 1.0),
        0.48 if st().line_art <= 0 else 0.78,
    )
    for sx in (-1, 1):
        line = ImageChops.lighter(line, scale_l(blur(ellipse_mask(CX + sx * (hw + 2.0), y + droop + 0.5, 4.5, 3.4), 2.2), 0.38))
    light = solid_layer(LIGHT_RGB, scale_l(blur(ellipse_mask(CX - 4, y + lo * 0.52, hw * 0.44, lo * 0.24), 3.0), 0.30))
    return (
        [("lips", lips_layer, {"blend": "normal", "color_slot": "lip"})]
        + keyline(lips, 0.7)
        + [
            ("line", solid_layer(SHADOW_RGB, line), {"blend": "multiply"}),
            ("light", light, {"blend": "screen"}),
        ]
    )


# --------------------------------------------------------------------------- #
# wrinkles (one asset, several age-driven parts)
# --------------------------------------------------------------------------- #


def bake_wrinkles() -> list[tuple[str, Image.Image, dict[str, Any]]]:
    def dark(mask: Image.Image, k: float) -> Image.Image:
        return detail(SHADOW_RGB, mask, k)

    forehead = new_l()
    for i, f in enumerate((0.21, 0.27, 0.33)):
        yy = hy(f)
        span = 0.46 * HEAD_HW - i * 5.0
        forehead = ImageChops.lighter(forehead, blur(stroke_mask([(CX - span, yy + 3.5), (CX, yy - 2.0), (CX + span, yy + 3.5)], 2.6), 1.5))
    glabella = new_l()
    for sx in (-1, 1):
        glabella = ImageChops.lighter(glabella, blur(stroke_mask([(CX + sx * 7, BROW_Y - 5), (CX + sx * 8.5, BROW_Y - 20)], 2.4), 1.4))

    crows = new_l()
    for sx in (-1, 1):
        x0 = CX + sx * (EYE_DX + 22)
        for k in range(3):
            crows = ImageChops.lighter(
                crows, blur(stroke_mask([(x0, EYE_Y - 4 + k * 6), (x0 + sx * (11 + k * 2), EYE_Y - 9 + k * 9)], 2.0), 1.2)
            )
    bags = new_l()
    for sx in (-1, 1):
        c = CX + sx * EYE_DX
        bags = ImageChops.lighter(bags, blur(stroke_mask([(c - 18, EYE_Y + 10), (c, EYE_Y + 14.5), (c + 18, EYE_Y + 10)], 5.0), 3.4))
    nasolabial = new_l()
    for sx in (-1, 1):
        nasolabial = ImageChops.lighter(
            nasolabial,
            blur(stroke_mask([(CX + sx * 19, NOSE_TIP_Y - 2), (CX + sx * 38, MOUTH_Y - 12), (CX + sx * 42, MOUTH_Y + 12)], 3.2), 2.0),
        )
    jaw = new_l()
    for sx in (-1, 1):
        jaw = ImageChops.lighter(
            jaw,
            blur(stroke_mask([(CX + sx * 0.82 * HEAD_HW, hy(0.68)), (CX + sx * 0.74 * HEAD_HW, JAW_Y + 6), (CX + sx * 0.42 * HEAD_HW, CHIN_Y - 10)], 7.0), 5.0),
        )

    # volume loss: temples and cheeks hollow out with age, which reads much
    # more strongly than lines do at small portrait sizes
    hollow = new_l()
    face = face_region()
    for sx in (-1, 1):
        hollow = ImageChops.lighter(hollow, ImageChops.multiply(face, blur(ellipse_mask(CX + sx * 0.86 * HEAD_HW, hy(0.33), 20, 30), 16)))
        hollow = ImageChops.lighter(hollow, ImageChops.multiply(face, blur(ellipse_mask(CX + sx * 0.66 * HEAD_HW, hy(0.72), 26, 26), 18)))
        hollow = ImageChops.lighter(hollow, ImageChops.multiply(face, blur(ellipse_mask(CX + sx * EYE_DX, EYE_Y - 12, 30, 12), 12)))

    return [
        ("hollowing", dark(hollow, 0.20), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 1.0}),
        ("nasolabial", dark(nasolabial, 0.42), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 1.0}),
        ("eye_bags", dark(bags, 0.32), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 0.95}),
        ("crows_feet", dark(crows, 0.38), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 0.9}),
        ("forehead", dark(forehead, 0.34), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 0.8}),
        ("glabella", dark(glabella, 0.34), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 0.7}),
        ("jaw_slack", dark(jaw, 0.24), {"blend": "multiply", "opacity_from": "wrinkle_strength", "opacity": 0.55}),
    ]


# --------------------------------------------------------------------------- #
# skin details
# --------------------------------------------------------------------------- #


def face_region() -> Image.Image:
    return poly_mask(head_contour({}))


def bake_skin_details() -> list[tuple[str, float, list[tuple[str, Image.Image, dict[str, Any]]], dict[str, Any]]]:
    face = face_region()
    out: list[tuple[str, float, list[tuple[str, Image.Image, dict[str, Any]]], dict[str, Any]]] = []

    # cyclist tan: pale forehead band where the helmet sits
    band = ImageChops.multiply(
        face,
        blur(poly_mask([(CX - 100, hy(0.10)), (CX + 100, hy(0.10)), (CX + 96, hy(0.36)), (CX - 96, hy(0.36))], iters=1), 9),
    )
    out.append(
        (
            "detail_01_helmet_tan",
            0.72,
            [("band", detail((255, 248, 236), band, 0.12), {"blend": "screen", "opacity_from": "tan_strength"})],
            {},
        )
    )
    # pale patch around the eyes from sunglasses
    goggles = new_l()
    for sx in (-1, 1):
        goggles = ImageChops.lighter(goggles, blur(ellipse_mask(CX + sx * EYE_DX, EYE_Y - 2, 40, 24), 10))
    out.append(
        (
            "detail_02_glasses_tan",
            0.46,
            [("patch", detail((255, 246, 232), ImageChops.multiply(face, goggles), 0.13), {"blend": "screen", "opacity_from": "tan_strength"})],
            {},
        )
    )
    # freckles
    rnd = random.Random(4242)
    freckles = new_l()
    for _ in range(120):
        sx = rnd.choice((-1, 1))
        x = CX + sx * rnd.uniform(10, 0.78 * HEAD_HW)
        y = rnd.uniform(EYE_Y + 12, MOUTH_Y - 14)
        r = rnd.uniform(0.9, 1.8)
        freckles = ImageChops.lighter(freckles, scale_l(blur(ellipse_mask(x, y, r, r * 0.9), 0.8), rnd.uniform(0.35, 0.75)))
    out.append(
        (
            "detail_03_freckles",
            0.13,
            [("dots", detail((132, 88, 62), ImageChops.multiply(face, freckles), 0.26), {"blend": "multiply"})],
            {},
        )
    )
    # shaved stubble shadow (suppressed when a dense beard asset is present)
    out.append(
        (
            "detail_04_stubble_shadow",
            0.34,
            [("shadow", detail(COOL_SHADOW_RGB, beard_area_mask("short"), 0.15), {"blend": "multiply"})],
            {"excludes_tags": ["beard_dense"]},
        )
    )
    mole = ImageChops.multiply(face, blur(ellipse_mask(CX + 33, hy(0.66), 2.4, 2.1), 0.7))
    out.append(("detail_05_mole", 0.10, [("mole", detail((92, 62, 46), mole, 0.72), {"blend": "multiply"})], {}))
    scar = ImageChops.multiply(face, blur(stroke_mask([(CX - 60, BROW_Y - 8), (CX - 46, BROW_Y - 18)], 2.4), 1.0))
    out.append(("detail_06_brow_scar", 0.07, [("scar", detail((255, 240, 232), scar, 0.32), {"blend": "screen"})], {}))
    rash = ImageChops.multiply(face, blur(ellipse_mask(CX + 66, hy(0.58), 13, 9), 5))
    out.append(("detail_07_road_rash", 0.05, [("rash", detail((178, 108, 96), rash, 0.13), {"blend": "multiply"})], {}))
    return out


# --------------------------------------------------------------------------- #
# hair
# --------------------------------------------------------------------------- #


def hair_polygon(t: float, hl_f: float, side_f: float, style: str) -> list[tuple[float, float]]:
    hw = HEAD_HW
    hl_y = hy(hl_f)
    side_y = hy(side_f)
    outer_right = [
        (CX, SKULL_TOP - t),
        (CX + 0.26 * hw, SKULL_TOP - t * 0.99 + 1),
        (CX + 0.58 * hw, SKULL_TOP - t * 0.84 + 5),
        (CX + 1.00 * hw + t * 0.58, hy(0.18)),
        (CX + 1.04 * hw + t * 0.36, hy(0.33)),
        (CX + 1.03 * hw + t * 0.14, hy(0.45)),
        (CX + 1.00 * hw, side_y),
    ]
    # hairline: temple recession + a slight central peak, i.e. never a straight line
    inner_right = [
        (CX + 0.92 * hw, side_y - 6.0),
        (CX + 0.88 * hw, hl_y + 40.0),
        (CX + 0.72 * hw, hl_y + 16.0),
        (CX + 0.44 * hw, hl_y + 1.0),
        (CX + 0.16 * hw, hl_y - 4.0),
        (CX, hl_y + 1.0),
    ]
    if style == "m_shape":
        inner_right[1] = (CX + 0.90 * hw, hl_y + 52.0)
        inner_right[2] = (CX + 0.78 * hw, hl_y + 34.0)
        inner_right[3] = (CX + 0.40 * hw, hl_y - 6.0)
        inner_right[4] = (CX + 0.14 * hw, hl_y + 4.0)
        inner_right[5] = (CX, hl_y + 12.0)
    elif style == "quiff":
        # volume lifted at the front, tight at the back
        outer_right[0] = (CX, SKULL_TOP - t * 1.9)
        outer_right[1] = (CX + 0.26 * hw, SKULL_TOP - t * 1.7)
        outer_right[2] = (CX + 0.60 * hw, SKULL_TOP - t * 0.9)
        outer_right[3] = (CX + 0.98 * hw + t * 0.2, hy(0.20))
        inner_right[3] = (CX + 0.46 * hw, hl_y - 10.0)
        inner_right[4] = (CX + 0.16 * hw, hl_y - 16.0)
        inner_right[5] = (CX, hl_y - 12.0)
    elif style == "fringe":
        # hair falls forward onto the forehead
        inner_right[2] = (CX + 0.74 * hw, hl_y + 34.0)
        inner_right[3] = (CX + 0.48 * hw, hl_y + 30.0)
        inner_right[4] = (CX + 0.20 * hw, hl_y + 22.0)
        inner_right[5] = (CX, hl_y + 26.0)
    elif style == "undercut":
        # narrow, nearly shaved sides with volume on top
        outer_right[2] = (CX + 0.92 * hw + t * 0.7, hy(0.16))
        outer_right[3] = (CX + 0.84 * hw, hy(0.30))
        outer_right[4] = (CX + 0.80 * hw, hy(0.40))
        outer_right[5] = (CX + 0.78 * hw, side_y)
        inner_right[0] = (CX + 0.72 * hw, side_y - 6.0)
        inner_right[1] = (CX + 0.70 * hw, hl_y + 30.0)
    elif style == "mid_part":
        inner_right[4] = (CX + 0.18 * hw, hl_y - 8.0)
        inner_right[5] = (CX + 0.03 * hw, hl_y + 16.0)
    elif style == "round":
        inner_right[2] = (CX + 0.66 * hw, hl_y + 8.0)
        inner_right[3] = (CX + 0.38 * hw, hl_y - 2.0)
        inner_right[4] = (CX + 0.14 * hw, hl_y - 5.0)
    elif style == "swept":
        outer_right[2] = (CX + 0.62 * hw, SKULL_TOP - t * 1.20 + 3)
        inner_right[3] = (CX + 0.50 * hw, hl_y - 6.0)
        inner_right[4] = (CX + 0.10 * hw, hl_y - 10.0)
        inner_right[5] = (CX - 0.20 * hw, hl_y - 4.0)
    left_outer = list(reversed(mirror_x(outer_right)))
    left_inner = list(reversed(mirror_x(inner_right)))[1:]
    return left_outer + outer_right[1:] + inner_right + left_inner


def wobble(mask: Image.Image, kind: str, amount: float, seed: int) -> Image.Image:
    """Adds curl/spike silhouette detail by stamping blobs along the outline."""
    rnd = random.Random(seed)
    out = mask
    for _ in range(56):
        ang = rnd.uniform(3.34, 6.08)  # upper hemisphere only
        x = CX + math.cos(ang) * (HEAD_HW + amount * 0.4) * rnd.uniform(0.86, 1.06)
        y = hy(0.42) + math.sin(ang) * (0.44 * HEAD_H + amount * 0.4) * rnd.uniform(0.86, 1.04)
        if kind == "curl":
            r = amount * rnd.uniform(0.45, 0.85)
            out = ImageChops.lighter(out, ellipse_mask(x, y, r, r * rnd.uniform(0.8, 1.1)))
        else:  # spike
            r = amount * rnd.uniform(0.3, 0.6)
            out = ImageChops.lighter(out, poly_mask([(x - r, y + r * 2), (x, y - r * 2.4), (x + r, y + r * 2)], iters=1))
    return out


HAIR_RECIPES: list[dict[str, Any]] = [
    {"id": "hair_01_buzz", "w": 0.11, "t": 5.0, "hl": 0.25, "side": 0.52, "style": "straight"},
    {"id": "hair_02_crop", "w": 0.16, "t": 10.0, "hl": 0.24, "side": 0.51, "style": "straight"},
    {"id": "hair_03_side_part", "w": 0.13, "t": 14.0, "hl": 0.23, "side": 0.50, "style": "swept", "part": True},
    {"id": "hair_04_messy_short", "w": 0.13, "t": 13.0, "hl": 0.24, "side": 0.51, "style": "straight", "wob": ("spike", 7.0)},
    {"id": "hair_05_swept_medium", "w": 0.08, "t": 18.0, "hl": 0.21, "side": 0.53, "style": "swept"},
    {
        "id": "hair_06_curly_short",
        "w": 0.08,
        "t": 14.0,
        "hl": 0.26,
        "side": 0.51,
        "style": "round",
        "wob": ("curl", 9.0),
        "region_weights": {"*": 1.0, "west_africa": 1.9, "east_africa": 1.8, "latin_america": 1.4, "north_africa": 1.3, "iberia": 1.2, "east_asia": 0.5, "scandinavia": 0.7},
    },
    {
        "id": "hair_07_curly_medium",
        "w": 0.05,
        "t": 21.0,
        "hl": 0.25,
        "side": 0.54,
        "style": "round",
        "wob": ("curl", 13.0),
        "region_weights": {"*": 1.0, "west_africa": 1.8, "east_africa": 1.7, "latin_america": 1.3, "east_asia": 0.4},
    },
    {
        "id": "hair_08_dense_short",
        "w": 0.05,
        "t": 17.0,
        "hl": 0.28,
        "side": 0.50,
        "style": "round",
        "wob": ("curl", 7.0),
        "region_weights": {"*": 1.0, "west_africa": 2.4, "east_africa": 2.2, "north_america": 1.2, "east_asia": 0.4, "scandinavia": 0.5},
    },
    {"id": "hair_09_spiky", "w": 0.05, "t": 15.0, "hl": 0.24, "side": 0.49, "style": "straight", "wob": ("spike", 11.0), "max_age": 32},
    {"id": "hair_10_slicked", "w": 0.06, "t": 11.0, "hl": 0.19, "side": 0.51, "style": "swept", "part": True},
    {"id": "hair_11_thinning", "w": 0.06, "t": 8.0, "hl": 0.185, "side": 0.51, "style": "m_shape", "requires": ("hairline_thinning",)},
    {"id": "hair_12_receded", "w": 0.07, "t": 8.0, "hl": 0.13, "side": 0.52, "style": "m_shape", "requires": ("hairline_receded",)},
    {"id": "hair_13_horseshoe", "w": 0.05, "t": 6.0, "hl": 0.05, "side": 0.53, "style": "m_shape", "requires": ("hairline_receded",), "min_age": 30},
    {"id": "hair_14_longer", "w": 0.03, "t": 19.0, "hl": 0.23, "side": 0.70, "style": "straight"},
    {"id": "hair_15_flat_helmet", "w": 0.07, "t": 9.0, "hl": 0.24, "side": 0.52, "style": "straight"},
    {"id": "hair_16_quiff", "w": 0.07, "t": 13.0, "hl": 0.20, "side": 0.48, "style": "quiff"},
    {"id": "hair_17_fringe", "w": 0.15, "t": 15.0, "hl": 0.18, "side": 0.51, "style": "fringe"},
    {"id": "hair_18_undercut", "w": 0.07, "t": 17.0, "hl": 0.21, "side": 0.50, "style": "undercut"},
    {"id": "hair_19_textured_spikes", "w": 0.11, "t": 15.0, "hl": 0.23, "side": 0.50, "style": "quiff", "wob": ("spike", 8.0)},
    {"id": "hair_20_wavy_medium", "w": 0.06, "t": 19.0, "hl": 0.22, "side": 0.55, "style": "swept", "wob": ("curl", 6.0)},
    {"id": "hair_21_crew_cut", "w": 0.09, "t": 7.0, "hl": 0.235, "side": 0.49, "style": "round"},
    {"id": "hair_22_high_fade", "w": 0.07, "t": 14.0, "hl": 0.22, "side": 0.42, "style": "undercut"},
    {"id": "hair_23_mid_part", "w": 0.05, "t": 17.0, "hl": 0.20, "side": 0.52, "style": "mid_part"},
    {
        "id": "hair_24_curly_tall",
        "w": 0.04,
        "t": 24.0,
        "hl": 0.26,
        "side": 0.51,
        "style": "round",
        "wob": ("curl", 15.0),
        "region_weights": {"*": 1.0, "west_africa": 2.2, "east_africa": 2.0, "latin_america": 1.3, "east_asia": 0.3, "scandinavia": 0.4},
    },
    {"id": "hair_25_shaved", "w": 0.05, "t": 3.0, "hl": 0.26, "side": 0.52, "style": "round"},
]


def bake_hair(r: dict[str, Any]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    hl_y = hy(r["hl"])
    mask = poly_mask(hair_polygon(r["t"], r["hl"], r["side"], r["style"]))
    if r.get("wob"):
        kind, amount = r["wob"]
        mask = wobble(mask, kind, amount, seed=abs(hash(r["id"])) % 9973)
    mask = blur(mask, 1.8)

    shade = mul(flat(1.0), grad_v(1.12, 0.70), grad_h(1.06, 0.86))
    shade = darken(shade, rim(mask, 8.0, 6.0), 0.22)
    shade = darken(
        shade,
        ImageChops.multiply(mask, blur(stroke_mask([(CX - HEAD_HW, hl_y + 24), (CX, hl_y + 2), (CX + HEAD_HW, hl_y + 24)], 14.0), 9.0)),
        0.18,
    )
    if r.get("part"):
        shade = darken(shade, ImageChops.multiply(mask, blur(stroke_mask([(CX - 24, hl_y - 4), (CX - 32, SKULL_TOP + 18)], 5.0), 3.0)), 0.32)
    if st().tone_steps < 2:
        strands = new_l()
        rnd = random.Random(abs(hash(r["id"])) % 9999)
        for _ in range(36):
            x0 = rnd.uniform(CX - HEAD_HW, CX + HEAD_HW)
            y0 = rnd.uniform(SKULL_TOP - 6, hl_y + 18)
            strands = ImageChops.lighter(strands, stroke_mask([(x0, y0), (x0 + rnd.uniform(-16, 16), y0 + rnd.uniform(14, 40))], 2.0))
        shade = darken(shade, ImageChops.multiply(mask, blur(strands, 1.6)), 0.10)
    # flat styles keep only the rim + hairline shading already applied above

    if st().line_art > 0:
        # one deliberate second-tone shape instead of fake strand texture
        shade = darken(
            shade,
            ImageChops.multiply(
                mask,
                blur(
                    poly_mask(
                        [
                            (CX + 0.10 * HEAD_HW, SKULL_TOP - r["t"]),
                            (CX + 1.3 * HEAD_HW, SKULL_TOP),
                            (CX + 1.3 * HEAD_HW, hy(r["side"])),
                            (CX + 0.34 * HEAD_HW, hy(r["side"])),
                        ],
                        iters=2,
                    ),
                    6,
                ),
            ),
            0.34,
        )

    sheen = solid_layer(
        LIGHT_RGB, scale_l(ImageChops.multiply(mask, blur(ellipse_mask(CX - 44, hy(0.16), 46, 28), 22)), 0.16)
    )
    # shadow the hair casts on the forehead: sits UNDER the hair itself, which is
    # why part order inside an asset is meaningful
    cast = ImageChops.multiply(
        face_region(),
        blur(stroke_mask([(CX - 0.95 * HEAD_HW, hl_y + 30), (CX, hl_y + 6), (CX + 0.95 * HEAD_HW, hl_y + 30)], 12.0), 7.0),
    )
    return (
        [
            ("cast_shadow", solid_layer(SHADOW_RGB, scale_l(cast, 0.16)), {"blend": "multiply"}),
            ("main", gray_layer(shade, mask, outline_of(mask)), {"blend": "normal", "color_slot": "hair"}),
        ]
        + keyline(mask)
        + [("sheen", sheen, {"blend": "screen"})]
    )


# --------------------------------------------------------------------------- #
# facial hair
# --------------------------------------------------------------------------- #


def beard_area_mask(coverage: str) -> Image.Image:
    face = face_region()
    hw = HEAD_HW
    if coverage == "full":
        pts = [
            (CX - 0.90 * hw, hy(0.645)),
            (CX - 0.86 * hw, JAW_Y - 6),
            (CX - 0.48 * hw, CHIN_Y + 2),
            (CX + 0.48 * hw, CHIN_Y + 2),
            (CX + 0.86 * hw, JAW_Y - 6),
            (CX + 0.90 * hw, hy(0.645)),
            (CX + 0.52 * hw, hy(0.74)),
            (CX - 0.52 * hw, hy(0.74)),
        ]
    elif coverage == "short":
        pts = [
            (CX - 0.84 * hw, hy(0.67)),
            (CX - 0.80 * hw, JAW_Y - 2),
            (CX - 0.45 * hw, CHIN_Y),
            (CX + 0.45 * hw, CHIN_Y),
            (CX + 0.80 * hw, JAW_Y - 2),
            (CX + 0.84 * hw, hy(0.67)),
            (CX + 0.50 * hw, hy(0.76)),
            (CX - 0.50 * hw, hy(0.76)),
        ]
    elif coverage == "chin":
        pts = [
            (CX - 0.31 * hw, MOUTH_Y + 4),
            (CX - 0.28 * hw, CHIN_Y - 6),
            (CX, CHIN_Y + 2),
            (CX + 0.28 * hw, CHIN_Y - 6),
            (CX + 0.31 * hw, MOUTH_Y + 4),
        ]
    elif coverage == "strap":
        pts = [
            (CX - 0.86 * hw, hy(0.60)),
            (CX - 0.82 * hw, JAW_Y),
            (CX - 0.44 * hw, CHIN_Y + 1),
            (CX + 0.44 * hw, CHIN_Y + 1),
            (CX + 0.82 * hw, JAW_Y),
            (CX + 0.86 * hw, hy(0.60)),
            (CX + 0.68 * hw, hy(0.70)),
            (CX + 0.38 * hw, CHIN_Y - 15),
            (CX - 0.38 * hw, CHIN_Y - 15),
            (CX - 0.68 * hw, hy(0.70)),
        ]
    else:  # moustache: wider than tall, dipping in the middle above the lip
        pts = [
            (CX - 36, MOUTH_Y - 15),
            (CX - 26, MOUTH_Y - 5),
            (CX - 9, MOUTH_Y - 8),
            (CX, MOUTH_Y - 6),
            (CX + 9, MOUTH_Y - 8),
            (CX + 26, MOUTH_Y - 5),
            (CX + 36, MOUTH_Y - 15),
            (CX + 17, MOUTH_Y - 21),
            (CX, MOUTH_Y - 18),
            (CX - 17, MOUTH_Y - 21),
        ]
    mask = ImageChops.multiply(face, poly_mask(pts))
    if coverage in ("full", "short", "strap"):
        # keep the lips clear so the mouth never disappears under the beard
        mask = ImageChops.subtract(mask, blur(ellipse_mask(CX, MOUTH_Y + 1, 40, 12), 4))
    return mask


FACIAL_RECIPES: list[dict[str, Any]] = [
    {"id": "fh_01_stubble_light", "w": 0.26, "cov": "short", "alpha": 0.24, "soft": 5.0, "min_age": 17},
    {"id": "fh_02_stubble_heavy", "w": 0.20, "cov": "full", "alpha": 0.34, "soft": 4.5, "min_age": 19},
    {"id": "fh_03_beard_short", "w": 0.16, "cov": "short", "alpha": 0.70, "soft": 3.2, "min_age": 21, "tags": ("beard_dense",), "moustache": True},
    {"id": "fh_04_beard_full", "w": 0.11, "cov": "full", "alpha": 0.80, "soft": 2.8, "min_age": 23, "tags": ("beard_dense",), "moustache": True},
    {"id": "fh_05_goatee", "w": 0.10, "cov": "chin", "alpha": 0.80, "soft": 2.4, "min_age": 20, "tags": ("beard_dense",), "moustache": True},
    {"id": "fh_06_chinstrap", "w": 0.09, "cov": "strap", "alpha": 0.76, "soft": 2.6, "min_age": 20, "tags": ("beard_dense",)},
    {"id": "fh_07_moustache", "w": 0.06, "cov": "moustache", "alpha": 0.60, "soft": 2.8, "min_age": 22, "tags": ("beard_dense",)},
]


def bake_facial_hair(r: dict[str, Any]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    mask = beard_area_mask(r["cov"])
    if r.get("moustache"):
        mask = ImageChops.lighter(mask, beard_area_mask("moustache"))
    # a flat-vector beard is a defined shape, a painted one is a soft shadow
    dense = r["alpha"] > 0.6 and r["cov"] in ("full", "short", "strap")
    softness = r["soft"] * (0.34 if (st().tone_steps >= 2 and dense) else 1.0)
    mask = scale_l(blur(mask, softness), r["alpha"])
    shade = mul(flat(1.0), grad_v(1.06, 0.86), grad_h(1.04, 0.90))
    crisp = st().tone_steps >= 2 and dense
    return [("hair", gray_layer(shade, mask, crisp=crisp), {"blend": "normal", "color_slot": "facial_hair"})]


# --------------------------------------------------------------------------- #
# glasses + helmets
# --------------------------------------------------------------------------- #

GLASSES_RECIPES = [
    {"id": "glasses_01_shield_dark", "w": 0.34, "lens": (28, 32, 40), "alpha": 0.82, "wrap": 1.0},
    {"id": "glasses_02_shield_mirror", "w": 0.26, "lens": (86, 150, 190), "alpha": 0.74, "wrap": 1.0},
    {"id": "glasses_03_shield_clear", "w": 0.16, "lens": (196, 206, 214), "alpha": 0.30, "wrap": 1.0},
    {"id": "glasses_04_half_frame", "w": 0.14, "lens": (40, 44, 52), "alpha": 0.78, "wrap": 0.78, "half": True},
    {"id": "glasses_05_amber", "w": 0.10, "lens": (196, 140, 62), "alpha": 0.64, "wrap": 0.92},
]


def bake_glasses(r: dict[str, Any]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    wrap = r.get("wrap", 1.0)
    top_y = EYE_Y - 22.0
    bot_y = EYE_Y + (24.0 if r.get("half") else 32.0)
    span = 1.06 * HEAD_HW * wrap
    lens_pts = [
        (CX - span, top_y + 6),
        (CX - span * 0.6, top_y - 4),
        (CX, top_y),
        (CX + span * 0.6, top_y - 4),
        (CX + span, top_y + 6),
        (CX + span * 0.86, bot_y - 6),
        (CX + span * 0.3, bot_y),
        (CX, bot_y - 4),
        (CX - span * 0.3, bot_y),
        (CX - span * 0.86, bot_y - 6),
    ]
    lens = poly_mask(lens_pts)
    frame_top = blur(stroke_mask(lens_pts[:5], 5.0), 0.8)
    arms = new_l()
    for sx in (-1, 1):
        arms = ImageChops.lighter(
            arms, blur(stroke_mask([(CX + sx * span * 0.98, top_y + 8), (CX + sx * (HEAD_HW + 2), top_y + 20)], 6.0), 0.8)
        )
    glint = ImageChops.multiply(
        lens,
        blur(poly_mask([(CX - span * 0.8, top_y + 30), (CX - span * 0.2, top_y + 8), (CX - span * 0.05, top_y + 12), (CX - span * 0.65, top_y + 40)]), 3),
    )
    return [
        ("lens", solid_layer(r["lens"], scale_l(lens, r["alpha"])), {"blend": "normal"}),
        ("frame", solid_layer((26, 26, 30), ImageChops.lighter(frame_top, arms)), {"blend": "normal"}),
        ("glint", solid_layer((255, 255, 255), scale_l(glint, 0.32)), {"blend": "screen"}),
    ]


HELMET_RECIPES = [
    {"id": "helmet_01_vented", "w": 0.40, "vents": 5, "back": 0.0, "aero": 0.0},
    {"id": "helmet_02_aero_road", "w": 0.28, "vents": 3, "back": 6.0, "aero": 0.35},
    {"id": "helmet_03_tt", "w": 0.12, "vents": 0, "back": 22.0, "aero": 0.9},
    {"id": "helmet_04_round", "w": 0.20, "vents": 4, "back": 2.0, "aero": 0.1},
]


def bake_helmet(r: dict[str, Any]) -> list[tuple[str, Image.Image, dict[str, Any]]]:
    hw = HEAD_HW
    aero = r.get("aero", 0.0)
    back = r.get("back", 0.0)
    top = SKULL_TOP - 20.0 + aero * 4.0
    brim = hy(0.31)  # front edge, roughly two finger widths above the brow line
    side = hy(0.46) + back * 0.10  # side edge, around the top of the ear
    outer_right = [
        (CX, top),
        (CX + 0.42 * hw, top + 4),
        (CX + 0.84 * hw, hy(0.05)),
        (CX + hw + 13, hy(0.21)),
        (CX + hw + 15 + back * 0.2, side - 12),
        (CX + 0.94 * hw, side),
    ]
    brim_right = [
        (CX + 0.88 * hw, side - 5),
        (CX + 0.76 * hw, brim + 27),
        (CX + 0.46 * hw, brim + 7),
        (CX, brim),
    ]
    shell = poly_mask(
        list(reversed(mirror_x(outer_right)))
        + outer_right[1:]
        + brim_right
        + list(reversed(mirror_x(brim_right)))[1:]
    )
    shade = mul(flat(1.0), grad_radial(CX - 0.45 * hw, hy(0.02), 2.1 * hw, 1.10, 0.72))
    shade = darken(shade, rim(shell, 9.0, 6.0), 0.20)
    shade = darken(shade, ImageChops.multiply(shell, blur(stroke_mask(brim_right + list(reversed(mirror_x(brim_right)))[1:], 8.0), 5.0)), 0.16)

    vents = new_l()
    n = r.get("vents", 0)
    for i in range(n):
        x = CX + (i - (n - 1) / 2.0) * (hw * 1.34 / max(1, n))
        vents = ImageChops.lighter(
            vents,
            ImageChops.multiply(shell, blur(poly_mask([(x - 5.5, top + 26), (x + 5.5, top + 22), (x + 4.5, brim - 6), (x - 4.5, brim - 2)]), 2.5)),
        )
    stripe = ImageChops.multiply(shell, blur(poly_mask([(CX - 12, top - 2), (CX + 12, top - 2), (CX + 16, brim + 4), (CX - 16, brim + 4)]), 2.0))
    straps = new_l()
    for sx in (-1, 1):
        straps = ImageChops.lighter(
            straps,
            blur(stroke_mask([(CX + sx * (hw - 4), side - 2), (CX + sx * (hw - 12), hy(0.66)), (CX + sx * 0.56 * hw, CHIN_Y - 6)], 3.4), 1.2),
        )
    glint = ImageChops.multiply(shell, blur(ellipse_mask(CX - 52, hy(0.08), 42, 24), 20))
    # shadow the shell casts on the forehead - without it the helmet floats
    cast = ImageChops.multiply(
        face_region(),
        blur(stroke_mask([(CX - 0.92 * hw, brim + 26), (CX, brim + 4), (CX + 0.92 * hw, brim + 26)], 14.0), 8.0),
    )
    return [
        ("cast_shadow", solid_layer(SHADOW_RGB, scale_l(cast, 0.24)), {"blend": "multiply"}),
        ("shell", shaded_color_layer((255, 255, 255), shade, shell, outline_of(shell)), {"blend": "normal", "color_slot": "team_primary"}),
        ("stripe", shaded_color_layer((255, 255, 255), shade, stripe), {"blend": "normal", "color_slot": "team_accent"}),
        ("vents", solid_layer((34, 34, 40), scale_l(vents, 0.62)), {"blend": "normal"}),
        ("strap", solid_layer((44, 44, 50), scale_l(straps, 0.66)), {"blend": "normal"}),
        ("glint", solid_layer((255, 255, 255), scale_l(glint, 0.20)), {"blend": "screen"}),
    ] + keyline(shell)


# --------------------------------------------------------------------------- #
# palettes and teams
# --------------------------------------------------------------------------- #

# the pack owns its palette; the renderer only interpolates what it is given
SKIN_RAMP: list[tuple[float, tuple[int, int, int]]] = [
    (0.00, (252, 226, 205)),
    (0.15, (243, 208, 180)),
    (0.30, (231, 187, 152)),
    (0.45, (208, 158, 120)),
    (0.60, (177, 126, 90)),
    (0.75, (137, 92, 62)),
    (0.88, (98, 63, 43)),
    (1.00, (68, 44, 32)),
]

# flat / poster packs reuse the skin stops already approved in the merged UI lab
# (09-avatar-lab.html), so portraits and the dashboard share one palette
SKIN_RAMP_FLAT: list[tuple[float, tuple[int, int, int]]] = [
    (0.00, (246, 209, 174)),
    (0.20, (242, 201, 164)),
    (0.36, (232, 178, 140)),
    (0.52, (209, 154, 114)),
    (0.68, (176, 123, 82)),
    (0.84, (138, 90, 59)),
    (1.00, (107, 66, 38)),
]

HAIR_COLORS = [
    ("hc_01_black", [25, 25, 25], 0.30, {"*": 1.0, "scandinavia": 0.5, "east_asia": 2.4, "west_africa": 2.6, "east_africa": 2.4, "south_asia": 2.2}),
    ("hc_02_dark_brown", [58, 42, 28], 0.27, {"*": 1.0, "east_asia": 1.2, "iberia": 1.4}),
    ("hc_03_brown", [107, 74, 44], 0.19, {"*": 1.0, "east_asia": 0.4, "west_africa": 0.3}),
    ("hc_04_light_brown", [138, 98, 58], 0.11, {"*": 1.0, "scandinavia": 1.5, "east_asia": 0.15, "west_africa": 0.1}),
    ("hc_05_dark_blond", [169, 124, 63], 0.07, {"*": 1.0, "scandinavia": 3.0, "west_europe": 1.3, "east_asia": 0.05, "west_africa": 0.05}),
    ("hc_06_blond", [201, 162, 75], 0.04, {"*": 1.0, "scandinavia": 4.0, "west_europe": 1.4, "east_asia": 0.02, "west_africa": 0.02}),
    ("hc_07_auburn", [181, 83, 60], 0.02, {"*": 1.0, "west_europe": 1.6, "scandinavia": 1.4, "east_asia": 0.05}),
]

IRIS_COLORS = [
    ("ic_01_dark_brown", [66, 44, 30], 0.34, {"*": 1.0, "east_asia": 2.2, "west_africa": 2.6, "south_asia": 2.2, "scandinavia": 0.4}),
    ("ic_02_brown", [98, 66, 40], 0.26, {"*": 1.0, "iberia": 1.3, "scandinavia": 0.5}),
    ("ic_03_hazel", [124, 100, 54], 0.13, {"*": 1.0, "east_asia": 0.4, "west_africa": 0.3}),
    ("ic_04_green", [86, 116, 78], 0.09, {"*": 1.0, "east_asia": 0.1, "west_africa": 0.05, "scandinavia": 1.4}),
    ("ic_05_blue", [92, 128, 156], 0.11, {"*": 1.0, "scandinavia": 3.2, "west_europe": 1.4, "east_asia": 0.05, "west_africa": 0.03}),
    ("ic_06_grey", [124, 136, 142], 0.07, {"*": 1.0, "scandinavia": 2.0, "east_asia": 0.1, "west_africa": 0.05}),
]

TEAMS: dict[str, dict[str, Any]] = {
    "team_01_azure": {"name": "Azure Racing", "primary": [26, 74, 148], "secondary": [240, 244, 250], "accent": [232, 176, 24], "nation_colors": [[224, 60, 52], [250, 250, 252]]},
    "team_02_verde": {"name": "Verde Pro Cycling", "primary": [24, 108, 74], "secondary": [246, 246, 240], "accent": [30, 32, 36], "nation_colors": [[236, 216, 60], [40, 120, 60]]},
    "team_03_rosso": {"name": "Rosso Corse", "primary": [168, 34, 40], "secondary": [40, 40, 46], "accent": [244, 244, 248], "nation_colors": [[36, 88, 168], [244, 244, 248]]},
    "team_04_noir": {"name": "Noir Endurance", "primary": [34, 36, 42], "secondary": [212, 216, 222], "accent": [242, 96, 34], "nation_colors": [[40, 40, 44], [232, 60, 56]]},
    "team_05_arctic": {"name": "Arctic Energy", "primary": [232, 236, 240], "secondary": [46, 156, 196], "accent": [24, 40, 60], "nation_colors": [[40, 80, 176], [246, 208, 40]]},
    "team_06_terra": {"name": "Terra Nova", "primary": [154, 98, 42], "secondary": [242, 226, 196], "accent": [52, 44, 38], "nation_colors": [[36, 132, 96], [246, 246, 248]]},
}


# --------------------------------------------------------------------------- #
# entry point
# --------------------------------------------------------------------------- #


def bake(root: str | Path, style: str = "flat", pack_version: str = "0.1.0-placeholder") -> Path:
    """Bake one placeholder pack in one style. The recipes are shared; only the
    StyleProfile changes, which is how the same peloton can be shown in several
    art directions without touching game code."""
    if style not in STYLES:
        raise ValueError(f"unknown style {style!r}, expected one of {sorted(STYLES)}")
    set_style(STYLES[style])
    root = Path(root)
    root.mkdir(parents=True, exist_ok=True)
    b = PackBuilder(root, "peloton_placeholder", pack_version, style)
    b.teams = TEAMS
    ramp = SKIN_RAMP_FLAT if STYLES[style].tone_steps >= 2 else SKIN_RAMP
    b.palettes["skin_ramp"] = {f"stop_{i:02d}": [int(t * 1000), *rgb] for i, (t, rgb) in enumerate(ramp)}

    for asset_id, weight, params, tags in HEAD_RECIPES:
        b.asset("head", asset_id, bake_head(params), weight=weight, tags=tags, anchor=(CX, EYE_Y))

    neck, jaw_shadow, neck_mask = bake_neck()
    b.asset(
        "neck",
        "neck_01",
        [("skin", neck, {"blend": "normal", "color_slot": "skin"})]
        + keyline(neck_mask, 0.9)
        + [("jaw_shadow", jaw_shadow, {"blend": "multiply"})],
        anchor=(CX, NECK_TOP),
    )

    for asset_id, weight, params in EAR_RECIPES:
        b.asset(
            "ears",
            asset_id,
            bake_ear(params),
            weight=weight,
            mirrored=True,
            anchor=(CX + HEAD_HW, (EAR_TOP + EAR_BOTTOM) / 2),
        )

    for asset_id, weight, params in EYE_RECIPES:
        b.asset("eyes", asset_id, bake_eye(params), weight=weight, mirrored=True, anchor=(CX + EYE_DX, EYE_Y))

    for asset_id, weight, params in BROW_RECIPES:
        b.asset("eyebrows", asset_id, bake_brow(params), weight=weight, mirrored=True, anchor=(CX + EYE_DX, BROW_Y))

    for asset_id, weight, params in NOSE_RECIPES:
        b.asset("nose", asset_id, bake_nose(params), weight=weight, anchor=(CX, NOSE_TIP_Y))

    for asset_id, weight, params in MOUTH_RECIPES:
        b.asset("mouth", asset_id, bake_mouth(params), weight=weight, anchor=(CX, MOUTH_Y))

    b.asset("wrinkles", "wrinkles_set_01", bake_wrinkles(), anchor=(CX, EYE_Y))

    for asset_id, weight, parts, extra in bake_skin_details():
        b.asset("skin_details", asset_id, parts, weight=weight, anchor=(CX, EYE_Y), excludes_tags=extra.get("excludes_tags", ()))

    for r in HAIR_RECIPES:
        b.asset(
            "hair",
            r["id"],
            bake_hair(r),
            weight=r["w"],
            anchor=(CX, hy(0.30)),
            min_age=r.get("min_age"),
            max_age=r.get("max_age"),
            requires_tags=r.get("requires", ()),
            excludes_tags=r.get("excludes", ()),
            region_weights=r.get("region_weights"),
        )

    for r in FACIAL_RECIPES:
        b.asset("facial_hair", r["id"], bake_facial_hair(r), weight=r["w"], anchor=(CX, MOUTH_Y), min_age=r.get("min_age"), tags=r.get("tags", ()))

    for r in GLASSES_RECIPES:
        b.asset("glasses", r["id"], bake_glasses(r), weight=r["w"], anchor=(CX, EYE_Y), roles=("rider",))

    for r in HELMET_RECIPES:
        b.asset("helmet", r["id"], bake_helmet(r), weight=r["w"], anchor=(CX, hy(0.20)), roles=("rider",))

    for tid, weight in (("jersey_01_raglan", 0.6), ("jersey_02_band", 0.4)):
        b.asset("jersey", tid, bake_jersey(tid), weight=weight, anchor=(CX, SHOULDER_Y), roles=("rider",))

    for tid, weight in (("outfit_01_polo", 0.45), ("outfit_02_softshell", 0.35), ("outfit_03_suit", 0.20)):
        b.asset("jersey", tid, bake_outfit(tid), weight=weight, anchor=(CX, SHOULDER_Y), roles=("manager",))

    b.asset("jersey_overlay", "overlay_bands_rainbow", bake_overlay_rainbow(), anchor=(CX, SHOULDER_Y))
    b.asset("jersey_overlay", "overlay_bands_champion", bake_overlay_champion(), anchor=(CX, SHOULDER_Y))

    for cid, rgb, weight, rw in HAIR_COLORS:
        b.virtual("hair_color", cid, rgb, weight, rw)
    for cid, rgb, weight, rw in IRIS_COLORS:
        b.virtual("iris_color", cid, rgb, weight, rw)

    b.write_manifest()
    return root
