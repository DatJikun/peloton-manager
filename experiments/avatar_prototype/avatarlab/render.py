"""Layered runtime compositor.

No AI, no network: this is plain image compositing over the asset pack.
`render(appearance, pack)` returns a 512x512 RGBA portrait.

The three mechanisms that make a small pack look varied:

1. discrete assets   - which head / eyes / nose / hair PNG is used
2. color slots       - grayscale PNGs tinted at runtime (skin, hair, iris, team)
3. continuous affine - per-feature scale/offset driven by normalized 0..1 params
"""

from __future__ import annotations

import hashlib
import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

import numpy as np
from PIL import Image

from .generate import Appearance
from .manifest import Asset, Manifest, Part

SIZE = 512
# fallback pivot; the real values come from manifest.canvas
EYE_Y = 204.0
CENTER_X = 256.0

# --------------------------------------------------------------------------- #
# color helpers
# --------------------------------------------------------------------------- #

# fallback ramp; a pack normally ships its own in palettes["skin_ramp"]
_SKIN_RAMP: tuple[tuple[float, tuple[int, int, int]], ...] = (
    (0.00, (252, 226, 205)),
    (0.15, (243, 208, 180)),
    (0.30, (231, 187, 152)),
    (0.45, (208, 158, 120)),
    (0.60, (177, 126, 90)),
    (0.75, (137, 92, 62)),
    (0.88, (98, 63, 43)),
    (1.00, (68, 44, 32)),
)


def ramp_from_palette(palette: dict[str, list[int]] | None):
    """`palettes["skin_ramp"] = {"stop_00": [t*1000, r, g, b], ...}` -> ramp."""
    if not palette:
        return _SKIN_RAMP
    stops = []
    for key in sorted(palette):
        t, r, g, b = palette[key]
        stops.append((t / 1000.0, (r, g, b)))
    return tuple(stops) or _SKIN_RAMP


def _ramp(ramp: Iterable[tuple[float, tuple[int, int, int]]], t: float) -> tuple[int, int, int]:
    pts = list(ramp)
    t = min(1.0, max(0.0, t))
    for (t0, c0), (t1, c1) in zip(pts, pts[1:]):
        if t <= t1:
            k = 0.0 if t1 == t0 else (t - t0) / (t1 - t0)
            return tuple(int(round(c0[i] + (c1[i] - c0[i]) * k)) for i in range(3))  # type: ignore[return-value]
    return pts[-1][1]


def skin_rgb(skin_tone: float, tan: float = 0.0, ramp=_SKIN_RAMP) -> tuple[int, int, int]:
    r, g, b = _ramp(ramp, skin_tone)
    # cyclists are tanned: push warmth/darkness slightly, never change ancestry
    k = 0.16 * tan
    return (
        int(min(255, r * (1 - k * 0.35))),
        int(min(255, g * (1 - k * 0.55))),
        int(min(255, b * (1 - k * 0.75))),
    )


LIFT = (118, 100, 88)  # warm grey the facial-hair tint is lifted towards


def lip_rgb(skin: tuple[int, int, int]) -> tuple[int, int, int]:
    # Between skin-close (thread) and brick-rose (sausage). Fill must still read.
    return (
        int(min(255, skin[0] * 0.91 + 9)),
        int(min(255, skin[1] * 0.57 + 3)),
        int(min(255, skin[2] * 0.55 + 2)),
    )


def gray_hair_rgb(base: tuple[int, int, int], gray: float) -> tuple[int, int, int]:
    target = (150, 150, 150)
    g = min(1.0, max(0.0, gray)) * 0.9
    return tuple(int(round(base[i] + (target[i] - base[i]) * g)) for i in range(3))  # type: ignore[return-value]


# --------------------------------------------------------------------------- #
# transforms
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class Xform:
    scale_x: float = 1.0
    scale_y: float = 1.0
    dx: float = 0.0
    dy: float = 0.0

    @property
    def identity(self) -> bool:
        return (
            abs(self.scale_x - 1) < 1e-4
            and abs(self.scale_y - 1) < 1e-4
            and abs(self.dx) < 1e-3
            and abs(self.dy) < 1e-3
        )


def _c(v: float, lo: float, hi: float) -> float:
    """Map a normalized 0..1 param onto a physical range."""
    return lo + (hi - lo) * min(1.0, max(0.0, v))


