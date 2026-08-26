#!/usr/bin/env python3
"""Bake placeholder asset packs, one per style, into out/pack_<style>/.

    python3 scripts/bake_pack.py             # bakes the default style (flat)
    python3 scripts/bake_pack.py all         # bakes every style
    python3 scripts/bake_pack.py soft flat   # bakes the named styles
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab.bake import pack as baker  # noqa: E402
from avatarlab.bake.draw import STYLES  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_STYLE = "poster"


def main(argv: list[str]) -> int:
    args = argv[1:]
    styles = sorted(STYLES) if args == ["all"] else (args or [DEFAULT_STYLE])
    for style in styles:
        out = ROOT / "out" / f"pack_{style}"
        t0 = time.perf_counter()
        baker.bake(out, style=style)
        files = sorted(out.rglob("*.png"))
        print(f"{style:13s} {len(files):3d} PNG layers in {time.perf_counter() - t0:5.1f}s -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
