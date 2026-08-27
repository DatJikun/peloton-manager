"""Experimental look passes for the cousin-face problem.

These do NOT change the locked `poster` StyleProfile. Each look remaps asset
weights and affine ranges, then the same rider ids are rendered side by side.

Sources (public agent skills / art-direction docs, fetched 2026-08-27):

- shape: character-design skill, 3-read rule + circle/square/triangle
  https://github.com/omer-metin/skills-for-antigravity/blob/main/skills/character-design/SKILL.md
- landmark: character-design-sheet DNA inverted for a CAST
  (one loud identity mark per face, not consistency of one hero)
  https://www.explainx.ai/skills/inference-sh/skills/character-design-sheet
- archetype: stylized-style exaggeration + ActorMIXER families
  Average / Heroic / Brute / Grumpy / Heavy / Thin
  https://github.com/arjun988/blender-skills/blob/main/.claude/skills/stylized-style/SKILL.md
"""

from __future__ import annotations

from typing import Any

from ..render import DEFAULT_SHAPE_RANGES

DEFAULT_RANGES = DEFAULT_SHAPE_RANGES


LOOKS: dict[str, dict[str, Any]] = {
    "shape": {
        "title": "Kształt (3-read / shape language)",
        "skill": "character-design · 3-read rule, circle/square/triangle",
        "source": "https://github.com/omer-metin/skills-for-antigravity/blob/main/skills/character-design/SKILL.md",
        "blurb": "Sylwetka najpierw: okrągła, kwadratowa albo trójkątna głowa. Crop nie może zdominować puli.",
        "shape_ranges": {
            "face_width": (0.76, 1.28),
            "face_height": (0.84, 1.18),
            "ear_size": (0.80, 1.22),
            "neck_thickness": (0.92, 1.10),
        },
        # Circle / square / triangle families get the mass. Oval is the muddy average.
        "weights": {
            "head_01_oval": 0.03,
            "head_19_soft_oval": 0.03,
            "head_22_narrow_oval": 0.04,
            "head_23_wide_oval": 0.04,
            "head_24_short_oval": 0.03,
            "head_25_tall_oval": 0.04,
            "head_04_round": 0.14,
            "head_21_soft_round": 0.12,
            "head_15_compact": 0.10,
            "head_03_square": 0.14,
            "head_20_soft_square": 0.12,
            "head_08_heavy_jaw": 0.11,
            "head_06_broad": 0.10,
            "head_05_angular": 0.13,
            "head_07_tapered": 0.12,
            "head_26_soft_angular": 0.11,
            "head_02_long": 0.10,
            "hair_02_crop": 0.015,
            "hair_01_buzz": 0.015,
            "hair_15_flat_helmet": 0.01,
            "hair_21_crew_cut": 0.02,
            "hair_41_soft_crop": 0.01,
            "hair_44_round_crop": 0.01,
            "hair_36_wavy_crop": 0.02,
            "hair_50_soft_caesar": 0.02,
            "hair_04_messy_short": 0.03,
            "hair_17_fringe": 0.18,
            "hair_52_heavy_fringe": 0.16,
            "hair_49_neat_fringe": 0.14,
            "hair_22_high_fade": 0.14,
            "hair_53_skin_fade": 0.14,
            "hair_42_low_fade": 0.12,
            "hair_18_undercut": 0.12,
            "hair_16_quiff": 0.12,
            "hair_12_receded": 0.11,
            "hair_51_high_forehead": 0.12,
            "hair_06_curly_short": 0.12,
            "hair_34_coils": 0.10,
            "hair_25_shaved": 0.10,
        },
    },
    "landmark": {
        "title": "Znak (DNA / distinctive mark)",
        "skill": "character-design-sheet · identity anchor, inverted for a cast",
        "source": "https://www.explainx.ai/skills/inference-sh/skills/character-design-sheet",
        "blurb": "Każda twarz ma jeden głośny znak: grzywka, fade, wysokie czoło, orli nos, wąskie usta, zarost.",
        "shape_ranges": {
            "nose_width": (0.72, 1.36),
            "nose_length": (0.72, 1.30),
            "mouth_height": (0.82, 1.12),
            "mouth_width": (0.90, 1.06),
        },
        "weights": {
            "head_01_oval": 0.04,
            "nose_01_straight": 0.01,
            "nose_22_even": 0.01,
            "nose_23_even_short": 0.01,
            "nose_24_even_long": 0.01,
            "nose_25_mild_wide": 0.01,
            "nose_26_mild_narrow": 0.01,
            "nose_29_straight_plus": 0.01,
            "nose_32_short_straight": 0.01,
            "nose_03_wide": 0.12,
            "nose_08_narrow": 0.11,
            "nose_05_aquiline": 0.12,
            "nose_12_hawk": 0.11,
            "nose_06_upturned": 0.11,
            "nose_11_snub": 0.10,
            "nose_14_button": 0.10,
            "nose_35_long_thin": 0.11,
            "nose_36_short_wide": 0.11,
            "nose_37_hook_long": 0.11,
            "mouth_01_medium": 0.02,
            "mouth_36_even": 0.01,
            "mouth_75_slim": 0.10,
            "mouth_76_slim_wide": 0.08,
            "mouth_80_razor": 0.09,
            "mouth_22_laugh": 0.08,
            "mouth_04_narrow": 0.08,
            "mouth_27_thick": 0.06,
            "mouth_60_heavy_lower": 0.07,
            "hair_02_crop": 0.015,
            "hair_01_buzz": 0.015,
            "hair_41_soft_crop": 0.01,
            "hair_44_round_crop": 0.01,
            "hair_21_crew_cut": 0.02,
            "hair_17_fringe": 0.16,
            "hair_52_heavy_fringe": 0.15,
            "hair_22_high_fade": 0.14,
            "hair_53_skin_fade": 0.14,
            "hair_12_receded": 0.13,
            "hair_32_combover": 0.10,
            "hair_51_high_forehead": 0.13,
            "hair_16_quiff": 0.12,
            "hair_28_curtains": 0.11,
            "hair_23_mid_part": 0.11,
            "hair_34_coils": 0.11,
            "hair_25_shaved": 0.10,
            "fh_01_stubble_light": 0.40,
            "fh_02_stubble_heavy": 0.28,
            "fh_03_beard_short": 0.22,
            "fh_04_beard_full": 0.16,
            "fh_05_goatee": 0.16,
        },
    },
    "archetype": {
        "title": "Archetyp (stylized / ActorMIXER)",
        "skill": "stylized-style + Average/Heroic/Brute/Grumpy/Heavy/Thin",
        "source": "https://github.com/arjun988/blender-skills/blob/main/.claude/skills/stylized-style/SKILL.md",
        "blurb": "Sześć rodzin proporcji, mocniej odepchniętych od średniej twarzy.",
        "shape_ranges": {
            "face_width": (0.74, 1.30),
            "face_height": (0.82, 1.20),
            "nose_width": (0.70, 1.38),
            "nose_length": (0.70, 1.32),
            "mouth_height": (0.80, 1.14),
            "ear_size": (0.78, 1.24),
            "neck_thickness": (0.90, 1.14),
            "shoulder_width": (0.88, 1.16),
        },
        "weights": {
            # Average
            "head_01_oval": 0.06,
            "head_19_soft_oval": 0.06,
            # Heroic
            "head_02_long": 0.12,
            "head_09_high_crown": 0.11,
            "head_25_tall_oval": 0.11,
            # Brute
            "head_03_square": 0.12,
            "head_08_heavy_jaw": 0.12,
            "head_06_broad": 0.11,
            # Grumpy
            "head_15_compact": 0.11,
            "head_24_short_oval": 0.10,
            "head_14_pear": 0.10,
            # Heavy
            "head_04_round": 0.12,
            "head_21_soft_round": 0.11,
            "head_23_wide_oval": 0.10,
            # Thin
            "head_05_angular": 0.12,
            "head_07_tapered": 0.12,
            "head_22_narrow_oval": 0.11,
            "head_26_soft_angular": 0.11,
            "hair_05_swept_medium": 0.13,
            "hair_20_wavy_medium": 0.12,
            "hair_17_fringe": 0.13,
            "hair_18_undercut": 0.12,
            "hair_06_curly_short": 0.12,
            "hair_16_quiff": 0.11,
            "hair_12_receded": 0.10,
            "hair_51_high_forehead": 0.10,
            "hair_02_crop": 0.02,
            "hair_01_buzz": 0.02,
            "hair_41_soft_crop": 0.01,
            "hair_44_round_crop": 0.01,
            "nose_05_aquiline": 0.10,
            "nose_07_broad_flat": 0.10,
            "nose_08_narrow": 0.10,
            "nose_13_roman": 0.10,
            "nose_11_snub": 0.09,
            "nose_35_long_thin": 0.10,
            "nose_36_short_wide": 0.10,
        },
    },
}