def _global_face(shape: dict[str, float]) -> Xform:
    return Xform(
        scale_x=_c(shape["face_width"], 0.87, 1.13),
        scale_y=_c(shape["face_height"], 0.92, 1.08),
    )


def _local_xform(category: str, shape: dict[str, float], mirrored: bool = False) -> Xform:
    """Per-feature affine driven by the continuous identity parameters.

    Ranges are the main tuning knob for "do all riders look like the same
    person": too tight and every face reads identically, too loose and faces
    stop looking human.
    """
    asym = shape.get("asymmetry", 0.0)
    if category == "eyes":
        return Xform(
            scale_x=_c(shape["eye_size"], 0.97, 1.02),
            scale_y=_c(shape["eye_size"], 0.97, 1.04),
            dx=_c(shape["eye_spacing"], -5.0, 5.0),
            dy=_c(shape["eye_height"], -6.0, 6.0) + (asym * 1.1 if mirrored else 0.0),
        )
    if category == "eyebrows":
        return Xform(
            scale_x=_c(shape["eye_size"], 0.94, 1.10),
            dx=_c(shape["eye_spacing"], -5.0, 5.0),
            dy=_c(shape["eye_height"], -6.0, 6.0)
            + _c(shape["brow_height"], -9.0, 5.0)
            + (asym * 1.6 if mirrored else 0.0),
        )
    if category == "nose":
        return Xform(
            scale_x=_c(shape["nose_width"], 0.88, 1.20),
            scale_y=_c(shape["nose_length"], 0.88, 1.16),
            dx=asym * 1.0,
        )
    if category == "mouth":
        return Xform(
            scale_x=_c(shape["mouth_width"], 0.98, 1.02),
            scale_y=_c(shape["mouth_height"], 0.99, 1.04),
            dx=asym * 1.6,
            dy=_c(shape.get("mouth_y", 0.5), -3.0, 3.0),
        )
    if category == "ears":
        return Xform(
            scale_x=_c(shape["ear_size"], 0.86, 1.14),
            scale_y=_c(shape["ear_size"], 0.86, 1.14),
        )
    if category == "neck":
        return Xform(scale_x=_c(shape["neck_thickness"], 0.97, 1.03))
    if category == "jersey":
        return Xform(scale_x=_c(shape["shoulder_width"], 0.93, 1.09))
    return Xform()


def _compose(
    g: Xform, l: Xform, anchor: tuple[float, float], mirrored: bool, pivot: tuple[float, float]
) -> Xform:
    """Fold local (about the asset anchor) and global (about the eye-line pivot)."""
    ax, ay = anchor
    px, py = pivot
    dx = -l.dx if mirrored else l.dx
    sx = g.scale_x * l.scale_x
    sy = g.scale_y * l.scale_y
    tx = g.scale_x * (ax + dx - l.scale_x * ax - px) + px
    ty = g.scale_y * (ay + l.dy - l.scale_y * ay - py) + py
    return Xform(sx, sy, tx, ty)


def _place(crop: Image.Image, box: tuple[int, int, int, int], x: Xform) -> tuple[Image.Image, int, int]:
    """Scale/translate a cropped layer straight into destination pixel space.

    Using resize(box=...) on the small crop instead of a full-canvas affine
    transform is what makes the compositor fast enough to bulk-render a whole
    peloton; both are mathematically the same scale+translate.
    """
    cx0, cy0, cx1, cy1 = box
    a, e = x.scale_x, x.scale_y
    tx0 = max(0, int(math.floor(a * cx0 + x.dx)))
    ty0 = max(0, int(math.floor(e * cy0 + x.dy)))
    tx1 = min(SIZE, int(math.ceil(a * cx1 + x.dx)))
    ty1 = min(SIZE, int(math.ceil(e * cy1 + x.dy)))
    if tx1 <= tx0 or ty1 <= ty0:
        return crop, tx0, ty0
    src = (
        max(0.0, (tx0 - x.dx) / a - cx0),
        max(0.0, (ty0 - x.dy) / e - cy0),
        min(float(crop.width), (tx1 - x.dx) / a - cx0),
        min(float(crop.height), (ty1 - x.dy) / e - cy0),
    )
    return crop.resize((tx1 - tx0, ty1 - ty0), Image.BICUBIC, src), tx0, ty0


