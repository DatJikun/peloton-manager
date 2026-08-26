"""Asset-pack validation.

This is the gate that keeps an asset library usable: every PNG must share the
master-reference canvas, must actually be transparent, must keep its pixels
inside the region its category is allowed to touch, and every manifest
reference must resolve. In production the same script also runs on AI output
before it is accepted into the pack.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
from PIL import Image

from . import manifest as manifest_mod
from .bake.draw import SIZE

# category -> (x0, y0, x1, y1) allowed pixel box in canvas space
REGIONS: dict[str, tuple[int, int, int, int]] = {
    "head": (130, 50, 382, 380),
    "neck": (148, 262, 364, 480),
    "ears": (330, 168, 400, 292),  # single (right) ear, mirrored at runtime
    "eyes": (260, 160, 345, 245),  # single (right) eye
    "eyebrows": (255, 150, 355, 215),
    "nose": (200, 165, 312, 310),
    "mouth": (190, 270, 322, 340),
    "wrinkles": (140, 110, 372, 368),
    "skin_details": (128, 85, 384, 380),
    "hair": (90, 20, 422, 300),
    "facial_hair": (140, 205, 372, 380),
    "glasses": (125, 160, 387, 255),
    "helmet": (115, 30, 397, 360),  # includes chin straps
    "jersey": (0, 375, 512, 512),
    "jersey_overlay": (0, 375, 512, 512),
}

MAX_PARTS_PER_AVATAR = 40


@dataclass
class Report:
    checked_files: int = 0
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    @property
    def ok(self) -> bool:
        return not self.errors

    def text(self) -> str:
        lines = [f"files checked: {self.checked_files}", f"errors: {len(self.errors)}", f"warnings: {len(self.warnings)}"]
        lines += [f"  ERROR   {e}" for e in self.errors]
        lines += [f"  WARN    {w}" for w in self.warnings]
        lines.append("RESULT: PASS" if self.ok else "RESULT: FAIL")
        return "\n".join(lines)


def validate(pack_root: str | Path) -> Report:
    root = Path(pack_root)
    rep = Report()
    mpath = root / "manifest.json"
    if not mpath.exists():
        rep.errors.append("manifest.json missing")
        return rep
    m = manifest_mod.load(mpath)

    if m.canvas.get("size") != [SIZE, SIZE]:
        rep.errors.append(f"manifest canvas size {m.canvas.get('size')} != [{SIZE}, {SIZE}]")

    seen_ids: set[str] = set()
    for asset in m.assets:
        if asset.asset_id in seen_ids:
            rep.errors.append(f"duplicate asset id {asset.asset_id}")
        seen_ids.add(asset.asset_id)
        if asset.category not in m.layer_order and asset.parts:
            rep.errors.append(f"{asset.asset_id}: category {asset.category!r} is not in layer_order")
        if asset.weight <= 0:
            rep.warnings.append(f"{asset.asset_id}: weight {asset.weight} makes the asset unreachable")
        if asset.min_age is not None and asset.max_age is not None and asset.min_age > asset.max_age:
            rep.errors.append(f"{asset.asset_id}: min_age > max_age")

        for part in asset.parts:
            path = root / part.file
            if not path.exists():
                rep.errors.append(f"{asset.asset_id}: missing file {part.file}")
                continue
            rep.checked_files += 1
            img = Image.open(path)
            if img.mode != "RGBA":
                rep.errors.append(f"{part.file}: mode {img.mode} != RGBA")
                continue
            if img.size != (SIZE, SIZE):
                rep.errors.append(f"{part.file}: size {img.size} != ({SIZE}, {SIZE})")
                continue
            alpha = np.asarray(img)[:, :, 3]
            if alpha.max() == 0:
                rep.errors.append(f"{part.file}: fully transparent (empty layer)")
                continue
            if alpha.min() == 255:
                rep.errors.append(f"{part.file}: fully opaque - no transparent background")
            ys, xs = np.nonzero(alpha > 2)
            box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
            allowed = REGIONS.get(asset.category)
            if allowed and not (
                box[0] >= allowed[0] and box[1] >= allowed[1] and box[2] <= allowed[2] and box[3] <= allowed[3]
            ):
                rep.errors.append(f"{part.file}: content box {box} escapes {asset.category} region {allowed}")
            if part.blend not in ("normal", "multiply", "screen"):
                rep.errors.append(f"{part.file}: unknown blend {part.blend!r}")
            if part.color_slot and part.color_slot not in (
                "skin",
                "lip",
                "hair",
                "facial_hair",
                "iris",
                "team_primary",
                "team_secondary",
                "team_accent",
            ):
                rep.errors.append(f"{part.file}: unknown color slot {part.color_slot!r}")

    # every category referenced by the runtime must have at least one asset
    for required in ("head", "eyes", "eyebrows", "nose", "mouth", "ears", "hair", "jersey", "neck"):
        if not m.by_category(required):
            rep.errors.append(f"category {required!r} is empty")

    # tag closure: requires_tags must be producible by some asset
    produced = {t for a in m.assets for t in a.tags} | {"hairline_thinning", "hairline_receded"}
    for asset in m.assets:
        for tag in asset.requires_tags:
            if tag not in produced:
                rep.errors.append(f"{asset.asset_id}: requires tag {tag!r} that nothing produces")

    # a rider must always have at least one legal hair choice at any age
    for age in (18, 25, 32, 40, 48):
        for tagset in ((), ("hairline_thinning",), ("hairline_receded",)):
            legal = [
                a
                for a in m.by_category("hair")
                if (a.min_age is None or age >= a.min_age)
                and (a.max_age is None or age <= a.max_age)
                and all(t in tagset for t in a.requires_tags)
                and not any(t in tagset for t in a.excludes_tags)
            ]
            if not legal:
                rep.errors.append(f"no legal hair for age {age} with tags {tagset}")
    return rep
