"""Asset-pack manifest: the data contract between the art pack and the game.

The manifest is plain JSON so it can be authored/edited without touching code,
and later re-implemented 1:1 in C#. Nothing here knows how the pixels were made
(hand-drawn, AI-generated, or the placeholder baker in this prototype).
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


@dataclass(frozen=True)
class Part:
    """One PNG belonging to an asset, with its blend mode and color slot."""

    file: str
    blend: str = "normal"  # normal | multiply | screen
    color_slot: str | None = None  # skin | hair | iris | lip | team_primary | ...
    opacity_from: str | None = None  # continuous param driving alpha (e.g. wrinkle_strength)
    opacity: float = 1.0


@dataclass(frozen=True)
class Asset:
    asset_id: str
    category: str
    parts: tuple[Part, ...]
    weight: float = 1.0
    anchor: tuple[float, float] = (256.0, 256.0)
    mirrored: bool = False  # single-side asset, composited twice
    tags: tuple[str, ...] = ()
    min_age: int | None = None
    max_age: int | None = None
    requires_tags: tuple[str, ...] = ()  # asset needs these tags on already-chosen assets
    excludes_tags: tuple[str, ...] = ()  # asset is invalid if any of these tags is present
    roles: tuple[str, ...] = ()  # empty = any role; otherwise rider / manager / ...
    region_weights: dict[str, float] = field(default_factory=dict)


@dataclass(frozen=True)
class Manifest:
    pack_id: str
    style: str
    asset_pack_version: str
    avatar_schema_version: int
    seed_version: int
    canvas: dict[str, Any]
    layer_order: tuple[str, ...]
    assets: tuple[Asset, ...]
    palettes: dict[str, dict[str, list[int]]]
    teams: dict[str, dict[str, Any]]

    def by_category(self, category: str) -> list[Asset]:
        return [a for a in self.assets if a.category == category]

    def get(self, asset_id: str) -> Asset:
        for a in self.assets:
            if a.asset_id == asset_id:
                return a
        raise KeyError(asset_id)


def _part_from_json(d: dict[str, Any]) -> Part:
    return Part(
        file=d["file"],
        blend=d.get("blend", "normal"),
        color_slot=d.get("color_slot"),
        opacity_from=d.get("opacity_from"),
        opacity=float(d.get("opacity", 1.0)),
    )


def _asset_from_json(d: dict[str, Any]) -> Asset:
    return Asset(
        asset_id=d["id"],
        category=d["category"],
        parts=tuple(_part_from_json(p) for p in d["parts"]),
        weight=float(d.get("weight", 1.0)),
        anchor=(float(d.get("anchor", [256, 256])[0]), float(d.get("anchor", [256, 256])[1])),
        mirrored=bool(d.get("mirrored", False)),
        tags=tuple(d.get("tags", ())),
        min_age=d.get("min_age"),
        max_age=d.get("max_age"),
        requires_tags=tuple(d.get("requires_tags", ())),
        excludes_tags=tuple(d.get("excludes_tags", ())),
        roles=tuple(d.get("roles", ())),
        region_weights={k: float(v) for k, v in d.get("region_weights", {}).items()},
    )


def load(path: str | Path) -> Manifest:
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    return Manifest(
        pack_id=data["pack_id"],
        style=data.get("style", "unspecified"),
        asset_pack_version=data["asset_pack_version"],
        avatar_schema_version=int(data["avatar_schema_version"]),
        seed_version=int(data["seed_version"]),
        canvas=data["canvas"],
        layer_order=tuple(data["layer_order"]),
        assets=tuple(_asset_from_json(a) for a in data["assets"]),
        palettes=data["palettes"],
        teams=data["teams"],
    )


def dump(manifest_dict: dict[str, Any], path: str | Path) -> None:
    Path(path).write_text(
        json.dumps(manifest_dict, indent=2, ensure_ascii=False, sort_keys=False) + "\n",
        encoding="utf-8",
    )