# --------------------------------------------------------------------------- #
# pack
# --------------------------------------------------------------------------- #


PAD = 2  # crop padding, keeps sub-pixel resize boxes inside the crop


class Pack:
    """Manifest + lazily loaded, process-cached PNG layers.

    Layers are cached already cropped to their non-transparent bounding box,
    which is what keeps both the transform and the blend cheap.
    """

    def __init__(self, root: str | Path) -> None:
        from . import manifest as manifest_mod

        self.root = Path(root)
        self.manifest: Manifest = manifest_mod.load(self.root / "manifest.json")
        self.skin_ramp = ramp_from_palette(self.manifest.palettes.get("skin_ramp"))
        self._images: dict[tuple[str, bool], tuple[Image.Image, tuple[int, int, int, int]]] = {}

    def layer(self, file: str, mirrored: bool = False) -> tuple[Image.Image, tuple[int, int, int, int]]:
        key = (file, mirrored)
        hit = self._images.get(key)
        if hit is None:
            img = Image.open(self.root / file)
            if img.size != (SIZE, SIZE):
                raise ValueError(f"{file}: expected {SIZE}x{SIZE}, got {img.size}")
            img = img.convert("RGBA")
            if mirrored:
                img = img.transpose(Image.FLIP_LEFT_RIGHT)
            bbox = img.getchannel("A").getbbox() or (0, 0, 1, 1)
            box = (
                max(0, bbox[0] - PAD),
                max(0, bbox[1] - PAD),
                min(SIZE, bbox[2] + PAD),
                min(SIZE, bbox[3] + PAD),
            )
            hit = (img.crop(box), box)
            self._images[key] = hit
        return hit


# --------------------------------------------------------------------------- #
# compositing
# --------------------------------------------------------------------------- #


def _blend_into(dst_rgb, dst_a, src_rgb, src_a, mode: str) -> None:
    """Blend `src` into the `dst` sub-arrays in place (straight, not premultiplied)."""
    a = src_a[:, :, None]
    if mode == "normal":
        out_a = src_a + dst_a * (1.0 - src_a)
        safe = np.maximum(out_a, 1e-6)[:, :, None]
        dst_rgb[...] = (src_rgb * a + dst_rgb * dst_a[:, :, None] * (1.0 - a)) / safe
        dst_a[...] = out_a
        return
    if mode == "multiply":
        dst_rgb *= 1.0 - a * (1.0 - src_rgb)
        return
    if mode == "screen":
        dst_rgb += a * (1.0 - dst_rgb) * src_rgb
        return
    raise ValueError(f"unknown blend mode {mode!r}")


def _tint_for(slot: str | None, app: Appearance, pack: Pack) -> tuple[float, float, float] | None:
    if slot is None:
        return None
    pal = pack.manifest.palettes
    ident, mut = app.identity, app.mutable
    skin = skin_rgb(ident["skin_tone"], mut.get("tan_strength", 0.0), pack.skin_ramp)
    if slot == "skin":
        rgb = skin
    elif slot == "lip":
        rgb = lip_rgb(skin)
    elif slot == "hair":
        base = pal["hair_color"].get(mut.get("hair_color") or "", [40, 34, 30])
        rgb = gray_hair_rgb(tuple(base), mut.get("gray", 0.0))  # type: ignore[arg-type]
    elif slot == "brow":
        base = pal["hair_color"].get(mut.get("hair_color") or "", [40, 34, 30])
        # brows read darker than hair and grey later than hair does
        g = gray_hair_rgb(tuple(base), mut.get("gray", 0.0) * 0.55)  # type: ignore[arg-type]
        rgb = (int(g[0] * 0.72), int(g[1] * 0.70), int(g[2] * 0.70))
    elif slot == "facial_hair":
        base = pal["hair_color"].get(mut.get("hair_color") or "", [40, 34, 30])
        rgb = gray_hair_rgb(tuple(base), min(1.0, mut.get("gray", 0.0) * 1.25))  # type: ignore[arg-type]
        # lift towards a warm grey: pure hair colour reads as a solid black
        # balaclava at portrait size
        rgb = tuple(int(rgb[i] + (LIFT[i] - rgb[i]) * 0.12) for i in range(3))  # type: ignore[assignment]
    elif slot == "iris":
        rgb = tuple(pal["iris_color"].get(ident.get("iris_color") or "", [92, 76, 58]))  # type: ignore[assignment]
    elif slot.startswith("team_"):
        kit = _kit_colors(app, pack)
        rgb = tuple(kit[slot])  # type: ignore[assignment]
    else:
        raise ValueError(f"unknown color slot {slot!r}")
    return (rgb[0] / 255.0, rgb[1] / 255.0, rgb[2] / 255.0)


