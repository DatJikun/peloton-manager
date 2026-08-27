#!/usr/bin/env python3
"""Validate asset packs against the master-reference contract.

    python3 scripts/validate_pack.py            # every out/pack_* directory
    python3 scripts/validate_pack.py <path>     # one pack
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab import validate  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


def main(argv: list[str]) -> int:
    if len(argv) > 1:
        packs = [Path(argv[1])]
    else:
        packs = sorted((ROOT / "out").glob("pack_*"))
    if not packs:
        print("no packs found; run scripts/bake_pack.py first")
        return 2
    failed = 0
    for path in packs:
        report = validate.validate(path)
        print(f"--- {path.name}")
        print(report.text())
        failed += 0 if report.ok else 1
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
