#!/usr/bin/env python3
"""Validate an asset pack against the master-reference contract."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab import validate  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


def main(argv: list[str]) -> int:
    pack = Path(argv[1]) if len(argv) > 1 else ROOT / "out" / "pack"
    report = validate.validate(pack)
    print(report.text())
    return 0 if report.ok else 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