# classification kits, colours taken from the merged UI lab (09-avatar-lab.html)
INK = [27, 28, 31]
JERSEY_OVERRIDES: dict[str, dict[str, list[int]]] = {
    "tour": {"team_primary": [255, 212, 0], "team_secondary": INK, "team_accent": INK},
    "giro": {"team_primary": [230, 111, 162], "team_secondary": [255, 255, 255], "team_accent": INK},
    "vuelta": {"team_primary": [209, 31, 31], "team_secondary": [255, 255, 255], "team_accent": INK},
    "world": {"team_primary": [252, 250, 244], "team_secondary": [238, 236, 230], "team_accent": INK},
}
# older names kept so a save written before the UI lab landed still resolves
OVERRIDE_ALIASES = {
    "leader": "tour",
    "leader_tour": "tour",
    "leader_giro": "giro",
    "leader_vuelta": "vuelta",
    "world_champion": "world",
    "national_champion": "national",
}
BAND_OVERLAYS = {"world": "overlay_bands_rainbow", "national": "overlay_bands_champion"}


def normalise_override(override: str | None) -> str | None:
    if override is None:
        return None
    return OVERRIDE_ALIASES.get(override, override)


def _kit_colors(app: Appearance, pack: Pack) -> dict[str, list[int]]:
    teams = pack.manifest.teams
    team_id = app.equipment.get("team_id") or next(iter(teams))
    team = teams.get(team_id, next(iter(teams.values())))
    kit = {
        "team_primary": team["primary"],
        "team_secondary": team["secondary"],
        "team_accent": team["accent"],
    }
    override = normalise_override(app.equipment.get("jersey_override"))
    if override in JERSEY_OVERRIDES:
        return dict(JERSEY_OVERRIDES[override])
    if override == "national":
        nat = team.get("nation_colors", [[220, 224, 230], [40, 60, 150]])
        return {"team_primary": [252, 250, 244], "team_secondary": nat[0], "team_accent": nat[1]}
    return kit


def _layer_plan(app: Appearance, pack: Pack) -> list[tuple[Asset, Part, bool]]:
    """Resolve appearance -> ordered (asset, part, mirrored) triples."""
    m = pack.manifest
    mut, eq = app.mutable, app.equipment
    selection: dict[str, list[str]] = {}

    def put(cat: str, asset_id: str | None) -> None:
        if asset_id:
            selection.setdefault(cat, []).append(asset_id)

    put("jersey", eq.get("jersey_template"))
    put("jersey_overlay", BAND_OVERLAYS.get(normalise_override(eq.get("jersey_override")) or ""))
    put("neck", app.identity.get("neck") or "neck_01")
    put("ears", app.identity.get("ears"))
    put("head", app.identity["head"])
    for cat in ("nose", "mouth", "eyes", "eyebrows"):
        put(cat, app.identity.get(cat))
    for det in mut.get("skin_details", ()):  # tan lines, freckles, scars...
        put("skin_details", det)
    put("wrinkles", "wrinkles_set_01")
    put("facial_hair", mut.get("facial_hair"))
    put("hair", mut.get("hair"))
    if eq.get("glasses_worn"):
        put("glasses", eq.get("glasses"))
    if eq.get("helmet_worn"):
        put("helmet", eq.get("helmet"))

    plan: list[tuple[Asset, Part, bool]] = []
    for cat in m.layer_order:
        for asset_id in selection.get(cat, ()):
            asset = m.get(asset_id)
            sides = (False, True) if asset.mirrored else (False,)
            for mirrored in sides:
                for part in asset.parts:
                    plan.append((asset, part, mirrored))
    return plan


def _part_opacity(part: Part, app: Appearance) -> float:
    o = part.opacity
    if part.opacity_from:
        src = app.mutable.get(part.opacity_from)
        if src is None:
            src = app.identity.get(part.opacity_from, 1.0)
        o *= float(src)
    return min(1.0, max(0.0, o))


