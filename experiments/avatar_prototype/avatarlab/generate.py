"""Deterministic appearance generation.

appearance = f(rider row in DB, manifest version constants)

Split into three blocks that have different lifetimes:

* identity  - permanent for the rider's whole career (skull, eyes, nose, ...)
* mutable   - recomputed from age / world state (hair, gray, wrinkles, beard)
* equipment - recomputed from team + result state (jersey, helmet, glasses)

Only the identity block is allowed to be stable-forever; the other two are
cheap derived data, which is why a team transfer or a birthday cannot change
a rider's face.
"""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any, Iterable

from .manifest import Asset, Manifest
from .rng import RiderRng, Stream, neg_log2_q32  # noqa: F401  (Stream re-exported)

# --------------------------------------------------------------------------- #
# rider input
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class Rider:
    """The subset of rider DB columns the avatar system is allowed to read."""

    rider_id: int
    age: int
    region: str = "west_europe"  # broad ancestry region, probabilistic only
    height_cm: int = 178
    weight_kg: int = 68
    discipline: str = "allrounder"  # sprinter | climber | classics | tt | allrounder
    team_id: str | None = None
    role: str = "rider"  # rider | manager (male peloton only for now)
    jersey_override: str | None = None  # world_champion | national_champion | leader_* | None
    visual_seed: int = 0  # optional manual override knob for editors


# --------------------------------------------------------------------------- #
# weighted selection with constraints
# --------------------------------------------------------------------------- #


def _eligible(asset: Asset, rider: Rider, chosen_tags: set[str]) -> bool:
    if asset.min_age is not None and rider.age < asset.min_age:
        return False
    if asset.max_age is not None and rider.age > asset.max_age:
        return False
    if asset.requires_tags and not all(t in chosen_tags for t in asset.requires_tags):
        return False
    if asset.excludes_tags and any(t in chosen_tags for t in asset.excludes_tags):
        return False
    if asset.roles and rider.role not in asset.roles:
        return False
    return True


def _weight_for(asset: Asset, rider: Rider) -> float:
    w = asset.weight
    if asset.region_weights:
        # region only nudges an existing distribution; it never selects an asset
        w *= asset.region_weights.get(rider.region, asset.region_weights.get("*", 1.0))
    return max(0.0, w)


WEIGHT_SCALE = 10_000  # weights become integer shares, so comparisons stay exact


def weighted_pick(
    rng: RiderRng,
    domain: str,
    assets: Iterable[Asset],
    rider: Rider,
    chosen_tags: set[str],
    salted: bool = False,
) -> Asset | None:
    """Weighted choice that is stable when the pack grows.

    The obvious implementation - one roll walked across the cumulative weights -
    is wrong for a live game: appending an asset changes the total, so the same
    roll lands on a different asset and a slice of the existing riders silently
    get a new face. Instead every candidate gets its own hashed draw and we run
    an exponential race: pick the smallest `-ln(u_i) / w_i`.

    Adding an asset with weight w therefore moves only w / (W + w) of the pool to
    it and leaves everyone else untouched; setting a weight to 0 only affects the
    riders who had that asset. Comparisons use integer cross-multiplication, so
    the result cannot drift between Python and C#.
    """
    best: Asset | None = None
    best_u = 0
    best_w = 0
    for asset in assets:
        if not _eligible(asset, rider, chosen_tags):
            continue
        w = int(round(_weight_for(asset, rider) * WEIGHT_SCALE))
        if w <= 0:
            continue
        # Exp(1) variate in Q32 fixed point: deterministic on every platform
        e = neg_log2_q32(rng.u64_for(domain, asset.asset_id, salted) | 1)
        if best is None:
            best, best_u, best_w = asset, e, w
            continue
        lhs, rhs = e * best_w, best_u * w
        if lhs < rhs or (lhs == rhs and asset.asset_id < best.asset_id):
            best, best_u, best_w = asset, e, w
    return best


# --------------------------------------------------------------------------- #
# appearance
# --------------------------------------------------------------------------- #


@dataclass
class Appearance:
    avatar_schema_version: int
    asset_pack_version: str
    seed_version: int
    rider_id: int
    salt: int
    identity: dict[str, Any] = field(default_factory=dict)
    shape: dict[str, float] = field(default_factory=dict)
    mutable: dict[str, Any] = field(default_factory=dict)
    equipment: dict[str, Any] = field(default_factory=dict)

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


