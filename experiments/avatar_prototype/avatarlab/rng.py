"""Deterministic, domain-separated random streams.

Every draw is derived from a hash of (seed_version, rider_id, domain, salt).
Domain separation means adding a new trait category later does not shift the
values already drawn for existing categories, so old riders keep their faces
when the asset library grows.
"""

from __future__ import annotations

import hashlib
import struct
from typing import Sequence

_MASK64 = (1 << 64) - 1


def _hash64(*parts: object) -> int:
    h = hashlib.blake2b(b"|".join(str(p).encode("utf-8") for p in parts), digest_size=8)
    return struct.unpack("<Q", h.digest())[0]


class Stream:
    """SplitMix64 stream seeded from a hashed domain key."""

    __slots__ = ("_state",)

    def __init__(self, *key: object) -> None:
        self._state = _hash64(*key)

    def next_u64(self) -> int:
        self._state = (self._state + 0x9E3779B97F4A7C15) & _MASK64
        z = self._state
        z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & _MASK64
        z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & _MASK64
        return z ^ (z >> 31)

    def unit(self) -> float:
        """Float in [0, 1)."""
        return (self.next_u64() >> 11) / float(1 << 53)

    def range(self, lo: float, hi: float) -> float:
        return lo + (hi - lo) * self.unit()

    def below(self, n: int) -> int:
        if n <= 0:
            raise ValueError("n must be positive")
        return self.next_u64() % n

    def chance(self, p: float) -> bool:
        return self.unit() < p

    def pick(self, items: Sequence[object]) -> object:
        return items[self.below(len(items))]

    def normal_unit(self, mean: float = 0.5, spread: float = 0.17) -> float:
        """Bell-ish value clamped to [0, 1] (mean of 3 uniforms; no libm needed)."""
        s = (self.unit() + self.unit() + self.unit()) / 3.0
        v = mean + (s - 0.5) * (spread / 0.1667) * 1.0
        return min(1.0, max(0.0, v))


def neg_log2_q32(u: int) -> int:
    """`-log2(u / 2**64)` in Q32 fixed point, integer math only.

    Weighted selection needs an Exp(1) variable per candidate, i.e. `-ln(u)`.
    Using a libm `log` would make the result depend on the platform's libm, so
    the game and the tools could disagree on a rider's face. This bit-by-bit
    fixed-point log2 gives the same integer on every platform, and the constant
    factor between log2 and ln cancels out because only ratios are compared.
    """
    if u <= 0:
        raise ValueError("u must be positive")
    k = 0
    x = u
    while x < (1 << 63):  # normalise to x / 2**63 in [1, 2)
        x <<= 1
        k += 1
    frac = 0
    y = x
    for i in range(1, 33):
        y = (y * y) >> 63
        if y >= (1 << 64):
            y >>= 1
            frac |= 1 << (32 - i)
    return ((k + 1) << 32) - frac


class RiderRng:
    """Factory of independent streams for one rider."""

    __slots__ = ("rider_id", "seed_version", "salt")

    def __init__(self, rider_id: object, seed_version: int, salt: int = 0) -> None:
        self.rider_id = rider_id
        self.seed_version = seed_version
        self.salt = salt

    def u64_for(self, domain: str, key: str, salted: bool = False) -> int:
        """One hashed draw bound to a single (domain, key) pair.

        Used by append-stable weighted selection: every candidate asset gets its
        own independent draw, so adding an asset to a category cannot reshuffle
        the riders who were not moved to it.
        """
        salt = self.salt if salted else 0
        return _hash64("pmav", self.seed_version, self.rider_id, domain, salt, key)

    def stream(self, domain: str, salted: bool = False) -> Stream:
        """Stream for `domain`.

        `salted=False` streams ignore the duplicate-resolution salt, so identity
        traits never move when a clone collision is fixed on secondary traits.
        """
        salt = self.salt if salted else 0
        return Stream("pmav", self.seed_version, self.rider_id, domain, salt)

    def with_salt(self, salt: int) -> "RiderRng":
        return RiderRng(self.rider_id, self.seed_version, salt)