def render(app: Appearance, pack: Pack) -> Image.Image:
    canvas = pack.manifest.canvas
    pivot = (float(canvas.get("center_x", CENTER_X)), float(canvas.get("eye_line_y", EYE_Y)))
    g = _global_face(app.shape)
    dst_rgb = np.zeros((SIZE, SIZE, 3), dtype=np.float32)
    dst_a = np.zeros((SIZE, SIZE), dtype=np.float32)
    tints: dict[str | None, tuple[float, float, float] | None] = {}

    for asset, part, mirrored in _layer_plan(app, pack):
        opacity = _part_opacity(part, app)
        if opacity <= 0.004:
            continue
        crop, box = pack.layer(part.file, mirrored)
        anchor = asset.anchor
        if mirrored:
            anchor = (SIZE - 1 - anchor[0], anchor[1])
        local = _local_xform(asset.category, app.shape, mirrored)
        # hair recedes upward/backward with the hairline; helmet/glasses stay put
        if asset.category == "hair":
            rec = app.mutable.get("hairline_recession", 0.0)
            local = Xform(local.scale_x, local.scale_y * (1.0 - 0.05 * rec), local.dx, local.dy - 3.0 * rec)
        body_layer = asset.category in ("jersey", "jersey_overlay", "neck")
        x = _compose(Xform() if body_layer else g, local, anchor, mirrored, pivot)

        if x.identity:
            placed, px0, py0 = crop, box[0], box[1]
        else:
            placed, px0, py0 = _place(crop, box, x)
        arr = np.asarray(placed, dtype=np.float32)
        px1 = min(SIZE, px0 + placed.width)
        py1 = min(SIZE, py0 + placed.height)
        if px1 <= px0 or py1 <= py0:
            continue
        arr = arr[: py1 - py0, : px1 - px0] * (1.0 / 255.0)
        src_rgb, src_a = arr[:, :, :3], arr[:, :, 3]

        if part.color_slot not in tints:
            tints[part.color_slot] = _tint_for(part.color_slot, app, pack)
        tint = tints[part.color_slot]
        if tint is not None:
            src_rgb = src_rgb * np.asarray(tint, dtype=np.float32)
        if opacity < 1.0:
            src_a = src_a * opacity
        _blend_into(dst_rgb[py0:py1, px0:px1], dst_a[py0:py1, px0:px1], src_rgb, src_a, part.blend)

    out = np.concatenate([np.clip(dst_rgb, 0, 1), np.clip(dst_a, 0, 1)[:, :, None]], axis=2)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), "RGBA")


# --------------------------------------------------------------------------- #
# cache identity
# --------------------------------------------------------------------------- #


def crop_head(img: Image.Image, pack: "Pack") -> Image.Image:
    """Tighter square crop for small UI sizes.

    A head-and-shoulders master downscaled to a 48 px list icon wastes most of
    its pixels on jersey and empty margin, so the UI should crop first.
    """
    box = pack.manifest.canvas.get("head_crop")
    if not box:
        return img
    return img.crop(tuple(int(v) for v in box))


def cache_key(app: Appearance) -> str:
    """Stable key over everything that can change a pixel.

    Age itself is NOT part of the key - only the age stage plus the derived
    aging values are, so a birthday inside the same stage is a cache hit.
    """
    payload = {
        "schema": app.avatar_schema_version,
        "pack": app.asset_pack_version,
        "seed": app.seed_version,
        "rider": app.rider_id,
        "salt": app.salt,
        "identity": app.identity,
        "shape": app.shape,
        "mutable": app.mutable,
        "equipment": app.equipment,
    }
    blob = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.blake2b(blob, digest_size=8).hexdigest()


def cache_path(app: Appearance, cache_dir: str | Path) -> Path:
    return Path(cache_dir) / f"{app.rider_id}_{app.asset_pack_version}_{cache_key(app)}.png"


def render_cached(app: Appearance, pack: Pack, cache_dir: str | Path) -> Image.Image:
    path = cache_path(app, cache_dir)
    if path.exists():
        return Image.open(path).convert("RGBA")
    path.parent.mkdir(parents=True, exist_ok=True)
    img = render(app, pack)
    img.save(path)
    return img