def remap_assets(assets: list[dict[str, Any]], look: str) -> list[dict[str, Any]]:
    spec = LOOKS[look]
    table: dict[str, float] = spec["weights"]
    out = []
    for a in assets:
        b = dict(a)
        if a["id"] in table and a.get("weight", 0) > 0:
            b["weight"] = table[a["id"]]
        out.append(b)
    return out


def merged_ranges(look: str) -> dict[str, list[float]]:
    """Full affine table for a look. Eye size stays on the 0.15.0 lock."""
    merged = dict(DEFAULT_RANGES)
    merged.update(LOOKS[look]["shape_ranges"])
    merged["eye_size"] = DEFAULT_RANGES["eye_size"]
    merged["eye_spacing"] = DEFAULT_RANGES["eye_spacing"]
    merged["eye_height"] = DEFAULT_RANGES["eye_height"]
    return {k: [float(v[0]), float(v[1])] for k, v in merged.items()}


def apply_look_to_manifest(raw: dict[str, Any], look: str) -> dict[str, Any]:
    if look not in LOOKS:
        raise KeyError(f"unknown look {look!r}; known: {sorted(LOOKS)}")
    out = dict(raw)
    out["look"] = look
    out["shape_ranges"] = merged_ranges(look)
    out["assets"] = remap_assets(list(raw["assets"]), look)
    base_ver = str(raw.get("asset_pack_version", "0.15.0-placeholder"))
    if "-look-" in base_ver:
        base_ver = base_ver.split("-look-")[0]
    out["asset_pack_version"] = f"{base_ver}-look-{look}"
    return out


def unknown_weight_ids(raw: dict[str, Any], look: str) -> list[str]:
    known = {a["id"] for a in raw["assets"]}
    return sorted(aid for aid in LOOKS[look]["weights"] if aid not in known)