# broad, deliberately overlapping ancestry mixes; nothing is deterministic per
# region, these only tilt a distribution that every region shares.
REGION_SKIN_BIAS: dict[str, tuple[float, float]] = {
    "west_europe": (0.18, 0.14),
    "east_europe": (0.16, 0.13),
    "scandinavia": (0.12, 0.12),
    "iberia": (0.30, 0.16),
    "latin_america": (0.40, 0.22),
    "north_africa": (0.52, 0.18),
    "east_africa": (0.74, 0.16),
    "west_africa": (0.80, 0.14),
    "middle_east": (0.42, 0.18),
    "east_asia": (0.30, 0.15),
    "south_asia": (0.52, 0.18),
    "oceania": (0.22, 0.16),
    "north_america": (0.30, 0.24),
}

DISCIPLINE_BUILD: dict[str, float] = {
    # 0.0 = lean climber build, 1.0 = heavy sprinter build (subtle range only)
    "climber": 0.18,
    "tt": 0.55,
    "classics": 0.62,
    "allrounder": 0.45,
    "sprinter": 0.82,
}


def _skin_tone(rng: RiderRng, rider: Rider) -> float:
    mean, spread = REGION_SKIN_BIAS.get(rider.region, (0.3, 0.2))
    s = rng.stream("identity.skin_tone")
    return round(min(1.0, max(0.02, s.normal_unit(mean, spread))), 4)


def _build_factor(rng: RiderRng, rider: Rider) -> float:
    base = DISCIPLINE_BUILD.get(rider.discipline, 0.45)
    bmi = rider.weight_kg / max(1.4, (rider.height_cm / 100.0) ** 2)
    bmi_term = min(1.0, max(0.0, (bmi - 18.5) / 5.0))
    jitter = rng.stream("identity.build").range(-0.12, 0.12)
    return round(min(1.0, max(0.0, 0.55 * base + 0.35 * bmi_term + 0.10 + jitter)), 4)


def _identity_shape(rng: RiderRng, rider: Rider, build: float) -> dict[str, float]:
    """Continuous permanent geometry, normalized 0..1 unless noted."""
    s = rng.stream("identity.shape")
    face_w = s.normal_unit(0.50, 0.20)
    return {
        "face_width": round(min(1.0, face_w * 0.85 + build * 0.15), 4),
        "face_height": round(s.normal_unit(0.50, 0.20), 4),
        "eye_spacing": round(s.normal_unit(0.50, 0.22), 4),
        "eye_height": round(s.normal_unit(0.50, 0.20), 4),
        "eye_size": round(s.normal_unit(0.50, 0.20), 4),
        "brow_height": round(s.normal_unit(0.50, 0.22), 4),
        "nose_length": round(s.normal_unit(0.50, 0.22), 4),
        "nose_width": round(s.normal_unit(0.50, 0.22), 4),
        "mouth_width": round(s.normal_unit(0.50, 0.20), 4),
        "mouth_height": round(s.normal_unit(0.50, 0.20), 4),
        "mouth_y": round(s.normal_unit(0.50, 0.20), 4),
        "ear_size": round(s.normal_unit(0.50, 0.22), 4),
        "asymmetry": round(s.range(-1.0, 1.0), 4),
        "neck_thickness": round(min(1.0, 0.25 + 0.6 * build + s.range(-0.08, 0.08)), 4),
        "shoulder_width": round(min(1.0, 0.22 + 0.65 * build + s.range(-0.08, 0.08)), 4),
    }


def _aging_genetics(rng: RiderRng) -> dict[str, float]:
    """Permanent 'how does this person age' profile, not the current age look."""
    s = rng.stream("identity.aging_genetics")
    return {
        "gray_onset_age": round(s.range(27.0, 46.0), 2),
        "gray_speed": round(s.range(0.35, 1.25), 3),
        "balding_propensity": round(s.normal_unit(0.35, 0.28), 3),
        "wrinkle_propensity": round(s.normal_unit(0.5, 0.22), 3),
        "sun_damage": round(s.normal_unit(0.55, 0.20), 3),
        "beard_capability": round(s.normal_unit(0.55, 0.25), 3),
    }


