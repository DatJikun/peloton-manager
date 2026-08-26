"""Drawing primitives and the canonical portrait coordinate system.

Everything in this package is PLACEHOLDER ART. It exists so the pipeline
(traits -> layers -> composite) can be judged visually before a single AI image
is generated. The numbers below are the part that must survive: they are the
master-reference contract that every real asset has to match.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

# --------------------------------------------------------------------------- #
# master reference (512x512, front-facing, head and shoulders)
# --------------------------------------------------------------------------- #

SIZE = 512
SS = 3  # supersampling factor used while drawing
W = SIZE * SS

CX = 256.0  # face centre line
SKULL_TOP = 72.0  # top of the cranium (hair may go above)
CHIN_Y = 352.0
HEAD_H = CHIN_Y - SKULL_TOP  # 280 px reference head height
HEAD_HW = 109.0  # half width at the cheekbones (width/height ~= 0.78)
BROW_Y = 186.0
EYE_Y = 204.0  # canonical eye line - never moves
NOSE_TIP_Y = 268.0
MOUTH_Y = 303.0
JAW_Y = 310.0
EYE_DX = 47.0  # nominal single-eye anchor offset from CX
EAR_TOP = 194.0
EAR_BOTTOM = 260.0
NECK_TOP = 330.0
SHOULDER_Y = 456.0
TORSO_HW = 246.0


def hy(f: float) -> float:
    """Y coordinate at fraction `f` of the reference head height."""
    return SKULL_TOP + f * HEAD_H

CANVAS_SPEC = {
    "size": [SIZE, SIZE],
    "center_x": CX,
    "eye_line_y": EYE_Y,
    "brow_y": BROW_Y,
    "skull_top_y": SKULL_TOP,
    "chin_y": CHIN_Y,
    "nose_tip_y": NOSE_TIP_Y,
    "mouth_y": MOUTH_Y,
    "head_half_width": HEAD_HW,
    "eye_offset_x": EYE_DX,
    "neck_top_y": NECK_TOP,
    "shoulder_y": SHOULDER_Y,
    "torso_half_width": TORSO_HW,
    "head_crop": [96, 44, 416, 364],  # square crop for small UI sizes
    "view": "front-facing, head and shoulders, fixed camera distance",
    "light_direction": "upper-left, 35 degrees elevation",
    "background": "transparent",
}


# --------------------------------------------------------------------------- #
# low level helpers (all coordinates are given in 512-space floats)
# --------------------------------------------------------------------------- #


# --------------------------------------------------------------------------- #
# style profiles
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class StyleProfile:
    """How a pack is rendered, independent of WHAT is drawn.

    The recipes (head shapes, hairstyles, ...) are shared by every style; only
    these numbers change. That is the whole point of the layered design: art
    direction is a property of the asset pack, not of the game code.
    """

    name: str
    tone_steps: int = 0  # 0 = smooth gradients, 2-4 = cel shading
    tone_floor: float = 0.66  # darkest tone the quantiser keeps
    form_strength: float = 1.0  # multiplier on every shading term
    highlight_strength: float = 1.0
    edge_hardness: float = 0.0  # 0 = as drawn, 1 = crisp vector edge
    gradient_scale: float = 1.0  # full-canvas gradients band when posterised
    outline: float = 0.0  # inner outline width in px (0 = none)
    outline_darkness: float = 0.62  # how dark the outline gets vs the fill
    detail_alpha: float = 1.0  # wrinkles / tan lines / freckles multiplier
    line_features: bool = False  # nose and lips get line work, not just shading
    line_art: float = 0.0  # true ink keyline width in px (0 = none)
    feature_boost: float = 1.0  # scales eyes/brows for a more graphic read


STYLES: dict[str, StyleProfile] = {
    # soft, semi-realistic painted look (the first prototype pass)
    "soft": StyleProfile(name="soft"),
    # flat vector: few tones, crisp edges, restrained shading
    "flat": StyleProfile(
        name="flat",
        tone_steps=3,
        tone_floor=0.70,
        form_strength=0.72,
        highlight_strength=0.55,
        edge_hardness=0.85,
        gradient_scale=0.34,
        detail_alpha=0.70,
        line_features=True,
    ),
    # flat vector with line art: same, plus a darker inner outline per shape
    "flat_outline": StyleProfile(
        name="flat_outline",
        tone_steps=3,
        tone_floor=0.72,
        form_strength=0.62,
        highlight_strength=0.45,
        edge_hardness=0.92,
        gradient_scale=0.28,
        outline=2.6,
        outline_darkness=0.58,
        detail_alpha=0.62,
        line_features=True,
    ),
    # constructivist poster: ink keylines, two flat tones, graphic features.
    # Matches the merged UI lab (paper #f3ede1 / red #d11f1f / black #0c0c0d,
    # 3 px black borders), where a soft painted portrait would read as a photo
    # dropped into a poster.
    "poster": StyleProfile(
        name="poster",
        tone_steps=2,
        tone_floor=0.78,
        form_strength=0.60,
        highlight_strength=0.0,
        edge_hardness=1.0,
        gradient_scale=0.16,
        detail_alpha=0.22,
        line_features=True,
        line_art=5.0,
        feature_boost=1.10,
    ),
    # Look proposals — neighbours of poster for owner review. Do not silently
    # replace poster; these exist so the same riders can be judged side by side.
    "poster_thin": StyleProfile(
        name="poster_thin",
        tone_steps=2,
        tone_floor=0.80,
        form_strength=0.48,
        highlight_strength=0.0,
        edge_hardness=1.0,
        gradient_scale=0.14,
        detail_alpha=0.16,
        line_features=True,
        line_art=3.0,
        feature_boost=1.00,
    ),
    "poster_cut": StyleProfile(
        name="poster_cut",
        tone_steps=2,
        tone_floor=0.82,
        form_strength=0.78,
        highlight_strength=0.0,
        edge_hardness=1.0,
        gradient_scale=0.12,
        detail_alpha=0.10,
        line_features=True,
        line_art=8.0,
        feature_boost=1.16,
    ),
    "poster_comic": StyleProfile(
        name="poster_comic",
        tone_steps=2,
        tone_floor=0.76,
        form_strength=0.55,
        highlight_strength=0.0,
        edge_hardness=1.0,
        gradient_scale=0.16,
        outline=1.8,
        outline_darkness=0.52,
        detail_alpha=0.18,
        line_features=True,
        line_art=5.5,
        feature_boost=1.28,
    ),
    "poster_stencil": StyleProfile(
        name="poster_stencil",
        tone_steps=2,
        tone_floor=0.88,
        form_strength=0.32,
        highlight_strength=0.0,
        edge_hardness=1.0,
        gradient_scale=0.10,
        detail_alpha=0.0,
        line_features=True,
        line_art=4.5,
        feature_boost=1.08,
    ),
    # high-contrast painted: more form, stronger highlights, soft edges
    "painted": StyleProfile(
        name="painted",
        tone_steps=0,
        form_strength=1.35,
        highlight_strength=1.45,
        edge_hardness=0.0,
        detail_alpha=1.15,
    ),
}

_ACTIVE = STYLES["soft"]


def set_style(style: StyleProfile) -> None:
    """Set once per bake; the baker is single-pack, single-threaded."""
    global _ACTIVE
    _ACTIVE = style


def st() -> StyleProfile:
    return _ACTIVE


def quantize_tones(img: Image.Image) -> Image.Image:
    """Posterise a shading map into the style's tone steps."""
    s = st()
    if s.tone_steps < 2:
        return img
    arr = np.asarray(img, dtype=np.float32) / 255.0
    lo = s.tone_floor
    t = np.clip((arr - lo) / max(1e-3, 1.0 - lo), 0.0, 1.0)
    q = np.round(t * (s.tone_steps - 1)) / (s.tone_steps - 1)
    out = lo + q * (1.0 - lo)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), "L")


