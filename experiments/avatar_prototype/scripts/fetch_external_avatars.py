#!/usr/bin/env python3
"""Fetch public seed-based avatar APIs for the generator comparison sheet.

DiceBear HTTP API: https://api.dicebear.com/9.x/{style}/png?seed=...
Multiavatar PNG endpoint currently returns 403 without a key (skipped).

    python3 scripts/fetch_external_avatars.py
"""

from __future__ import annotations

import time
import urllib.error
import urllib.request
from pathlib import Path

OUT = Path("/tmp/avatar_gens/dicebear")
STYLES = ("toon-head", "personas", "avataaars", "adventurer", "micah")
N = 8
BG = "f3ede1"


def url_for(style: str, rid: int) -> str:
    seed = f"peloton-{rid}"
    return (
        f"https://api.dicebear.com/9.x/{style}/png"
        f"?seed={seed}&size=256&backgroundColor={BG}&gender=male"
    )


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    ok = 0
    for i in range(N):
        rid = 500_000 + i * 13
        for style in STYLES:
            dest = OUT / f"{style}_{rid}.png"
            req = urllib.request.Request(url_for(style, rid), headers={"User-Agent": "peloton-avatar-lab/0.15"})
            try:
                with urllib.request.urlopen(req, timeout=20) as resp:
                    dest.write_bytes(resp.read())
                print(f"  ok  {dest.name}")
                ok += 1
            except urllib.error.HTTPError as exc:
                print(f"  fail {dest.name} HTTP {exc.code}")
            time.sleep(0.12)
    print(f"fetched {ok} files into {OUT}")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
