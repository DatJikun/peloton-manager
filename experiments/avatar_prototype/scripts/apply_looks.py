#!/usr/bin/env python3
"""Build experimental look overlays on top of the locked poster pack.

Copies `out/pack_poster` (PNG hardlinks + a rewritten manifest) into
`out/pack_poster_<look>`. Does not rebake, does not touch StyleProfile, and
does not change the default `0.15.0-placeholder` pack.

    python3 scripts/apply_looks.py
    python3 scripts/apply_looks.py shape landmark
"""

from __future__ import annotations

import json
import os
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from avatarlab.bake.looks import LOOKS, apply_look_to_manifest, unknown_weight_ids

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "out" / "pack_poster"


def _copy_pack(src: Path, dst: Path) -> None:
    if dst.exists():
        shutil.rmtree(dst)

    def _copy(src_f: str, dst_f: str) -> str:
        # Hardlink PNGs; copy manifest so a later rewrite cannot mutate poster.
        if Path(src_f).name == "manifest.json":
            return shutil.copy2(src_f, dst_f)
        try:
            os.link(src_f, dst_f)
            return dst_f
        except OSError:
            return shutil.copy2(src_f, dst_f)

    shutil.copytree(src, dst, copy_function=_copy)


def apply_look(look: str) -> Path:
    if look not in LOOKS:
        raise SystemExit(f"unknown look {look!r}; known: {sorted(LOOKS)}")
    if not (SRC / "manifest.json").exists():
        raise SystemExit("run scripts/bake_pack.py poster first")
    dst = ROOT / "out" / f"pack_poster_{look}"
    _copy_pack(SRC, dst)
    raw = json.loads((dst / "manifest.json").read_text(encoding="utf-8"))
    missing = unknown_weight_ids(raw, look)
    if missing:
        raise SystemExit(f"{look}: weight ids not in pack: {missing}")
    rewritten = apply_look_to_manifest(raw, look)
    (dst / "manifest.json").write_text(
        json.dumps(rewritten, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    live = sum(1 for a in rewritten["assets"] if a.get("weight", 0) > 0)
    print(f"  {look:10} -> {dst.name}  version={rewritten['asset_pack_version']}  live_assets={live}")
    return dst


def main(argv: list[str]) -> int:
    looks = argv[1:] or list(LOOKS)
    print(f"source {SRC.name}  looks={looks}")
    for look in looks:
        apply_look(look)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