def harden_edges(alpha: Image.Image) -> Image.Image:
    """Steepen an alpha ramp: soft painted falloff -> crisp vector edge."""
    k = st().edge_hardness
    if k <= 0.0:
        return alpha
    slope = 1.0 + 7.0 * k
    return alpha.point(lambda v: int(min(255, max(0, 128 + (v - 128) * slope))))


def new_l(value: int = 0) -> Image.Image:
    return Image.new("L", (W, W), value)


def draw_on(img: Image.Image) -> ImageDraw.ImageDraw:
    return ImageDraw.Draw(img)


def s(pts):
    return [(x * SS, y * SS) for x, y in pts]


def blur(img: Image.Image, radius: float) -> Image.Image:
    if radius <= 0:
        return img
    return img.filter(ImageFilter.GaussianBlur(radius * SS))


def down(img: Image.Image) -> Image.Image:
    return img.resize((SIZE, SIZE), Image.LANCZOS)


def smooth(points, iters: int = 3, closed: bool = True):
    """Chaikin corner cutting: turns coarse control points into soft curves."""
    pts = list(points)
    for _ in range(iters):
        out = []
        n = len(pts)
        rng = range(n) if closed else range(n - 1)
        for i in rng:
            p0 = pts[i]
            p1 = pts[(i + 1) % n]
            out.append((0.75 * p0[0] + 0.25 * p1[0], 0.75 * p0[1] + 0.25 * p1[1]))
            out.append((0.25 * p0[0] + 0.75 * p1[0], 0.25 * p0[1] + 0.75 * p1[1]))
        if not closed:
            out = [pts[0]] + out + [pts[-1]]
        pts = out
    return pts