def _identity(rng: RiderRng, rider: Rider, m: Manifest) -> tuple[dict[str, Any], dict[str, float]]:
    build = _build_factor(rng, rider)
    tags: set[str] = set()

    head = weighted_pick(rng, "identity.head", m.by_category("head"), rider, tags)
    if head is None:
        raise RuntimeError("pack has no head assets")
    tags |= set(head.tags)

    ident: dict[str, Any] = {"head": head.asset_id}
    for cat in ("ears", "eyes", "eyebrows", "nose", "mouth"):
        pick = weighted_pick(rng, f"identity.{cat}", m.by_category(cat), rider, tags)
        if pick is not None:
            ident[cat] = pick.asset_id
            tags |= set(pick.tags)

    iris = weighted_pick(rng, "identity.iris", m.by_category("iris_color"), rider, tags)
    ident["iris_color"] = iris.asset_id if iris else None
    ident["skin_tone"] = _skin_tone(rng, rider)
    ident["build"] = build
    ident["aging"] = _aging_genetics(rng)
    ident["tags"] = sorted(tags)

    shape = _identity_shape(rng, rider, build)
    return ident, shape


RECESSION_THINNING = 0.30  # tag threshold: hairline_thinning
RECESSION_RECEDED = 0.62  # tag threshold: hairline_receded


def hairline_tags(recession: float) -> set[str]:
    """Tag vocabulary a hair recipe can gate on, from the continuous value."""
    if recession > RECESSION_RECEDED:
        return {"hairline_receded"}
    if recession > RECESSION_THINNING:
        return {"hairline_thinning"}
    return set()


def active_tags(app: Appearance, m: Manifest) -> set[str]:
    """Every tag a rider carries once his appearance is resolved.

    Used by tooling to prove a gating rule actually fires, instead of trusting
    that an asset with `excludes` never reaches a rider who has the tag.
    """
    tags = set(app.identity.get("tags", ()))
    tags |= hairline_tags(app.mutable.get("hairline_recession", 0.0))
    for cat in ("facial_hair", "hair"):
        asset_id = app.mutable.get(cat)
        if asset_id:
            tags |= set(m.get(asset_id).tags)
    return tags


def age_stage(age: int) -> int:
    """Coarse visual age buckets; the cache key only moves when this moves."""
    for i, upper in enumerate((21, 25, 29, 33, 37, 42, 48, 55, 200)):
        if age <= upper:
            return i
    return 8


def _mutable(rng: RiderRng, rider: Rider, m: Manifest, ident: dict[str, Any]) -> dict[str, Any]:
    gen = ident["aging"]
    age = rider.age
    tags = set(ident["tags"])

    # --- hairline recession: continuous, personal, monotonic in age ----------
    recession_drive = max(0.0, (age - (23.0 + 12.0 * (1.0 - gen["balding_propensity"]))) / 22.0)
    recession = min(1.0, max(0.0, recession_drive * (0.35 + 1.15 * gen["balding_propensity"])))
    tags |= hairline_tags(recession)

    hair = weighted_pick(rng, "mutable.hair", m.by_category("hair"), rider, tags, salted=True)
    hair_color = weighted_pick(rng, "mutable.hair_color", m.by_category("hair_color"), rider, tags, salted=True)

    # --- gray ---------------------------------------------------------------
    gray = min(1.0, max(0.0, (age - gen["gray_onset_age"]) / 18.0 * gen["gray_speed"]))

    # --- facial hair --------------------------------------------------------
    beard_p = 0.10 + 0.42 * gen["beard_capability"]
    if age < 21:
        beard_p *= 0.35
    elif age < 24:
        beard_p *= 0.7
    facial = None
    if rng.stream("mutable.beard_gate", salted=True).chance(beard_p):
        facial = weighted_pick(rng, "mutable.beard", m.by_category("facial_hair"), rider, tags, salted=True)

    # --- wrinkles / skin wear ----------------------------------------------
    wear = min(1.0, max(0.0, (age - 25.0) / 24.0))
    wrinkle = round(min(1.0, wear * (0.40 + 0.80 * gen["wrinkle_propensity"])), 4)
    tan = round(min(1.0, 0.25 + 0.55 * gen["sun_damage"]), 4)

    details: list[str] = []
    for asset in m.by_category("skin_details"):
        s = rng.stream(f"mutable.detail.{asset.asset_id}")
        if _eligible(asset, rider, tags) and s.unit() < asset.weight:
            details.append(asset.asset_id)

    return {
        "hair": hair.asset_id if hair else None,
        "hair_color": hair_color.asset_id if hair_color else None,
        "hairline_recession": round(recession, 4),
        "gray": round(gray, 4),
        "facial_hair": facial.asset_id if facial else None,
        "wrinkle_strength": wrinkle,
        "tan_strength": tan,
        "skin_details": details,
        "age_stage": age_stage(age),
    }


