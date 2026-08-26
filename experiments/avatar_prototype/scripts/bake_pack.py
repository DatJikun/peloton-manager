#!/usr/bin/env python3
"""Bake the placeholder asset pack into out/pack/."""

from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab.bake import pack as baker  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    out = ROOT / "out" / "pack"
    t0 = time.perf_counter()
    baker.bake(out)
    files = sorted(out.rglob("*.png"))
    print(f"baked {len(files)} PNG layers in {time.perf_counter() - t0:.1f}s -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