def mirror_x(points, axis: float = CX):
    return [(2 * axis - x, y) for x, y in points]


def poly_mask(points, iters: int = 3) -> Image.Image:
    img = new_l()
    draw_on(img).polygon(s(smooth(points, iters)), fill=255)
    return img


def stroke_mask(points, width: float, closed: bool = False, iters: int = 3) -> Image.Image:
    img = new_l()
    pts = s(smooth(points, iters, closed=closed))
    if closed:
        pts = pts + [pts[0]]
    draw_on(img).line(pts, fill=255, width=max(1, int(width * SS)), joint="curve")
    return img


def ellipse_mask(cx: float, cy: float, rx: float, ry: float) -> Image.Image:
    img = new_l()
    draw_on(img).ellipse(s([(cx - rx, cy - ry), (cx + rx, cy + ry)]), fill=255)
    return img


def erode(mask: Image.Image, r: float) -> Image.Image:
    """Approximate erosion: blur, then re-threshold."""
    return blur(mask, r).point(lambda v: 255 if v > 168 else 0)


def rim(mask: Image.Image, r: float, softness: float = 3.0) -> Image.Image:
    """Soft band just inside the silhouette edge, used for edge shading."""
    return blur(ImageChops.subtract(mask, erode(mask, r)), softness)


def _compress(v: float) -> float:
    """Pull a gradient endpoint towards 1.0 for flat styles."""
    return 1.0 + (v - 1.0) * _ACTIVE.gradient_scale


def grad_v(top: float, bottom: float) -> Image.Image:
    top, bottom = _compress(top), _compress(bottom)
    col = np.linspace(top, bottom, W, dtype=np.float32)
    arr = np.repeat(col[:, None], W, axis=1)
    return Image.fromarray(np.clip(arr * 255.0, 0, 255).astype(np.uint8), "L")


def grad_h(left: float, right: float) -> Image.Image:
    left, right = _compress(left), _compress(right)
    row = np.linspace(left, right, W, dtype=np.float32)
    arr = np.repeat(row[None, :], W, axis=0)
    return Image.fromarray(np.clip(arr * 255.0, 0, 255).astype(np.uint8), "L")