def _equipment(rng: RiderRng, rider: Rider, m: Manifest) -> dict[str, Any]:
    tags: set[str] = set()
    helmet = weighted_pick(rng, "equip.helmet", m.by_category("helmet"), rider, tags)
    glasses = None
    if rng.stream("equip.glasses_gate").chance(0.55):
        glasses = weighted_pick(rng, "equip.glasses", m.by_category("glasses"), rider, tags)
    jersey = weighted_pick(rng, "equip.jersey_cut", m.by_category("jersey"), rider, tags)
    return {
        "jersey_template": jersey.asset_id if jersey else None,
        "team_id": rider.team_id,
        "jersey_override": rider.jersey_override,
        "helmet": helmet.asset_id if helmet else None,
        "helmet_worn": False,
        "glasses": glasses.asset_id if glasses else None,
        "glasses_worn": False,
    }


def generate(rider: Rider, m: Manifest, salt: int = 0) -> Appearance:
    seed_key = rider.visual_seed or rider.rider_id
    rng = RiderRng(seed_key, m.seed_version, salt)
    ident, shape = _identity(rng, rider, m)
    return Appearance(
        avatar_schema_version=m.avatar_schema_version,
        asset_pack_version=m.asset_pack_version,
        seed_version=m.seed_version,
        rider_id=rider.rider_id,
        salt=salt,
        identity=ident,
        shape=shape,
        mutable=_mutable(rng, rider, m, ident),
        equipment=_equipment(rng, rider, m),
    )


# --------------------------------------------------------------------------- #
# duplicate prevention
# --------------------------------------------------------------------------- #


def _bucket(v: float, n: int) -> int:
    return int(min(n - 1, max(0.0, v) * n))


def core_fingerprint(app: Appearance) -> tuple:
    """Permanent-identity signature, used for diversity reporting only.

    Continuous params are bucketed because two riders differing by 0.01
    face_width are visually the same person.
    """
    i, s = app.identity, app.shape
    return (
        i["head"],
        i.get("eyes"),
        i.get("nose"),
        i.get("mouth"),
        i.get("eyebrows"),
        _bucket(i["skin_tone"], 8),
        _bucket(s["face_width"], 4),
        _bucket(s["nose_width"], 4),
        _bucket(s["eye_spacing"], 4),
        _bucket(s["mouth_width"], 3),
    )


def similarity_key(app: Appearance) -> tuple:
    """Perceptual signature used for clone detection.

    Deliberately coarse: it only contains the traits a player actually notices
    in a 64 px list portrait. If it were as detailed as the full appearance,
    two riders could differ by one invisible parameter and still count as
    distinct. It ends with the secondary traits, which are the only ones a
    re-roll is allowed to move.
    """
    i, mu = app.identity, app.mutable
    return (
        i["head"],
        i.get("eyes"),
        i.get("nose"),
        i.get("mouth"),
        _bucket(i["skin_tone"], 6),
        _bucket(app.shape["face_width"], 3),
        mu.get("hair"),
        mu.get("hair_color"),
        mu.get("facial_hair") is not None,
    )


@dataclass
class PoolReport:
    riders: int
    rerolled: int
    unresolved: int
    distinct_similar: int
    distinct_core: int


def generate_pool(
    riders: list[Rider], m: Manifest, max_salt: int = 8
) -> tuple[dict[int, Appearance], PoolReport]:
    """Generate a whole pool, re-rolling secondary traits on fingerprint clashes.

    Riders are processed in ascending rider_id so the outcome never depends on
    input order, and only the salted (secondary) streams move on a re-roll.
    """
    seen: set[tuple] = set()
    out: dict[int, Appearance] = {}
    rerolled = 0
    unresolved = 0
    for rider in sorted(riders, key=lambda r: r.rider_id):
        salt = 0
        app = generate(rider, m, salt)
        while similarity_key(app) in seen and salt < max_salt:
            salt += 1
            app = generate(rider, m, salt)
        if salt:
            rerolled += 1
        if similarity_key(app) in seen:
            unresolved += 1
        seen.add(similarity_key(app))
        out[rider.rider_id] = app
    report = PoolReport(
        riders=len(out),
        rerolled=rerolled,
        unresolved=unresolved,
        distinct_similar=len({similarity_key(a) for a in out.values()}),
        distinct_core=len({core_fingerprint(a) for a in out.values()}),
    )
    return out, report