def grad_radial(cx: float, cy: float, radius: float, inner: float, outer: float) -> Image.Image:
    """Radial falloff used as the main form-shading term (light from upper-left)."""
    inner, outer = _compress(inner), _compress(outer)
    yy, xx = np.mgrid[0:W, 0:W].astype(np.float32)
    d = np.sqrt((xx - cx * SS) ** 2 + (yy - cy * SS) ** 2) / (radius * SS)
    t = np.clip(d, 0.0, 1.0)
    arr = inner + (outer - inner) * (t * t)
    return Image.fromarray(np.clip(arr * 255.0, 0, 255).astype(np.uint8), "L")


def brighten(shade: Image.Image, mask: Image.Image, strength: float) -> Image.Image:
    return ImageChops.add(shade, scale_l(mask, strength))


def mul(*imgs: Image.Image) -> Image.Image:
    out = imgs[0]
    for i in imgs[1:]:
        out = ImageChops.multiply(out, i)
    return out


def add(*imgs: Image.Image) -> Image.Image:
    out = imgs[0]
    for i in imgs[1:]:
        out = ImageChops.lighter(out, i)
    return out


def scale_l(img: Image.Image, k: float) -> Image.Image:
    return img.point(lambda v: int(min(255, max(0, v * k))))


def flat(value: float) -> Image.Image:
    return new_l(int(round(min(1.0, max(0.0, value)) * 255)))


def as_layer(shade: Image.Image, alpha: Image.Image) -> Image.Image:
    """Pack a shading map + coverage mask into the final 512x512 RGBA asset."""
    sh = down(shade)
    al = down(alpha)
    return Image.merge("RGBA", (sh, sh, sh, al))


def as_color_layer(rgb: tuple[int, int, int], alpha: Image.Image, shade: Image.Image | None = None) -> Image.Image:
    al = down(alpha)
    if shade is None:
        base = Image.new("RGB", (SIZE, SIZE), rgb)
    else:
        sh = down(shade)
        base = Image.merge(
            "RGB",
            tuple(
                sh.point(lambda v, c=c: int(min(255, v * c / 255.0)))  # type: ignore[misc]
                for c in rgb
            ),
        )
    r, g, b = base.split()
    return Image.merge("RGBA", (r, g, b, al))


def clip_to(mask: Image.Image, region: Image.Image) -> Image.Image:
    return ImageChops.multiply(mask, region)


def head_contour(p: dict[str, float]) -> list[tuple[float, float]]:
    """Skull + jaw silhouette from named shape parameters.

    Control points are placed as fractions of the head height so a head recipe
    stays valid if the master framing is ever re-scaled. Parameters are
    multipliers around 1.0: a new head asset is a small edit of an existing
    recipe, not free-hand drawing.
    """
    hw = HEAD_HW
    top = SKULL_TOP - p.get("crown", 0.0)
    chin_y = CHIN_Y + p.get("chin_len", 0.0)
    h = chin_y - top

    def y(f: float) -> float:
        return top + f * h

    right = [
        (CX, top),
        (CX + 0.60 * hw * p.get("crown_w", 1.0), y(0.05)),
        (CX + 0.96 * hw * p.get("cranium_w", 1.0), y(0.20)),
        (CX + 1.00 * hw * p.get("temple_w", 1.0), y(0.35)),
        (CX + 1.00 * hw * p.get("cheek_w", 1.0), y(0.55)),
        (CX + 0.93 * hw * p.get("jaw_w", 1.0), y(0.74)),
        (CX + 0.78 * hw * p.get("jaw_w", 1.0), y(0.865)),
        (CX + 0.48 * hw * p.get("chin_w", 1.0), y(0.955)),
        (CX + 0.21 * hw * p.get("chin_w", 1.0), y(1.0)),
        (CX, y(1.005)),
    ]
    return right + mirror_x(list(reversed(right[1:-1])))
